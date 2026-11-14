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
            if (File.Exists(destination)) { continue; }
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
        .Select(Path.GetFileNameWithoutExtension).Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool Exists(string name) => File.Exists(this.UserPath(name)) || File.Exists(Path.Combine(this.BundledDirectory, name));

    public string Read(string name)
    {
        string userPath = this.UserPath(name);
        return File.ReadAllText(File.Exists(userPath) ? userPath : Path.Combine(this.BundledDirectory, name));
    }

    public void Write(string name, string content)
    {
        Directory.CreateDirectory(this.UserDirectory);
        AtomicTextFile.Write(this.UserPath(name), content);
    }

    string UserPath(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Choose a model filename without a directory path.");
        return Path.Combine(this.UserDirectory, name);
    }

    static string[] Files(string directory) => Directory.Exists(directory) ? Directory.GetFiles(directory)
        .Where(path => Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase)).ToArray() : Array.Empty<string>();
}
