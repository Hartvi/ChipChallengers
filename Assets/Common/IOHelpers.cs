using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class IOHelpers
{
    public const string ModelsDirectoryName = "Models/";
    public const string MapsDirectoryName = "Maps/";
    public const string ModelFileType = "txt";

    public static string EnsureFileExtension(string fileName)
    {
        string extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            return fileName + UIStrings.ModelExtension;
        }

        return fileName;
    }

    public static void SaveModel(string modelName, string modelString)
    {
        // leave the checking of the existence of the file to the UI part of the program
        IOHelpers.SaveTextFile(Path.Join(ModelsDirectoryName, modelName), modelString);
    }

    public static string LoadModel(string modelName)
    {
        // leave the checking of the existence of the file to the UI part of the program
        return IOHelpers.LoadTextFile(Path.Join(ModelsDirectoryName, modelName));
    }

    public static string GetMapsDirectory()
    {
        string MapsDir = Path.Join(Application.streamingAssetsPath, IOHelpers.MapsDirectoryName);

        if (!Directory.Exists(MapsDir))
        {
            Directory.CreateDirectory(MapsDir);
        }
        return MapsDir;
    }
    public static string[] GetAllMaps()
    {
        string MapsDir = Path.Join(Application.streamingAssetsPath, IOHelpers.MapsDirectoryName);

        if (!Directory.Exists(MapsDir))
        {
            Directory.CreateDirectory(MapsDir);
        }

        DirectoryInfo d = new DirectoryInfo(MapsDir);
        FileInfo[] fi = d.GetFiles("*.*").Where(f => f.Extension.Equals(UIStrings.MapExtension)).ToArray();

        return fi.Select(x => x.Name.Substring(0, x.Name.Length - UIStrings.MapExtension.Length)).ToArray();
    }

    public static string[] GetAllModels()
    {
        string ModelsDir = Path.Join(Application.streamingAssetsPath, IOHelpers.ModelsDirectoryName);

        if (!Directory.Exists(ModelsDir))
        {
            Directory.CreateDirectory(ModelsDir);
        }


        DirectoryInfo d = new DirectoryInfo(ModelsDir);

        FileInfo[] fi = d.GetFiles("*.*").Where(f => f.Extension.Equals(UIStrings.ModelExtension)).ToArray();

        return fi.Select(x => x.Name.Substring(0, x.Name.Length - UIStrings.ModelExtension.Length)).ToArray();
    }

    public static bool ModelExists(string modelName)
    {
        return IOHelpers.FileExists(Path.Join(IOHelpers.ModelsDirectoryName, modelName));
    }

    public static bool FileExists(string fileName)
    {
        return File.Exists(Path.Join(Application.streamingAssetsPath, fileName));
    }

    public static string LoadTextFile(string fileName)
    {
        // Get the full file path
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        // Check the file exists
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }

        // Read and return the file content
        return File.ReadAllText(filePath);
    }
    public static void SaveTextFile(string fileName, string content)
    {
        // Get the full file path
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        // Write the content to the file
        File.WriteAllText(filePath, content);

        //Debug.Log($"Saved content to file: {filePath}");
    }
}
