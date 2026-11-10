using System;
using System.IO;
using System.Security.Cryptography;

// ChallengeContentHash identifies the exact Lua and map contents independently of their filenames.
public static class ChallengeContentHash
{
    // Compute returns a lowercase SHA-256 hash of length-prefixed Lua bytes followed by length-prefixed map bytes.
    public static string Compute(byte[] luaContents, byte[] mapContents)
    {
        if (luaContents == null) { throw new ArgumentNullException(nameof(luaContents)); }
        if (mapContents == null) { throw new ArgumentNullException(nameof(mapContents)); }
        using (var sha = SHA256.Create())
        using (var stream = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(luaContents.Length);
            writer.Write(luaContents);
            writer.Write(mapContents.Length);
            writer.Write(mapContents);
            writer.Flush();
            stream.FlushFinalBlock();
            return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
