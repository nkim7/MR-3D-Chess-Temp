using System.Collections;
using UnityEngine;
using UnityChess;

// Bridges logical chess piece to GameObject
public class ChessPiece : MonoBehaviour
{
    //might not need fields
    public Square logicalSquare { get; private set; }

    public Piece logicalPiece { get; private set; }

    public int startFile = 2;
    public int startRank = 8;

    public ChessPiece()
    {
        logicalSquare = new Square(startFile, startRank); // Default starting position
    }

    public void SetLogicalPiece(Piece piece)
    {
        logicalPiece = piece;
    }

    public bool SetSquare(Square square)
    {
        Square old = logicalSquare;
        
        Game game = this.GetComponentInParent<GameManager>().chessGame;
        if (game != null)
        {
            bool move = game.TryExecuteMove(new Movement(old, square));
            if (!move)
            {
                Debug.LogWarning($"Invalid move attempted from {old} to {square}");
                return false;
            }
            else
            {
                logicalSquare = square;
                Debug.Log($"Move executed from {old} to {square}");
                return true;
            }
            

            // // TODO: Check for Checkmate
            // if (!game.BoardTimeline.TryGetCurrent(out Board board))
            // {
            //     Debug.Log("Could not get current board state");
            //     return;
            // }
        }
        return false;               
        // public void ShowAvailableMoves()
        // {
        //     // TODO: implement, and call in SnapToBoardOnRelease
        // }

    }
}

