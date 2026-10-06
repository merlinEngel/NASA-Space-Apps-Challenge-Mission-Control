using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MissionGame.UI
{
    public class TabList : MonoBehaviour
    {

        public UnityEvent<int> onTabSelected;

        TabListTab[] AllTabs
        {
            get
            {
                return GetComponentsInChildren<TabListTab>(true);
            }
        }

        public TabListTab GetTab(int index)
        {
            return AllTabs.Length > index ? AllTabs[index] : AllTabs.Last();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            int i = 0;
            foreach (TabListTab tab in AllTabs)
            {
                tab.tabIndex = i;
                i++;
            }
            SelectTab(0);
        }

        TabListTab GetOption(int index)
        {
            foreach (TabListTab option in AllTabs)
            {
                if (option.tabIndex == index) return option;
            }
            return AllTabs.Last();
        }

        public void SelectTab(int index)
        {
            onTabSelected.Invoke(index);
            foreach (TabListTab tab in AllTabs)
            {
                tab.Deselect();
            }
            GetOption(index).Select();
        }
    }
}