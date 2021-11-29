using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using UnityEngine.UI;

public class playerTrackerUI : MonoBehaviourPunCallbacks {

    /*void Update()
    {

        if (!PhotonNetwork.IsMasterClient)
            return;

        bool playerListChanged = false;

        List<GameObject> players = gameObject.GetComponent<Server>().players;

        for(int i = 0; i < players.Count; i++) {     //Grabbed all players
            if(!GameObject.Find(players[i].name)) {
                print("Player seems to not be in scene any longer!");
                players.RemoveAt(i);     //Removed missing players from list
                playerListChanged = true;
            }

        }

        //updatePlayerListUI(players.ToArray());

        if (playerListChanged == true) {
            List<string> playerNames = new List<string>();

            for (int i = 0; i < players.Count; i++) {     //Made player name list
                playerNames.Add(players[i].name);
            }

            if (playerNames.Count > 0) {
                print(playerNames[0]);
                gameObject.GetComponent<PhotonView>().RPC("updatePlayers", RpcTarget.All, playerNames.ToArray());     //Calls to send info about a player to all clients.
            }
        }
    }*/

    /// <summary>
    /// Client requests host to gather list of player in server
    /// <param name="actorId">Actor ID of who called for host to perform list grab</param>
    /// </summary>
    [PunRPC]
    public void globalUpdatePlayerUI(int actorId) {
        if (!PhotonNetwork.IsMasterClient)
            return;

        bool playerListChanged = false;

        //Get all players
        List<GameObject> players = gameObject.GetComponent<Server>().players;
        for (int i = 0; i < players.Count; i++) {     //Grabbed all players
            if (!GameObject.Find(players[i].name)) {
                print("Player seems to not be in scene any longer!");
                players.RemoveAt(i);     //Removed missing players from list
                playerListChanged = true;
            }
        }

        print("Player count check: " + players.Count);

        //Convert player gameobjects to list of names
        List<string> playerNames = new List<string>();
        for (int i = 0; i < players.Count; i++) {     //Made player name list
            playerNames.Add(players[i].GetComponent<Player>().playerName);
        }

        string[] pNamesArr = playerNames.ToArray();

        /*if(pNamesArr.Length > 0)
            gameObject.GetComponent<PhotonView>().RPC("updatePlayerListUI", PhotonNetwork.CurrentRoom.Players[actorId], pNamesArr);  */   //Calls to send info about a player to all clients.
    }


    [PunRPC]
    public void updatePlayerListUI(string[] playerNamesArr) {

        for (int i = 0; i < playerNamesArr.Length; i++) {     //Made player name list

            GameObject playerUI = GameObject.Find("Player:" + playerNamesArr[i]).transform.Find("camera/Canvas/Players").gameObject;
            playerUI.GetComponent<Text>().text = "Players: \n";
            for (int j = 0; j < playerNamesArr.Length; j++) {     //Made player name list
                playerUI.GetComponent<Text>().text += "   " + playerNamesArr[j] + "\n";
            }
        }

    }

}
