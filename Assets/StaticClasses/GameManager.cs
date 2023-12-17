using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    Dictionary<string, Action<int>> SettingUpdateFunctions = new Dictionary<string, Action<int>>();

    void Awake()
    {
        SettingUpdateFunctions[UIStrings.Framerate] = this.SetFrameRate;
        SettingUpdateFunctions[UIStrings.PhysicsRate] = this.SetPhysicsRate;
        SettingUpdateFunctions[UIStrings.PhysicsParticles] = this.SetPhysicsParticles;
        SettingUpdateFunctions[UIStrings.Volume] = this.SetVolume;
    }

    public static GameManager Instance
    {
        get { return GameManager.instance ?? new GameObject("GameManager").AddComponent<GameManager>(); }
    }

    private static GameManager instance;

    void SetFrameRate(int f)
    {
        Debug.Log($"Setting frame rate to {f}");
        Application.targetFrameRate = f;
    }

    void SetPhysicsRate(int f)
    {
        Debug.Log($"Setting physics rate to {f}");
        Time.fixedDeltaTime = 1f / (float)f;
    }

    void SetVolume(int v)
    {
        Debug.Log($"TODO: set global volume to {v}");
        //AudioManager.Instance.
    }

    void SetPhysicsParticles(int f)
    {
        Debug.Log($"TODO: set physics particles to {f}");
    }

    public void UpdateSettings()
    {
        foreach(string intSetting in UIStrings.SettingsIntProperties)
        {
            int i = PlayerPrefs.GetInt(intSetting);
            this.SettingUpdateFunctions[intSetting].Invoke(i);
        }
    }

}

