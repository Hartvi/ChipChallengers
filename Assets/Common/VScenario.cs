using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class VScenario
{
    public const string DefaultFileName = "";

    string scenarioName = "";
    public string FileName { get { return this.scenarioName; } }

    GameObject loadedObject;

    public void LoadNewScenario(string name)
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
        this.scenarioName = name.Substring(0, name.Length - UIStrings.ScenarioExtension.Length);

        string error = "";
        string scenarioPath = Path.Join(IOHelpers.GetScenariosDirectory(), name).Replace("\\", "/");

        if (!File.Exists(scenarioPath))
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

            this.loadedObject = new GameObject(UIStrings.ScenarioString);
            var gs = this.loadedObject.AddComponent<GameScript>();

            byte[] scenarioContents = File.ReadAllBytes(scenarioPath);
            using (var reader = new StreamReader(new MemoryStream(scenarioContents)))
                gs.Activate(reader.ReadToEnd(), name, scenarioContents);

            this.loadedObject.SetActive(true);

            error = string.Empty;
        }
    }

    // UnloadScenario stops scenario logic and releases its temporary physics settings before destroying its objects.
    public void UnloadScenario()
    {
        if (this.loadedObject == null) { return; }
        this.loadedObject.SetActive(false);
        GameObject.Destroy(this.loadedObject);
        this.loadedObject = null;
    }

}
