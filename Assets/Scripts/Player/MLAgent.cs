using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MLAgent : MonoBehaviourPunCallbacks {

    public List<GameObject> agents = new List<GameObject>();
    
    //Host spawns agents for first time
    [PunRPC]
    public void hostSpawnAgents() {
        GameObject agent = PhotonNetwork.Instantiate("Player", new Vector3(UnityEngine.Random.Range(20f, 25f), 1, UnityEngine.Random.Range(10f, 35f)), Quaternion.identity);     //Grabs player prefab and spawns them in spawn area

        int agentViewID = agent.GetComponent<PhotonView>().ViewID;

        agent.transform.Find("camera").gameObject.SetActive(false);

        string playerName = "Robo:" + Random.Range(0, 999);     //Assign username to player

        agent.GetComponent<Player>().isHuman = false;
        agent.GetComponent<Player>().playerName = playerName;

        agent.gameObject.name = "Player:" + playerName;
        agent.transform.Find("textUsername").GetComponent<TextMeshPro>().text = playerName;

        agent.GetComponent<Player>().isHuman = false;

        agents.Add(agent);
        gameObject.GetComponent<Server>().players.Add(agent);
    }


    //host sends agent data to clients when they join
    [PunRPC]
    public void othersRequestAgents(int actorNum) {

        int[] agentsViewIDs = new int[agents.Count];
        string[] agentNames = new string[agents.Count];

        for (int i = 0; i < agents.Count; i++) {
            agentsViewIDs[i] = agents[i].GetComponent<Player>().photonView.ViewID;
            agentNames[i] = agents[i].GetComponent<Player>().playerName;
        }

        gameObject.GetComponent<PhotonView>().RPC("othersRequestSpawnAgent", PhotonNetwork.CurrentRoom.Players[actorNum], agentNames, agentsViewIDs);     //Calls to send info about a player to all clients.
    }

    //Client receives data that host is sending
    [PunRPC]
    public void othersRequestSpawnAgent(string[] mlName, int[] mlViewIDs) {

        for (int i = 0; i < mlName.Length; i++) {
            //GameObject agent = PhotonNetwork.Instantiate("Player", new Vector3(UnityEngine.Random.Range(20, 25), 1, UnityEngine.Random.Range(20, 25)), Quaternion.identity);
            GameObject agent = PhotonView.Find(mlViewIDs[i]).gameObject;
            Destroy(agent.transform.Find("camera").gameObject);
            //agent.gameObject.GetComponent<PhotonView>().ViewID = mlViewIDs[i];
            agent.gameObject.GetComponent<Player>().playerName = mlName[i];
            agent.gameObject.name = "Player:" + mlName[i];
            agent.transform.Find("textUsername").GetComponent<TextMeshPro>().text = mlName[i];
            gameObject.GetComponent<Server>().players.Add(agent);
        }

    }
}
