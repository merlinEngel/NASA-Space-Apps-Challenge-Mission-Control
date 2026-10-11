using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Theme.Binders
{
    // Applies one theme color to several UI graphics (Image, RawImage, TMP text).
    [AddComponentMenu("Theme/Graphics Color Binder")]
    public class GraphicsColorBinder : BaseColorBinder
    {
        [SerializeField] Graphic[] targets = new Graphic[0];

        [Tooltip("Only set RGB from the theme and keep the alpha that is set on each target.")]
        [SerializeField] bool keepTargetAlpha = true;

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

        // With keepTargetAlpha the alpha is ignored, so it is fixed to 1 here and in GetColor.
        // That way the base class can still compare colors and skip unnecessary updates.
        protected override Color GetTargetColor(ColorData colorData)
        {
            Color color = base.GetTargetColor(colorData);
            if (keepTargetAlpha) color.a = 1f;
            return color;
        }

        protected override void SetColorInternal(Color color)
        {
            foreach (Graphic target in targets)
            {
                if (target == null) continue;
                target.color = keepTargetAlpha
                    ? new Color(color.r, color.g, color.b, target.color.a)
                    : color;
            }
        }

        // The base class skips the update when this equals the theme color.
        // So only report a color if all targets share it; otherwise null forces an update.
        public override Color? GetColor()
        {
            Color? shared = null;
            foreach (Graphic target in targets)
            {
                if (target == null) continue;
                Color color = target.color;
                if (keepTargetAlpha) color.a = 1f;

                if (shared == null) shared = color;
                else if (shared.Value != color) return null;
            }
            return shared;
        }
    }
}
