using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dummiesman;

public class VMap
{
    public const string DefaultFileName = "first_map.obj";
    string fileName = "first_map.obj";
    GameObject loadedObject;

    public void LoadNewMap(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        this.fileName = name;

        string error = "";
        string objPath = Path.Join(IOHelpers.GetMapsDirectory(), name);
        Debug.LogWarning($"TODO: load map using assimp and clear up old map. {name}");
        // TODO ASSIMP LOADING
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
                GameObject.Destroy(this.loadedObject);
            this.loadedObject = new OBJLoader().Load(objPath);

            MeshRenderer[] meshRenderers = this.loadedObject.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer m in meshRenderers)
            {
                m.gameObject.AddComponent<MeshCollider>();
            }
            error = string.Empty;
        }
    }
}

