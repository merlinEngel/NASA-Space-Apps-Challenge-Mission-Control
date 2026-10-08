using MissionGame;
using UnityEngine;
using UnityEngine.Audio;

public class UiAudio : Singleton<UiAudio>
{
    public UiSoundLibrary soundLibrary;
    [SerializeField] private int sourceCount = 4;
    [SerializeField] private AudioMixerGroup output;   // UI group, can stay empty for now

    private AudioSource[] sources;
    public AudioSource[] Sources => GetComponents<AudioSource>();
    private int currentSourceId = 0;
    public AudioSource CurrentSource {
        get
        {
            currentSourceId = (currentSourceId+1)%sourceCount;
            return Sources[currentSourceId];
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        sources = new AudioSource[sourceCount];
        for (int i = 0; i < sourceCount; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.outputAudioMixerGroup = output;
            sources[i] = s;
        }
    }

    /// <summary>
    /// Play a oneshot UI sound
    /// </summary>
    /// <param name="sound">kind of sound</param>
    public void Play(UiSound sound)
    {
        UiSoundEntry entry = soundLibrary.GetSoundEntry(sound);
        if (entry.clips.Length > 0)
        {
            AudioClip clip = entry.RandomClip;
            float volume = entry.RandomVolume;
            float pitch = entry.RandomPitch;

            CurrentSource.pitch = pitch;
            CurrentSource.PlayOneShot(clip, volume);
        }
    }
}
