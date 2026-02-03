using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace BEKStudio
{
    [Serializable]
    public class APIResponse
    {
        public string status;
        public string msg;
        public float new_balance;
        public int user_id;
        public float prize;
    }

    public class WordPressAPI : MonoBehaviour
    {
        public static WordPressAPI Instance;

        // API Endpoints - Change these to your WordPress site
        private const string BASE_URL = "https://tasktrophy.in/games/carrom/api";
        private const string DEDUCT_FEE_URL = BASE_URL + "/deduct_fee.php";
        private const string CREDIT_WIN_URL = BASE_URL + "/credit_win.php";

        // Security key - must match PHP file
        private const string SECRET_KEY = "TaskTrophy_Secure_2026";

        // Entry fee and prize amounts
        public const int ENTRY_FEE = 10;
        public const int WIN_PRIZE = 18;

        // Callbacks
        public Action<bool, string, float> OnDeductFeeComplete;
        public Action<bool, string> OnCreditWinComplete;

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

        /// <summary>
        /// Deduct entry fee from user's wallet
        /// </summary>
        public void DeductEntryFee(Action<bool, string, float> callback)
        {
            OnDeductFeeComplete = callback;
            StartCoroutine(DeductFeeCoroutine());
        }

        /// <summary>
        /// Credit winnings to user's wallet
        /// </summary>
        public void CreditWinnings(Action<bool, string> callback)
        {
            OnCreditWinComplete = callback;
            StartCoroutine(CreditWinCoroutine());
        }

        private IEnumerator DeductFeeCoroutine()
        {
            WWWForm form = new WWWForm();
            // No additional data needed - WordPress uses session/cookies for user_id

            using (UnityWebRequest request = UnityWebRequest.Post(DEDUCT_FEE_URL, form))
            {
                // Important for WebGL - allows cookies/session
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log("DeductFee Response: " + json);

                        APIResponse response = JsonUtility.FromJson<APIResponse>(json);

                        if (response.status == "success")
                        {
                            OnDeductFeeComplete?.Invoke(true, "Fee deducted", response.new_balance);
                        }
                        else
                        {
                            OnDeductFeeComplete?.Invoke(false, response.msg, 0);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("JSON Parse Error: " + e.Message);
                        OnDeductFeeComplete?.Invoke(false, "Server error", 0);
                    }
                }
                else
                {
                    Debug.LogError("API Error: " + request.error);
                    OnDeductFeeComplete?.Invoke(false, "Network error", 0);
                }
            }
        }

        private IEnumerator CreditWinCoroutine()
        {
            WWWForm form = new WWWForm();
            form.AddField("secret_key", SECRET_KEY);

            using (UnityWebRequest request = UnityWebRequest.Post(CREDIT_WIN_URL, form))
            {
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log("CreditWin Response: " + json);

                        APIResponse response = JsonUtility.FromJson<APIResponse>(json);

                        if (response.status == "success")
                        {
                            OnCreditWinComplete?.Invoke(true, "Winnings credited!");
                        }
                        else
                        {
                            OnCreditWinComplete?.Invoke(false, response.msg);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("JSON Parse Error: " + e.Message);
                        OnCreditWinComplete?.Invoke(false, "Server error");
                    }
                }
                else
                {
                    Debug.LogError("API Error: " + request.error);
                    OnCreditWinComplete?.Invoke(false, "Network error");
                }
            }
        }
    }
}
