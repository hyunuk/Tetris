using System;
using UnityEngine.Audio;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;

    void Awake() {
        foreach (Sound s in sounds) {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    public void Play(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Play();
    }

    public void Stop(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Stop();
    }

    public void Mute(string name, bool isMute) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.mute = isMute;
    }
}
