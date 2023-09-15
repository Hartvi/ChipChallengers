using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class IOHelpers
{
    public static bool ModelExists(string modelName)
    {
        return File.Exists(modelName);
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

        Debug.Log($"Saved content to file: {filePath}");
    }
}
