using UnityEngine;

namespace _Project.Code.Core {
    /// <summary>
    ///     Minimal generic MonoBehaviour singleton for the jam. Access via <c>T.Instance</c>.
    ///     If no instance is placed in the scene, one is created automatically on first
    ///     access. Survives scene loads (DontDestroyOnLoad), and a duplicate that sneaks
    ///     into a scene destroys itself.
    /// </summary>
    /// <example>public class GameManager : Singleton&lt;GameManager&gt; { }</example>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T> {
        private static T _instance;
        private static bool _isQuitting;

        /// <summary>True when a usable instance exists and the app isn't shutting down.</summary>
        public static bool Exists => _instance != null && !_isQuitting;

        public static T Instance {
            get {
                if (_isQuitting) {
                    return null;
                }

                if (_instance == null) {
                    _instance = FindFirstObjectByType<T>();
                }

                if (_instance == null) {
                    GameObject go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }

                return _instance;
            }
        }

        protected virtual void Awake() {
            if (_instance != null && _instance != this) {
                // A second instance ended up in the scene — discard it.
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            _isQuitting = false;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit() {
            _isQuitting = true;
        }

        protected virtual void OnDestroy() {
            if (_instance == this) {
                _instance = null;
            }
        }
    }
}
