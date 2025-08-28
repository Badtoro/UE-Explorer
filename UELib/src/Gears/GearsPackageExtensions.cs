using System;
using System.Collections.Generic;
using System.IO;

namespace UELib.Gears
{
    /// <summary>
    /// Extension helpers for very basic decoding of Gears of War 3 map files.
    /// The real project contains advanced logic for compression, actor parsing
    /// and geometry extraction. Here we merely expose a tiny demo that treats
    /// each line in the file as an actor name.
    /// </summary>
    public static class GearsPackageExtensions
    {
        private const int GearsOfWar3Version = 1000; // Placeholder version

        public static bool IsGearsOfWar3(this UPackage package)
            => package.Version >= GearsOfWar3Version || package.FileName.EndsWith(".xxx", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Very naive actor enumeration. Each line in the file is considered an
        /// actor and converted to a <see cref="GearsActor"/> instance.
        /// </summary>
        public static IEnumerable<GearsActor> ExportMapActors(this UPackage package)
        {
            using var reader = new StreamReader(new MemoryStream(package.Data));
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                yield return new GearsActor { Name = line.Trim(), ClassName = "GearsActor" };
            }
        }
    }

    /// <summary>
    /// Minimal representation of a map actor used for creating T3D output.
    /// </summary>
    public class GearsActor
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public string ToT3D() => $"Begin Actor Class={ClassName} Name={Name}\nEnd Actor";
    }
}
