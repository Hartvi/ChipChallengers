using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dummiesman;

public class VMap
{
    public const string DefaultFileName = "first_map.obj";
    
    string mapName = "first_map.obj";
    public string FileName { get { return this.mapName; } }

    GameObject loadedObject;

    // LoadNewMap replaces the active map and synchronizes its colliders before spawn queries run.
    public bool LoadNewMap(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            string msg = "File is null/empty";
            DisplaySingleton.Instance.DisplayText(x =>
            {
                DisplaySingleton.ErrorMsgModification(x);
                x.SetText(msg);
            },
            3f);
            return false;
        }
        string loadedMapName = name.Substring(0, name.Length - UIStrings.MapExtension.Length);

        string error = "";
        string objPath = Path.Join(IOHelpers.GetMapsDirectory(), name).Replace("\\", "/");

        if (!File.Exists(objPath))
        {
            error = "File doesn't exist.";
            DisplaySingleton.Instance.DisplayText(x =>
            {
                DisplaySingleton.ErrorMsgModification(x);
                x.SetText(error);
            },
            3f);
        }
        else
        {
            if (this.loadedObject != null)
            {
                this.loadedObject.SetActive(false);
                GameObject.Destroy(this.loadedObject);
            }

            this.loadedObject = new OBJLoader().Load(objPath);

            MeshRenderer[] meshRenderers = this.loadedObject.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer m in meshRenderers)
            {
                m.gameObject.AddComponent<MeshCollider>();
                m.gameObject.SetActive(true);
            }
            this.loadedObject.SetActive(true);
            Physics.SyncTransforms();
            this.mapName = loadedMapName;
            return true;
        }
        return false;
    }
}

