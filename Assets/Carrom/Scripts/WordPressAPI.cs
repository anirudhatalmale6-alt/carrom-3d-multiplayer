using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

namespace BEKStudio
{
    [Serializable]
    public class CoinResponse
    {
        public string status;
        public string msg;
        public int user_id;
        public string username;
        public int coins;
    }

    public class WordPressAPI : MonoBehaviour
    {
        public static WordPressAPI Instance;

        // API Endpoints - Change these to your WordPress site
        private const string BASE_URL = "https://tasktrophy.in";
        private const string GET_BALANCE_URL = BASE_URL + "/get_coin_balance.php";
        private const string UPDATE_BALANCE_URL = BASE_URL + "/update_coin_balance.php";

        // Security key - must match PHP file
        private const string SECRET_KEY = "TaskTrophy_Secure_2026";

        // Entry fee per match (for local coin deduction)
        public const int ENTRY_FEE = 10;
        public const int WIN_PRIZE = 18;

        // Cached user data
        public int UserId { get; private set; }
        public string Username { get; private set; }
        public int CoinBalance { get; private set; }
        public bool IsLoaded { get; private set; }

        // Debug text for showing user ID on screen
        public static string DebugMessage = "Initializing...";

        // Callbacks
        public Action<bool, string, int> OnBalanceLoaded;
        public Action<bool, string> OnBalanceUpdated;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            Debug.Log("WordPressAPI started");
            DebugMessage = "WordPressAPI Started";

            // Parse URL immediately using Application.absoluteURL
            ParseUrlAndLoadBalance();
        }

        /// <summary>
        /// Parse user_id from URL using Application.absoluteURL
        /// This works reliably in WebGL without JavaScript interop
        /// </summary>
        void ParseUrlAndLoadBalance()
        {
            string url = Application.absoluteURL;
            Debug.Log("Application.absoluteURL: " + url);
            DebugMessage = "URL: " + url;

            // Parse user_id from URL
            if (!string.IsNullOrEmpty(url) && url.Contains("user_id="))
            {
                try
                {
                    // Split by "user_id=" to get the part after it
                    string[] parts = url.Split(new string[] { "user_id=" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        // Get the ID part (stop at & or end of string)
                        string idStr = parts[1].Split('&')[0];
                        Debug.Log("Parsed user_id string: " + idStr);

                        int id = 0;
                        if (int.TryParse(idStr, out id) && id > 0)
                        {
                            UserId = id;
                            PlayerPrefs.SetInt("current_user_id", id);
                            PlayerPrefs.Save();

                            DebugMessage = "Debug ID: " + id;
                            Debug.Log("User ID set to: " + id);

                            // Now call API to get balance
                            StartCoroutine(LoadBalanceFromAPI(id));
                        }
                        else
                        {
                            DebugMessage = "Invalid ID: " + idStr;
                            Debug.LogError("Could not parse user_id: " + idStr);
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugMessage = "Parse Error: " + e.Message;
                    Debug.LogError("URL parse error: " + e.Message);
                }
            }
            else
            {
                DebugMessage = "No user_id in URL";
                Debug.LogWarning("No user_id found in URL: " + url);
            }
        }

        /// <summary>
        /// Load balance from API with specific user ID
        /// </summary>
        private IEnumerator LoadBalanceFromAPI(int userId)
        {
            string url = GET_BALANCE_URL + "?user_id=" + userId;
            Debug.Log("Loading balance from: " + url);
            DebugMessage = "Loading balance for ID: " + userId;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log("API Response: " + json);

                        CoinResponse response = JsonUtility.FromJson<CoinResponse>(json);

                        if (response.status == "success")
                        {
                            CoinBalance = response.coins;
                            Username = response.username;
                            IsLoaded = true;

                            // Update PlayerPrefs
                            PlayerPrefs.SetInt("coin", response.coins);
                            PlayerPrefs.Save();

                            DebugMessage = "ID: " + userId + " | Coins: " + response.coins;
                            Debug.Log("Balance loaded: " + response.coins + " for user: " + response.username);

                            // Update UI
                            if (MenuController.Instance != null)
                            {
                                MenuController.Instance.UpdateCurrencyText();
                            }
                        }
                        else
                        {
                            DebugMessage = "API Error: " + response.msg;
                            Debug.LogError("API error: " + response.msg);
                        }
                    }
                    catch (Exception e)
                    {
                        DebugMessage = "JSON Error: " + e.Message;
                        Debug.LogError("JSON parse error: " + e.Message);
                    }
                }
                else
                {
                    DebugMessage = "Network Error: " + request.error;
                    Debug.LogError("Network error: " + request.error);
                }
            }
        }

        /// <summary>
        /// Load coin balance from WordPress DB on game start
        /// </summary>
        public void LoadCoinBalance(Action<bool, string, int> callback = null)
        {
            OnBalanceLoaded = callback;
            StartCoroutine(GetBalanceCoroutine());
        }

        /// <summary>
        /// Save coin balance to WordPress DB on game over
        /// </summary>
        public void SaveCoinBalance(int newBalance, Action<bool, string> callback = null)
        {
            OnBalanceUpdated = callback;
            StartCoroutine(UpdateBalanceCoroutine(newBalance));
        }

        /// <summary>
        /// Deduct entry fee locally (no API call)
        /// </summary>
        public bool DeductEntryFeeLocal()
        {
            if (CoinBalance >= ENTRY_FEE)
            {
                CoinBalance -= ENTRY_FEE;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add winnings locally (no API call)
        /// </summary>
        public void AddWinningsLocal()
        {
            CoinBalance += WIN_PRIZE;
        }

        /// <summary>
        /// Set balance directly (for syncing with game's internal coin system)
        /// </summary>
        public void SetBalance(int balance)
        {
            CoinBalance = balance;
        }

        /// <summary>
        /// Set user ID directly (called from JavaScript)
        /// </summary>
        public void SetUserId(int id)
        {
            UserId = id;
            Debug.Log("User ID set to: " + id);
        }

        /// <summary>
        /// Called from JavaScript to set user ID and reload balance
        /// </summary>
        public void SetUserIdFromJS(string userIdStr)
        {
            Debug.Log("SetUserIdFromJS called with: " + userIdStr);
            int id = 0;
            if (int.TryParse(userIdStr, out id) && id > 0)
            {
                UserId = id;
                Debug.Log("User ID set from JS: " + UserId);
                // Start coroutine to reload balance
                StartCoroutine(ReloadBalanceFromJS());
            }
        }

        private IEnumerator ReloadBalanceFromJS()
        {
            // Wait a frame to avoid any stack issues
            yield return null;

            // Build URL with user_id
            string url = GET_BALANCE_URL + "?user_id=" + UserId;
            Debug.Log("Reloading balance from: " + url);

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log("JS Balance Response: " + json);

                        CoinResponse response = JsonUtility.FromJson<CoinResponse>(json);

                        if (response.status == "success")
                        {
                            CoinBalance = response.coins;
                            Username = response.username;
                            IsLoaded = true;

                            // Update PlayerPrefs
                            PlayerPrefs.SetInt("coin", response.coins);
                            PlayerPrefs.Save();

                            // Update UI safely
                            if (MenuController.Instance != null)
                            {
                                MenuController.Instance.UpdateCurrencyText();
                            }

                            Debug.Log("Balance updated from JS: " + response.coins);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("JSON error: " + e.Message);
                    }
                }
                else
                {
                    Debug.LogError("Network error: " + request.error);
                }
            }
        }

        private IEnumerator GetBalanceCoroutine()
        {
            // Build URL with user_id if we have it
            string url = GET_BALANCE_URL;
            if (UserId > 0)
            {
                url += "?user_id=" + UserId;
            }

            Debug.Log("Loading balance from: " + url);

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log("GetBalance Response: " + json);

                        CoinResponse response = JsonUtility.FromJson<CoinResponse>(json);

                        if (response.status == "success")
                        {
                            UserId = response.user_id;
                            Username = response.username;
                            CoinBalance = response.coins;
                            IsLoaded = true;
                            OnBalanceLoaded?.Invoke(true, "Balance loaded", CoinBalance);
                        }
                        else
                        {
                            Debug.LogWarning("API returned error: " + response.msg);
                            OnBalanceLoaded?.Invoke(false, response.msg, 0);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("JSON Parse Error: " + e.Message + " - Raw: " + request.downloadHandler.text);
                        OnBalanceLoaded?.Invoke(false, "Server error", 0);
                    }
                }
                else
                {
                    Debug.LogError("API Error: " + request.error);
                    OnBalanceLoaded?.Invoke(false, "Network error", 0);
                }
            }
        }

        private IEnumerator UpdateBalanceCoroutine(int newBalance)
        {
            // Skip if no user ID
            if (UserId <= 0)
            {
                Debug.LogWarning("Cannot update balance - no user ID");
                OnBalanceUpdated?.Invoke(false, "No user ID");
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("user_id", UserId);
            form.AddField("coins", newBalance);
            form.AddField("key", SECRET_KEY);

            Debug.Log("Updating balance for user " + UserId + " to " + newBalance);

            using (UnityWebRequest request = UnityWebRequest.Post(UPDATE_BALANCE_URL, form))
            {
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log("UpdateBalance Response: " + json);

                        CoinResponse response = JsonUtility.FromJson<CoinResponse>(json);

                        if (response.status == "success")
                        {
                            CoinBalance = newBalance;
                            OnBalanceUpdated?.Invoke(true, "Balance saved");
                        }
                        else
                        {
                            Debug.LogWarning("Update failed: " + response.msg);
                            OnBalanceUpdated?.Invoke(false, response.msg);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("JSON Parse Error: " + e.Message);
                        OnBalanceUpdated?.Invoke(false, "Server error");
                    }
                }
                else
                {
                    Debug.LogError("API Error: " + request.error);
                    OnBalanceUpdated?.Invoke(false, "Network error");
                }
            }
        }
    }
}
