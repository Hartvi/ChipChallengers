using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

// Installed examples are read-only; player work lives in a separate user-data directory.
public sealed class ModelFileStore
{
    // The colon makes this a selection label that cannot collide with a valid model filename.
    public const string BundledPrefix = "Bundled: ";
    public static bool IsBundledSelection(string name) => name != null && name.StartsWith(BundledPrefix, StringComparison.Ordinal);
    public static string SelectionFileName(string name) => ModelFileNames.Normalize(
        IsBundledSelection(name) ? name.Substring(BundledPrefix.Length) : name);
    public string UserDirectory { get; }
    public string BundledDirectory { get; }
    const string UserOwned = "user";
    string MigrationHashesPath => Path.Combine(this.UserDirectory, ".migrated-model-hashes");

    public ModelFileStore(string userDirectory, string bundledDirectory)
    {
        this.UserDirectory = Path.GetFullPath(userDirectory);
        this.BundledDirectory = Path.GetFullPath(bundledDirectory);
    }

    public void MigrateLegacyFiles()
    {
        Directory.CreateDirectory(this.UserDirectory);
        string marker = Path.Combine(this.UserDirectory, ".legacy-models-migrated");
        bool alreadyMigrated = File.Exists(marker);
        if (alreadyMigrated && File.Exists(this.MigrationHashesPath)) { return; }
        var hashes = this.ReadMigrationHashes();
        foreach (string source in Files(this.BundledDirectory))
        {
            string name = ModelFileNames.Normalize(Path.GetFileName(source));
            string existing = this.Resolve(this.UserDirectory, name);
            if (existing != null)
            {
                // Upgrade old migrations conservatively: only identical copies
                // can be identified as untouched without their original hashes.
                string bundledHash = HashFile(source);
                if (!hashes.ContainsKey(name) && HashFile(existing) == bundledHash) hashes[name] = bundledHash;
                continue;
            }
            if (alreadyMigrated) { continue; }
            string destination = this.UserPath(name);
            string temporary = destination + ".migrate-" + Guid.NewGuid().ToString("N");
            try
            {
                File.Copy(source, temporary);
                string copiedHash = HashFile(temporary);
                try { File.Move(temporary, destination); hashes[name] = copiedHash; }
                catch (IOException) when (File.Exists(destination))
                {
                    if (HashFile(destination) == copiedHash) hashes[name] = copiedHash;
                }
            }
            finally { if (File.Exists(temporary)) { File.Delete(temporary); } }
        }
        this.WriteMigrationHashes(hashes);
        if (!alreadyMigrated) AtomicTextFile.Write(marker, "Legacy models copied without replacing existing user files.");
    }

    public string[] Names() => Files(this.UserDirectory).Concat(Files(this.BundledDirectory))
        .Select(path => ModelFileNames.DisplayName(Path.GetFileName(path))).Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool Exists(string name) => this.Resolve(this.UserDirectory, name) != null || this.Resolve(this.BundledDirectory, name) != null;

    public string[] LoadableNames() => this.Names().Concat(Files(this.BundledDirectory)
        .Select(path => BundledPrefix + ModelFileNames.DisplayName(Path.GetFileName(path))))
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool BundledExists(string name) => this.Resolve(this.BundledDirectory, name) != null;

    public string ReadBundled(string name)
    {
        string path = this.Resolve(this.BundledDirectory, name);
        if (path == null) throw new FileNotFoundException("Bundled model not found.", ModelFileNames.Normalize(name));
        return File.ReadAllText(path);
    }

    public string Read(string name)
    {
        string user = this.Resolve(this.UserDirectory, name), bundled = this.Resolve(this.BundledDirectory, name);
        string path = user ?? bundled;
        if (user != null && bundled != null && this.ReadMigrationHashes().TryGetValue(ModelFileNames.Normalize(name), out string migratedHash) &&
            migratedHash != UserOwned && HashFile(user) == migratedHash)
            path = bundled; // Untouched migrated examples follow updates; the original copy remains intact.
        if (path == null) { throw new FileNotFoundException("Model not found.", ModelFileNames.Normalize(name)); }
        return File.ReadAllText(path);
    }

    public void Write(string name, string content)
    {
        Directory.CreateDirectory(this.UserDirectory);
        AtomicTextFile.Write(this.Resolve(this.UserDirectory, name) ?? this.UserPath(name), content);
        var hashes = this.ReadMigrationHashes();
        // An explicit save owns this name, even when the text matches the example.
        hashes[ModelFileNames.Normalize(name)] = UserOwned;
        this.WriteMigrationHashes(hashes);
    }

    static string HashFile(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    Dictionary<string, string> ReadMigrationHashes()
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(this.MigrationHashesPath)) return hashes;
        foreach (string line in File.ReadAllLines(this.MigrationHashesPath))
        {
            string[] parts = line.Split('\t');
            if (parts.Length == 2 && ModelFileNames.TryNormalize(parts[0], out string name) &&
                (parts[1] == UserOwned || (parts[1].Length == 64 && parts[1].All(Uri.IsHexDigit)))) hashes[name] = parts[1];
        }
        return hashes;
    }

    void WriteMigrationHashes(Dictionary<string, string> hashes) =>
        AtomicTextFile.Write(this.MigrationHashesPath, string.Join("\n", hashes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key + "\t" + pair.Value)));

    string UserPath(string name)
    {
        return Path.Combine(this.UserDirectory, ModelFileNames.Normalize(name));
    }

    string Resolve(string directory, string name)
    {
        string canonical = ModelFileNames.Normalize(name);
        string path = Path.Combine(directory, canonical);
        if (File.Exists(path)) { return path; }
        return Files(directory).FirstOrDefault(file => ModelFileNames.Normalize(Path.GetFileName(file))
            .Equals(canonical, StringComparison.OrdinalIgnoreCase));
    }

    static string[] Files(string directory) => Directory.Exists(directory) ? Directory.GetFiles(directory)
        .Where(path => Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase) &&
            ModelFileNames.TryNormalize(Path.GetFileName(path), out _)).ToArray() : Array.Empty<string>();
}
