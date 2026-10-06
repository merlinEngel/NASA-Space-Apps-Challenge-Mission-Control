using System;
using UnityEngine;

namespace MissionGame
{
    [Serializable]
    public struct UiSoundEntry
    {
        public UiSound sound;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume;
        // Random offset added to pitch 1.0, e.g. (-0.05, 0.05).
        public Vector2 pitchRange;
        // Random offset added to volume, e.g. (-0.1, 0).
        public Vector2 volumeRange;

        public readonly float RandomPitch => 1f + UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        public readonly float RandomVolume => Mathf.Clamp01(volume + UnityEngine.Random.Range(volumeRange.x, volumeRange.y));
        public readonly AudioClip RandomClip => clips[UnityEngine.Random.Range(0, clips.Length - 1)];

        public UiSoundEntry(UiSound sound, AudioClip[] clips, float volume, Vector2 pitchRange, Vector2 volumeRange)
        {
            this.sound = sound;
            this.clips = clips;
            this.volume = volume;
            this.pitchRange = pitchRange;
            this.volumeRange = volumeRange;
        }

        /// <summary>Sensible start values for a new entry (a struct default would be volume 0 = silent).</summary>
        public static UiSoundEntry Default(UiSound sound) =>
            new(sound, Array.Empty<AudioClip>(), 1f, new Vector2(-0.05f, 0.05f), Vector2.zero);
    }

    public enum UiSound
    {
        Hover,
        Click,
        Select,
        TabSwitch,
        SliderTick,
        Error,
        StatusRed,
        StatusGreen
    }
}
