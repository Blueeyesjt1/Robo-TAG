using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
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

    private void Update() {
        gameObject.GetComponent<PhotonView>().RPC("Turning", RpcTarget.All);
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
            return;

        if (isHuman || (gameObject.GetComponent<MLBrain>() != null && gameObject.GetComponent<MLBrain>().userOverrideTraining)) {     //If a real user, use mouse/keyboard inputs

            if (Input.GetKey(KeyCode.W))     //Front back movement
                movVert = 1;
            else if(Input.GetKey(KeyCode.S))
                movVert = -1;
            else
                movVert = 0; 
            

            if (Input.GetKey(KeyCode.A))     //Left right movement
                movHorz = -1;
            else if (Input.GetKey(KeyCode.D))
                movHorz = 1;
            else
                movHorz = 0;


            if (Input.GetAxisRaw("Mouse X") != 0)     //Left right xTurning
                xTurn = Input.GetAxisRaw("Mouse X");
            else
                xTurn = 0;


            if (Input.GetAxisRaw("Mouse Y") != 0)     //Up down xTurning
                yTurn = Input.GetAxisRaw("Mouse Y");
            else
                yTurn = 0;
        }
        else {     //If a ML agent, use ML choices

            if (gameObject.GetComponent<MLBrain>().waitOverride == 1) {
                if (gameObject.GetComponent<MLBrain>().forwBack == 2)     //Front back movement
                    movVert = 1;
                else if (gameObject.GetComponent<MLBrain>().forwBack == 1)
                    movVert = -1;
                else if (gameObject.GetComponent<MLBrain>().forwBack == 0)
                    movVert = 0;

                if (gameObject.GetComponent<MLBrain>().leftRight == 1)     //Left right movement
                    movHorz = -1;
                else if (gameObject.GetComponent<MLBrain>().leftRight == 2)
                    movHorz = 1;
                else if (gameObject.GetComponent<MLBrain>().leftRight == 0)
                    movHorz = 0;
            }
            else {
                movVert = 0;
                movHorz = 0;
            }

            if (gameObject.GetComponent<MLBrain>().rotY == 2)     //Left right xTurning
                xTurn = .4f;
            else if (gameObject.GetComponent<MLBrain>().rotY == 1)     //Left right xTurning
                xTurn = -.4f;
            else if (gameObject.GetComponent<MLBrain>().rotY == 0)
                xTurn = 0;
        }

        RayCasts();

        if(isFrozen == false)
            gameObject.GetComponent<PhotonView>().RPC("Movement", RpcTarget.All);

        if(isTagger && gameObject.GetComponent<MeshRenderer>().material.color != Color.red)
            GameObject.Find("serverLight").GetComponent<PhotonView>().RPC("loadTagger", RpcTarget.All, photonView.ViewID);
    }

    private void OnTriggerEnter(Collider col) {

        if (!photonView.IsMine || !col.name.Contains("Player"))
            return;

        if (!isFrozen && isTagger && !col.gameObject.GetComponent<Player>().isTagger && !col.gameObject.GetComponent<Player>().isFrozen) {     //If unfrozen-client is a tagger and tags unfrozen-someone who isn't,

            int newTaggerVID = col.gameObject.GetComponent<PhotonView>().ViewID;

            print("Tagged someone!");

            GameObject.Find("serverLight").GetComponent<PhotonView>().RPC("newTagger", RpcTarget.All, newTaggerVID);
        }
    }

    void RayCasts() {
        RaycastHit forwHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), transform.forward, out forwHit, 1.1f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position + transform.forward, Color.green, .5f);
            hitWallForw = 0;
        }
        else
            hitWallForw = 1;

        RaycastHit backHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), -transform.forward, out backHit, 1.1f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position - transform.forward, Color.red, .5f);
            hitWallBack = 0;
        }
        else
            hitWallBack = 1;

        RaycastHit leftHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), -transform.right, out leftHit, 1.1f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position - transform.right, Color.yellow, .5f);
            hitWallLeft = 0;
        }
        else
            hitWallLeft = 1;

        RaycastHit rightHit;
        if (Physics.Raycast(transform.position + new Vector3(0, .5f, 0), transform.right, out rightHit, 1.1f, 1 << 0)) {
            Debug.DrawLine(transform.position, transform.position + transform.right, Color.cyan, .5f);
            hitWallRight = 0;
        }
        else
            hitWallRight = 1;


        int rayCount = 0;
        for(int i = 0; i < 4; i++) {
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

        if (Physics.Raycast(transform.position + new Vector3(0, .75f, 0), gameObject.transform.TransformDirection(.1f,0,1), out eyeHits[1], Mathf.Infinity)) {
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

    [PunRPC]
    void Movement() {

        if (!photonView.IsMine)
            return;

        if (movHorz == -1)
            gameObject.transform.Translate(speedMult * 5f * movHorz * hitWallLeft * Time.fixedDeltaTime, 0, 0, Space.Self);     //Move left
        if (movHorz == 1)
            gameObject.transform.Translate(speedMult * 5f * movHorz * hitWallRight * Time.fixedDeltaTime, 0, 0, Space.Self);     //Move right
        
        if(movVert == 1)
            gameObject.transform.Translate(0, 0, speedMult * 5f * movVert * hitWallForw * Time.fixedDeltaTime, Space.Self);     //Move forward
        if (movVert == -1)
            gameObject.transform.Translate(0, 0, speedMult * 5f * movVert * hitWallBack * Time.fixedDeltaTime, Space.Self);     //Move backward
    }

    [PunRPC]
    void Turning() {

        if (xTurn != 0)
            gameObject.transform.Rotate(new Vector3(0, xTurn * 500 * Time.deltaTime, 0), Space.Self);     //Horizontal rotation

        if (isHuman && photonView.IsMine) {
            var camRot = gameObject.transform.Find("camera").transform.localEulerAngles;
            gameObject.transform.Find("camera").transform.Rotate(-yTurn * 500 * Time.deltaTime, 0, 0, Space.Self);     //No rotation limits yet
        }

    }

}
