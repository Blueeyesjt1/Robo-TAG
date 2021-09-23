using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;

public class Tag : MonoBehaviourPunCallbacks {

    public List<GameObject> tagPlayers;     //List of taggers in the game

    IEnumerator restartDelay() {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.IsLocal) {
            if (PhotonNetwork.IsMasterClient && tagPlayers.Count + 1 >= gameObject.GetComponent<Server>().players.Count) {     //Ensure that the players have still yet to be untagged (Bug preventer)
                gameObject.GetComponent<PhotonView>().RPC("Removetaggers", RpcTarget.All);
                yield return new WaitForSeconds(3);
                gameObject.GetComponent<PhotonView>().RPC("newTagger", RpcTarget.All, gameObject.GetComponent<Server>().players[Random.Range(0, gameObject.GetComponent<Server>().players.Count)].GetComponent<PhotonView>().ViewID);
            }

            print("Restarted game");
        }
    }

    /// <summary>
    /// Everyone removes all taggers from tag list
    /// </summary>
    [PunRPC]
    public void Removetaggers() {

        gameObject.GetComponent<Tag>().tagPlayers = new List<GameObject>();
        for (int i = 0; i < gameObject.GetComponent<Server>().players.Count; i++)     //Turn all players to hiders
            turnToHider(gameObject.GetComponent<Server>().players[i].GetComponent<PhotonView>().ViewID);

    }

    /// <summary>
    /// Everyone is informed that a player has been tagged and is now a tagger as well
    /// </summary>
    [PunRPC]
    public void newTagger(int viewID) {        
        if(tagPlayers.Count + 1 < gameObject.GetComponent<Server>().players.Count) {     //If there's not 1 hider left, tag them
            GameObject newtaggedPlayer = PhotonView.Find(viewID).gameObject;

            newtaggedPlayer.GetComponent<MeshRenderer>().material.color = Color.red;     //Make new tagger red
            tagPlayers.Add(newtaggedPlayer);     //New tagger is set to global tagger list
            StartCoroutine(FreezeTagger(viewID));

            Debug.DrawLine(newtaggedPlayer.transform.position, newtaggedPlayer.transform.position + new Vector3(0, 10, 0), Color.red, 1f);
        }     //If there are no more hiders, host restarts game
        else if(PhotonNetwork.IsMasterClient && PhotonNetwork.LocalPlayer.IsLocal) {
            print("Restarting game");
            StartCoroutine(restartDelay());
            return;
        }
    }

    /// <summary>
    /// Freezes the player for a short time once they're tagged
    /// view ID to simply inform newly joines users on who is the tagger
    /// <param name="viewID">Photon view ID of player for identification over network</param>
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
    }

    /// <summary>
    /// Used to load taggers when a new player joins the server or to ensure that all tagger are correctly accounted for
    /// <param name="viewID">Photon view ID of player for identification over network</param>
    /// </summary>
    [PunRPC]
    public void loadTagger(int viewID) {

        GameObject taggedPlayer = PhotonView.Find(viewID).gameObject;

        //New tagger
        tagPlayers.Add(taggedPlayer);     //New tagger is set to global tagger list
        taggedPlayer.GetComponent<MeshRenderer>().material.color = Color.red;     //Make new tagger red
        taggedPlayer.GetComponent<Player>().isTagger = true;     //Unfreeze tagger
        taggedPlayer.GetComponent<SphereCollider>().enabled = true;     //Make old tagger have their tagging collider disabled
    }

    /// <summary>
    /// Used to turn a tagger back into a hider (Mainly used for match restart)
    /// <param name="viewID">Photon view ID of player for identification over network</param>
    /// </summary>
    [PunRPC]
    public void turnToHider(int viewID) {

        GameObject taggedPlayer = PhotonView.Find(viewID).gameObject;

        taggedPlayer.GetComponent<SphereCollider>().enabled = false;     //Make old tagger have their tagging collider disabled
        taggedPlayer.GetComponent<Player>().isTagger = false;     //Unfreeze tagger
        taggedPlayer.GetComponent<MeshRenderer>().material.color = Color.white;     //Make new tagger red


        if (taggedPlayer.GetComponent<MLBrain>() != null && taggedPlayer.GetComponent<BehaviorParameters>().Model == null)     //If an ML agent is training, reset their environment once everyone has been tagged
            taggedPlayer.GetComponent<MLBrain>().EndEpisode();
    }
}
