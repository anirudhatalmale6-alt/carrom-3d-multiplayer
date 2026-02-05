using UnityEngine;

namespace BEKStudio
{
    /// <summary>
    /// Bridge for JavaScript to Unity communication in WebGL
    /// This object is created on load and receives messages from JavaScript
    /// </summary>
    public class JSBridge : MonoBehaviour
    {
        public static JSBridge Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                gameObject.name = "JSBridge"; // Ensure the name matches what JS expects
                Debug.Log("JSBridge initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called from JavaScript to set user ID
        /// </summary>
        public void SetUserIdFromJS(string userIdStr)
        {
            Debug.Log("JSBridge.SetUserIdFromJS called with: " + userIdStr);

            if (WordPressAPI.Instance != null)
            {
                WordPressAPI.Instance.SetUserIdFromJS(userIdStr);
            }
            else
            {
                Debug.LogError("WordPressAPI.Instance is null!");
                // Try to find it
                WordPressAPI api = FindObjectOfType<WordPressAPI>();
                if (api != null)
                {
                    api.SetUserIdFromJS(userIdStr);
                }
            }
        }
    }
}
