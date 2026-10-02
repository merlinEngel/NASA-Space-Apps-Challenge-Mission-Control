using UnityEngine;

namespace MissionGame
{
    // Base class for scene-wide managers that exist exactly once.
    // Usage: public class CatalogManager : Singleton<CatalogManager> { ... }
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"Second {typeof(T).Name} found on '{name}', destroying it.", this);
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}