using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class VChallenge
{
    public const string DefaultFileName = "";

    string challengeName = "";
    public string FileName { get { return this.challengeName; } }

    GameObject loadedObject;
    public GameScript Runtime => this.loadedObject == null ? null : this.loadedObject.GetComponent<GameScript>();

    public void LoadNewChallenge(string name)
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
        this.challengeName = name.Substring(0, name.Length - UIStrings.ChallengeExtension.Length);

        string error = "";
        string challengePath = Path.Join(IOHelpers.GetChallengesDirectory(), name).Replace("\\", "/");

        if (!File.Exists(challengePath))
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

            this.loadedObject = new GameObject(UIStrings.ChallengeString);
            var gs = this.loadedObject.AddComponent<GameScript>();

            byte[] challengeContents = File.ReadAllBytes(challengePath);
            using (var reader = new StreamReader(new MemoryStream(challengeContents)))
                gs.Activate(reader.ReadToEnd(), name, challengeContents);

            this.loadedObject.SetActive(true);

            error = string.Empty;
        }
    }

    // UnloadChallenge stops challenge logic and releases its temporary physics settings before destroying its objects.
    public void UnloadChallenge()
    {
        if (this.loadedObject == null) { return; }
        this.loadedObject.SetActive(false);
        GameObject.Destroy(this.loadedObject);
        this.loadedObject = null;
    }

}
