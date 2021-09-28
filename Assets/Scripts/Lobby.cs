using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine.UI;

public class Lobby : MonoBehaviourPunCallbacks, IInRoomCallbacks, IMatchmakingCallbacks {

    /// <summary>
    /// On main menu load, connect to photon network and initiate other protocols
    /// </summary>
    public void Start() {
        Application.targetFrameRate = 256;     //Set target framerate to prevent excessive input advantages
        OnConnectedToMaster();     //Connect to photon network
    }

    /// <summary>
    /// Photon's default network connection method with overrides
    /// </summary>
    public override void OnConnectedToMaster() {
        PhotonNetwork.ConnectUsingSettings();     //Ex: Connect to US region servers
        PhotonNetwork.AutomaticallySyncScene = true;     //Sync scenes when joining rooms
        PhotonNetwork.JoinLobby();     //Connect to list of rooms
    }

    /// <summary>
    /// When the connect button is pressed, join server
    /// </summary>
    public void ConnectPressed() {

        string userName = gameObject.transform.Find("Canvas/InputField (1)/Text").GetComponent<Text>().text;   //User's inputted username
        string serverName = gameObject.transform.Find("Canvas/InputField/Text").GetComponent<Text>().text;   //User's inputted server name

        PhotonNetwork.LocalPlayer.NickName = userName;     //Assign username to our photon client

        RoomOptions roomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = 25 };

        PhotonNetwork.JoinOrCreateRoom(serverName, roomOptions, null, null);
        PhotonNetwork.LoadLevel("tagArena");
    }

}
