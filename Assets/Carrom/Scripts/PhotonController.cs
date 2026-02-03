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
            PhotonNetwork.KeepAliveInBackground = 60;
            if (!PhotonNetwork.IsConnectedAndReady) {
                PhotonNetwork.ConnectUsingSettings();
            } else {
                JoinLobby();
            }
        }

        public override void OnConnectedToMaster() {
            if (PlayerPrefs.HasKey("username")){
                PhotonNetwork.NickName = PlayerPrefs.GetString("username");
            }
            
            JoinLobby();
        }

        public void JoinLobby() {
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby(){
            
        }

        public override void OnLeftLobby(){
            
        }

        public void FindRoom() {
            MenuController.Instance.vsMsgText.text = "Searching room...";

            ExitGames.Client.Photon.Hashtable roomHastable = new ExitGames.Client.Photon.Hashtable {
                { "roomType", whichRoom }
            };

            PhotonNetwork.JoinRandomRoom(roomHastable, 0);
        }

        public void CreatePracticeRoom(){
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomType" };
            roomOptions.IsOpen = false;
            roomOptions.IsVisible = false;
            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnJoinRandomFailed(short returnCode, string message) {
            ExitGames.Client.Photon.Hashtable roomHastable = new ExitGames.Client.Photon.Hashtable {
                { "roomType", whichRoom }
            };

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomProperties = roomHastable;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomType" };
            roomOptions.IsOpen = true;
            roomOptions.IsVisible = true;
            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnJoinedRoom() {
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
            
        }

    }
}