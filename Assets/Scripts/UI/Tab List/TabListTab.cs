using UnityEngine;
using UnityEngine.UI;

namespace MissionGame.UI
{
    public class TabListTab : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] GameObject accentBar;
        [SerializeField] TabList list;

         [System.NonSerialized] public int tabIndex;

        public void Clicked()
        {
            list.SelectTab(tabIndex);
        }

        public void Select()
        {
            accentBar.SetActive(true);
        } 

        public void Deselect()
        {
            accentBar.SetActive(false);
        }
    }
}