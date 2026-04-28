using Antlr4.Runtime;
using System;
using System.Collections.Generic;

namespace PLC_Project
{
    public static class Errors
    {
        private static readonly List<string> ErrorList = new List<string>();

        public static void ReportError(IToken token, string message)
        {
            ErrorList.Add($"{token.Line}:{token.Column} - {message}");
        }

        public static void ReportError(int line, int column, string message)
        {
            ErrorList.Add($"{line}:{column} - {message}");
        }

        public static int NumberOfErrors => ErrorList.Count;

        public static void PrintAndClearErrors()
        {
            foreach (var e in ErrorList)
                Console.Error.WriteLine(e);
            ErrorList.Clear();
        }
    }
}
