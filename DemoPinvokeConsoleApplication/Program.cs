using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Roslyn.Compilers.CSharp;

namespace DemoPinvokeConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            PInvoke.r(0);

            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var input = Path.Combine(directory, @"..\..\PInvoke.cs");

            var syntaxTree = SyntaxTree.ParseFile(Path.GetFullPath(input));
            var root = syntaxTree.GetRoot();

            var pinvokeRewriter = new PinvokeRewriter();
            var rewritten = pinvokeRewriter.Visit(root).NormalizeWhitespace();

            using (var writer = new StreamWriter(Path.GetFullPath(Path.Combine(directory, @"..\..\PInvokeRewritten.cs"))))
            {
                rewritten.WriteTo(writer);
            }
        }
    }
}