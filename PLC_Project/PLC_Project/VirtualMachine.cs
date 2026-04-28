using System;
using System.Collections.Generic;

namespace PLC_Project
{
    public class VirtualMachine
    {
        private readonly Stack<object> _stack = new Stack<object>();
        private readonly Dictionary<string, object> _memory = new Dictionary<string, object>();
        private readonly List<string[]> _code = new List<string[]>();
        private readonly Dictionary<int, int> _labels = new Dictionary<int, int>();

        public VirtualMachine(string source)
        {
            var lines = source.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Trim().Split(' ');
                _code.Add(parts);
                if (parts[0] == "label")
                    _labels[int.Parse(parts[1])] = i;
            }
        }

        public void Run()
        {
            int ip = 0;
            while (ip < _code.Count)
            {
                var ins = _code[ip];
                switch (ins[0])
                {
                    case "push":
                        ip = ExecPush(ins, ip);
                        break;
                    case "pop":
                        _stack.Pop();
                        ip++;
                        break;
                    case "load":
                        _stack.Push(_memory[ins[1]]);
                        ip++;
                        break;
                    case "save":
                        _memory[ins[1]] = _stack.Pop();
                        ip++;
                        break;
                    case "label":
                        ip++;
                        break;
                    case "jmp":
                        ip = _labels[int.Parse(ins[1])];
                        break;
                    case "fjmp":
                        ip = (bool)_stack.Pop() ? ip + 1 : _labels[int.Parse(ins[1])];
                        break;
                    case "print":
                        ExecPrint(int.Parse(ins[1]));
                        ip++;
                        break;
                    case "read":
                        ExecRead(ins[1]);
                        ip++;
                        break;
                    case "itof":
                        _stack.Push((float)(int)_stack.Pop());
                        ip++;
                        break;
                    case "add":  case "sub": case "mul": case "div":
                    case "mod":  case "concat":
                    case "and":  case "or":
                    case "lt":   case "gt":  case "eq":
                        ip = ExecBinary(ins, ip);
                        break;
                    case "uminus":
                        ip = ExecUMinus(ins, ip);
                        break;
                    case "not":
                        _stack.Push(!(bool)_stack.Pop());
                        ip++;
                        break;
                    default:
                        throw new Exception($"Unknown instruction: {ins[0]}");
                }
            }
        }

        private int ExecPush(string[] ins, int ip)
        {
            switch (ins[1])
            {
                case "I": _stack.Push(int.Parse(ins[2])); break;
                case "F": _stack.Push(float.Parse(ins[2], System.Globalization.CultureInfo.InvariantCulture)); break;
                case "B": _stack.Push(ins[2] == "true"); break;
                case "S":
                    // string may contain spaces — rejoin from index 2
                    var s = string.Join(" ", ins, 2, ins.Length - 2);
                    // strip surrounding quotes
                    _stack.Push(s.Length >= 2 ? s.Substring(1, s.Length - 2) : s);
                    break;
            }
            return ip + 1;
        }

        private void ExecPrint(int count)
        {
            var items = new object[count];
            for (int i = count - 1; i >= 0; i--)
                items[i] = _stack.Pop();
            for (int i = 0; i < count; i++)
            {
                if (i == count - 1)
                    Console.WriteLine(FormatValue(items[i]));
                else
                    Console.Write(FormatValue(items[i]));
            }
        }

        private void ExecRead(string typeTag)
        {
            var line = Console.ReadLine() ?? "";
            switch (typeTag)
            {
                case "I": _stack.Push(int.Parse(line)); break;
                case "F": _stack.Push(float.Parse(line, System.Globalization.CultureInfo.InvariantCulture)); break;
                case "B": _stack.Push(line.Trim() == "true"); break;
                case "S": _stack.Push(line); break;
            }
        }

        private int ExecBinary(string[] ins, int ip)
        {
            var right = _stack.Pop();
            var left  = _stack.Pop();

            switch (ins[0])
            {
                case "add":    _stack.Push(NumericOp(left, right, ins[1], (a,b)=>a+b, (a,b)=>a+b)); break;
                case "sub":    _stack.Push(NumericOp(left, right, ins[1], (a,b)=>a-b, (a,b)=>a-b)); break;
                case "mul":    _stack.Push(NumericOp(left, right, ins[1], (a,b)=>a*b, (a,b)=>a*b)); break;
                case "div":    _stack.Push(NumericOp(left, right, ins[1], (a,b)=>a/b, (a,b)=>a/b)); break;
                case "mod":    _stack.Push((int)left % (int)right); break;
                case "concat": _stack.Push((string)left + (string)right); break;
                case "and":    _stack.Push((bool)left && (bool)right); break;
                case "or":     _stack.Push((bool)left || (bool)right); break;
                case "lt":     _stack.Push(CompareOp(left, right, ins[1], (a,b)=>a<b, (a,b)=>a<b)); break;
                case "gt":     _stack.Push(CompareOp(left, right, ins[1], (a,b)=>a>b, (a,b)=>a>b)); break;
                case "eq":     _stack.Push(EqOp(left, right, ins[1])); break;
            }
            return ip + 1;
        }

        private int ExecUMinus(string[] ins, int ip)
        {
            var v = _stack.Pop();
            _stack.Push(ins[1] == "I" ? (object)(-(int)v) : (object)(-(float)v));
            return ip + 1;
        }

        private static object NumericOp(object l, object r, string tag,
            Func<int,int,int> intOp, Func<float,float,float> floatOp)
        {
            if (tag == "I") return intOp((int)l, (int)r);
            return floatOp(ToFloat(l), ToFloat(r));
        }

        private static bool CompareOp(object l, object r, string tag,
            Func<int,int,bool> intOp, Func<float,float,bool> floatOp)
        {
            if (tag == "I") return intOp((int)l, (int)r);
            return floatOp(ToFloat(l), ToFloat(r));
        }

        private static bool EqOp(object l, object r, string tag)
        {
            return tag switch
            {
                "I" => (int)l == (int)r,
                "F" => ToFloat(l) == ToFloat(r),
                "S" => (string)l == (string)r,
                _   => l.Equals(r)
            };
        }

        private static float ToFloat(object v) => v is int i ? (float)i : (float)v;

        private static string FormatValue(object v) => v switch
        {
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            bool b  => b ? "true" : "false",
            _       => v.ToString() ?? ""
        };
    }
}
