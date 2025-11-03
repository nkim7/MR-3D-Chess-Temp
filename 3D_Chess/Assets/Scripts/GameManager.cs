using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityChess;

public class GameManager : MonoBehaviour
{
    // Chess Pieces
    public GameObject whitePawnPrefab;
    public GameObject whiteRookPrefab;
    public GameObject whiteKnightPrefab;
    public GameObject whiteBishopPrefab;
    public GameObject whiteQueenPrefab;
    public GameObject whiteKingPrefab;
    public GameObject blackPawnPrefab;
    public GameObject blackRookPrefab;
    public GameObject blackKnightPrefab;
    public GameObject blackBishopPrefab;
    public GameObject blackQueenPrefab;
    public GameObject blackKingPrefab;

    public Game chessGame;
    // private Dictionary<Square, ChessPiece> squareToPiece = new Dictionary<Square, ChessPiece>();
    // private Dictionary<ChessPiece, Square> pieceToSquare = new Dictionary<ChessPiece, Square>();
    // // private Dictionary<Piece, ChessPiece> physicalPieces = new Dictionary<Piece, ChessPiece>();
    // public Transform chessBoard;
    void Start()
    {
        chessGame = new Game();
        // SpawnPieces();

    }

    // private void SpawnPieces() 
    // {
    //     // Clear existing pieces if any
    //     foreach (var piece in physicalPieces.Values) 
    //     {
    //         if (piece != null) 
    //         {
    //             Destroy(piece.gameObject);
    //         }
    //     }
    //     piecesBySquare.Clear();
    //     physicalPieces.Clear();

    //     // Get the current board
    //     if (!chessGame.BoardTimeline.TryGetCurrent(out Board board))
    //     {
    //         Debug.Log("Could not get current board state");
    //         return;
    //     }

    //     // Spawn pieces for each square
    //     for (int rank = 1; rank <= 8; rank++) 
    //     {
    //         for (int file = 1; file <= 8; file++) 
    //         {
    //             //skipping empty squares
    //             if (file > 2 && file < 7) continue; 
    //             // create new square and get piece on square
    //             Square square = new Square(file, rank);
    //             Piece piece = board[square];
    //             // Spawn the piece
    //             if (piece != null) 
    //             {
    //                 GameObject prefab = GetPiecePrefab(piece);
    //                 if (prefab == null) return;
    //                 // Calculate world position for the piece
    //                 Vector3 position = GetSquareWorldPosition(square);
    //                 GameObject pieceObject = Instantiate(prefab, position, Quaternion.identity,chessBoard);

    //                 ChessPiece chessPiece = pieceObject.GetComponent<ChessPiece>();
    //                 if (chessPiece != null)
    //                 {
    //                     chessPiece.SetSquare(square);
    //                     piecesBySquare[square] = chessPiece;
    //                     physicalPieces[piece] = chessPiece;
    //                 }
    //                 else 
    //                 {
    //                     Debug.LogError("Prefab missing ChessPiece component");
    //                 }
    //             }
    //         }
    //     }
    // }

    // private GameObject GetPiecePrefab(Piece piece)
    // {
    //     bool isWhite = piece.Owner == Side.White;
        
    //     if (piece is Pawn) return isWhite ? whitePawnPrefab : blackPawnPrefab;
    //     if (piece is Rook) return isWhite ? whiteRookPrefab : blackRookPrefab;
    //     if (piece is Knight) return isWhite ? whiteKnightPrefab : blackKnightPrefab;
    //     if (piece is Bishop) return isWhite ? whiteBishopPrefab : blackBishopPrefab;
    //     if (piece is Queen) return isWhite ? whiteQueenPrefab : blackQueenPrefab;
    //     if (piece is King) return isWhite ? whiteKingPrefab : blackKingPrefab;
        
    //     return null;
    // }

    // private Vector3 GetSquareWorldPosition(Square square)
    // {
    //     // Get the actual board size from its collider
    //     Bounds bounds = GetComponent<Collider>().bounds;
    //     float boardSize = bounds.size.x; // Use the width of the board
    //     float squareSize = boardSize / 8f;
        
    //     // Calculate position relative to board center
    //     float xPos = (square.File - 3.5f) * squareSize;
    //     float zPos = (square.Rank - 3.5f) * squareSize;

    //     return transform.position + new Vector3(xPos, 0.0f, zPos);
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }
}
