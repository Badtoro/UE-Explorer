using System;
using System.IO;

namespace UELib
{
    /// <summary>
    /// Minimal placeholder implementation of an Unreal package used for the
    /// GearT3DDumper demo. It only loads the raw bytes of a file and exposes
    /// a version value for format detection.
    /// </summary>
    public sealed class UPackage : IDisposable
    {
        public string FileName { get; }
        public int Version { get; }
        public byte[] Data { get; }

        public UPackage(string fileName, byte[] data, int version = 0)
        {
            FileName = fileName;
            Data = data;
            Version = version;
        }

        public void Dispose()
        {
            // No resources to release in this placeholder implementation.
        }
    }

    /// <summary>
    /// Simplified loader that reads an entire file into memory.
    /// </summary>
    public static class UnrealLoader
    {
        public static UPackage LoadFullPackage(string path, FileAccess access)
        {
            var data = File.ReadAllBytes(path);
            // In a real implementation the version would be read from the file header.
            return new UPackage(path, data, version: 0);
        }
    }
}
