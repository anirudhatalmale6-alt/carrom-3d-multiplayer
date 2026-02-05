using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

namespace BEKStudio{
    public class PhotonController : MonoBehaviourPunCallbacks {
        public static PhotonController Instance;
        public string whichMode;
        public string whichRoom;
        public int roomEntryPice = 0;
        public bool playWithBot;
        public int botAvatar;
        public string botName;


        void Awake() {
            if (Instance == null) {
                Instance = this;
            }
        }

        public void Start() {
            Debug.Log("PhotonController Start - Connecting to Photon...");
            PhotonNetwork.KeepAliveInBackground = 60;
            if (!PhotonNetwork.IsConnectedAndReady) {
                PhotonNetwork.ConnectUsingSettings();
            } else {
                JoinLobby();
            }
        }

        public override void OnConnectedToMaster() {
            Debug.Log("OnConnectedToMaster - Region: " + PhotonNetwork.CloudRegion);
            if (PlayerPrefs.HasKey("username")){
                PhotonNetwork.NickName = PlayerPrefs.GetString("username");
            }

            JoinLobby();
        }

        public void JoinLobby() {
            Debug.Log("JoinLobby called");
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby(){
            Debug.Log("OnJoinedLobby - Ready to find rooms!");
        }

        public override void OnLeftLobby(){
            
        }

        private int connectionRetryCount = 0;
        private const int MAX_RETRIES = 3;

        public void FindRoom() {
            Debug.Log("FindRoom called - IsConnected: " + PhotonNetwork.IsConnected + ", IsConnectedAndReady: " + PhotonNetwork.IsConnectedAndReady);

            if (!PhotonNetwork.IsConnectedAndReady){
                if (connectionRetryCount >= MAX_RETRIES){
                    MenuController.Instance.vsMsgText.text = "Connection failed. Please refresh.";
                    connectionRetryCount = 0;
                    return;
                }
                connectionRetryCount++;
                MenuController.Instance.vsMsgText.text = "Connecting...";
                StartCoroutine(WaitForConnectionAndFindRoom());
                return;
            }

            connectionRetryCount = 0;
            MenuController.Instance.vsMsgText.text = "Searching room...";

            ExitGames.Client.Photon.Hashtable roomHastable = new ExitGames.Client.Photon.Hashtable {
                { "roomType", whichRoom }
            };

            PhotonNetwork.JoinRandomRoom(roomHastable, 0);
        }

        IEnumerator WaitForConnectionAndFindRoom(){
            float timeout = 8f;
            float elapsed = 0f;

            while (!PhotonNetwork.IsConnectedAndReady && elapsed < timeout){
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                MenuController.Instance.vsMsgText.text = "Connecting... " + Mathf.CeilToInt(timeout - elapsed) + "s";
            }

            if (PhotonNetwork.IsConnectedAndReady){
                // Success - search for room
                MenuController.Instance.vsMsgText.text = "Searching room...";
                ExitGames.Client.Photon.Hashtable roomHastable = new ExitGames.Client.Photon.Hashtable {
                    { "roomType", whichRoom }
                };
                PhotonNetwork.JoinRandomRoom(roomHastable, 0);
            } else {
                // Failed - try reconnecting
                MenuController.Instance.vsMsgText.text = "Retrying connection...";
                if (!PhotonNetwork.IsConnected){
                    PhotonNetwork.ConnectUsingSettings();
                }
                yield return new WaitForSeconds(3f);
                FindRoom();
            }
        }

        public void CreatePracticeRoom(){
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomType" };
            roomOptions.IsOpen = false;
            roomOptions.IsVisible = false;
            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnJoinRandomFailed(short returnCode, string message) {
            Debug.Log("OnJoinRandomFailed - Code: " + returnCode + ", Message: " + message);
            MenuController.Instance.vsMsgText.text = "Creating room...";

            ExitGames.Client.Photon.Hashtable roomHastable = new ExitGames.Client.Photon.Hashtable {
                { "roomType", whichRoom }
            };

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomProperties = roomHastable;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomType" };
            roomOptions.IsOpen = true;
            roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = 2;
            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnCreateRoomFailed(short returnCode, string message){
            Debug.LogError("OnCreateRoomFailed - Code: " + returnCode + ", Message: " + message);
            MenuController.Instance.vsMsgText.text = "Room error: " + message;
            // Don't retry automatically - could cause infinite loop
        }

        public override void OnJoinedRoom() {
            Debug.Log("OnJoinedRoom - Mode: " + whichMode + ", Room: " + whichRoom + ", PlayerCount: " + PhotonNetwork.PlayerList.Length);

            if (whichMode == "practice"){
                SceneManager.LoadScene("Practice");
            } else{
                ExitGames.Client.Photon.Hashtable userHastable = new ExitGames.Client.Photon.Hashtable();
                userHastable.Add("avatar", PlayerPrefs.GetInt("avatar"));

                if (PhotonNetwork.PlayerList.Length == 1){
                    userHastable.Add("tag", "White");
                } else {
                    userHastable.Add("tag", "Black");
                }

                PhotonNetwork.LocalPlayer.SetCustomProperties(userHastable);
                Debug.Log("Properties set, calling VsJoinedRoom");
                MenuController.Instance.VsJoinedRoom();
                PhotonNetwork.AutomaticallySyncScene = true;
            }
        }

        public override void OnLeftRoom() {
            PhotonNetwork.AutomaticallySyncScene = false;

            if (SceneManager.GetActiveScene().name == "Menu") {
                MenuController.Instance.VsOnLeftRoom();
            } else if (SceneManager.GetActiveScene().name == "Game" || SceneManager.GetActiveScene().name == "Practice"){
                SceneManager.LoadScene("Menu");
            }
        }

        public override void OnCreatedRoom() {
            
        }

        public override void OnPlayerEnteredRoom(Player newPlayer) {
            if (PhotonNetwork.IsMasterClient) {
                MenuController.Instance.VsOnPlayerJoinedRoom();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer) {
            if (SceneManager.GetActiveScene().name == "Game") {
                GameController.Instance.CheckRoomPlayers();
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient){
            GameController.Instance.masterClientTag = (string)newMasterClient.CustomProperties["tag"];
            if (GameController.Instance.whichPlayer == GameController.WhichPlayer.ME){
                GameController.Instance.whichPlayer = GameController.WhichPlayer.OTHER;
            } else{
                GameController.Instance.whichPlayer = GameController.WhichPlayer.ME;
            }
            GameController.Instance.CheckTurn();
        }

        public override void OnDisconnected(DisconnectCause cause) {
            Debug.LogError("Disconnected from Photon - Cause: " + cause);
            // Don't auto-reconnect here - let the user retry manually
        }

    }
}