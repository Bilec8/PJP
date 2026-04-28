namespace PLC_Lab10
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var input = File.ReadAllText("input.txt");
            VirtualMachine vm = new VirtualMachine(input);
            vm.Run();
        }
    }
}
