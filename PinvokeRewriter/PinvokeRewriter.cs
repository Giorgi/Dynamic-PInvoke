using System.IO;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Formatting;

namespace Giorgi.Roslyn.PinvokeRewriter
{
    public class Rewriter
    {
        public void RewritePinvoke(string inputFile, string outputFile)
        {
            var syntaxTree = SyntaxTree.ParseFile(Path.GetFullPath(inputFile));
            var root = syntaxTree.GetRoot();

            var compilation = Compilation.Create("PinvokeRewriter")
                .AddSyntaxTrees(syntaxTree)
                .AddReferences(new MetadataFileReference(typeof(object).Assembly.Location));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var pinvokeRewriter = new PinvokeSyntaxRewriter(semanticModel);
            var rewritten = pinvokeRewriter.Visit(root).Format(FormattingOptions.GetDefaultOptions()).GetFormattedRoot();

            using (var writer = new StreamWriter(outputFile))
            {
                rewritten.WriteTo(writer);
            }
        }
    }
}