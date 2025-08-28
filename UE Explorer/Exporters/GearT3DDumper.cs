using System;
using System.IO;
using UELib;
using UELib.Gears;

namespace UEExplorer.Exporters
{
    /// <summary>
    /// Simple exporter that converts Gears of War 3 map packages (.xxx) into
    /// plain-text .t3d files. The implementation relies on a minimal subset of
    /// UELib that understands how to load a package and enumerate map actors.
    /// </summary>
    public static class GearT3DDumper
    {
        public static void Dump(string inputPath, string outputPath)
        {
            using var package = UnrealLoader.LoadFullPackage(inputPath, FileAccess.Read);
            if (!package.IsGearsOfWar3())
                throw new InvalidOperationException("The provided package is not recognized as a Gears of War 3 map.");

            using var writer = new StreamWriter(outputPath);
            writer.WriteLine($"// Generated from {Path.GetFileName(inputPath)}");

            foreach (var actor in package.ExportMapActors())
            {
                writer.WriteLine(actor.ToT3D());
            }
        }
    }
}
