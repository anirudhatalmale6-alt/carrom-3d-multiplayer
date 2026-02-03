using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using BEKStudio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BEKStudio{
    public class GameController : MonoBehaviour{
        public static GameController Instance;
        public enum GameState{ READY, WAIT, SWITCH_MASTER, WIN, LOSE };
        public GameState gameState;
        public enum WhichPlayer{ ME, OTHER };
        public WhichPlayer whichPlayer;
        public Rigidbody2D playerPuck;
        public PhotonView playerPhotonView;
        public List<GameObject> homePucksCollected;
        public List<GameObject> awayPucksCollected;
        public List<GameObject> pucksCollected;
        public Sprite[] avatars;
        [Header("Top")]
        public TextMeshProUGUI topTotalBetText;
        public Image topHomeAvatar;
        public Image topHomeAvatarTimer;
        public TextMeshProUGUI topHomeNameText;
        public TextMeshProUGUI topHomeScoreText;
        public Image topAwayAvatar;
        public Image topAwayAvatarTimer;
        public TextMeshProUGUI topAwayNameText;
        public TextMeshProUGUI topAwayScoreText;
        public Transform topCoinParentForAnim;
        [Header("Toast Message")]
        public GameObject toastMessage;
        public TextMeshProUGUI toastMessageText;
        [Header("Settings")]
        public GameObject settingsScreen;
        public Image settingsBackground;
        public RectTransform settingsContinue;
        public RectTransform settingsMenu;
        public Slider playerSlider;
        public Slider opponentSlider;
        [Header("Puck")]
        public Puck[] allPucks;
        public float playerPuckMinX;
        public float playerPuckMaxX;
        [Header("Others")]
        public bool isPucksFixed;
        public bool shootAgain;
        string targetTag;
        public string masterClientTag;
        public float timer;
        public bool reduceTimer;
        public GameObject redPuck;
        public GameObject statusScreen;
        public Image statusBackground;
        public RectTransform statusPanel;
        public TextMeshProUGUI statusPanelText;
        public bool redPuckCollected;
        public bool practiceMode;
        public bool redPuckWaiting;


        void Awake(){
            if (Instance == null){
                Instance = this;
            }
        }
        
        void Start(){
            if (!PhotonNetwork.IsMasterClient){
                whichPlayer = WhichPlayer.OTHER;
            }
            masterClientTag = "White";
            CheckTurn();
            
            homePucksCollected = new List<GameObject>();
            awayPucksCollected = new List<GameObject>();
            pucksCollected = new List<GameObject>();

            redPuck.SetActive(PhotonController.Instance.whichMode == "carrom");

            targetTag = (string)PhotonNetwork.LocalPlayer.CustomProperties["tag"];

            topTotalBetText.text = (PhotonController.Instance.roomEntryPice * 2).ToString("###,###,###");

            if (PhotonController.Instance.playWithBot){
                SetTopUsersForBot();
            } else {
                SetTopUsersForGame();
            }

            if (practiceMode){
                topTotalBetText.text = "0";
                topAwayNameText.transform.parent.gameObject.SetActive(false);
                topHomeScoreText.transform.parent.gameObject.SetActive(false);
            }

            isPucksFixed = true;

            StartMessage();
        }

        void SetTopUsersForBot(){
            topHomeNameText.text = PhotonNetwork.PlayerList[0].NickName;
            topHomeAvatar.sprite = avatars[(int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"]];

            topAwayNameText.text = PhotonController.Instance.botName;
            topAwayAvatar.sprite = avatars[PhotonController.Instance.botAvatar];
        }

        void SetTopUsersForGame(){
            if (PhotonNetwork.PlayerList.Length == 2){
                topHomeNameText.text = PhotonNetwork.PlayerList[0].NickName;
                topHomeAvatar.sprite = avatars[(int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"]];
                topAwayNameText.text = PhotonNetwork.PlayerList[1].NickName;
                topAwayAvatar.sprite = avatars[(int)PhotonNetwork.PlayerList[1].CustomProperties["avatar"]];
            } else{
                topHomeNameText.text = PhotonNetwork.PlayerList[0].NickName;
                topHomeAvatar.sprite = avatars[(int)PhotonNetwork.PlayerList[0].CustomProperties["avatar"]];
                topAwayNameText.text = "";
                topAwayAvatar.sprite = null;
            }
        }

        void StartMessage(){
            if (practiceMode){
                statusPanelText.text = "YOU START";
                toastMessageText.text = "Practice Mode";
            } else if (targetTag == "White"){
                statusPanelText.text = "YOU START";
                toastMessageText.text = "Pot all white pucks to win the match";
            } else if (targetTag == "Black"){
                statusPanelText.text = "OPPONENT START";
                toastMessageText.text = "Pot all black pucks to win the match";
            }

            statusPanel.anchoredPosition = new Vector2(-940f, statusPanel.anchoredPosition.y);
            statusBackground.color = new Color(0, 0, 0, 0);
            statusScreen.SetActive(true);

            LeanTween.alpha(statusBackground.GetComponent<RectTransform>(), 0.5f, 0.5f);
            LeanTween.move(statusPanel, new Vector2(0, statusPanel.anchoredPosition.y), 0.3f).setDelay(0.5f)
                .setEase(LeanTweenType.easeOutBack).setOnComplete(() => {
                    LeanTween.move(statusPanel, new Vector2(940, statusPanel.anchoredPosition.y), 0.3f).setDelay(1f)
                        .setEase(LeanTweenType.easeInBack).setOnComplete(
                            () => { statusScreen.SetActive(false); });
                });
        }

        void Update(){
            if (practiceMode) return;

            RunTimer();
        }

        void FixedUpdate(){
            if (gameState == GameState.READY || gameState == GameState.WAIT){
                isPucksFixed = GetPuckStatus();
            }
        }

        void RunTimer(){
            if (gameState != GameState.READY) return;

            if (reduceTimer){
                timer -= 1 * Time.deltaTime;
            }

            if (PhotonNetwork.InRoom){
                if (masterClientTag == "White"){
                    topHomeAvatarTimer.fillAmount = Mathf.InverseLerp(0, Constants.PLAY_TIME_FOR_PLAYER, timer);
                } else{
                    topAwayAvatarTimer.fillAmount = Mathf.InverseLerp(0, Constants.PLAY_TIME_FOR_PLAYER, timer);
                }
            }

            if (timer <= 0 && playerPhotonView.IsMine){
                gameState = GameState.WAIT;
                PlayerPuck.Instance.isTouch = false;
                PlayerPuck.Instance.arrow.SetActive(false);
                PlayerPuck.Instance.tutorial.SetActive(false);
                PlayerPuck.Instance.tutorialShowDelay = 2f;
                shootAgain = false;

                StartCoroutine(WaitForPucks());
            }
        }

        public void SettingsBtn(){
            if (gameState == GameState.WIN || gameState == GameState.LOSE) return;

            settingsContinue.anchoredPosition = new Vector2(-940f, settingsContinue.anchoredPosition.y);
            settingsMenu.anchoredPosition = new Vector2(-940f, settingsMenu.anchoredPosition.y);
            settingsBackground.color = new Color(0, 0, 0, 0);
            settingsScreen.SetActive(true);

            LeanTween.alpha(settingsBackground.GetComponent<RectTransform>(), 0.75f, 0.25f);
            LeanTween.move(settingsContinue, new Vector2(0, settingsContinue.anchoredPosition.y), 0.3f).setDelay(0.25f)
                .setEase(LeanTweenType.easeOutBack);
            LeanTween.move(settingsMenu, new Vector2(0, settingsMenu.anchoredPosition.y), 0.3f).setDelay(0.5f)
                .setEase(LeanTweenType.easeOutBack);
        }

        public void SettingsContinueBtn(){
            settingsScreen.SetActive(false);
        }

        public void SettingsMenuBtn(){
            settingsContinue.GetComponent<Button>().interactable = false;
            settingsMenu.GetComponent<Button>().interactable = false;

            PhotonNetwork.AutomaticallySyncScene = false;
            AdsManager.Instance.ShowInterstitialAd();
            if (PhotonNetwork.InRoom){
                PhotonNetwork.LeaveRoom();
            }
        }

        public void Shoot(){
            playerSlider.interactable = false;
            if (opponentSlider != null){
                opponentSlider.interactable = false;
            }

            shootAgain = false;
            gameState = GameState.WAIT;

            playerSlider.value = 0.5f;
            if (opponentSlider != null){
                opponentSlider.value = 0.5f;
            }

            StartCoroutine(WaitForPucks());
        }

        public void NavigatorSliderValueChanged(){
            if (!playerPhotonView.IsMine || gameState != GameState.READY) return;

            playerPuck.transform.localPosition = new Vector2(Mathf.Lerp(playerPuckMinX, playerPuckMaxX, playerSlider.value),playerPuck.transform.localPosition.y);
        }

        bool GetPuckStatus(){
            for (int i = 0; i < allPucks.Length; i++){
                if (allPucks[i].isMoving){
                    reduceTimer = false;
                    return false;
                }
            }

            if (PlayerPuck.Instance.isMoving){
                return false;
            }

            return true;
        }

        public void CheckTurn(){
            redPuckWaiting = false;
            reduceTimer = true;
            timer = Constants.PLAY_TIME_FOR_PLAYER;
            topHomeAvatarTimer.fillAmount = 0;
            topAwayAvatarTimer.fillAmount = 0;

            if (whichPlayer == WhichPlayer.ME){
                PlayerPuck.Instance.ResetPosition();
                
                playerSlider.interactable = true;
                if (opponentSlider != null){
                    opponentSlider.interactable = false;
                }
            } else{
                if (PhotonController.Instance.playWithBot){
                    PlayerPuck.Instance.ShootBot();
                }
                playerSlider.interactable = false;
                if (opponentSlider != null){
                    opponentSlider.interactable = true;
                }
            }

            gameState = GameState.READY;

            if (practiceMode){
                allPucks[0].ResetPosition();
            }

            for (int i = 0; i < allPucks.Length; i++){
                allPucks[i].GetComponent<CircleCollider2D>().enabled = true;
            }

            PlayerPuck.Instance.EnablePuckPhysic();
        }

        public void PuckOnHole(string puckTag, string puckName){
            if (practiceMode) return;
            if(gameState == GameState.WIN || gameState == GameState.LOSE) return;
            
            if (puckTag == "Player"){
                pucksCollected.Add(playerPuck.gameObject);
            } else {
                Puck getPuck = getPuckFromList(puckName);
                if (getPuck != null){
                    pucksCollected.Add(getPuck.gameObject);
                }
            }
        }

        Puck getPuckFromList(string puckName){
            for (int i = 0; i < allPucks.Length; i++){
                if (allPucks[i].name == puckName){
                    return allPucks[i];
                }
            }

            return null;
        }

        IEnumerator WaitForPucks(){
            yield return new WaitForSecondsRealtime(1f);
            yield return new WaitUntil(() => isPucksFixed);
            yield return new WaitForSecondsRealtime(1f);

            CheckGameStatus();
        }

        void CheckGameStatus(){
            if (pucksCollected.Contains(playerPuck.gameObject)){
                CheckPlayerPenalty();
            } else if (pucksCollected.Contains(redPuck)){
                CheckRedPuckPenalty();
            } else {
                CheckPucks();
            }
            
            UpdateScoreText();

            if (practiceMode){
                CheckTurn();
                return;
            }

            if (PhotonController.Instance.whichMode == "disc"){
                if (homePucksCollected.Count == 9){
                    gameState = targetTag == "White" ? GameState.WIN : GameState.LOSE;
                } else if (awayPucksCollected.Count == 9){
                    gameState = targetTag == "Black" ? GameState.WIN : GameState.LOSE;
                }
            } else if (PhotonController.Instance.whichMode == "carrom"){
                if (redPuckCollected){
                    if (homePucksCollected.Count == 9){
                        gameState = targetTag == "White" ? GameState.WIN : GameState.LOSE;
                    } else if (awayPucksCollected.Count == 9){
                        gameState = targetTag == "Black" ? GameState.WIN : GameState.LOSE;
                    }
                }
            }

            if (gameState == GameState.WIN || gameState == GameState.LOSE){
                GameOver();
                return;
            }

            reduceTimer = true;

            if (gameState == GameState.WAIT){
                if (!shootAgain){
                    gameState = GameState.SWITCH_MASTER;
                    if (!playerPhotonView.IsMine) return;
                    StartCoroutine(SwitchMasterDelay());
                } else{
                    if (!playerPhotonView.IsMine) return;

                    timer = Constants.PLAY_TIME_FOR_PLAYER;
                    gameState = GameState.READY;
                    if (whichPlayer == WhichPlayer.OTHER){
                        PlayerPuck.Instance.ShootBot();
                        playerSlider.interactable = false;
                        if (opponentSlider != null) {
                            opponentSlider.interactable = true;
                        }
                    } else {
                        PlayerPuck.Instance.ResetPosition();
                        playerSlider.interactable = true;
                        if (opponentSlider != null) {
                            opponentSlider.interactable = false;
                        }
                    }
                }
            } else{
                gameState = GameState.READY;
            }
        }

        IEnumerator SwitchMasterDelay(){
            yield return new WaitForSecondsRealtime(1.5f);
            if (PhotonController.Instance.playWithBot){
                if (whichPlayer == WhichPlayer.ME){
                    whichPlayer = WhichPlayer.OTHER;
                } else{
                    whichPlayer = WhichPlayer.ME;
                }
                
                if (masterClientTag == "White"){
                    masterClientTag = "Black";
                } else{
                    masterClientTag = "White";
                }
                
                CheckTurn();
            } else {
                PhotonNetwork.SetMasterClient(PhotonNetwork.MasterClient.GetNext());
            }
        }

        void CheckPlayerPenalty(){
            if (pucksCollected.Contains(redPuck) || redPuckWaiting){
                pucksCollected.Remove(redPuck);
                redPuck.GetComponent<Puck>().ResetPosition();
            }
            
            for (int i = 0; i < pucksCollected.Count; i++){
                GameObject targetPuck = pucksCollected[i];
                
                if(targetPuck.CompareTag("Player")) continue;
                
                if (targetPuck.CompareTag(masterClientTag)){
                    targetPuck.GetComponent<Puck>().ResetPosition();
                } else {
                    if (masterClientTag == "White"){
                        if (awayPucksCollected.Count < 2){
                            awayPucksCollected.Add(targetPuck);
                        }
                    } else {
                        if (homePucksCollected.Count < 2){
                            homePucksCollected.Add(targetPuck);
                        }
                    }
                    
                    targetPuck.GetComponent<Puck>().ResetPosition();
                }
            }
            
            pucksCollected.Clear();
            
            if (masterClientTag == "White"){
                if (homePucksCollected.Count > 0){
                    GameObject targetPuck = homePucksCollected[0];
                    homePucksCollected.Remove(targetPuck);
                    targetPuck.GetComponent<Puck>().ResetPosition();
                }
            } else {
                if (awayPucksCollected.Count > 0){
                    GameObject targetPuck = awayPucksCollected[0];
                    awayPucksCollected.Remove(targetPuck);
                    targetPuck.GetComponent<Puck>().ResetPosition();
                }
            }
        }

        void CheckRedPuckPenalty(){
            if (pucksCollected.Count == 1){
                redPuckWaiting = true;
                shootAgain = true;
                pucksCollected.Remove(redPuck);
                return;
            }
            
            for (int i = 0; i < pucksCollected.Count; i++){
                GameObject targetPuck = pucksCollected[i];
                
                if(targetPuck.CompareTag("Player")) continue;
                
                targetPuck.GetComponent<Puck>().ResetPosition();
            }
            
            pucksCollected.Clear();
        }

        void CheckPucks(){
            bool tempRedPuckWaiting = redPuckWaiting;
            
            if (pucksCollected.Count == 0){
                if (redPuckWaiting){
                    redPuckWaiting = false;
                    redPuck.GetComponent<Puck>().ResetPosition();
                }
                return;
            }
            
            for (int i = 0; i < pucksCollected.Count; i++){
                var targetPuck = pucksCollected[i];

                if (targetPuck.CompareTag(masterClientTag)){
                    shootAgain = true;
                }

                if (PhotonController.Instance.whichMode == "carrom"){
                    if (masterClientTag == "White"){
                        if (targetPuck.CompareTag("White")){
                            if (tempRedPuckWaiting){
                                redPuckCollected = true;
                                tempRedPuckWaiting = false;
                            }
                            
                            if (homePucksCollected.Count == 8 && !redPuckCollected){
                                targetPuck.GetComponent<Puck>().ResetPosition();
                            } else {
                                homePucksCollected.Add(targetPuck);
                            }
                        } else {
                            if (tempRedPuckWaiting){
                                tempRedPuckWaiting = false;
                                redPuck.GetComponent<Puck>().ResetPosition();
                                targetPuck.GetComponent<Puck>().ResetPosition();
                            } else {
                                if (awayPucksCollected.Count == 8){
                                    if (redPuckCollected){
                                        awayPucksCollected.Add(targetPuck);
                                    } else {
                                        targetPuck.GetComponent<Puck>().ResetPosition();
                                    }
                                } else{
                                    awayPucksCollected.Add(targetPuck);
                                }
                            }
                        }
                    } else if (masterClientTag == "Black"){
                        if (targetPuck.CompareTag("Black")){
                            if (tempRedPuckWaiting){
                                redPuckCollected = true;
                                tempRedPuckWaiting = false;
                            }
                            if (awayPucksCollected.Count == 8 && !redPuckCollected){
                                targetPuck.GetComponent<Puck>().ResetPosition();
                            } else {
                                awayPucksCollected.Add(targetPuck);
                            }
                        } else {
                            if (tempRedPuckWaiting){
                                tempRedPuckWaiting = false;
                                redPuck.GetComponent<Puck>().ResetPosition();
                                targetPuck.GetComponent<Puck>().ResetPosition();
                            } else {
                                if (homePucksCollected.Count == 8){
                                    if (redPuckCollected){
                                        homePucksCollected.Add(targetPuck);
                                    } else {
                                        targetPuck.GetComponent<Puck>().ResetPosition();
                                    }
                                } else{
                                    homePucksCollected.Add(targetPuck);
                                }
                            }
                        }
                    }
                } else {
                    if (targetPuck.CompareTag("White")){
                        homePucksCollected.Add(targetPuck);
                    } else if (targetPuck.CompareTag("Black")){
                        awayPucksCollected.Add(targetPuck);
                    }
                }
            }
            pucksCollected.Clear();
            redPuckWaiting = tempRedPuckWaiting;
        }

        void UpdateScoreText(){
            if (homePucksCollected.Contains(redPuck)){
                topHomeScoreText.text = (homePucksCollected.Count - 1).ToString();
            } else{
                topHomeScoreText.text = homePucksCollected.Count.ToString();
            }

            if (awayPucksCollected.Contains(redPuck)){
                topAwayScoreText.text = (awayPucksCollected.Count - 1).ToString();
            } else {
                topAwayScoreText.text = awayPucksCollected.Count.ToString();
            }
        }

        public void CheckRoomPlayers(){
            if (gameState == GameState.WIN || gameState == GameState.LOSE) return;

            if (PhotonNetwork.PlayerList.Length == 1){
                gameState = GameState.WIN;
                GameOver();
            }
        }

        void GameOver(){
            settingsScreen.SetActive(false);
            PhotonNetwork.AutomaticallySyncScene = false;

            if (gameState == GameState.WIN){
                // Call WordPress API to credit winnings
                WordPressAPI.Instance.CreditWinnings((success, message) => {
                    if (success){
                        Debug.Log("Winnings credited successfully!");
                    } else {
                        Debug.LogError("Failed to credit winnings: " + message);
                    }
                });
                statusPanelText.text = "YOU WIN\n+Rs" + WordPressAPI.WIN_PRIZE;
            } else if (gameState == GameState.LOSE){
                statusPanelText.text = "YOU LOSE";
            }

            statusPanel.anchoredPosition = new Vector2(-940f, statusPanel.anchoredPosition.y);
            statusBackground.color = new Color(0, 0, 0, 0);
            statusScreen.SetActive(true);

            LeanTween.alpha(statusBackground.GetComponent<RectTransform>(), 0.75f, 0.5f);
            LeanTween.move(statusPanel, new Vector2(0, statusPanel.anchoredPosition.y), 0.3f).setDelay(0.5f)
                .setEase(LeanTweenType.easeOutBack).setOnComplete(() => {
                    LeanTween.move(statusPanel, new Vector2(940, statusPanel.anchoredPosition.y), 0.3f).setDelay(1f)
                        .setEase(LeanTweenType.easeInBack).setOnComplete(
                            () => {
                                statusBackground.color = new Color(0, 0, 0, 0);
                                Vector2 targetPos = topCoinParentForAnim.position;
                                if (gameState == GameState.WIN){
                                    if (targetTag == "White"){
                                        targetPos = topHomeAvatarTimer.transform.position;
                                    } else{
                                        targetPos = topAwayAvatarTimer.transform.position;
                                    }
                                } else if (gameState == GameState.LOSE){
                                    if (targetTag == "White"){
                                        targetPos = topAwayAvatarTimer.transform.position;
                                    } else{
                                        targetPos = topHomeAvatarTimer.transform.position;
                                    }
                                }

                                topCoinParentForAnim.gameObject.SetActive(true);
                                for (int i = 0; i < topCoinParentForAnim.childCount; i++){
                                    GameObject go = topCoinParentForAnim.GetChild(i).gameObject;
                                    LeanTween.move(go, targetPos, 0.5f).setDelay(i * 0.1f).setOnComplete(() => {
                                        go.SetActive(false);
                                    });
                                }

                                LeanTween.value(PhotonController.Instance.roomEntryPice * 2, 0, 1.5f).setOnUpdate(
                                    (float val) => {
                                        if (val < 1){
                                            topTotalBetText.text = "0";
                                        } else{
                                            topTotalBetText.text = ((int)val).ToString("###,###,###");
                                        }
                                    }).setOnComplete(() => {
                                    AdsManager.Instance.ShowInterstitialAd();
                                    if (PhotonNetwork.InRoom){
                                        PhotonNetwork.LeaveRoom();
                                    }
                                });
                            });
                });
        }
    }
}