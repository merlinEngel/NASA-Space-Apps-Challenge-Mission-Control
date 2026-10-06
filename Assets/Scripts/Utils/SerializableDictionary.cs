using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissionGame
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [Serializable]
        private struct Entry
        {
            public TKey key;
            public TValue value;
        }

        [SerializeField] private List<Entry> entries = new();

        // Unity is about to save: copy the dictionary into the list,
        // unless the list already matches (keeps half-edited inspector rows alive).
        public void OnBeforeSerialize()
        {
            if (ListMatchesDictionary()) return;

            entries.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
                entries.Add(new Entry { key = pair.Key, value = pair.Value });
        }

        // Unity has loaded the list: rebuild the dictionary from it.
        public void OnAfterDeserialize()
        {
            Clear();
            foreach (Entry e in entries)
            {
                if (e.key == null || ContainsKey(e.key)) continue;   // empty or duplicate key
                this[e.key] = e.value;
            }
        }

        private bool ListMatchesDictionary()
        {
            var seen = new HashSet<TKey>();
            var comparer = EqualityComparer<TValue>.Default;
            foreach (Entry e in entries)
            {
                if (e.key == null || !seen.Add(e.key)) continue;
                if (!TryGetValue(e.key, out TValue value) || !comparer.Equals(value, e.value)) return false;
            }
            return seen.Count == Count;
        }
    }
}