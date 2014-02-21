using System.IO;
using System.Reflection;

using Giorgi.Roslyn.PinvokeRewriter;

namespace PinvokeRewriterDemoApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var inputFile = Path.Combine(directory, @"..\..\PInvoke.cs");
            var outputFile = Path.GetFullPath(Path.Combine(directory, @"..\..\PInvokeRewritten.cs"));
            
            new Rewriter().RewritePinvoke(inputFile, outputFile);
        }
    }
}