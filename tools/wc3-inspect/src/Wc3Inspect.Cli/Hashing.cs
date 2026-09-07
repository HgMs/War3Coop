using System.Security.Cryptography;

namespace Wc3Inspect;

internal static class Hashing
{
    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
}

