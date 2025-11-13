using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBoard : MonoBehaviour
{
    public ChessBoard chessboard;

    public void onClicked()
    {
        gameObject.SetActive(false);
        chessboard.finish_placement();
    }
}
