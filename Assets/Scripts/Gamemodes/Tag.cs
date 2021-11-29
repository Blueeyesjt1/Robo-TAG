using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.UI;

public class Tag : MonoBehaviourPunCallbacks {

    public List<GameObject> tagPlayers;     //List of taggers in the game
    public AudioClip slap;
    public AudioClip boop;

    /// <summary>
    /// Restarts the round across the server
    /// </summary>
    IEnumerator restartDelay() {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.IsLocal) {     //Ensure that the players have still yet to be untagged (Bug preventer)
            yield return new WaitForSeconds(5);     //Wait delay to prevent last-second tagging again
            gameObject.GetComponent<PhotonView>().RPC("Removetaggers", RpcTarget.All);     //Remove all the taggers in the game
            gameObject.GetComponent<PhotonView>().RPC("reSpawn", RpcTarget.All);
            yield return new WaitForSeconds(5);     //Wait delay to prevent last-second tagging again
            gameObject.GetComponent<PhotonView>().RPC("loadTagger", RpcTarget.All, gameObject.GetComponent<Server>().players[Random.Range(0, gameObject.GetComponent<Server>().players.Count)].GetComponent<PhotonView>().ViewID);     //Select a new random player as tagger
            
        }
    }

    /// <summary>
    /// Client respawns themselves in a new position in map
    /// </summary>
    [PunRPC]
    public void reSpawn() {

        if (!PhotonNetwork.LocalPlayer.IsLocal || !photonView.IsMine)
            return;

        var players = gameObject.GetComponent<Server>().players;

        for (int i = 0; i < players.Count; i++) {
            if (photonView.IsMine) {
                players[i].transform.position = gameObject.GetComponent<Server>().RandomSpawn();
                players[i].transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0);
                break;
            }
        }

       /* if (PhotonNetwork.IsMasterClient) {     //If host, reset all agents postitions and training episode
            var agents = gameObject.GetComponent<MLAgent>().agents;

            for (int j = 0; j < agents.Count; j++) {
                if (agents[j].GetComponent<MLBrain>() != null) {
                    agents[j].GetComponent<MLBrain>().EndEpisode();*//*
                    agents[j].transform.position = new Vector3(UnityEngine.Random.Range(20f, 25f), 1, UnityEngine.Random.Range(10f, 35f));
                    agents[j].transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0);*//*
                }
            }
        }*/

    }

    /// <summary>
    /// All players remove all taggers across their client-game
    /// </summary>
    [PunRPC]
    public void Removetaggers() {

        gameObject.GetComponent<Tag>().tagPlayers = new List<GameObject>();     //Remove old list of taggers
        for (int i = 0; i < gameObject.GetComponent<Server>().players.Count; i++)     //Turn all players in server to runners
            turnToHider(gameObject.GetComponent<Server>().players[i].GetComponent<PhotonView>().ViewID);
    }

    /// <summary>
    /// All clients are informed that a player has been tagged and is now a tagger
    /// <param name="viewID">Photon view ID of tagged player for identification over network</param>
    /// </summary>
    [PunRPC]
    public void newTagger(int viewID) {   
        GameObject newTaggedPlayer = PhotonView.Find(viewID).gameObject;
        Debug.DrawLine(newTaggedPlayer.transform.position, newTaggedPlayer.transform.position + new Vector3(0, 10, 0), Color.red, 1f);     //Used for visual debugging
        StartCoroutine(FreezeTagger(newTaggedPlayer));

        if(newTaggedPlayer.GetComponent<Player>().isHuman)
            gameObject.GetComponent<PhotonView>().RPC("makeAnnouncement", PhotonNetwork.CurrentRoom.Players[newTaggedPlayer.GetComponent<Player>().playerActorNum], newTaggedPlayer.name, "You\'re a <color=red>tagger!</color>", "slap");
    }

    /// <summary>
    /// All clients freeze specific player for a short period once they're tagged
    /// <param name="viewID">Photon view ID of tagged player for identification over network</param>
    /// </summary>
    /// 
    IEnumerator FreezeTagger(GameObject newTaggedPlayer) {
        newTaggedPlayer.GetComponent<Player>().isTagger = true;     //Unfreeze tagger
        newTaggedPlayer.GetComponent<MeshRenderer>().material.color = Color.black;     //Make new tagger red
        newTaggedPlayer.GetComponent<Player>().isFrozen = true;     //Freeze tagger
        yield return new WaitForSeconds(4);     //Delay for feezing
        newTaggedPlayer.GetComponent<MeshRenderer>().material.color = Color.red;     //Make new tagger red
        newTaggedPlayer.GetComponent<Player>().isFrozen = false;     //Unfreeze tagger
        newTaggedPlayer.GetComponent<SphereCollider>().enabled = true;
        print("Turned " + newTaggedPlayer.GetComponent<Player>().playerName + " to tagger");
        tagPlayers.Add(newTaggedPlayer);     //New tagger is set to global tagger list

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.IsLocal && tagPlayers.Count >= gameObject.GetComponent<Server>().players.Count && gameObject.GetComponent<Server>().players.Count > 1) {     //If there are no more hiders and there is more than 1 player, host restarts game
            print("Restarting game");
            StartCoroutine(restartDelay());
        }
    }

    /// <summary>
    /// Used to load a tagger(s) when a new player joins the server or to ensure that all tagger are correctly accounted for
    /// <param name="viewID">Photon view ID of a tagged player for identification over network</param>
    /// </summary>
    [PunRPC]
    public void loadTagger(int viewID) {

        GameObject taggedPlayer = PhotonView.Find(viewID).gameObject;     //Found tagger within scene

        taggedPlayer.GetComponent<MeshRenderer>().material.color = Color.red;     //Make new tagger red
        taggedPlayer.GetComponent<Player>().isTagger = true;     //Unfreeze tagger
        taggedPlayer.GetComponent<SphereCollider>().enabled = true;     //Make old tagger have their tagging collider disabled

        if (taggedPlayer.GetComponent<Player>().isHuman)
            gameObject.GetComponent<PhotonView>().RPC("makeAnnouncement", PhotonNetwork.CurrentRoom.Players[taggedPlayer.GetComponent<Player>().playerActorNum], taggedPlayer.name, "You\'re a <color=red>tagger!</color>", "slap");

        tagPlayers.Add(taggedPlayer);     //New tagger is set to global tagger list
    }

    /// <summary>
    /// Used to turn a tagger back into a hider (Mainly used for match restart)
    /// <param name="viewID">Photon view ID of player for identification over network</param>
    /// </summary>
    [PunRPC]
    public void turnToHider(int viewID) {

        GameObject taggedPlayer = PhotonView.Find(viewID).gameObject;     //Found tagger within scene

        taggedPlayer.GetComponent<SphereCollider>().enabled = false;     //Make old tagger have their tagging collider disabled
        taggedPlayer.GetComponent<Player>().isTagger = false;     //They are no longer tagger
        taggedPlayer.GetComponent<Player>().isFrozen = false;     //They are no longer frozen (if they were)
        taggedPlayer.GetComponent<MeshRenderer>().material.color = Color.white;     //Make white
        
        if (taggedPlayer.GetComponent<Player>().isHuman)
            gameObject.GetComponent<PhotonView>().RPC("makeAnnouncement", PhotonNetwork.CurrentRoom.Players[taggedPlayer.GetComponent<Player>().playerActorNum], taggedPlayer.name, "You\'re a <color=white>hider!</color>", "boop");
    }

    [PunRPC]
    public void makeAnnouncement(string player, string message, string sound) {

        GameObject playerG = GameObject.Find(player);

        if(sound == "slap")
            playerG.GetComponent<AudioSource>().clip = slap;
        else
            playerG.GetComponent<AudioSource>().clip = boop;

        playerG.GetComponent<AudioSource>().Play();

        playerG.transform.Find("camera/Canvas/Announcement").GetComponent<Text>().text = message;
        playerG.transform.Find("camera/Canvas/Announcement").GetComponent<Text>().enabled = true;

        print("Displaying announcement for " + playerG.name);

        StartCoroutine(turnOffAnnounce(playerG));
    }

    IEnumerator turnOffAnnounce(GameObject player) {
        yield return new WaitForSeconds(2);
        player.transform.Find("camera/Canvas/Announcement").GetComponent<Text>().enabled = false;

    }
}
