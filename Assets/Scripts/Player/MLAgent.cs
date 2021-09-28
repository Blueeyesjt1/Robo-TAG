using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MLAgent : MonoBehaviourPunCallbacks {

    public List<GameObject> agents = new List<GameObject>();

    /// <summary>
    /// Host spawns each agent at the start of the game
    /// </summary>
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


    /// <summary>
    /// When a client joins, they ask the host for all player data
    /// <param name="actorNum">Actor number joining player</param>
    /// </summary>
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

    /// <summary>
    /// When a client joins, they ask the host for all bot data
    /// <param name="mlName">List of bot names</param>
    /// <param name="mlViewIDs">List of bot view ID's</param>
    /// </summary>
    [PunRPC]
    public void othersRequestSpawnAgent(string[] mlNames, int[] mlViewIDs) {

        for (int i = 0; i < mlNames.Length; i++) {
            GameObject agent = PhotonView.Find(mlViewIDs[i]).gameObject;
            Destroy(agent.transform.Find("camera").gameObject);
            agent.gameObject.GetComponent<Player>().playerName = mlNames[i];
            agent.gameObject.name = "Player:" + mlNames[i];
            agent.transform.Find("textUsername").GetComponent<TextMeshPro>().text = mlNames[i];
            gameObject.GetComponent<Server>().players.Add(agent);
        }

    }
}
