using MissionGame;
using UnityEngine;

namespace MissionGame.UI
{
    public class TabManager : MonoBehaviour
    {
        [SerializeField] public SerializableDictionary<int, RectTransform> tabContents = new();

        void Start()
        {
            TabClicked(0);
        }

        public void TabClicked(int index)
        {
            if (tabContents.ContainsKey(index)) ShowTab(index);
        }

        private void ShowTab(int index)
        {
            foreach (int tabKey in tabContents.Keys)
            {
                tabContents[tabKey].gameObject.SetActive(false);
            }
            tabContents[index].gameObject.SetActive(true);
        }
    }
}
