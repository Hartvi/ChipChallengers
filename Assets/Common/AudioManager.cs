using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds = Array.Empty<Sound>();
    public HashSet<Sound> allSounds = new HashSet<Sound>();
    readonly Dictionary<string, List<Sound>> reservations = new Dictionary<string, List<Sound>>();
    readonly Dictionary<string, int> nextReservation = new Dictionary<string, int>();
    readonly List<AudioSource> ownedSources = new List<AudioSource>();
    public static float volume = 1f;
    private static AudioManager instance = null;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null) instance = FindObjectOfType<AudioManager>();
            if (instance == null) instance = new GameObject("AudioManager").AddComponent<AudioManager>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            if (Application.isPlaying) Destroy(this);
            else DestroyImmediate(this);
            return;
        }
        instance = this;
        if (sounds == null) sounds = Array.Empty<Sound>();
        foreach (Sound s in this.sounds)
        {
            if (s == null) continue;
            s.source = this.CreateSource(s);
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        foreach (var source in this.ownedSources)
        {
            if (source == null) continue;
            if (Application.isPlaying) Destroy(source);
            else DestroyImmediate(source);
        }
        this.ownedSources.Clear();
        this.reservations.Clear();
        this.allSounds.Clear();
    }

    AudioSource CreateSource(Sound definition)
    {
        var source = gameObject.AddComponent<AudioSource>();
        this.ownedSources.Add(source);
        source.clip = definition.clip;
        source.volume = definition.volume;
        source.pitch = definition.pitch;
        source.loop = definition.loop;
        return source;
    }

    public bool Play(string name)
    {
        Sound s = Array.Find(this.sounds, sound => sound != null && sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound " + name + " not found!");
            return false;
        }
        if (!s.source.isPlaying)
        {
            s.source.Play();
            return true;
        }
        else
        {
            return false;
        }
    }
    public void StopAll()
    {
        foreach (Sound s in this.sounds)
        {
            if (s?.source != null) s.source.Stop();
        }
        this.StopSounds();
    }
    public void PlaySounds()
    {
        foreach (var s in allSounds)
        {
            if (s?.source != null) s.source.Play();
        }
    }
    public void Stop(string name)
    {
        Sound s = Array.Find(this.sounds, sound => sound != null && sound.name == name);
        if (s?.source != null) s.source.Stop();
    }
    public Action<float> ReserveSound(string name, Action<float, AudioSource> soundFunc)
    {
        if (soundFunc == null) throw new ArgumentNullException(nameof(soundFunc));
        Sound s = Array.Find(this.sounds, sound => sound != null && sound.name == name);
        if (s == null) throw new ArgumentException("Unknown sound: " + name, nameof(name));
        if (s.maxInstanceNumber < 1) throw new InvalidOperationException("Sound instance limit must be positive: " + name);
        if (!this.reservations.TryGetValue(name, out var pool))
        {
            pool = new List<Sound>();
            this.reservations.Add(name, pool);
            this.nextReservation.Add(name, 0);
        }
        AudioSource src;
        if (pool.Count < s.maxInstanceNumber)
        {
            var reserved = new Sound { name = name, source = this.CreateSource(s) };
            pool.Add(reserved);
            this.allSounds.Add(reserved);
            src = reserved.source;
            src.volume = 0f;
            src.Play();
        }
        else
        {
            int index = this.nextReservation[name] % pool.Count;
            src = pool[index].source;
            this.nextReservation[name] = (index + 1) % pool.Count;
        }
        return value => { if (src != null) soundFunc(value, src); };
    }
    public static void TireSoundFunc(float slip, AudioSource src)
    {
        float deltaSlip = slip - 0.18f;
        float volume = deltaSlip * 0.50f;
        SetEndVolume(src, volume > 1f ? 1f : volume < 0f ? 0f : volume);
        // src.volume = volume > 1f ? 1f : volume < 0f ? 0f : volume;
        if (volume > 0f)
        {
            src.pitch = 0.55f + Mathf.Min(Mathf.Max(0f, 0.005f * deltaSlip), 0.1f);
        }
    }
    public static void CrashSoundFunc(float relativeSpeed, AudioSource src)
    {
        float newvolume = relativeSpeed * 0.011f;
        newvolume = newvolume > 0.8f ? 0.8f : newvolume < 0f ? 0f : newvolume;
        PlayAtVolume(src, newvolume);
        // src.Play();
        // if(volume > 0f) {
        //     src.pitch = 0.8f + Mathf.Min(Mathf.Max(0f, 0.01f*relativeSpeed), 0.1f);
        // }
    }
    public static void MotorSoundFunc(float omega, AudioSource src)
    {
        float absomega = Mathf.Abs(omega);
        float newVolume = 0.0f + 5e-4f * absomega;
        // if(newVolume < 0.1f && src.volume < 0.1f) return;
        if (newVolume > 1f) return;
        float newPitch = 0.25f + 1.0e-3f * absomega;
        // if(newPitch < 0.2f) return;
        if (newPitch > 1f) return;
        // if(newVolume < 0.1f) newVolume = 0f;
        // if(Mathf.Round(1e-1f*omega) != Mathf.Round(src.pitch)) {
        src.pitch = newPitch;
        SetEndVolume(src, newVolume);
        // src.volume = newVolume;
        // }
    }

    public static void FanSoundFunc(float val, AudioSource src)
    {
        float newVolume = Mathf.Min(Mathf.Abs(val) * 7e-5f, 0.7f);
        SetEndVolume(src, newVolume);
    }

    public static void GunSoundFunc(float val, AudioSource src)
    {
        float newVolume = Mathf.Min(0.5f + Mathf.Abs(val) * 5e-4f, 1f);
        PlayAtVolume(src, newVolume);
    }

    public void StopSounds()
    {
        foreach (Sound s in this.allSounds)
        {
            if (s?.source == null) continue;
            s.source.volume = 0f;
            s.source.Stop();
        }
    }
    static void PlayAtVolume(AudioSource src, float newvolume)
    {
        src.volume = newvolume * volume;
        src.Play();
    }
    static void SetEndVolume(AudioSource src, float newVol)
    {
        src.volume = volume * newVol;
    }
}
