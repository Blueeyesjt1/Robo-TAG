using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.MLAgents;
using Photon.Realtime;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Demonstrations;

public class Server : MonoBehaviourPunCallbacks {

    public List<GameObject> players = new List<GameObject>();
    int aiCount = 0;

    /// <summary>
    /// Every client calls method when a new player joins
    /// </summary>
    public override void OnJoinedRoom() {

        base.OnJoinedRoom();      //Default to PUN

        GameObject player = PhotonNetwork.Instantiate("Player", new Vector3(UnityEngine.Random.Range(20f, 25f), 1, UnityEngine.Random.Range(10f, 35f)), Quaternion.identity);     //Grabs player prefab and spawns them in spawn area

        Destroy(player.GetComponent<DemonstrationRecorder>());     //Since not a bot, remove this component
        Destroy(player.GetComponent<DecisionRequester>());     //Since not a bot, remove this component
        Destroy(player.GetComponent<MLBrain>());     //Since not a bot, remove this component
        Destroy(player.GetComponent<BehaviorParameters>());     //Since not a bot, remove this component

        if (!player.GetComponent<PhotonView>().IsMine || !PhotonNetwork.LocalPlayer.IsLocal)     //If not our client, ignore everything proceeding this
            return;

        UnityEngine.Cursor.visible = false;     //Hide mouse
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;     //Lock mouse to game window

        player.transform.Find("camera").gameObject.SetActive(true);     //Enable camera for only client

        string playerName = "Noob" + Random.Range(0, 999);     //Assign username to player

        int playerActorNum = PhotonNetwork.LocalPlayer.ActorNumber;     //Grab local player actor number

        if (PhotonNetwork.LocalPlayer.NickName != "" && PhotonNetwork.LocalPlayer.NickName != null)     //If player inputted own nickname on main menu, use it
            playerName = PhotonNetwork.LocalPlayer.NickName;

        print("Our own client, " + playerName + ", has joined the server");
        
        bool playeralreadyExists = false;

        if (players.Count > 0) {
            for (int i = 0; i < players.Count; i++) {     //Confirm whether the player has already been added (Bug preventer)
                if (players[i].name == player.name) {
                    playeralreadyExists = true;
                    print("ERROR: " + player.name + " is already on the player list");
                }
            }
        }
        else
            playeralreadyExists = false;

        if (playeralreadyExists == false)
            players.Add(player);     //Add this player to this client's global player list

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.IsLocal) {     //If host is local player,

            print("Our own client, " + playerName + ", is the host so:");

            for (int i = 0; i < aiCount; i++)
                gameObject.GetComponent<PhotonView>().RPC("hostSpawnAgents", RpcTarget.MasterClient);     //Calls to send info about a player to all clients.
            print("     Spawn agents");

            gameObject.GetComponent<PhotonView>().RPC("newTagger", RpcTarget.All, gameObject.GetComponent<PhotonView>().ViewID);
            print("     Make host 1st tagger");
        }

        gameObject.GetComponent<PhotonView>().RPC("playerLoadUpSend", RpcTarget.AllBuffered, gameObject.GetPhotonView().ViewID, playerName, playerActorNum);     //Calls to send info about a player to all clients.

    }

    /// <summary>
    /// Joined client sends their information to all other clients in server
    /// <param name="viewID">Photon view ID of newly joined player for identification over network</param>
    /// <param name="pName">Name of joined-player</param>
    /// <param name="actorNum">Actor number joined-player</param>
    /// </summary>
    [PunRPC]
    public void playerLoadUpSend(int viewID, string pName, int actorNum) {

        GameObject joinedClient = PhotonView.Find(viewID).gameObject;     //Have this client find the newly-joined player in the scene

        bool playeralreadyExists = false;

        if (players.Count > 0) {
            for (int i = 0; i < players.Count; i++) {     //Confirm whether the player has already been added (Bug preventer)
                if (players[i].name == joinedClient.name)
                    playeralreadyExists = true;
            }
        }
        else
            playeralreadyExists = false;

        if (playeralreadyExists == false)
            players.Add(joinedClient);     //Add this player to this client's global player list

        joinedClient.GetComponent<Player>().playerName = pName;     //Assign name
        joinedClient.GetComponent<Player>().playerActorNum = actorNum;      //Ensure this object is being controlled by correct client on our side
        joinedClient.name = "Player:" + pName;     //Assign game object name to be the client's username
        joinedClient.transform.Find("textUsername").GetComponent<TextMeshPro>().text = pName;
        Destroy(joinedClient.GetComponent<DemonstrationRecorder>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<DecisionRequester>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<MLBrain>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<BehaviorParameters>());     //Since not a bot, remove this component

        //Everything past this is the host informing our newly joined client
        if (!PhotonNetwork.IsMasterClient)     //If host, inform our new client about the server's information
            return;

        gameObject.GetComponent<PhotonView>().RPC("othersRequestAgents", RpcTarget.MasterClient, actorNum);     //Calls to send info about a player to all clients.
        StartCoroutine(WaitOnTagger(actorNum));
    }

    /// <summary>
    /// After host has received a new player's info, the host informs the new player about the server's data
    /// <param name="actorNum">Actor number joining player</param>
    /// </summary>
    IEnumerator WaitOnTagger(int actorNum) {

        while(gameObject.GetComponent<Tag>().tagPlayers == null)     //Wait and ensure that the host knows who the latest tagger is- (Bug fix)
            yield return new WaitForEndOfFrame();

        for(int i = 0; i < GetComponent<Tag>().tagPlayers.Count; i++)     //For each tagger there is, let newly-joined client know
            gameObject.GetComponent<PhotonView>().RPC("loadTagger", PhotonNetwork.CurrentRoom.Players[actorNum], gameObject.GetComponent<Tag>().tagPlayers[i].GetComponent<PhotonView>().ViewID);     //Host tells new client who tagger is
    
    }

}
