using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public HashSet<Sound> allSounds = new HashSet<Sound>();
    int newReservationId = 0;
    Dictionary<string, int> ReservationCounts = new Dictionary<string, int>();
    Dictionary<int, Sound> SoundReservations = new Dictionary<int, Sound>();
    public static float volume = 1f;
    private static AudioManager instance = null;
    public static AudioManager Instance
    {
        get
        {
            return instance ?? new GameObject("AudioManager").AddComponent<AudioManager>();
        }
    }

    void Awake()
    {
        foreach (Sound s in this.sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            ReservationCounts[s.name] = 0;
        }
    }

    public bool Play(string name)
    {
        Sound s = Array.Find(this.sounds, sound => sound.name == name);
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
            s.source.Stop();
        }
    }
    public void PlaySounds()
    {
        foreach (var s in allSounds)
        {
            s.source.Play();
        }
    }
    public void Stop(string name)
    {
        Sound s = Array.Find(this.sounds, sound => sound.name == name);
        s.source.Stop();
    }
    public Action<float> ReserveSound(string name, Action<float, AudioSource> soundFunc)
    {
        print("sounds: " + sounds.Length);
        Sound s = Array.Find(this.sounds, sound => sound.name == name);
        Action<float> ret;
        int _newReservationId;
        if (ReservationCounts[name] < s.maxInstanceNumber)
        {
            Sound newSound = new Sound();
            allSounds.Add(newSound);
            newSound.name = name;
            newSound.source = gameObject.AddComponent<AudioSource>();
            newSound.source.clip = s.clip;

            newSound.source.volume = s.volume;
            newSound.source.pitch = s.pitch;
            newSound.source.loop = s.loop;
            SoundReservations[newReservationId] = newSound;
            _newReservationId = newReservationId;
            newSound.source.volume = 0f;
            newSound.source.Play();
            newReservationId++;
        }
        else
        {
            Sound[] sounds = new Sound[SoundReservations.Values.Count];
            SoundReservations.Values.CopyTo(sounds, 0);
            Sound[] nameSounds = Array.FindAll(sounds, sound => sound.name == name);
            Sound selectedSound = nameSounds[ReservationCounts[name] % s.maxInstanceNumber];
            int resId = Array.FindIndex(sounds, sound => sound == selectedSound);
            _newReservationId = resId;
        }
        AudioSource src = SoundReservations[_newReservationId].source;
        ret = (slip) => soundFunc(slip, src);
        // {
        //     float deltaSlip = slip-0.18f;
        //     float volume = deltaSlip*0.50f;
        //     var src = SoundReservations[_newReservationId].source;
        //     src.volume = volume > 1f ? 1f : volume < 0f ? 0f : volume;
        //     if(volume > 0f) {
        //         src.pitch = 0.8f + Mathf.Min(Mathf.Max(0f, 0.01f*deltaSlip), 0.1f);
        //     }
        // };
        ReservationCounts[name]++;
        return ret;
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
