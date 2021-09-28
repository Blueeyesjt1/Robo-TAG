using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;

public class Tag : MonoBehaviourPunCallbacks {

    public List<GameObject> tagPlayers;     //List of taggers in the game

    /// <summary>
    /// Restarts the round across the server
    /// </summary>
    IEnumerator restartDelay() {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.IsLocal) {
            if (PhotonNetwork.IsMasterClient && tagPlayers.Count + 1 >= gameObject.GetComponent<Server>().players.Count) {     //Ensure that the players have still yet to be untagged (Bug preventer)
                gameObject.GetComponent<PhotonView>().RPC("Removetaggers", RpcTarget.All);     //Remove all the taggers in the game
                yield return new WaitForSeconds(3);     //Wait delay to prevent last-second tagging again
                gameObject.GetComponent<PhotonView>().RPC("newTagger", RpcTarget.All, 
                    gameObject.GetComponent<Server>().players[Random.Range(0, gameObject.GetComponent<Server>().players.Count)].GetComponent<PhotonView>().ViewID);     //Select a new random player as tagger
            }
        }
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
        if(tagPlayers.Count + 1 < gameObject.GetComponent<Server>().players.Count) {     //If there's not 1 hider left, tag them

            GameObject newtaggedPlayer = PhotonView.Find(viewID).gameObject;
            newtaggedPlayer.GetComponent<MeshRenderer>().material.color = Color.red;     //Make new tagger red

            bool taggerAlreadyExists = false;

            if (tagPlayers.Count > 1) {
                for (int i = 0; i < tagPlayers.Count; i++) {     //Confirm whether the player has already been added (Bug preventer)
                    if (tagPlayers[i].name == newtaggedPlayer.name)
                        taggerAlreadyExists = true;
                }
            }
            else
                taggerAlreadyExists = false;

            if (taggerAlreadyExists == true) {     //Bug preventer
                newtaggedPlayer.GetComponent<MeshRenderer>().material.color = Color.red;     //Make new tagger red
                newtaggedPlayer.GetComponent<Player>().isTagger = true;     //Unfreeze tagger
                newtaggedPlayer.GetComponent<SphereCollider>().enabled = true;     //Make old tagger have their tagging collider disabled
                return;
            }

            StartCoroutine(FreezeTagger(viewID));

            Debug.DrawLine(newtaggedPlayer.transform.position, newtaggedPlayer.transform.position + new Vector3(0, 10, 0), Color.red, 1f);
        }     //If there are no more hiders, host restarts game
        else if(tagPlayers.Count > 1 && PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.IsLocal) {
            print("Restarting game");
            StartCoroutine(restartDelay());
            return;
        }
    }

    /// <summary>
    /// All clients freeze specific player for a short period once they're tagged
    /// <param name="viewID">Photon view ID of tagged player for identification over network</param>
    /// </summary>
    /// 
    IEnumerator FreezeTagger(int viewID) {
        GameObject newTaggedPlayer = PhotonView.Find(viewID).gameObject;

        newTaggedPlayer.GetComponent<Player>().isFrozen = true;     //Freeze tagger
        yield return new WaitForSeconds(4);     //Delay for feezing
        newTaggedPlayer.GetComponent<Player>().isFrozen = false;     //Unfreeze tagger
        newTaggedPlayer.GetComponent<Player>().isTagger = true;     //Unfreeze tagger
        newTaggedPlayer.GetComponent<SphereCollider>().enabled = true;
        print("Turned " + newTaggedPlayer.GetComponent<Player>().playerName + " red, to tagger");

        bool taggerAlreadyExists = false;
        if (tagPlayers.Count > 0) {
            for (int i = 0; i < tagPlayers.Count; i++) {     //Confirm whether the player has already been added (Bug preventer)
                if (tagPlayers[i].name == newTaggedPlayer.name)
                    taggerAlreadyExists = true;
            }
        }
        else
            taggerAlreadyExists = false;

        if(taggerAlreadyExists == false)
            tagPlayers.Add(newTaggedPlayer);     //New tagger is set to global tagger list
    }

    /// <summary>
    /// Used to load a tagger(s) when a new player joins the server or to ensure that all tagger are correctly accounted for
    /// <param name="viewID">Photon view ID of a tagged player for identification over network</param>
    /// </summary>
    [PunRPC]
    public void loadTagger(int viewID) {

        GameObject taggedPlayer = PhotonView.Find(viewID).gameObject;     //Found tagger within scene

        bool taggerAlreadyExists = false;

        if (tagPlayers.Count > 0) {
            for (int i = 0; i < tagPlayers.Count; i++) {     //Confirm whether the player has already been added (Bug preventer)
                if (tagPlayers[i].name == taggedPlayer.name)
                    taggerAlreadyExists = true;
            }
        }
        else
            taggerAlreadyExists = false;

        taggedPlayer.GetComponent<MeshRenderer>().material.color = Color.red;     //Make new tagger red
        taggedPlayer.GetComponent<Player>().isTagger = true;     //Unfreeze tagger
        taggedPlayer.GetComponent<SphereCollider>().enabled = true;     //Make old tagger have their tagging collider disabled

        if (taggerAlreadyExists == false)
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
        taggedPlayer.GetComponent<MeshRenderer>().material.color = Color.white;     //Make new tagger red

        if (taggedPlayer.GetComponent<MLBrain>() != null && taggedPlayer.GetComponent<BehaviorParameters>().Model == null)     //If an ML agent is training, reset their environment
            taggedPlayer.GetComponent<MLBrain>().EndEpisode();
    }
}
