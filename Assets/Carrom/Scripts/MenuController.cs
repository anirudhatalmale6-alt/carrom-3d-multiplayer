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

            ResetBotSettings();

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

            mainDiscPoolRect.anchoredPosition = new Vector2(-759f, -198f);
            mainCarromRect.anchoredPosition = new Vector2(759f, -198f);
            mainPracticeRect.anchoredPosition = new Vector2(0, -1103);

            LeanTween.move(mainDiscPoolRect, new Vector2(-216f, -198f), 0.5f).setEaseOutBack();
            LeanTween.move(mainCarromRect, new Vector2(227, -198f), 0.5f).setEaseOutBack();
            LeanTween.move(mainPracticeRect, new Vector2(0, -744), 0.5f).setEaseOutBack();
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
            if(!PhotonNetwork.IsConnectedAndReady) return;
            
            if (PlayerPrefs.GetInt("coin") == 0){
                ShopBtn();
                return;
            }

            PhotonController.Instance.whichMode = "disc";
            ShowRooms();
        }

        public void CarromBtn(){
            if(!PhotonNetwork.IsConnectedAndReady) return;
            
            if (PlayerPrefs.GetInt("coin") == 0){
                ShopBtn();
                return;
            }

            PhotonController.Instance.whichMode = "carrom";
            ShowRooms();
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
            roomLondonEntryPriceText.text = Constants.ROOM_LONDON_ENTRY_PRICE.ToString("###,###,###");
            roomLondonWinPriceText.text = (Constants.ROOM_LONDON_ENTRY_PRICE * 2).ToString("###,###,###");
            roomParisEntryPriceText.text = Constants.ROOM_PARIS_ENTRY_PRICE.ToString("###,###,###");
            roomParisWinPriceText.text = (Constants.ROOM_PARIS_ENTRY_PRICE * 2).ToString("###,###,###");
            roomBerlinEntryPriceText.text = Constants.ROOM_BERLIN_ENTRY_PRICE.ToString("###,###,###");
            roomBerlinWinPriceText.text = (Constants.ROOM_BERLIN_ENTRY_PRICE * 2).ToString("###,###,###");

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
            vsHomeUserBetText.text = PhotonController.Instance.roomEntryPice.ToString("###,###,###");
            vsAwayUserBetText.text = PhotonController.Instance.roomEntryPice.ToString("###,###,###");
            vsTotalBetText.text = "0";

            vsBackBtn.gameObject.SetActive(true);
            vsBackBtn.interactable = true;
            vsMsgText.text = "Waiting opponent...";
            vsUsersParent.SetActive(true);

            if (PhotonNetwork.PlayerList.Length == 1){
                vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;
                vsUsersParent.transform.GetChild(0).GetComponent<Image>().sprite = avatars[(int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"]];

                if (botCountdown == null){
                    botCountdown = BotCountdown();
                } else {
                    StopCoroutine(botCountdown);
                    botCountdown = BotCountdown();
                }

                StartCoroutine(botCountdown);
            } else if (PhotonNetwork.PlayerList.Length == 2){
                vsHomeUsernameText.text = PhotonNetwork.PlayerList[0].NickName;
                vsAwayUsernameText.text = PhotonNetwork.PlayerList[1].NickName;
                
                vsUsersParent.transform.GetChild(0).GetComponent<Image>().sprite = avatars[(int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"]];
                vsUsersParent.transform.GetChild(1).GetComponent<Image>().sprite = avatars[(int)PhotonNetwork.PlayerList[1].CustomProperties["avatar"]];

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
            // Wait 15 seconds for real player, showing countdown
            float waitTime = 15f;
            float elapsed = 0f;

            while (elapsed < waitTime){
                int remaining = Mathf.CeilToInt(waitTime - elapsed);
                vsMsgText.text = "Finding player... " + remaining + "s";
                yield return new WaitForSeconds(1f);
                elapsed += 1f;

                // Check if real player joined
                if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2){
                    yield break; // Exit - real player found
                }
            }

            // No player found - start with bot
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonController.Instance.botAvatar = Random.Range(0, avatars.Length);

            // Generate realistic bot name
            string firstName = botFirstNames[Random.Range(0, botFirstNames.Length)];
            string botName = firstName + "_" + Random.Range(10, 99);
            PhotonController.Instance.botName = botName;

            PhotonController.Instance.playWithBot = true;
            vsMsgText.text = "Opponent found!";
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
            vsMsgText.text = "Processing payment...";

            // Call WordPress API to deduct entry fee
            WordPressAPI.Instance.DeductEntryFee((success, message, newBalance) => {
                if (success){
                    // Fee deducted successfully - start the match
                    StartMatchAnimation();
                } else {
                    // Failed - show error and go back
                    vsMsgText.text = message;
                    StartCoroutine(ShowErrorAndGoBack(message));
                }
            });
        }

        IEnumerator ShowErrorAndGoBack(string message){
            yield return new WaitForSeconds(2f);
            if (PhotonNetwork.InRoom){
                PhotonNetwork.LeaveRoom();
            }
        }

        void StartMatchAnimation(){
            float currentCoin = WordPressAPI.ENTRY_FEE;
            float newCoin = 0;

            vsBackBtn.gameObject.SetActive(false);

            LeanTween.value(WordPressAPI.ENTRY_FEE, 0, 1f).setOnUpdate((float val) => {
                vsHomeUserBetText.text = "" + (int)val;
                vsAwayUserBetText.text = "" + (int)val;
            });

            LeanTween.value(0, WordPressAPI.ENTRY_FEE * 2, 1f).setOnUpdate((float val) => {
                vsTotalBetText.text = "" + (int)val;
            }).setOnComplete(() => {
                if (PhotonNetwork.IsMasterClient){
                    SceneManager.LoadScene("Game");
                }
            });
        }

    }
}