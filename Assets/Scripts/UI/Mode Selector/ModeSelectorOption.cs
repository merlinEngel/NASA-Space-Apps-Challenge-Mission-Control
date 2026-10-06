using Unity.Theme.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame.UI
{
    public class ModeSelectorOption : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] ImageColorBinder colorBinder;
        [SerializeField] ModeSelector selector;

         [System.NonSerialized] public int optionIndex;

        public void Clicked()
        {
            selector.SelectOption(optionIndex);
        }

        public void Select()
        {
            colorBinder.SetColorByName("Border");
        } 

        public void Deselect()
        {
            colorBinder.SetColorByName("Background");
        }
    }
}