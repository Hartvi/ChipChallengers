using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dummiesman;
using System;

public class VMap
{
    public const string DefaultFileName = "industrial_test_track.obj";
    
    string mapName = DefaultFileName;
    public string FileName { get { return this.mapName; } }
    public string ContentHash { get; private set; }
    public event Action Loaded;

    GameObject loadedObject;
    public bool IsLoaded => this.loadedObject != null && this.loadedObject.activeSelf;

    // LoadNewMap replaces the active map and synchronizes its colliders before spawn queries run.
    public bool LoadNewMap(string name, bool fromHost = false)
    {
        if (WorldSession.IsRemoteClient && !fromHost)
        {
            WorldSession.Report("The host chooses the multiplayer map.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name ||
            !name.EndsWith(UIStrings.MapExtension, StringComparison.OrdinalIgnoreCase))
        {
            WorldSession.Report("Choose a valid map from the map list.");
            return false;
        }
        string loadedMapName = name.Substring(0, name.Length - UIStrings.MapExtension.Length);

        string objPath = Path.Join(IOHelpers.GetMapsDirectory(), name).Replace("\\", "/");

        if (!File.Exists(objPath))
        {
            WorldSession.Report("Map file does not exist.");
        }
        else
        {
            GameObject replacement = null;
            try
            {
                string hash = ChallengeContentHash.Compute(Array.Empty<byte>(), File.ReadAllBytes(objPath));
                replacement = new OBJLoader().Load(objPath);
                replacement.SetActive(false);
                foreach (var renderer in replacement.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.gameObject.AddComponent<MeshCollider>();
                    renderer.gameObject.SetActive(true);
                }
                if (this.loadedObject != null)
                {
                    this.loadedObject.SetActive(false);
                    GameObject.Destroy(this.loadedObject);
                }
                this.loadedObject = replacement;
                this.loadedObject.SetActive(true);
                Physics.SyncTransforms();
                this.mapName = loadedMapName;
                this.ContentHash = hash;
            }
            catch (Exception error)
            {
                if (replacement != null) { GameObject.Destroy(replacement); }
                WorldSession.Report("Map could not be loaded: " + error.Message);
                return false;
            }
            this.Loaded?.Invoke();
            return true;
        }
        return false;
    }
}

