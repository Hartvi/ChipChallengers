using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

// ChallengeContentHash identifies the exact Lua and map contents independently of their filenames.
public static class ChallengeContentHash
{
    public static bool IsHash(string value) => value != null && value.Length == 64 && value.All(Uri.IsHexDigit);

    // Reuse the hash of the geometry actually loaded by VMap, with a new domain so legacy Lua-only records stay separate.
    public static string ComputeForMapHash(byte[] luaContents, string mapContentHash)
    {
        if (!IsHash(mapContentHash)) { throw new ArgumentException("A loaded map content hash is required."); }
        return Compute(luaContents, Encoding.UTF8.GetBytes("loaded-map-sha256-v1:" + mapContentHash.ToLowerInvariant()));
    }

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
