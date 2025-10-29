using UnityEngine;
using UnityChess;
using UnityChess;


public class ChessLibTest : MonoBehaviour
{
    private Game game;

    void Start()
    {
        game = new Game();
        Debug.Log("Initial position:");
        PrintBoard();

        // 1. f2-f3
        Square startSquare = new Square("f2");
        Square endSquare = new Square("f3");
        bool success1 = game.TryExecuteMove(new Movement(startSquare, endSquare));
        Debug.Log("After 1. f2-f3:");
        PrintBoard();

        // 1...e7-e6
        startSquare = new Square("e7");
        endSquare = new Square("e6");
        bool success2 = game.TryExecuteMove(new Movement(startSquare, endSquare));
        Debug.Log("After 1...e7-e6:");
        PrintBoard();

        // 2. g2-g4
        startSquare = new Square("g2");
        endSquare = new Square("g4");
        bool success3 = game.TryExecuteMove(new Movement(startSquare, endSquare));
        Debug.Log("After 2. g2-g4:");
        PrintBoard();

        // 2...Qd8-h4# (Checkmate!)
        startSquare = new Square("d8");
        endSquare = new Square("h4");
        bool success4 = game.TryExecuteMove(new Movement(startSquare, endSquare));
        Debug.Log("After 2...Qh4# (Checkmate!):");
        PrintBoard();

        // Log the success of each move
        Debug.Log($"Moves successful: {success1}, {success2}, {success3}, {success4}");

        startSquare = new Square("d5");  // Black pawn on d5
        endSquare = new Square("e4");    // Capturing white pawn on e4
        bool success5 = game.TryExecuteMove(new Movement(startSquare, endSquare));
        Debug.Log($"After black pawn captures on e4: Success = {success5}");
        PrintBoard();
    }

    private void PrintBoard()
    {
        if (!game.BoardTimeline.TryGetCurrent(out Board board))
        {
            Debug.Log("Could not get current board state");
            return;
        }

        string boardString = "";
        for (int rank = 8; rank >= 1; rank--)
        {
            boardString += $"{rank} ";
            for (int file = 1; file <= 8; file++)
            {
                Piece piece = board[file, rank];
                boardString += piece != null ? piece.ToString()[0] + " " : ". ";
            }
            boardString += "\n";
        }
        
        // Add file labels
        boardString += "  a b c d e f g h";

        Debug.Log("\n" + boardString);
    }
}
