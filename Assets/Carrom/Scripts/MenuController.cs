using System.Collections;
using System.Collections.Generic;
using BEKStudio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace BEKStudio{
    public class MenuController : MonoBehaviour{
        public static MenuController Instance;
        public GameObject dontDestroyPrefab;
        public TextMeshProUGUI coinText;

        // Debug text to show user_id on screen
        private TextMeshProUGUI debugText;
        public Image avatarImg;
        public TextMeshProUGUI avatarUsernameText;
        public Sprite[] avatars;
        IEnumerator botCountdown;
        [Header("Main")] public GameObject mainScreen;
        public RectTransform mainDiscPoolRect;
        public RectTransform mainCarromRect;
        public RectTransform mainPracticeRect;
        [Header("Room")] public GameObject roomScreen;
        public TextMeshProUGUI roomLondonEntryPriceText;
        public TextMeshProUGUI roomLondonWinPriceText;
        public TextMeshProUGUI roomParisEntryPriceText;
        public TextMeshProUGUI roomParisWinPriceText;
        public TextMeshProUGUI roomBerlinEntryPriceText;
        public TextMeshProUGUI roomBerlinWinPriceText;
        [Header("Vs")] public GameObject vsScreen;
        public TextMeshProUGUI vsMsgText;
        public Button vsBackBtn;
        public GameObject vsUsersParent;
        public TextMeshProUGUI vsHomeUsernameText;
        public TextMeshProUGUI vsHomeUserBetText;
        public TextMeshProUGUI vsAwayUsernameText;
        public TextMeshProUGUI vsAwayUserBetText;
        public TextMeshProUGUI vsTotalBetText;
        [Header("Username")] public GameObject usernameScreen;
        public TMP_InputField usernameText;
        [Header("Shop")] public GameObject shopScreen;


        void Awake(){
            if (Instance == null){
                Instance = this;
            }
        }
        
        void Start(){
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Create debug text at top of screen
            CreateDebugText();

            if (!PlayerPrefs.HasKey("default")){
                PlayerPrefs.SetInt("default", 1);
                PlayerPrefs.SetInt("avatar", Random.Range(0, 20));
                PlayerPrefs.SetInt("coin", Constants.DEFAULT_COIN);
                PlayerPrefs.Save();
            }

            avatarImg.sprite = avatars[PlayerPrefs.GetInt("avatar")];

            GameObject dontDestroyObj = GameObject.Find("DontDestroy");
            if (dontDestroyObj == null){
                dontDestroyObj = Instantiate(dontDestroyPrefab);
                dontDestroyObj.name = "DontDestroy";
                DontDestroyOnLoad(dontDestroyObj);
            }

            // Create JSBridge for JavaScript communication
            if (GameObject.Find("JSBridge") == null){
                GameObject jsBridgeObj = new GameObject("JSBridge");
                jsBridgeObj.AddComponent<JSBridge>();
            }

            // Create WordPressAPI if it doesn't exist (CRITICAL - this was missing!)
            if (WordPressAPI.Instance == null){
                GameObject wpApiObj = new GameObject("WordPressAPI");
                wpApiObj.AddComponent<WordPressAPI>();
                DontDestroyOnLoad(wpApiObj);
                Debug.Log("WordPressAPI created!");
            }

            ResetBotSettings();

            // Load coins from WordPress DB (background sync)
            // Wait a moment for WordPressAPI to initialize
            StartCoroutine(DelayedLoadCoins());

            UpdateCurrencyText();

            mainScreen.SetActive(false);
            roomScreen.SetActive(false);
            vsScreen.SetActive(false);
            shopScreen.SetActive(false);
            usernameScreen.SetActive(false);

            if (PlayerPrefs.HasKey("username")){
                avatarUsernameText.text = PlayerPrefs.GetString("username");
                OpenMainScreen();
            }  else {
                usernameScreen.SetActive(true);
            }

            Hashtable playerProperties = new Hashtable{
                { "avatar", PlayerPrefs.GetInt("avatar") }
            };
            PhotonNetwork.SetPlayerCustomProperties(playerProperties);
        }

        IEnumerator DelayedLoadCoins(){
            // Wait for WordPressAPI to initialize and parse URL
            yield return new WaitForSeconds(0.5f);
            LoadCoinsFromServer();
        }

        void LoadCoinsFromServer(){
            // Load coins from WordPress DB in background
            if (WordPressAPI.Instance == null){
                Debug.LogError("WordPressAPI.Instance is still null!");
                return;
            }
            WordPressAPI.Instance.LoadCoinBalance((success, message, coins) => {
                if (success){
                    // Sync server coins to local
                    PlayerPrefs.SetInt("coin", coins);
                    PlayerPrefs.Save();
                    UpdateCurrencyText();
                    Debug.Log("Coins loaded from server: " + coins);
                } else {
                    Debug.Log("Using local coins (server sync failed): " + message);
                }
            });
        }

        void CreateDebugText(){
            // Create a canvas for debug text if needed
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null){
                // Create debug text object
                GameObject debugObj = new GameObject("DebugText");
                debugObj.transform.SetParent(canvas.transform, false);

                debugText = debugObj.AddComponent<TextMeshProUGUI>();
                debugText.text = "Debug: Loading...";
                debugText.fontSize = 24;
                debugText.color = Color.yellow;
                debugText.alignment = TextAlignmentOptions.Center;

                // Position at top center
                RectTransform rect = debugObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -50);
                rect.sizeDelta = new Vector2(600, 60);

                Debug.Log("Debug text created");
            }
        }

        void Update(){
            // Update debug text with current status
            if (debugText != null){
                debugText.text = WordPressAPI.DebugMessage;
            }
        }

        void ResetBotSettings(){
            PhotonController.Instance.playWithBot = false;
            PhotonController.Instance.botAvatar = 0;
            PhotonController.Instance.botName = "";
        }

        public void UsernameSaveBtn(){
            if (usernameText.text.Length < 3) return;

            PlayerPrefs.SetString("username", usernameText.text);
            PlayerPrefs.Save();

            if (PhotonNetwork.IsConnectedAndReady){
                PhotonNetwork.NickName = usernameText.text;
            }

            avatarUsernameText.text = PlayerPrefs.GetString("username");

            OpenMainScreen();
        }

        void OpenMainScreen(){
            mainScreen.SetActive(true);
            roomScreen.SetActive(false);
            vsScreen.SetActive(false);
            shopScreen.SetActive(false);
            usernameScreen.SetActive(false);

            // HIDE Disc Pool - Single mode Carrom only
            mainDiscPoolRect.gameObject.SetActive(false);

            // Hide free coins/watch video section
            HideFreeCoinsSection();

            // Center Carrom button (since Disc Pool is hidden)
            mainCarromRect.anchoredPosition = new Vector2(759f, -198f);
            mainPracticeRect.anchoredPosition = new Vector2(0, -1103);

            // Animate only Carrom (centered) and Practice
            LeanTween.move(mainCarromRect, new Vector2(0, -198f), 0.5f).setEaseOutBack();  // Center position
            LeanTween.move(mainPracticeRect, new Vector2(0, -744), 0.5f).setEaseOutBack();
        }

        void HideFreeCoinsSection(){
            // Find and hide the free coins / watch video banner
            // Try multiple approaches to find it

            // Method 1: Search by common names
            string[] namesToFind = new string[] {
                "FreeCoins", "WatchVideo", "RewardedAd", "FreeCoin", "AdBanner",
                "CoinBanner", "VideoBanner", "RewardBanner", "Ad", "Reward",
                "FreeCoinsBtn", "WatchVideoBtn", "RewardedAdBtn", "BannerAd"
            };

            foreach (string objName in namesToFind){
                GameObject obj = GameObject.Find(objName);
                if (obj != null){
                    obj.SetActive(false);
                    Debug.Log("Hidden: " + objName);
                }
            }

            // Method 2: Search ALL objects in scene for keywords
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
            foreach (GameObject obj in allObjects){
                string name = obj.name.ToLower();
                if (name.Contains("freecoin") || name.Contains("watchvideo") ||
                    name.Contains("rewardedad") || name.Contains("adreward") ||
                    name.Contains("250") || name.Contains("bonus")){
                    obj.SetActive(false);
                    Debug.Log("Hidden by keyword: " + obj.name);
                }
            }

            // Method 3: Find any TextMeshPro with "250" or "WATCH VIDEO" text
            TMPro.TextMeshProUGUI[] allTexts = FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
            foreach (var txt in allTexts){
                if (txt.text.Contains("250") || txt.text.ToUpper().Contains("WATCH VIDEO") ||
                    txt.text.ToUpper().Contains("FREE COIN")){
                    // Hide the parent of this text (the button/panel)
                    if (txt.transform.parent != null){
                        txt.transform.parent.gameObject.SetActive(false);
                        Debug.Log("Hidden text parent: " + txt.transform.parent.name);
                    }
                }
            }

            // Method 4: Hide children of mainScreen that might be the banner
            if (mainScreen != null){
                for (int i = 0; i < mainScreen.transform.childCount; i++){
                    Transform child = mainScreen.transform.GetChild(i);
                    string childName = child.name.ToLower();
                    // The banner is likely a panel/image with coin-related content
                    if (childName.Contains("coin") || childName.Contains("video") ||
                        childName.Contains("reward") || childName.Contains("free") ||
                        childName.Contains("ad") || childName.Contains("banner") ||
                        childName.Contains("bonus")){
                        child.gameObject.SetActive(false);
                        Debug.Log("Hidden mainScreen child: " + child.name);
                    }
                }
            }

            // Method 5: Find by Button component onClick that calls FreeCoinsBtn
            Button[] allButtons = FindObjectsOfType<Button>(true);
            foreach (Button btn in allButtons){
                // Check if this button's parent/grandparent contains relevant text
                TMPro.TextMeshProUGUI[] childTexts = btn.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var txt in childTexts){
                    if (txt.text.Contains("250") || txt.text.ToUpper().Contains("WATCH") ||
                        txt.text.ToUpper().Contains("COIN") || txt.text.ToUpper().Contains("FREE")){
                        // This is likely the free coins button - hide its parent panel
                        Transform parentPanel = btn.transform.parent;
                        if (parentPanel != null){
                            parentPanel.gameObject.SetActive(false);
                            Debug.Log("Hidden button parent: " + parentPanel.name);
                        }
                        break;
                    }
                }
            }
        }

        public void UpdateCurrencyText(){
            if (PlayerPrefs.GetInt("coin") == 0){
                coinText.text = "0";
            }
            else{
                coinText.text = PlayerPrefs.GetInt("coin", 0).ToString("###,###,###");
            }
        }

        public void FreeCoinsBtn(){
            AdsManager.Instance.ShowRewardedAd();
        }

        public void DiscPoolBtn(){
            // Disc Pool disabled - redirect to Carrom
            CarromBtn();
        }

        public void CarromBtn(){
            if(!PhotonNetwork.IsConnectedAndReady) return;

            int entryFee = WordPressAPI.ENTRY_FEE;  // Rs 10
            if (PlayerPrefs.GetInt("coin") < entryFee){
                // Not enough coins - show error message
                vsMsgText.text = "Need ₹" + entryFee + " to play!";
                return;
            }

            // Single mode: Carrom only, skip room selection, go directly to matchmaking
            PhotonController.Instance.whichMode = "carrom";
            PhotonController.Instance.whichRoom = "london";  // Default room
            PhotonController.Instance.roomEntryPice = entryFee;

            // Go directly to VS/Matchmaking screen
            mainScreen.SetActive(false);
            roomScreen.SetActive(false);
            vsBackBtn.gameObject.SetActive(false);
            vsBackBtn.interactable = true;
            vsUsersParent.SetActive(false);
            vsScreen.SetActive(true);

            PhotonController.Instance.FindRoom();
        }

        public void PracticeModeBtn(){
            if(!PhotonNetwork.IsConnectedAndReady) return;
            
            PhotonController.Instance.whichMode = "practice";
            PhotonController.Instance.CreatePracticeRoom();
        }

        public void ShopBtn(){
            if (shopScreen.activeInHierarchy) return;

            mainScreen.SetActive(false);
            shopScreen.SetActive(true);
        }

        public void ShopItemBtn(int id){
            Purchaser.Instance.BuyConsumable(id);
        }

        public void ShopBackBtn(){
            mainScreen.SetActive(true);
            shopScreen.SetActive(false);
        }

        public void ShowRooms(){
            // Entry fee is Rs 10, Win prize is Rs 18
            roomLondonEntryPriceText.text = WordPressAPI.ENTRY_FEE.ToString("###,###,###");
            roomLondonWinPriceText.text = WordPressAPI.WIN_PRIZE.ToString("###,###,###");
            roomParisEntryPriceText.text = WordPressAPI.ENTRY_FEE.ToString("###,###,###");
            roomParisWinPriceText.text = WordPressAPI.WIN_PRIZE.ToString("###,###,###");
            roomBerlinEntryPriceText.text = WordPressAPI.ENTRY_FEE.ToString("###,###,###");
            roomBerlinWinPriceText.text = WordPressAPI.WIN_PRIZE.ToString("###,###,###");

            mainScreen.SetActive(false);
            roomScreen.SetActive(true);
        }

        public void RoomsBtn(string room){
            roomScreen.SetActive(false);

            vsBackBtn.gameObject.SetActive(false);
            vsBackBtn.interactable = true;
            vsUsersParent.SetActive(false);
            vsScreen.SetActive(true);

            PhotonController.Instance.whichRoom = room;
            if (room == "london"){
                PhotonController.Instance.roomEntryPice = Constants.ROOM_LONDON_ENTRY_PRICE;
            }
            else if (room == "paris"){
                PhotonController.Instance.roomEntryPice = Constants.ROOM_PARIS_ENTRY_PRICE;
            }
            else if (room == "berlin"){
                PhotonController.Instance.roomEntryPice = Constants.ROOM_BERLIN_ENTRY_PRICE;
            }

            PhotonController.Instance.FindRoom();

        }

        public void RoomsBackBtn(){
            mainScreen.SetActive(true);
            roomScreen.SetActive(false);
        }

        public void VsBackBtn(){
            vsBackBtn.interactable = false;
            vsMsgText.text = "Leaveing room...";
            if (botCountdown != null){
                StopCoroutine(botCountdown);
            }
            ResetBotSettings();
            
            PhotonNetwork.LeaveRoom();
        }

        public void VsOnLeftRoom(){
            vsBackBtn.gameObject.SetActive(false);
            vsBackBtn.interactable = true;
            vsUsersParent.SetActive(false);
            vsScreen.SetActive(false);
            mainScreen.SetActive(true);
        }

        public void VsJoinedRoom(){
            Debug.Log("VsJoinedRoom called - PlayerCount: " + PhotonNetwork.PlayerList.Length);

            // Show Rs 10 entry fee for each player
            vsHomeUserBetText.text = WordPressAPI.ENTRY_FEE.ToString("###,###,###");
            vsAwayUserBetText.text = WordPressAPI.ENTRY_FEE.ToString("###,###,###");
            vsTotalBetText.text = "0";

            vsBackBtn.gameObject.SetActive(true);
            vsBackBtn.interactable = true;
            vsMsgText.text = "Waiting opponent...";
            vsUsersParent.SetActive(true);

            if (PhotonNetwork.PlayerList.Length == 1){
                vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;

                // Safe avatar access with fallback
                int homeAvatar = 0;
                if (PhotonNetwork.PlayerList[0].CustomProperties.ContainsKey("avatar")){
                    homeAvatar = (int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"];
                }
                vsUsersParent.transform.GetChild(0).GetComponent<Image>().sprite = avatars[homeAvatar];

                // Start bot countdown
                if (botCountdown != null){
                    StopCoroutine(botCountdown);
                }
                botCountdown = BotCountdown();
                StartCoroutine(botCountdown);
                Debug.Log("Bot countdown started");
            } else if (PhotonNetwork.PlayerList.Length == 2){
                vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;
                vsAwayUsernameText.text = PhotonNetwork.PlayerList[1].NickName;

                // Safe avatar access with fallback
                int homeAvatar = 0;
                int awayAvatar = 0;
                if (PhotonNetwork.PlayerList[0].CustomProperties.ContainsKey("avatar")){
                    homeAvatar = (int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"];
                }
                if (PhotonNetwork.PlayerList[1].CustomProperties.ContainsKey("avatar")){
                    awayAvatar = (int)PhotonNetwork.PlayerList[1].CustomProperties["avatar"];
                }
                vsUsersParent.transform.GetChild(0).GetComponent<Image>().sprite = avatars[homeAvatar];
                vsUsersParent.transform.GetChild(1).GetComponent<Image>().sprite = avatars[awayAvatar];

                VsStartMatch();
            }
        }

        // Realistic Indian names for bot opponents
        private string[] botFirstNames = new string[] {
            "Rahul", "Amit", "Priya", "Neha", "Vikram", "Ankit", "Pooja", "Rohit",
            "Sneha", "Ajay", "Kavita", "Deepak", "Ravi", "Simran", "Mohit", "Anjali",
            "Arun", "Divya", "Sanjay", "Meera", "Karan", "Nisha", "Gaurav", "Shreya",
            "Vishal", "Komal", "Rakesh", "Swati", "Nikhil", "Preeti", "Arjun", "Sakshi",
            "Manish", "Aarti", "Suresh", "Megha", "Rajesh", "Tanvi", "Vivek", "Richa"
        };

        IEnumerator BotCountdown(){
            Debug.Log("BotCountdown started");

            // Wait 15 seconds for real player, showing countdown
            float waitTime = 15f;
            float elapsed = 0f;

            while (elapsed < waitTime){
                int remaining = Mathf.CeilToInt(waitTime - elapsed);
                vsMsgText.text = "Finding player... " + remaining + "s";
                Debug.Log("Bot countdown: " + remaining + "s remaining");
                yield return new WaitForSeconds(1f);
                elapsed += 1f;

                // Check if real player joined
                if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2){
                    Debug.Log("Real player found - stopping bot countdown");
                    yield break; // Exit - real player found
                }
            }

            Debug.Log("Bot countdown complete - starting with bot");

            // No player found - start with bot
            if (PhotonNetwork.CurrentRoom != null){
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }
            PhotonController.Instance.botAvatar = Random.Range(0, avatars.Length);

            // Generate realistic bot name
            string firstName = botFirstNames[Random.Range(0, botFirstNames.Length)];
            string botName = firstName + "_" + Random.Range(10, 99);
            PhotonController.Instance.botName = botName;

            PhotonController.Instance.playWithBot = true;
            vsMsgText.text = "Opponent found!";
            Debug.Log("Bot name: " + botName);
            VsOnPlayerJoinedRoom();
        }

        public void VsOnPlayerJoinedRoom(){
            if (PhotonController.Instance.playWithBot){
                vsAwayUsernameText.text = PhotonController.Instance.botName;
                vsUsersParent.transform.GetChild(1).GetComponent<Image>().sprite = avatars[PhotonController.Instance.botAvatar];
                VsStartMatch();
            } else {
                if (PhotonNetwork.CurrentRoom.PlayerCount == 2){
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                    PhotonNetwork.CurrentRoom.IsVisible = false;
                
                    vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;
                    vsAwayUsernameText.text = PhotonNetwork.PlayerList[1].NickName;
                    VsStartMatch();
                }
            }
        }

        void VsStartMatch(){
            if (PhotonNetwork.PlayerList.Length == 2){
                if (botCountdown != null){
                    StopCoroutine(botCountdown);
                }
            }

            vsBackBtn.gameObject.SetActive(false);

            // Use Rs 10 entry fee from WordPressAPI
            int entryFee = WordPressAPI.ENTRY_FEE;  // Rs 10
            int currentCoins = PlayerPrefs.GetInt("coin", 0);

            if (currentCoins >= entryFee){
                // Deduct Rs 10 entry fee locally
                PlayerPrefs.SetInt("coin", currentCoins - entryFee);
                PlayerPrefs.Save();
                UpdateCurrencyText();

                // Sync deduction to WordPress in background
                if (WordPressAPI.Instance != null){
                    WordPressAPI.Instance.SaveCoinBalance(currentCoins - entryFee, (success, message) => {
                        Debug.Log("Entry fee sync result: " + success + " - " + message);
                    });
                }

                // Start match immediately
                StartMatchAnimation();
            } else {
                // Not enough coins - show error
                vsMsgText.text = "Not enough coins!";
                StartCoroutine(ShowErrorAndGoBack("Not enough coins!"));
            }
        }

        IEnumerator ShowErrorAndGoBack(string message){
            yield return new WaitForSeconds(2f);
            if (PhotonNetwork.InRoom){
                PhotonNetwork.LeaveRoom();
            }
        }

        void StartMatchAnimation(){
            int entryFee = WordPressAPI.ENTRY_FEE;  // Rs 10
            int winPrize = WordPressAPI.WIN_PRIZE;  // Rs 18

            vsBackBtn.gameObject.SetActive(false);
            vsMsgText.text = "Match starting...";

            LeanTween.value(entryFee, 0, 1f).setOnUpdate((float val) => {
                vsHomeUserBetText.text = "" + (int)val;
                vsAwayUserBetText.text = "" + (int)val;
            });

            LeanTween.value(0, winPrize, 1f).setOnUpdate((float val) => {
                vsTotalBetText.text = "" + (int)val;
            }).setOnComplete(() => {
                if (PhotonNetwork.IsMasterClient){
                    SceneManager.LoadScene("Game");
                }
            });
        }

    }
}