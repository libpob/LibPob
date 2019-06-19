using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using LibPob;

namespace Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var programPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pobDirectory = Path.Combine(programPath, "PathOfBuilding");

            var wrapper = new PobWrapper(pobDirectory);

            if (Debugger.IsAttached)
                Console.ReadLine();
        }
    }
}
