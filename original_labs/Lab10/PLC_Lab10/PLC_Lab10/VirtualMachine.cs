using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Lab10
{
    public class VirtualMachine
    {
        private Stack<object> stack = new Stack<object>();
        private List<string[]> code = new List<string[]>();
        Dictionary<string, object> memory = new Dictionary<string, object>();

        public VirtualMachine(string code)
        {
            this.code=code.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Split(" ")).ToList();
        }
        public void Run()
        {
            foreach(var instruction in this.code)
            {
                if (instruction[0].StartsWith("PUSH"))
                {
                    if (instruction[1] == "I") stack.Push(int.Parse(instruction[2]));
                    else stack.Push(float.Parse(instruction[2]));
                }
                else if (instruction[0].Equals("PRINT"))
                {
                    if (instruction[1] == "I") Console.WriteLine((int)stack.Pop());
                    else Console.WriteLine((float)stack.Pop());
                }
                else if (instruction[0].Equals("SAVE"))
                {
                    var value = stack.Pop();
                    if (instruction[1] == "I")  memory[instruction[2]]=(int)value;
                    else memory[instruction[2]] = (float)(value is int ? (int)value : (float)value);
                }
                else if (instruction[0].Equals("LOAD"))
                {
                    stack.Push(memory[instruction[1]]);
                }
                else
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    switch (instruction[0]) 
                    {
                        case "ADD" when left is int && right is int: stack.Push((int)left + (int)right); break;
                        case "ADD" : stack.Push((float)(left is int? (int)left:(float)left)+ (float)(right is int ? (int)right : (float)right)); break;
                        case "SUB" when left is int && right is int: stack.Push((int)left - (int)right); break;
                        case "SUB": stack.Push((float)(left is int ? (int)left : (float)left) - (float)(right is int ? (int)right : (float)right)); break;
                        case "DIV" when left is int && right is int: stack.Push((int)left / (int)right); break;
                        case "DIV": stack.Push((float)(left is int ? (int)left : (float)left) / (float)(right is int ? (int)right : (float)right)); break;
                        case "MUL" when left is int && right is int: stack.Push((int)left * (int)right); break;
                        case "MUL": stack.Push((float)(left is int ? (int)left : (float)left) * (float)(right is int ? (int)right : (float)right)); break;
                        case "MOD": stack.Push((int)left % (int)right); break;
                    }

                }
            }
        }
    }
}
