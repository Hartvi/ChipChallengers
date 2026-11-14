using System;
using System.IO;
using System.Text;

public static class AtomicTextFile
{
    public static void Write(string path, string content) => Write(path, writer => writer.Write(content));

    public static void Write(string path, Action<TextWriter> write)
    {
        string destination = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                write(writer);
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(destination)) { File.Replace(temporary, destination, destination + ".bak"); }
            else { File.Move(temporary, destination); }
        }
        finally { if (File.Exists(temporary)) { File.Delete(temporary); } }
    }
}
