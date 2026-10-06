using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System;
using MissionGame;
using System.Linq;

[CreateAssetMenu(fileName = "UiSoundLibrary", menuName = "Scriptable Objects/UiSoundLibrary")]
public class UiSoundLibrary : ScriptableObject
{
    [Label("Sounds")] public List<UiSoundEntry> soundEntries = GetEmptySoundEntries();

    public UiSoundEntry GetSoundEntry(UiSound sound) => soundEntries.First(e => e.sound == sound);
    
    public static List<UiSoundEntry> GetEmptySoundEntries()
    {
        List<UiSoundEntry> entries = new();
        foreach(UiSound sound in Enum.GetValues(typeof(UiSound)))
        {
            entries.Add(UiSoundEntry.Default(sound));
        }
        return entries;
    }
}
