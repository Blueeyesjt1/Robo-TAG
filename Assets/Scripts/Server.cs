using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//using Unity.MLAgents;
using Photon.Realtime;
//using Unity.MLAgents.Policies;
//using Unity.MLAgents.Demonstrations;

public class Server : MonoBehaviourPunCallbacks {

    public List<GameObject> players = new List<GameObject>();
    int aiCount = 0;

    public string NameGen() {

        string userName = "";

        while (userName == "") {

            string[] userColor = new string[] {
            "Red", "Blue", "Green", "Yellow", "Black", "White", "Green", "Purple","Orange", "Cyan" };

            string[] userTag = new string[] {
            "xXx", "-_-", "--", "**", "!", "?", "._.", "x_x", "##", "$", "^^", "+=+", "_-_", "@", "/\\", "o_o", "UwU", "<3" };

            string[] userFood = new string[] {
            "Mango", "Potato", "Watermelon", "Tomato", "Carrot", "Apple", "Corn", "Brocolli", "Banana", "Berry", "Grape", "Cereal", "Beef", "Milk", "Taco" };

            string[] userAnimal = new string[] {
            "Tiger", "Lion", "Panda", "Bear", "Penguin", "Monster", "Gorilla", "Snake", "Shark", "Turtle", "Seal", "Goat", "Dog", "Cat" };

            string[] userExtra = new string[] {
            "Sussy", "Sus", "Backa", "Joker", "Skull", "Frtnite", "AmongUs", "Halo", "CoD", "<3", "LOL", "JK", "Memez", "xD", "Xbox", "Anime", "IH8", "I<3", "xP" };

            string[] userPunchLine = new string[] {
            "Killer", "Fighter", "Jumper", "Runner", "Tagger", "Sleeper", "Talker", "Smacker", "Wrestler", "Picker" };

            for (int i = 0; i < 7; i++) {

                int lineRand = Random.Range(0, 7);

                switch (lineRand) {

                    case 0:
                        if (Random.Range(0, 3) == 0)
                            userName += userColor[Random.Range(0, userColor.Length)];
                        break;

                    case 1:
                        if (Random.Range(0, 3) == 0)
                            userName += userFood[Random.Range(0, userFood.Length)];
                        break;

                    case 2:
                        if (Random.Range(0, 3) == 0)
                            userName += userAnimal[Random.Range(0, userAnimal.Length)];
                        break;

                    case 3:
                        if (Random.Range(0, 3) == 0)
                            userName += userExtra[Random.Range(0, userExtra.Length)];
                        break;

                    case 4:
                        if (Random.Range(0, 3) == 0)
                            userName += userPunchLine[Random.Range(0, userPunchLine.Length)];
                        break;

                    case 5:
                        if (Random.Range(0, 2) == 0 && userName != "") {
                            string chosenTag = userTag[Random.Range(0, userTag.Length)];
                            userName = chosenTag + userName + chosenTag;
                        }
                        break;

                    case 6:
                        if (Random.Range(0, 2) == 0 && userName != "")
                            userName += Random.Range(0, 999);
                        break;
                }

            }

            if (userName != "")
                    break;
        }

        return userName;
    }

    public Vector3 RandomSpawn() {
        int randArea = Random.Range(0, 5);
        Vector3 spawnArea = new Vector3(0, 0, 0);

        switch (randArea) {
            case 0:
                spawnArea = new Vector3(UnityEngine.Random.Range(20f, 25f), 1, UnityEngine.Random.Range(10f, 35f));
                break;
            case 1:
                spawnArea = new Vector3(UnityEngine.Random.Range(43f, 56f), 1, UnityEngine.Random.Range(-6f, 6f));
                break;
            case 2:
                spawnArea = new Vector3(UnityEngine.Random.Range(-6f, 6f), 1, UnityEngine.Random.Range(-11f, 1f));
                break;
            case 3:
                spawnArea = new Vector3(UnityEngine.Random.Range(-11f, 1f), 1, UnityEngine.Random.Range(38f, 51f));
                break;
            case 4:
                spawnArea = new Vector3(UnityEngine.Random.Range(38f, 52f), 1, UnityEngine.Random.Range(43f, 56f));
                break;
        }

        return spawnArea;
    }

    /// <summary>
    /// Every client calls method when a new player joins
    /// </summary>
    public override void OnJoinedRoom() {

        print("Player joined the server");

        base.OnJoinedRoom();      //Default to PUN


        GameObject player = PhotonNetwork.Instantiate("Player", RandomSpawn(), Quaternion.identity);     //Grabs player prefab and spawns them in spawn area


        if (!player.GetComponent<PhotonView>().IsMine || !PhotonNetwork.LocalPlayer.IsLocal)     //If not our client, ignore everything proceeding this
            return;

        UnityEngine.Cursor.visible = false;     //Hide mouse
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;     //Lock mouse to game window

        player.transform.Find("camera").gameObject.SetActive(true);     //Enable camera for only client

        int playerActorNum = PhotonNetwork.LocalPlayer.ActorNumber;     //Grab local player actor number

        string playerName = NameGen();     //Assign username to player
        if (PhotonNetwork.LocalPlayer.NickName != "" && PhotonNetwork.LocalPlayer.NickName != null)     //If player inputted own nickname on main menu, use it
            playerName = PhotonNetwork.LocalPlayer.NickName;

        gameObject.GetComponent<PhotonView>().RPC("playerLoadUpSend", RpcTarget.AllBuffered, player.GetPhotonView().ViewID, playerName, playerActorNum);     //Calls to send info about a player to all clients.s

        if (PhotonNetwork.IsMasterClient) {     //If host is local player,

            print("Our own client, " + playerName + ", is the host so:");

            for (int i = 0; i < aiCount; i++)
                gameObject.GetComponent<PhotonView>().RPC("hostSpawnAgents", RpcTarget.MasterClient);     //Calls to send info about a player to all clients.
            print("     Spawn agents");

            gameObject.GetComponent<PhotonView>().RPC("newTagger", RpcTarget.All, player.GetComponent<PhotonView>().ViewID);
            print("     Make host 1st tagger");
        }
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

        players.Add(joinedClient);     //Add this player to this client's global player list

        gameObject.GetComponent<PhotonView>().RPC("globalUpdatePlayerUI", RpcTarget.MasterClient, actorNum);     //Calls to host to tell rest of users UI about new player

        print("Added player to player list");

        joinedClient.GetComponent<Player>().playerName = pName;     //Assign name
        joinedClient.GetComponent<Player>().playerActorNum = actorNum;      //Ensure this object is being controlled by correct client on our side
        joinedClient.name = "Player:" + pName;     //Assign game object name to be the client's username
        joinedClient.transform.Find("textUsername").GetComponent<TextMeshPro>().text = pName;
        /*Destroy(joinedClient.GetComponent<DemonstrationRecorder>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<DecisionRequester>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<MLBrain>());     //Since not a bot, remove this component
        Destroy(joinedClient.GetComponent<BehaviorParameters>());     //Since not a bot, remove this component*/

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
