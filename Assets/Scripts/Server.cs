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
    int aiCount = 10;

    public override void OnJoinedRoom() {

        base.OnJoinedRoom();      //Default to PUN

        GameObject player = PhotonNetwork.Instantiate("Player", new Vector3(UnityEngine.Random.Range(20f, 25f), 1, UnityEngine.Random.Range(10f, 35f)), Quaternion.identity);     //Grabs player prefab and spawns them in spawn area

        Destroy(player.GetComponent<DemonstrationRecorder>());     //Since not a bot, remove this component
        Destroy(player.GetComponent<DecisionRequester>());     //Since not a bot, remove this component
        Destroy(player.GetComponent<MLBrain>());     //Since not a bot, remove this component
        Destroy(player.GetComponent<BehaviorParameters>());     //Since not a bot, remove this component

        if (!player.GetComponent<PhotonView>().IsMine)     //If not our client,
            return;

        UnityEngine.Cursor.visible = false;     //Hide mouse
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;     //Lock mouse

        player.transform.Find("camera").gameObject.SetActive(true);     //Enable camera for only client

        string playerName = "Noob" + Random.Range(0, 999);     //Assign username to player

        int playerActorNum = PhotonNetwork.LocalPlayer.ActorNumber;


        if (PhotonNetwork.LocalPlayer.NickName != "" && PhotonNetwork.LocalPlayer.NickName != null)
            playerName = PhotonNetwork.LocalPlayer.NickName;

        gameObject.GetComponent<PhotonView>().RPC("playerLoadUpSend", RpcTarget.AllBuffered, player.gameObject.GetPhotonView().ViewID, playerName, playerActorNum);     //Calls to send info about a player to all clients.

        players.Add(player);

        if (PhotonNetwork.IsMasterClient) {     //If host,
            for (int i = 0; i < aiCount; i++)
                gameObject.GetComponent<PhotonView>().RPC("hostSpawnAgents", RpcTarget.MasterClient);     //Calls to send info about a player to all clients.

            if (PhotonNetwork.LocalPlayer.IsLocal) {
                print("Host is assigning tagger...");
                gameObject.GetComponent<PhotonView>().RPC("newTagger", RpcTarget.All, players[Random.Range(0, players.Count)].GetComponent<PhotonView>().ViewID);
            }
        }
        else
            gameObject.GetComponent<PhotonView>().RPC("othersRequestAgents", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);     //Calls to send info about a player to all clients.

        if (PhotonNetwork.IsMasterClient) {
            }
    }

    /// <summary>
    /// Joined client send their information to all other clients in server
    /// <param name="viewID">Photon view ID of player for identification over network</param>
    /// <param name="pName">Name of joining player</param>
    /// <param name="actorNum">Actor number joining player</param>
    /// </summary>
    [PunRPC]
    public void playerLoadUpSend(int viewID, string pName, int actorNum) {

        GameObject joinedClient = PhotonView.Find(viewID).gameObject;

        joinedClient.GetComponent<Player>().playerName = pName;
        joinedClient.GetComponent<Player>().playerActorNum = actorNum;
        joinedClient.name = "Player:" + pName;
        joinedClient.transform.Find("textUsername").GetComponent<TextMeshPro>().text = pName;
        Destroy(joinedClient.GetComponent<DemonstrationRecorder>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<DecisionRequester>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<MLBrain>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<BehaviorParameters>());     //Since not a bot, remove this component

        if (PhotonNetwork.IsMasterClient)     //If host, inform our new client on who the taggers are in the game
            StartCoroutine(WaitOnTagger(actorNum));

    }

    /// <summary>
    /// After host has received a new player's info, the host informs the new player on who the taggers are
    /// <param name="viewID">Photon view ID of player for identification over network</param>
    /// <param name="pName">Name of joining player</param>
    /// <param name="actorNum">Actor number joining player</param>
    /// </summary>
    IEnumerator WaitOnTagger(int actorNum) {

        while(gameObject.GetComponent<Tag>().tagPlayers == null)     //Ensure that the host knows who the latest tagger is- (A bit of a bug fix)
            yield return new WaitForEndOfFrame();

        for(int i = 0; i < GetComponent<Tag>().tagPlayers.Count; i++) {
            gameObject.GetComponent<PhotonView>().RPC("loadTagger", PhotonNetwork.CurrentRoom.Players[actorNum], gameObject.GetComponent<Tag>().tagPlayers[i].GetComponent<PhotonView>().ViewID);     //Host sends server info to newly joined client
        }
    }
}
