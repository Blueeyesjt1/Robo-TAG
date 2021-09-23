using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine.UI;

public class Lobby : MonoBehaviourPunCallbacks, IInRoomCallbacks, IMatchmakingCallbacks {


    public void Start() {
        Application.targetFrameRate = 256;
        OnConnectedToMaster();
    }

    public override void OnConnectedToMaster() {
        base.OnConnectedToMaster();     //Ready for matchmaking
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;     //Sync scene
        print("Connected to server");
        PhotonNetwork.JoinLobby();     //Connect to room list
    }

    public void ConnectPressed() {

        string userName = gameObject.transform.Find("Canvas/InputField (1)/Text").GetComponent<Text>().text;   //Server name
        string serverName = gameObject.transform.Find("Canvas/InputField/Text").GetComponent<Text>().text;   //Server name

        PhotonNetwork.LocalPlayer.NickName = userName;

        RoomOptions roomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = 25 };

        PhotonNetwork.JoinOrCreateRoom(serverName, roomOptions, null, null);
        PhotonNetwork.LoadLevel("tagArena");
    }


}
