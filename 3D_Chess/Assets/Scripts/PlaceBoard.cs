using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBoard : MonoBehaviour
{
    [Header("Drag the Chess Board here")]
    public ChessBoard chessboard;

    [Header("Drag the 'UI_Dialog' here")]
    public GameObject dialogCanvas; 

    // This function is called by the Button
    public void onClicked()
    {
        // 1. Hide the UI (The Dialog), NOT the Board
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("You forgot to drag UI_Dialog into the 'Dialog Canvas' slot!");
        }

        // 2. Tell the ChessBoard to start the game
        if (chessboard != null)
        {
            chessboard.finish_placement();
        }
    }
}