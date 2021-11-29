using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class quitEvent : MonoBehaviour
{
    public void quitClick() {
        print("QUIT GAME");
        Application.Quit();
    }
}
