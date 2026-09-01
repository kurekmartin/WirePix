using System;
using System.IO;
using System.Security.Cryptography;

namespace WirePix.Core.Files;

public static class FileOperations
{
    public static byte[] Hash(Stream stream)
    {
        using (stream)
        using (var md5 = MD5.Create())
            return md5.ComputeHash(stream);
    }

    public static bool Verify(byte[] expected, string path) => expected != null && expected.SequenceEqual(Hash(File.OpenRead(path)));

    public static string UniqueName(string path, string sourcePath, bool alwaysUnique = false)
    {
        string candidate = path;
        var index = 1;
        while (File.Exists(candidate))
        {
            if (!alwaysUnique && Verify(Hash(File.OpenRead(sourcePath)), candidate))
            {
                break;
            }

            string extension = Path.GetExtension(path);
            candidate = Path.Combine(Path.GetDirectoryName(candidate) ?? string.Empty, Path.GetFileNameWithoutExtension(path) + "(" + index++ + ")" + extension);
        }

        return candidate;
    }
}