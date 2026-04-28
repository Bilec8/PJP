using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace PLC_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: PLC_Project <source.plc> [--run]");
                return;
            }

            var inputPath = args[0];
            bool runAfter = args.Length > 1 && args[1] == "--run";

            var inputStream = new AntlrInputStream(new StreamReader(inputPath));
            var lexer       = new PLCLexer(inputStream);
            var tokens      = new CommonTokenStream(lexer);
            var parser      = new PLCParser(tokens);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new VerboseErrorListener());

            IParseTree tree = parser.program();

            if (Errors.NumberOfErrors > 0)
            {
                Errors.PrintAndClearErrors();
                return;
            }

            var typeChecker = new TypeCheckingListener();
            ParseTreeWalker.Default.Walk(typeChecker, tree);

            if (Errors.NumberOfErrors > 0)
            {
                Errors.PrintAndClearErrors();
                return;
            }

            var generator = new CodeGeneratorVisitor(typeChecker.Types, typeChecker.SymbolTable);
            var code = generator.Visit(tree);

            if (runAfter)
            {
                var vm = new VirtualMachine(code);
                vm.Run();
            }
            else
            {
                Console.Write(code);
            }
        }
    }
}
