using System.IO;
using System.Reflection;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace DemoPinvokeConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var input = Path.Combine(directory, @"..\..\PInvoke.cs");

            var syntaxTree = SyntaxTree.ParseFile(Path.GetFullPath(input));
            var root = syntaxTree.GetRoot();

            var compilation = Compilation.Create("PinvokeRewriter")
                                         .AddSyntaxTrees(syntaxTree)
                                         .AddReferences(new MetadataFileReference(typeof(object).Assembly.Location));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            
            var pinvokeRewriter = new PinvokeRewriter(semanticModel);
            var rewritten = pinvokeRewriter.Visit(root).NormalizeWhitespace();

            using (var writer = new StreamWriter(Path.GetFullPath(Path.Combine(directory, @"..\..\PInvokeRewritten.cs"))))
            {
                rewritten.WriteTo(writer);
            }
        }
    }
}