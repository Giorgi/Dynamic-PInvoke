using System.Collections.Generic;
using System.Linq;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace DemoPinvokeConsoleApplication
{
    class PinvokeSyntaxRewriter : SyntaxRewriter
    {
        private readonly SemanticModel semanticModel;
        List<DelegateDeclarationSyntax> delegateDeclarations = new List<DelegateDeclarationSyntax>();
        List<MethodDeclarationSyntax> methodDeclarations = new List<MethodDeclarationSyntax>();

        public PinvokeSyntaxRewriter(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(node);
            var dllImportData = methodSymbol.GetDllImportData();

            if (dllImportData == null)
            {
                return base.VisitMethodDeclaration(node);
            }

            var delegateKeyword = Syntax.Token(SyntaxKind.DelegateKeyword);

            var unmanagedFunctionPointerAttribute = BuildUnmanagedFunctionPointerAttribute(dllImportData);

            var returnKeywordAttributes = node.AttributeLists.Where(syntax => syntax.Target != null && syntax.Target.Identifier.Kind == SyntaxKind.ReturnKeyword);

            var delegateDeclaration = Syntax.DelegateDeclaration(node.ReturnType, node.Identifier.ValueText + "Delegate")
                                              .WithDelegateKeyword(delegateKeyword)
                                              .WithParameterList(node.ParameterList)
                                              .WithAttributeLists(unmanagedFunctionPointerAttribute)
                                              .AddAttributeLists(returnKeywordAttributes.ToArray());

            var variableDeclarator = Syntax.VariableDeclarator(string.Format("library = new UnmanagedLibrary(\"{0}\")", dllImportData.ModuleName));
            var declarationSyntax = Syntax.VariableDeclaration(Syntax.ParseTypeName("var"), Syntax.SeparatedList(variableDeclarator));

            var functionDeclarationStatement = "var function = library.GetUnmanagedFunction<{0}Delegate>(\"{1}\");";

            var nativeFunctionName = dllImportData.EntryPointName ?? node.Identifier.ValueText;
            var functionStatement = Syntax.ParseStatement(string.Format(functionDeclarationStatement, node.Identifier.ValueText, nativeFunctionName));

            var args = node.ParameterList.Parameters.Select(syntax => Syntax.Argument(Syntax.IdentifierName(syntax.Identifier))).ToList();
            var argSeparators = Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), args.Count - 1).ToList();

            var invocationExpressionSyntax = Syntax.InvocationExpression(Syntax.ParseExpression("function"), Syntax.ArgumentList(Syntax.SeparatedList(args, argSeparators)));

            var lastStatement = methodSymbol.ReturnsVoid
                ? (StatementSyntax)Syntax.ExpressionStatement(invocationExpressionSyntax)
                : Syntax.ReturnStatement(invocationExpressionSyntax);

            var usingStatementBody = Syntax.Block(functionStatement, lastStatement);

            var usingStatement = Syntax.UsingStatement(declarationSyntax, null, usingStatementBody);

            var blockSyntax = Syntax.Block(usingStatement);

            var methodDeclaration = Syntax.MethodDeclaration(node.ReturnType, node.Identifier)
                .WithParameterList(node.ParameterList)
                .WithModifiers(Syntax.TokenList(node.Modifiers.Where(token => token.Kind != SyntaxKind.ExternKeyword)))
                .WithBody(blockSyntax);

            delegateDeclarations.Add(delegateDeclaration);
            methodDeclarations.Add(methodDeclaration);

            return default(SyntaxNode);
        }

        private AttributeListSyntax BuildUnmanagedFunctionPointerAttribute(DllImportData dllImportData)
        {
            var charset = string.Format("CharSet = CharSet.{0}", dllImportData.CharacterSet);
            var charsetArgument = Syntax.AttributeArgument(Syntax.ParseExpression(charset));

            var setLastError = string.Format("SetLastError = {0}", dllImportData.SetLastError.ToProperString());
            var setLastErrorArgument = Syntax.AttributeArgument(Syntax.ParseExpression(setLastError));

            var callingConvention = string.Format("CallingConvention.{0}", dllImportData.CallingConvention);
            var callingConventionArgument = Syntax.AttributeArgument(Syntax.ParseExpression(callingConvention));

            var arguments = Syntax.SeparatedList(callingConventionArgument).Add(setLastErrorArgument, charsetArgument);

            if (dllImportData.BestFitMapping.HasValue)
            {
                var bestFitMapping = string.Format("BestFitMapping = {0}", dllImportData.BestFitMapping.ToProperString());
                var bestFitMappingArgument = Syntax.AttributeArgument(Syntax.ParseExpression(bestFitMapping));

                arguments = arguments.Add(bestFitMappingArgument);
            }

            if (dllImportData.ThrowOnUnmappableCharacter.HasValue)
            {
                var throwOnUnmappableCharacter = string.Format("ThrowOnUnmappableChar = {0}", dllImportData.ThrowOnUnmappableCharacter.ToProperString());
                var throwOnUnmappableCharacterArgument = Syntax.AttributeArgument(Syntax.ParseExpression(throwOnUnmappableCharacter));

                arguments = arguments.Add(throwOnUnmappableCharacterArgument);
            }

            var unmanagedFunctionPointerAttribute = Syntax.Attribute(Syntax.ParseName("UnmanagedFunctionPointer"))
                                                          .WithArgumentList(Syntax.AttributeArgumentList(arguments));

            return Syntax.AttributeList(Syntax.SeparatedList(unmanagedFunctionPointerAttribute));
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var visitClassDeclaration = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            visitClassDeclaration = visitClassDeclaration.AddMembers(delegateDeclarations.ToArray())
                                                         .AddMembers(methodDeclarations.ToArray());

            var identifier = Syntax.Identifier(node.Identifier.ValueText + "Dynamic");

            return visitClassDeclaration.WithIdentifier(identifier);
        }
    }

    static class BooleanExtensions
    {
        public static string ToProperString(this bool @value)
        {
            return @value.ToString().ToLower();
        }

        public static string ToProperString(this bool? @value)
        {
            if (@value.HasValue)
            {
                return @value.ToString().ToLower();
            }

            return "";
        }
    }
}