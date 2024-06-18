using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public void SetModel(string m)
    {
        //print($"setting model to {m}");
        this.selectedModel = m;
    }
    public string GetModel()
    {
        //print($"getting model {this.selectedModel}"); 
        return this.selectedModel;
    }
    protected string selectedModel = "";

    public static readonly Dictionary<string, Tuple<int, int>> minMaxIntSettings = new Dictionary<string, Tuple<int, int>>()
    {
        { UIStrings.Framerate, new Tuple<int, int>(5, 100) },
        { UIStrings.PhysicsRate, new Tuple<int, int>(50, 2000) },
        { UIStrings.Volume, new Tuple<int, int>(0, 100) },
        { UIStrings.PhysicsParticles, new Tuple<int, int>(0, 100) }
    };

    public static readonly Dictionary<string, int> defaultIntSettings = new Dictionary<string, int>()
    {

        { UIStrings.Framerate, 50 },
        { UIStrings.PhysicsRate, 500 },
        { UIStrings.Volume, 20 },
        { UIStrings.PhysicsParticles, 100 }
    };

    public static CameraMoveMode cameraMoveMode = CameraMoveMode.Follow;

    Dictionary<string, Func<int, int>> SettingUpdateFunctions = new Dictionary<string, Func<int, int>>();
    public Dictionary<string, Func<int>> GettingUpdateFunctions = new Dictionary<string, Func<int>>();

    public static class RealTimeSettings
    {
        public static float Volume
        {
            get { return RealTimeSettings.InMenu ? 0f : RealTimeSettings._Volume; }
            set { RealTimeSettings._Volume = value; }
        }
        private static float _Volume = 1f;
        public static bool InMenu = false;

        public static float ParticleRate;
    }

    void Awake()
    {
        SettingUpdateFunctions[UIStrings.Framerate] = this.SetFrameRate;
        SettingUpdateFunctions[UIStrings.PhysicsRate] = this.SetPhysicsRate;
        SettingUpdateFunctions[UIStrings.PhysicsParticles] = this.SetPhysicsParticles;
        SettingUpdateFunctions[UIStrings.Volume] = this.SetVolume;

        GettingUpdateFunctions[UIStrings.Framerate] = this.GetFrameRate;
        GettingUpdateFunctions[UIStrings.PhysicsRate] = this.GetPhysicsRate;
        GettingUpdateFunctions[UIStrings.PhysicsParticles] = this.GetPhysicsParticles;
        GettingUpdateFunctions[UIStrings.Volume] = this.GetVolume;
    }

    public static GameManager Instance
    {
        get { return GameManager.instance ?? (GameManager.instance = new GameObject("GameManager").AddComponent<GameManager>()); }
    }

    private static GameManager instance;

    int GetFrameRate()
    {
        string s = UIStrings.Framerate;
        int i = PlayerPrefs.GetInt(s);

        if (i < minMaxIntSettings[s].Item1 || i > minMaxIntSettings[s].Item2)
        {
            i = defaultIntSettings[s];
        }
        return i;
    }

    int GetPhysicsRate()
    {
        string s = UIStrings.PhysicsRate;
        int i = PlayerPrefs.GetInt(s);

        if (i < minMaxIntSettings[s].Item1 || i > minMaxIntSettings[s].Item2)
        {
            i = defaultIntSettings[s];
        }
        return i;
    }

    int GetPhysicsParticles()
    {
        string s = UIStrings.PhysicsParticles;
        int i = PlayerPrefs.GetInt(s);

        if (i < minMaxIntSettings[s].Item1 || i > minMaxIntSettings[s].Item2)
        {
            i = defaultIntSettings[s];
        }
        GameManager.RealTimeSettings.ParticleRate = (float)(i) / 100f;
        return i;
    }

    int GetVolume()
    {
        string s = UIStrings.Volume;
        int i = PlayerPrefs.GetInt(s);

        if (i < minMaxIntSettings[s].Item1 || i > minMaxIntSettings[s].Item2)
        {
            i = defaultIntSettings[s];
        }

        GameManager.RealTimeSettings.Volume = (float)(i) / 100f;
        return i;
    }

    int SetFrameRate(int i)
    {
        string s = UIStrings.Framerate;
        int newf = SaturateIntSetting(s, i);

        // if it has been set as too large somehow

        Application.targetFrameRate = newf;

        PlayerPrefs.SetInt(s, newf);
        PlayerPrefs.Save();
        return newf;
    }

    int SetPhysicsRate(int i)
    {
        string s = UIStrings.PhysicsRate;
        int newf = SaturateIntSetting(s, i);
        //Debug.Log($"Setting physics rate to {f}");

        Time.fixedDeltaTime = 1f / (float)newf;

        PlayerPrefs.SetInt(s, newf);
        PlayerPrefs.Save();
        return newf;
    }

    int SetVolume(int i)
    {
        string s = UIStrings.Volume;
        int newf = SaturateIntSetting(s, i);

        Debug.LogWarning($"TODO: set global volume to {i}");

        //AudioManager.Instance.
        PlayerPrefs.SetInt(s, newf);
        PlayerPrefs.Save();
        return newf;
    }

    int SetPhysicsParticles(int i)
    {
        string s = UIStrings.PhysicsParticles;
        int newf = SaturateIntSetting(s, i);

        Debug.LogWarning($"TODO: set physics particles to {i}");

        PlayerPrefs.SetInt(s, newf);
        PlayerPrefs.Save();
        return newf;
    }

    public void UpdateSettings()
    {
        foreach (string intSetting in UIStrings.SettingsIntProperties)
        {
            int i = this.GettingUpdateFunctions[intSetting].Invoke();
            this.SettingUpdateFunctions[intSetting].Invoke(i);
        }
    }

    public int SaturateIntSetting(string settingName, int val)
    {
        Tuple<int, int> minMax = GameManager.minMaxIntSettings[settingName];
        return Math.Min(minMax.Item2, Math.Max(minMax.Item1, val));
    }
}

