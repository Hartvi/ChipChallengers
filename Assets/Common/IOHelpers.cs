using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class IOHelpers
{
    public const string ModelsDirectoryName = "Models/";
    public const string MapsDirectoryName = "Maps/";
    public const string ChallengesDirectoryName = "Challenges/";
    public const string ModelFileType = "txt";
    static ModelFileStore modelFiles;
    static bool migrationWarningShown;

    static ModelFileStore Models
    {
        get
        {
            if (modelFiles == null)
                modelFiles = new ModelFileStore(Path.Combine(Application.persistentDataPath, ModelsDirectoryName),
                    Path.Combine(Application.streamingAssetsPath, ModelsDirectoryName));
            try { modelFiles.MigrateLegacyFiles(); }
            catch (System.Exception error) when (error is IOException || error is System.UnauthorizedAccessException)
            {
                if (!migrationWarningShown) { Debug.LogWarning("Could not migrate legacy models: " + error.Message); }
                migrationWarningShown = true;
            }
            return modelFiles;
        }
    }

    public static string GetModelsDirectory() => Models.UserDirectory;

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
        Models.Write(modelName, modelString);
    }

    public static string LoadModel(string modelName)
    {
        // leave the checking of the existence of the file to the UI part of the program
        return Models.Read(modelName);
    }

    public static string GetChallengesDirectory()
    {
        string ChallengesDir = Path.Join(Application.streamingAssetsPath, IOHelpers.ChallengesDirectoryName);

        if (!Directory.Exists(ChallengesDir))
        {
            Directory.CreateDirectory(ChallengesDir);
        }
        return ChallengesDir;
    }

    public static string[] GetAllChallenges()
    {
        string ChallengesDir = Path.Join(Application.streamingAssetsPath, IOHelpers.ChallengesDirectoryName);

        if (!Directory.Exists(ChallengesDir))
        {
            Directory.CreateDirectory(ChallengesDir);
        }

        DirectoryInfo d = new DirectoryInfo(ChallengesDir);
        FileInfo[] fi = d.GetFiles("*.*").Where(f => f.Extension.Equals(UIStrings.ChallengeExtension)).ToArray();

        return fi.Select(x => x.Name.Substring(0, x.Name.Length - UIStrings.ChallengeExtension.Length)).ToArray();
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
        return Models.Names();
    }

    public static bool ModelExists(string modelName)
    {
        return Models.Exists(modelName);
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
