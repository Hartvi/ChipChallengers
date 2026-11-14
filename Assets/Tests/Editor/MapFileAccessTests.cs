using System;
using System.IO;
using Dummiesman;
using NUnit.Framework;
using UnityEngine;

public class MapFileAccessTests
{
    [Test]
    public void MapAndMaterialFilesSupportReadOnlyFilesAndConcurrentReaders()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ChipMapReadTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string obj = Path.Combine(directory, "map.obj"), mtl = Path.Combine(directory, "map.mtl");
        GameObject loaded = null;
        try
        {
            File.WriteAllText(obj, "mtllib map.mtl\no test\nv 0 0 0\nv 1 0 0\nv 0 1 0\nusemtl red\nf 1 2 3\n");
            File.WriteAllText(mtl, "newmtl red\nKd 1 0 0\n");
            File.SetAttributes(obj, FileAttributes.ReadOnly);
            File.SetAttributes(mtl, FileAttributes.ReadOnly);
            using (var objectReader = new FileStream(obj, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var materialReader = new FileStream(mtl, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                loaded = new OBJLoader().Load(obj);
                Assert.That(loaded.GetComponentsInChildren<MeshFilter>(), Has.Length.EqualTo(1));
                Assert.That(loaded.GetComponentInChildren<MeshRenderer>().sharedMaterial.color.r, Is.EqualTo(1f));
            }
        }
        finally
        {
            if (loaded != null)
            {
                foreach (var filter in loaded.GetComponentsInChildren<MeshFilter>()) UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
                foreach (var renderer in loaded.GetComponentsInChildren<MeshRenderer>())
                    foreach (var material in renderer.sharedMaterials) UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(loaded);
            }
            foreach (string path in Directory.GetFiles(directory)) File.SetAttributes(path, FileAttributes.Normal);
            Directory.Delete(directory, true);
        }
    }
}
