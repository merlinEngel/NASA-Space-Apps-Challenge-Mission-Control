using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MissionGame.UI
{
    public class ModeSelector : MonoBehaviour
    {

        public UnityEvent<int> onOptionSelected;

        public ModeSelectorOption[] AllOptions
        {
            get
            {
                return GetComponentsInChildren<ModeSelectorOption>(true);
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            int i = 0;
            foreach (ModeSelectorOption option in AllOptions)
            {
                option.optionIndex = i;
                i++;
            }
            SelectOption(0);
        }

        ModeSelectorOption GetOption(int index)
        {
            foreach (ModeSelectorOption option in AllOptions)
            {
                if (option.optionIndex == index) return option;
            }
            return AllOptions.Last();
        }

        public void SelectOption(int index)
        {
            onOptionSelected.Invoke(index);
            foreach (ModeSelectorOption option in AllOptions)
            {
                option.Deselect();
            }
            GetOption(index).Select();
        }
    }
}