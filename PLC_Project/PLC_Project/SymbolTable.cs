using Antlr4.Runtime;
using System.Collections.Generic;

namespace PLC_Project
{
    public class SymbolTable
    {
        private Dictionary<string, Type> memory = new Dictionary<string, Type>();

        public void Add(IToken variable, Type type)
        {
            var name = variable.Text.Trim();
            if (memory.ContainsKey(name))
                Errors.ReportError(variable, $"Variable '{name}' was already declared.");
            else
                memory.Add(name, type);
        }

        public Type this[IToken variable]
        {
            get
            {
                var name = variable.Text.Trim();
                if (memory.ContainsKey(name)) return memory[name];
                Errors.ReportError(variable, $"Variable '{name}' was not declared.");
                return Type.Error;
            }
        }

        public bool Contains(string name) => memory.ContainsKey(name);

        public Type Get(string name) => memory.ContainsKey(name) ? memory[name] : Type.Error;
    }
}
