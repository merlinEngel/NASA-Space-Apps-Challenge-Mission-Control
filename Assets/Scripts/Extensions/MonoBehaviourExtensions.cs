using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame
{
    public static class MonoBehaviourExtensions
    {
        public static void Refresh(this MonoBehaviour mb, RectTransform transform)
        {
            mb.StartCoroutine(RebuildLayoutNextFrame(transform));
        }

        static IEnumerator RebuildLayoutNextFrame(RectTransform rt)
        {
            // Wait one frame so all rows exist and TMP has measured its texts.
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}