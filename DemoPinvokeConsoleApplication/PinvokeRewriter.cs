using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace DemoPinvokeConsoleApplication
{
    class PinvokeRewriter : SyntaxRewriter
    {
        List<DelegateDeclarationSyntax> delegateDeclarations = new List<DelegateDeclarationSyntax>();
        List<MethodDeclarationSyntax> methodDeclarations = new List<MethodDeclarationSyntax>();

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var allAttributes = node.AttributeLists.SelectMany(syntax => syntax.Attributes).Where(syntax => syntax.Name is IdentifierNameSyntax);

            var dllImportAttribute = allAttributes.FirstOrDefault(syntax => ((IdentifierNameSyntax)syntax.Name).Identifier.ValueText == "DllImport");

            if (dllImportAttribute == null)
            {
                return base.VisitMethodDeclaration(node);
            }

            var entryPointArgument = dllImportAttribute.ArgumentList.Arguments.FirstOrDefault(syntax => syntax.NameEquals != null && syntax.NameEquals.Name.Identifier.ValueText == "EntryPoint");

            var entryPoint = node.Identifier.ValueText;
            string entryPointIdentifier = null;

            if (entryPointArgument != null)
            {
                var literalExpressionSyntax = entryPointArgument.Expression as LiteralExpressionSyntax;

                if (literalExpressionSyntax != null)
                {
                    entryPoint = literalExpressionSyntax.Token.ValueText;
                }

                var identifierNameSyntax = entryPointArgument.Expression as IdentifierNameSyntax;

                if (identifierNameSyntax != null)
                {
                    entryPointIdentifier = identifierNameSyntax.Identifier.ValueText;
                }
            }

            var delegateKeyword = Syntax.Token(SyntaxKind.DelegateKeyword);

            var unmanagedFunctionPointerAttribute = BuildUnmanagedFunctionPointerAttribute(dllImportAttribute);

            var delegateDeclaration = Syntax.DelegateDeclaration(node.ReturnType, node.Identifier.ValueText + "Delegate")
                                              .WithParameterList(node.ParameterList)
                                              .WithDelegateKeyword(delegateKeyword);

            if (unmanagedFunctionPointerAttribute != null)
            {
                delegateDeclaration = delegateDeclaration.WithAttributeLists(unmanagedFunctionPointerAttribute);
            }

            var dllNameArgument = dllImportAttribute.ArgumentList.Arguments.Single(syntax => syntax.NameEquals == null);
            var dllNameSyntax = dllNameArgument.Expression as LiteralExpressionSyntax;

            var variableDeclarator = Syntax.VariableDeclarator(string.Format("library = new UnmanagedLibrary(\"{0}\")", dllNameSyntax.Token.ValueText));
            var declarationSyntax = Syntax.VariableDeclaration(Syntax.ParseTypeName("var"), Syntax.SeparatedList(variableDeclarator));

            var functionDeclarationStatement = string.IsNullOrEmpty(entryPointIdentifier)
                ? "var function = library.GetUnmanagedFunction<{0}Delegate>(\"{1}\");"
                : "var function = library.GetUnmanagedFunction<{0}Delegate>({1});";

            var functionStatement = Syntax.ParseStatement(string.Format(functionDeclarationStatement, node.Identifier.ValueText, entryPointIdentifier ?? entryPoint));

            var args = node.ParameterList.Parameters.Select(syntax => Syntax.Argument(Syntax.IdentifierName(syntax.Identifier))).ToList();
            var argSeparators = Enumerable.Repeat(Syntax.Token(SyntaxKind.CommaToken), args.Count - 1).ToList();

            var invocationExpressionSyntax = Syntax.InvocationExpression(Syntax.ParseExpression("function"), Syntax.ArgumentList(Syntax.SeparatedList(args, argSeparators)));

            var isVoid = (node.ReturnType is PredefinedTypeSyntax) &&
                         ((PredefinedTypeSyntax)(node.ReturnType)).Keyword.Kind == SyntaxKind.VoidKeyword;

            var lastStatement = isVoid
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

        private AttributeListSyntax BuildUnmanagedFunctionPointerAttribute(AttributeSyntax dllImportAttribute)
        {
            var dllImportArguments = dllImportAttribute.ArgumentList.Arguments
                                            .Where(syntax => syntax.NameEquals != null)
                                            .Where(syntax =>
                                            {
                                                var argumentName = syntax.NameEquals.Name.Identifier.ValueText;
                                                return argumentName != "EntryPoint"
                                                    && argumentName != "ExactSpelling" && argumentName != "PreserveSig";
                                            }).ToArray();

            if (dllImportArguments.Length == 0)
            {
                return null;
            }

            var callingConventionAttribute = dllImportAttribute.ArgumentList.Arguments.FirstOrDefault(syntax => syntax.NameEquals != null && syntax.NameEquals.Name.Identifier.ValueText == "CallingConvention");

            var callingConvention = "Winapi";
            if (callingConventionAttribute != null)
            {
                var expressionSyntax = callingConventionAttribute.Expression as MemberAccessExpressionSyntax;
                callingConvention = expressionSyntax.Name.Identifier.ValueText;

                dllImportArguments = dllImportArguments.Where(syntax => syntax.NameEquals.Name.Identifier.ValueText != "CallingConvention").ToArray();
            }

            var arguments = Syntax.SeparatedList(Syntax.AttributeArgument(Syntax.ParseExpression(string.Format("CallingConvention.{0}", callingConvention))))
                                    .Add(dllImportArguments);

            var unmanagedFunctionPointerAttribute = Syntax.Attribute(Syntax.ParseName("UnmanagedFunctionPointer")).WithArgumentList(Syntax.AttributeArgumentList(arguments));

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
}