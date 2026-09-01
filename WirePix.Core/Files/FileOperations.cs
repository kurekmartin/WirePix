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
        {
            return md5.ComputeHash(inputStream: stream);
        }
    }

    public static bool Verify(byte[] expected, string path)
    {
        return expected != null && expected.SequenceEqual(other: Hash(stream: File.OpenRead(path: path)));
    }

    public static string UniqueName(string path, string sourcePath, bool alwaysUnique = false)
    {
        string candidate = path;
        var index = 1;
        while (File.Exists(path: candidate))
        {
            if (!alwaysUnique && Verify(expected: Hash(stream: File.OpenRead(path: sourcePath)), path: candidate))
            {
                break;
            }

            string extension = Path.GetExtension(path: path);
            candidate = Path.Combine(path1: Path.GetDirectoryName(path: candidate) ?? string.Empty, path2: Path.GetFileNameWithoutExtension(path: path) + "(" + index++ + ")" + extension);
        }

        return candidate;
    }
}