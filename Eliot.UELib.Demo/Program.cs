using System;
using System.IO;
using UEExplorer.Exporters;

namespace Eliot.UELib.Demo
{
    /// <summary>
    /// Small console front-end that exposes the GearT3DDumper. It accepts a
    /// Gears of War 3 map file (.xxx) and writes a .t3d file to disk.
    /// </summary>
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: demo <map.xxx> [output.t3d]");
                return;
            }

            var input = args[0];
            var output = args.Length > 1 ? args[1] : Path.ChangeExtension(input, ".t3d");

            GearT3DDumper.Dump(input, output);
            Console.WriteLine($"T3D exported to {output}");
        }
    }
}
