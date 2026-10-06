using UnityEngine;
using UnityEngine.UI;

namespace Unity.Theme.Binders
{
    /// <summary>
    /// Binds theme colors to a Unity Slider: Background, Fill and Handle.
    /// </summary>
    [AddComponentMenu("Theme/Slider Color Binder")]
    public class SliderColorBinder : GenericMultiColorBinder<Slider>
    {
        private const int BACKGROUND_INDEX = 0;
        private const int FILL_INDEX = 1;
        private const int HANDLE_INDEX = 2;

        protected override string[] ColorEntries => new string[]
        {
            "Background",
            "Fill",
            "Handle"
        };

        protected override void SetColors(Slider targetComponent, Color[] colors)
        {
            SetGraphicColor(GetBackground(targetComponent), colors[BACKGROUND_INDEX]);
            SetGraphicColor(GetFill(targetComponent), colors[FILL_INDEX]);
            SetGraphicColor(GetHandle(targetComponent), colors[HANDLE_INDEX]);
        }

        protected override Color[] GetColors(Slider target)
        {
            return new Color[]
            {
                GetGraphicColor(GetBackground(target)),
                GetGraphicColor(GetFill(target)),
                GetGraphicColor(GetHandle(target))
            };
        }

        // Standard Slider hierarchy: Slider/Background, Slider/Fill Area/Fill, Slider/Handle Slide Area/Handle
        private static Graphic GetBackground(Slider slider)
        {
            var background = slider.transform.Find("Background");
            return background != null ? background.GetComponent<Graphic>() : null;
        }

        private static Graphic GetFill(Slider slider)
            => slider.fillRect != null ? slider.fillRect.GetComponent<Graphic>() : null;

        private static Graphic GetHandle(Slider slider)
            => slider.handleRect != null ? slider.handleRect.GetComponent<Graphic>() : null;

        private static void SetGraphicColor(Graphic graphic, Color color)
        {
            if (graphic != null)
                graphic.color = color;
        }

        private static Color GetGraphicColor(Graphic graphic)
            => graphic != null ? graphic.color : Color.white;
    }
}
