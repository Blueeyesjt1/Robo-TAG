using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
//using Unity.MLAgents;
using UnityEngine;

public class Player : MonoBehaviourPunCallbacks {

    public bool isHuman = true;     //Is the player a real person playing?

    public bool isFrozen = false;     //Did they just get tagged?

    public bool isTagger = false;     //Are we one of the taggers?

    public int playerActorNum = 0;     //Client's public actor number

    public int speedMult = 5;

    public int hitWallForw = 1;     //1 = not hitting, 0 = hitting
    public int hitWallBack = 1;     //1 = not hitting, 0 = hitting
    public int hitWallLeft = 1;     //1 = not hitting, 0 = hitting
    public int hitWallRight = 1;     //1 = not hitting, 0 = hitting

    public int movVert = 0;     //0 = not moving, 1 = forward, -1 = backward
    public int movHorz = 0;     //0 = not moving, 1 = left, -1 = right

    public float xTurn = 0;     //0 = not turning
    public float yTurn = 0;     //0 = not turning

    public string playerName = "Noob";     //userName to seperate players

    public RaycastHit[] MLDistHits = new RaycastHit[20];     //Used for ML agents to detect environment
    public RaycastHit[] eyeHits = new RaycastHit[5];     //Used for ML agents to find hiders and taggers

    //MLBrain mlBrain = null;     //Brain of player, if a bot

    /// <summary>
    /// Player's interaction with the environment and server, non-synced framerate
    /// </summary>
    void Update() {
        if (!photonView.IsMine)     //If we're not the local client, ignore
            return;     //We only want the local client

        Turning();
    }

    /// <summary>
    /// Player's start method at the beginning of every joined-server
    /// </summary>
    /*private void Start() {
        if (photonView.IsMine && gameObject.GetComponent<MLBrain>() != null)
            mlBrain = gameObject.GetComponent<MLBrain>();
    }*/

    /// <summary>
    /// Player's interaction with the environment and server, synced framerate
    /// </summary>
    void FixedUpdate()
    {
        if (!photonView.IsMine)     //If we're not the local client, ignore
            return;     //We only want the local client

        //if(!isHuman)
        RayCasts();     //Raycast detection for bots and humans

        if (!isFrozen)
            PlayerInputs();     //Player movement, whether it be a bot or real user

        if(isTagger && gameObject.GetComponent<MeshRenderer>().material.color != Color.red)
            GameObject.Find("serverLight").GetComponent<PhotonView>().RPC("loadTagger", RpcTarget.All, photonView.ViewID);
    }

    /// <summary>
    /// Player's input, whether it be a bot or a real user
    /// </summary>
    void PlayerInputs() {

        if (isHuman /*|| (mlBrain != null && mlBrain.userOverrideTraining == true)*/) {     //If a real user or heuristic agent, use mouse/keyboard inputs

            if (Input.GetKey(KeyCode.W))     //Front back movement
                movVert = 1;
            else if (Input.GetKey(KeyCode.S))
                movVert = -1;
            else
                movVert = 0;

            if (Input.GetKey(KeyCode.A))     //Left right movement
                movHorz = -1;
            else if (Input.GetKey(KeyCode.D))
                movHorz = 1;
            else
                movHorz = 0;

            if (Input.GetAxisRaw("Mouse X") != 0)     //Left right turning
                xTurn = Input.GetAxisRaw("Mouse X");
            else
                xTurn = 0;

            if (Input.GetAxisRaw("Mouse Y") != 0)     //Up down turning
                yTurn = Input.GetAxisRaw("Mouse Y");
            else
                yTurn = 0;

            if (Input.GetKeyDown(KeyCode.Escape)) {     //Left right movement
                if (gameObject.transform.Find("camera/CanvasQuit").gameObject.GetComponent<Canvas>().enabled == false) {
                    gameObject.transform.Find("camera/CanvasQuit").gameObject.GetComponent<Canvas>().enabled = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else {
                    gameObject.transform.Find("camera/CanvasQuit").gameObject.GetComponent<Canvas>().enabled = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

        }
        /*else if(!isHuman) {     //If a ML agent, use ML choices

            if (mlBrain.waitOverride == 1) {
                if (mlBrain.forwBack == 2)     //Front back movement
                    movVert = 1;
                else if (mlBrain.forwBack == 1)
                    movVert = -1;
                else if (mlBrain.forwBack == 0)
                    movVert = 0;

                if (mlBrain.leftRight == 1)     //Left right movement
                    movHorz = -1;
                else if (mlBrain.leftRight == 2)
                    movHorz = 1;
                else if (mlBrain.leftRight == 0)
                    movHorz = 0;
            }
            else {
                movVert = 0;
                movHorz = 0;
            }

            if (mlBrain.rotY == 2)     //Left right xTurning
                xTurn = .4f;
            else if (mlBrain.rotY == 1)     //Left right xTurning
                xTurn = -.4f;
            else if (mlBrain.rotY == 0)
                xTurn = 0;
        }*/

        Movement();     //Inform other users about movement
    }

    /// <summary>
    /// If player is the tagger, detect when they tag someone
    /// </summary>
    private void OnTriggerEnter(Collider col) {

        if (!col.name.Contains("Player"))
            return;

        if (!isFrozen && isTagger && !col.gameObject.GetComponent<Player>().isTagger && !col.gameObject.GetComponent<Player>().isFrozen) {     //If unfrozen-client is a tagger and tags unfrozen-someone who isn't,

            int newTaggerVID = col.gameObject.GetComponent<PhotonView>().ViewID;

            print("Tagged someone!");

            GameObject.Find("serverLight").GetComponent<PhotonView>().RPC("newTagger", RpcTarget.All, newTaggerVID);
        }
    }

    /// <summary>
    /// Bot's raycasting used for player and environment depth-detection
    /// </summary>
    void RayCasts() {
        RaycastHit forwHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), transform.forward, out forwHit, 1.3f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position + transform.forward, Color.green, .5f);
            hitWallForw = 0;
        }
        else
            hitWallForw = 1;

        RaycastHit backHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), -transform.forward, out backHit, 1.3f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position - transform.forward, Color.red, .5f);
            hitWallBack = 0;
        }
        else
            hitWallBack = 1;

        RaycastHit leftHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), -transform.right, out leftHit, 1.3f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position - transform.right, Color.yellow, .5f);
            hitWallLeft = 0;
        }
        else
            hitWallLeft = 1;

        RaycastHit rightHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), transform.right, out rightHit, 1.3f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position + transform.right, Color.cyan, .5f);
            hitWallRight = 0;
        }
        else
            hitWallRight = 1;

        if (!isHuman) {
            int rayCount = 0;
            for (int i = 0; i < 4; i++) {
                for (int r = 0; r < 4; r++) {

                    int iSign = 1;     //Determines sign of variable I
                    int rSign = 1;     //Determines sign of variable J

                    if (i == 0) {
                        iSign = 1;
                        rSign = 1;
                    }
                    else if (i == 1) {
                        iSign = 1;
                        rSign = -1;
                    }
                    else if (i == 2) {
                        iSign = -1;
                        rSign = 1;
                    }
                    else if (i == 3) {
                        iSign = -1;
                        rSign = -1;
                    }

                    if (Physics.Raycast(transform.position + new Vector3(0, .75f, 0), new Vector3(iSign * r, 0, rSign * (r - 4)), out MLDistHits[rayCount], Mathf.Infinity)) {
                        Debug.DrawLine(transform.position + new Vector3(0, .75f, 0), MLDistHits[rayCount].point, Color.blue, .01f);
                    }

                    rayCount++;
                }
            }

            if (Physics.Raycast(transform.position + new Vector3(0, .75f, 0), gameObject.transform.TransformDirection(0, 0, 1), out eyeHits[0], Mathf.Infinity)) {
                Debug.DrawLine(transform.position + new Vector3(0, .75f, 0), eyeHits[0].point, Color.yellow, .01f);
            }

            if (Physics.Raycast(transform.position + new Vector3(0, .75f, 0), gameObject.transform.TransformDirection(.1f, 0, 1), out eyeHits[1], Mathf.Infinity)) {
                Debug.DrawLine(transform.position + new Vector3(0, .75f, 0), eyeHits[1].point, Color.yellow, .01f);
            }

            if (Physics.Raycast(transform.position + new Vector3(0, .75f, 0), gameObject.transform.TransformDirection(.1f, 0, 1), out eyeHits[2], Mathf.Infinity)) {
                Debug.DrawLine(transform.position + new Vector3(0, .75f, 0), eyeHits[2].point, Color.yellow, .01f);
            }

            if (Physics.Raycast(transform.position + new Vector3(0, .75f, 0), gameObject.transform.TransformDirection(-.1f, 0, 1), out eyeHits[3], Mathf.Infinity)) {
                Debug.DrawLine(transform.position + new Vector3(0, .75f, 0), eyeHits[3].point, Color.yellow, .01f);
            }

            if (Physics.Raycast(transform.position + new Vector3(0, .75f, 0), gameObject.transform.TransformDirection(-.1f, 0, 1), out eyeHits[4], Mathf.Infinity)) {
                Debug.DrawLine(transform.position + new Vector3(0, .75f, 0), eyeHits[4].point, Color.yellow, .01f);
            }
        }
    }

    /// <summary>
    /// Player's movement, whether it be player or bot
    /// </summary>
    void Movement() {

        if (movHorz == -1)
            gameObject.transform.Translate(speedMult * 5f * movHorz * hitWallLeft * Time.fixedDeltaTime, 0, 0, Space.Self);     //Move left
        if (movHorz == 1)
            gameObject.transform.Translate(speedMult * 5f * movHorz * hitWallRight * Time.fixedDeltaTime, 0, 0, Space.Self);     //Move right
        
        if(movVert == 1)
            gameObject.transform.Translate(0, 0, speedMult * 5f * movVert * hitWallForw * Time.fixedDeltaTime, Space.Self);     //Move forward
        if (movVert == -1)
            gameObject.transform.Translate(0, 0, speedMult * 5f * movVert * hitWallBack * Time.fixedDeltaTime, Space.Self);     //Move backward
    }

    /// <summary>
    /// Player's rotation, whether it be player or bot
    /// </summary>
    void Turning() {

        int turnMult = 1;

        if (Application.isEditor)
            turnMult = 5;

            //If input turning left or right, rotate player
        if (xTurn != 0)
            gameObject.transform.Rotate(new Vector3(0, xTurn * 100 * turnMult * Time.deltaTime, 0), Space.Self);     //Horizontal rotation
        else
            gameObject.transform.Rotate(new Vector3(0, 0, 0), Space.Self);     //Horizontal rotation

        //If real person and wanting to look up or down, proceed
        if (isHuman)
            gameObject.transform.Find("camera").transform.Rotate(-yTurn * 100 * turnMult * Time.deltaTime, 0, 0, Space.Self);     //No rotation limits yet
    }

}
