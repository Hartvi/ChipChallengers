using System;
using System.IO;
using System.Linq;

// Installed examples are read-only; player work lives in a separate user-data directory.
public sealed class ModelFileStore
{
    public string UserDirectory { get; }
    public string BundledDirectory { get; }

    public ModelFileStore(string userDirectory, string bundledDirectory)
    {
        this.UserDirectory = Path.GetFullPath(userDirectory);
        this.BundledDirectory = Path.GetFullPath(bundledDirectory);
    }

    public void MigrateLegacyFiles()
    {
        Directory.CreateDirectory(this.UserDirectory);
        string marker = Path.Combine(this.UserDirectory, ".legacy-models-migrated");
        if (File.Exists(marker)) { return; }
        foreach (string source in Files(this.BundledDirectory))
        {
            string destination = this.UserPath(Path.GetFileName(source));
            if (this.Resolve(this.UserDirectory, Path.GetFileName(source)) != null) { continue; }
            string temporary = destination + ".migrate-" + Guid.NewGuid().ToString("N");
            try
            {
                File.Copy(source, temporary);
                try { File.Move(temporary, destination); }
                catch (IOException) when (File.Exists(destination)) { } // Another session already preserved this model.
            }
            finally { if (File.Exists(temporary)) { File.Delete(temporary); } }
        }
        File.WriteAllText(marker, "Legacy models copied without replacing existing user files.");
    }

    public string[] Names() => Files(this.UserDirectory).Concat(Files(this.BundledDirectory))
        .Select(path => ModelFileNames.DisplayName(Path.GetFileName(path))).Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool Exists(string name) => this.Resolve(this.UserDirectory, name) != null || this.Resolve(this.BundledDirectory, name) != null;

    public string Read(string name)
    {
        string path = this.Resolve(this.UserDirectory, name) ?? this.Resolve(this.BundledDirectory, name);
        if (path == null) { throw new FileNotFoundException("Model not found.", ModelFileNames.Normalize(name)); }
        return File.ReadAllText(path);
    }

    public void Write(string name, string content)
    {
        Directory.CreateDirectory(this.UserDirectory);
        AtomicTextFile.Write(this.Resolve(this.UserDirectory, name) ?? this.UserPath(name), content);
    }

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
