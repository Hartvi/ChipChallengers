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

    public void LoadNewMap(string name)
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
            return;
        }
        this.mapName = name.Substring(0, name.Length - UIStrings.MapExtension.Length);;

        string error = "";
        string objPath = Path.Join(IOHelpers.GetMapsDirectory(), name).Replace("\\", "/");

        //Debug.LogWarning($"TODO: load map using assimp and clear up old map. {name}");
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
        //string mmm = $"LOADING NEW MAP: \n  {objPath}";
        //DisplaySingleton.Instance.DisplayText(x =>
        //{
        //    DisplaySingleton.ErrorMsgModification(x);
        //    x.SetText(mmm);
        //},
        //3f);

            this.loadedObject = new OBJLoader().Load(objPath);
            //error = $"Path: \n{objPath}";
            //DisplaySingleton.Instance.DisplayText(x =>
            //{
            //    DisplaySingleton.ErrorMsgModification(x);
            //    x.SetText(error);
            //},
            //3f);

            MeshRenderer[] meshRenderers = this.loadedObject.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer m in meshRenderers)
            {
                m.gameObject.AddComponent<MeshCollider>();
                m.gameObject.SetActive(true);
            }
            this.loadedObject.SetActive(true);
            error = string.Empty;
        }
    }
}

