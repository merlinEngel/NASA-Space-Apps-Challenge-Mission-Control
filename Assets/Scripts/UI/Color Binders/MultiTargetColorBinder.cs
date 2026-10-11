using System.Collections.Generic;
using Unity.Theme.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame.UI
{
    // Applies one theme color to several UI graphics (Image, RawImage, TMP text).
    [AddComponentMenu("Theme/Multi Target Color Binder")]
    public class MultiTargetColorBinder : BaseColorBinder
    {
        [SerializeField] Graphic[] targets = new Graphic[0];

        // Marks the targets dirty when the color changes, so prefab edits are saved.
        protected override IEnumerable<Object> ColorTargets
        {
            get
            {
                foreach (Graphic target in targets)
                    if (target != null) yield return target;
            }
        }

        protected override bool CanApplyColor() => targets != null && targets.Length > 0;

        protected override void SetColorInternal(Color color)
        {
            foreach (Graphic target in targets)
                if (target != null) target.color = color;
        }

        // The base class skips the update when this equals the theme color.
        // So only report a color if all targets share it; otherwise null forces an update.
        public override Color? GetColor()
        {
            Color? shared = null;
            foreach (Graphic target in targets)
            {
                if (target == null) continue;
                if (shared == null) shared = target.color;
                else if (shared.Value != target.color) return null;
            }
            return shared;
        }
    }
}
