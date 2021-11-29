using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MLBrain : Agent
{
    public int forwBack = 0;     //0 = not moving, 1 = backward, 2 = forward
    public int leftRight = 0;     //0 = not moving, 1 = left, 2 = right
    public int rotY = 0;     //0 = not moving, 1 = rotate left, 2 = rotate right
    public int waitOverride = 0;     //0 = not moving, 1 = allowed to move

    bool isTagger = false;

    public bool userOverrideTraining = false;     //Used to train the agent through user input

    Vector3 lastKnownTaggerPos = new Vector3(0, 0, 0);     //Last known position the tagger was seen at
    Vector3 lastKnownHiderPos = new Vector3(0, 0, 0);     //Last known position a hider was seen at

    string ourName;
    Vector3 ourPosition;

    GameObject seenObject;

    int hitCounter = 0;

    /// <summary>
    /// Resets the training environment for bots
    /// </summary>
    public override void OnEpisodeBegin() {
        base.OnEpisodeBegin();

        hitCounter = 0;

        gameObject.transform.position = GameObject.Find("serverLight").gameObject.GetComponent<Server>().RandomSpawn();

        gameObject.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0);
    }

    /// <summary>
    /// Updating method for bot training decisions
    /// </summary>
    private void Update() {

        ourName = gameObject.name;

        for(int i = 0; i < gameObject.GetComponent<Player>().eyeHits.Length; i++) {     //Agent vision
            if (gameObject.GetComponent<Player>().eyeHits[i].collider != null)
                seenObject = gameObject.GetComponent<Player>().eyeHits[i].collider.gameObject;
        }

        ourPosition = gameObject.transform.position;

        if (!isTagger) {     //If we're not the tagger,

            if (seenObject != null && seenObject.name.Contains("Player")) {     //If agent sees a person,

                //This enforces observation of the agents
                if (seenObject.GetComponent<Player>().isTagger) {     //Spot a tagger, get rewarded
                    lastKnownTaggerPos = seenObject.transform.position;
                    AddReward(.1f);
                }
                else if (!seenObject.GetComponent<Player>().isTagger) {     //Spot a hider, get rewarded less
                    lastKnownHiderPos = seenObject.transform.position;
                    AddReward(.025f);
                }
            }

            if (lastKnownTaggerPos != new Vector3(0, 0, 0)) {     //If we have a last know position of tagger, get away from them and be rewarded
                AddReward(Mathf.Abs(gameObject.transform.position.x - lastKnownTaggerPos.x) * .1f);     //Reward distance from tagger
                AddReward(Mathf.Abs(gameObject.transform.position.z - lastKnownTaggerPos.z) * .1f);     //Reward distance from tagger
            }

            AddReward(.1f);     //Rewarded for being hider
        }
        else {     //If tagger,
            
            //This enforces observation of the agents
            if (seenObject.GetComponent<Player>().isTagger) {     //Spot a tagger, get rewarded less
                lastKnownTaggerPos = seenObject.transform.position;
                AddReward(.025f);
            }
            else if (!seenObject.GetComponent<Player>().isTagger) {     //Spot a hider, get rewarded
                lastKnownHiderPos = seenObject.transform.position;
                AddReward(.1f);
            }

            if (lastKnownHiderPos != new Vector3(0, 0, 0)) {     //If we have a last know position of a player, go towards and be rewarded (Hurt less)
                AddReward(-1 * Mathf.Abs(gameObject.transform.position.x - lastKnownTaggerPos.x) * .1f);
                AddReward(-1 * Mathf.Abs(gameObject.transform.position.z - lastKnownTaggerPos.z) * .1f);
            }
            else
                AddReward(-.1f);

            if ((lastKnownHiderPos.x < gameObject.transform.position.z + .5f || lastKnownHiderPos.x > gameObject.transform.position.z - .5f) &&
                (lastKnownHiderPos.z < gameObject.transform.position.z + .5f || lastKnownHiderPos.z > gameObject.transform.position.z - .5f)) {     //If we go to last known hider position, get rid of that position and get rewarded for trying
                lastKnownHiderPos = new Vector3(0, 0, 0);
                AddReward(.5f);
            }            

            AddReward(-.05f);     //Lose points for being tagger
        }

        for (int i = 0; i < gameObject.GetComponent<Player>().MLDistHits.Length; i++) {     //Observer agent's movement depth

            Vector3 hitPoint = gameObject.GetComponent<Player>().MLDistHits[i].point;

            if (Mathf.Abs(hitPoint.x - hitPoint.x) < 1 || Mathf.Abs(hitPoint.z - hitPoint.z) < 2f)     //If player is too close to a wall, 
                AddReward(-.025f * ((Mathf.Abs(hitPoint.x - hitPoint.x) + Mathf.Abs(hitPoint.z - hitPoint.z)) / 2 ) );     //Reduce points by average distance away from walls
        }

        if (gameObject.GetComponent<Player>().hitWallBack == 0) {     //Don't collide with walls
            AddReward(-.01f * hitCounter);
            hitCounter++;
        }
        if (gameObject.GetComponent<Player>().hitWallForw == 0) {     //Don't collide with walls
            AddReward(-.01f * hitCounter);
            hitCounter++;
        }
        if (gameObject.GetComponent<Player>().hitWallLeft == 0) {     //Don't collide with walls
            AddReward(-.01f * hitCounter);
            hitCounter++;
        }
        if (gameObject.GetComponent<Player>().hitWallRight == 0) {     //Don't collide with walls
            AddReward(-.01f * hitCounter);
            hitCounter++;
        }

        if (hitCounter > 100) {
            EndEpisode();
            AddReward(-1);
        }

    }

    /// <summary>
    /// Observations made by bots to learn and adapt from environmental factors
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {

        base.CollectObservations(sensor);

        sensor.AddObservation(hitCounter);     //Observe position

        sensor.AddObservation(transform.position);     //Observe position
        sensor.AddObservation(transform.rotation);     //observe rotation

        for(int i = 0; i < gameObject.GetComponent<Player>().eyeHits.Length; i++)
            sensor.AddObservation(gameObject.GetComponent<Player>().eyeHits[i].point);     //Observe agent's vision

        sensor.AddObservation(lastKnownTaggerPos);     //Observe last known position of tagger
        sensor.AddObservation(lastKnownHiderPos);     //Observe last known position of a player

        sensor.AddObservation(gameObject.GetComponent<Player>().hitWallBack);     //Observe when hitting wall
        sensor.AddObservation(gameObject.GetComponent<Player>().hitWallForw);     //Observe when hitting wall
        sensor.AddObservation(gameObject.GetComponent<Player>().hitWallLeft);     //Observe when hitting wall
        sensor.AddObservation(gameObject.GetComponent<Player>().hitWallRight);     //Observe when hitting wall

        sensor.AddObservation(isTagger);     //Observe last known position of tagger

        sensor.AddObservation(hitCounter);     //Observe how many times player has hit wall

        for (int i = 0; i < gameObject.GetComponent<Player>().MLDistHits.Length; i++)     //Observer agent's movement depth
            sensor.AddObservation(gameObject.GetComponent<Player>().MLDistHits[i].point);
    }

    /// <summary>
    /// Actions the ML bot decides
    /// </summary>
    public override void OnActionReceived(float[] vectorAction) {
        //base.OnActionReceived(vectorAction); 

        forwBack = (int)vectorAction[0];
        leftRight = (int)vectorAction[1];
        rotY = (int)vectorAction[2];
        waitOverride = (int)vectorAction[3];     //Is the agent allowed to move?
    }

    /// <summary>
    /// Override method for controlling a bot
    /// </summary>
    public override void Heuristic(float[] actionsOut) {

        actionsOut[0] = gameObject.GetComponent<Player>().movVert;
        actionsOut[1] = gameObject.GetComponent<Player>().movHorz;

        if(Input.GetAxisRaw("Mouse X") > 0)
            actionsOut[2] = 2;
        if (Input.GetAxisRaw("Mouse X") < 0)
            actionsOut[2] = 1;
        if (Input.GetAxisRaw("Mouse X") == 0)
            actionsOut[2] = 0;

        if(gameObject.GetComponent<Player>().movVert  == 0 && gameObject.GetComponent<Player>().movHorz == 0)
            actionsOut[3] = 0;
        else
            actionsOut[3] = 1;
    }
}
