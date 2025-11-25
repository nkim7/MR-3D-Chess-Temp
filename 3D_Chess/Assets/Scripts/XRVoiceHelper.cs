using UnityEngine;
using System.Collections.Generic;
using UnityChess;

public class XRVoiceHelper : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private float indicatorHeight = 0.02f;
    [SerializeField] private Color indicatorColor = new Color(0f, 1f, 0f, 0.5f);

    [Header("Legal Move Highlight")]
    [SerializeField] private Material legalMoveMaterial;
    [SerializeField] private Color legalMoveColor = new Color(0f, 1f, 1f, 0.6f);
    [SerializeField] private float legalMoveIndicatorHeight = 0.015f;
    [SerializeField] private float legalMoveIndicatorScale = 0.35f;

    [Header("Piece Highlight")]
    [Tooltip("Material for the piece actively selected or moving.")]
    [SerializeField] private Material selectedPieceMaterial;
    [Tooltip("Material for ambiguous pieces or valid candidates.")]
    [SerializeField] private Material candidatePieceMaterial;
    
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color candidateColor = Color.yellow; // Default yellow for ambiguity
    [SerializeField] private float pieceHighlightScale = 0.25f;
    [SerializeField] private float pieceHighlightHeight = 0.2f;

    [Header("Animation")]
    [SerializeField] private bool animateHighlights = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinScale = 0.8f;
    [SerializeField] private float pulseMaxScale = 1.2f;

    private List<GameObject> moveIndicators = new List<GameObject>();
    private List<GameObject> pieceHighlights = new List<GameObject>();
    
    // Stores initial scale to calculate pulsing without drifting
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();

    void Update()
    {
        if (animateHighlights)
        {
            AnimatePulse();
        }
    }

    void AnimatePulse()
    {
        // Simple sine wave pulse for "breathing" effect
        float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        // Iterate backwards or check nulls to be safe
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in originalScales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.localScale = kvp.Value * pulse;
            }
            else
            {
                toRemove.Add(kvp.Key);
            }
        }

        // Cleanup destroyed keys
        foreach (var key in toRemove)
        {
            originalScales.Remove(key);
        }
    }

    /// <summary>
    /// Highlights a specific list of squares (e.g., valid moves for a piece).
    /// </summary>
    public void ShowLegalMoves(List<Square> legalMoves, Transform board)
    {
        ClearMoveIndicators();

        if (board == null || legalMoves == null) return;

        foreach (Square sq in legalMoves)
        {
            CreateLegalMoveIndicator(sq, board);
        }
    }

    /// <summary>
    /// Debug helper to show all squares.
    /// </summary>
    public void ShowPossibleDestinations(Square fromSquare, Transform board)
    {
        ClearMoveIndicators();

        if (board == null) return;

        for (int file = 1; file <= 8; file++)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                Square toSquare = new Square(file, rank);
                if (toSquare != fromSquare)
                {
                    CreateMoveIndicator(toSquare, board);
                }
            }
        }
    }

    /// <summary>
    /// Creates a visual sphere above a piece.
    /// </summary>
    /// <param name="isSelected">If true, uses Selected Color (Green). If false, uses Candidate/Ambiguous Color (Yellow).</param>
    public void HighlightPiece(Transform pieceTransform, bool isSelected)
    {
        if (pieceTransform == null) return;

        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.name = isSelected ? "SelectedPieceHighlight" : "CandidatePieceHighlight";
        highlight.transform.SetParent(pieceTransform);
        highlight.transform.localPosition = Vector3.up * pieceHighlightHeight;
        highlight.transform.localScale = Vector3.one * pieceHighlightScale;

        // Physics cleanup
        Destroy(highlight.GetComponent<Collider>());

        Renderer renderer = highlight.GetComponent<Renderer>();
        Material mat = isSelected ? selectedPieceMaterial : candidatePieceMaterial;
        
        if (mat != null)
        {
            renderer.material = mat;
        }
        else
        {
            // Fallback if no material assigned in Inspector
            Material newMat = new Material(Shader.Find("Unlit/Color"));
            newMat.color = isSelected ? selectedColor : candidateColor;
            renderer.material = newMat;
        }

        pieceHighlights.Add(highlight);
        originalScales[highlight] = highlight.transform.localScale;
    }

    /// <summary>
    /// Highlights a list of pieces. Used by VoiceMoveInterpreter for Ambiguity.
    /// </summary>
    /// <param name="pieces">List of piece transforms to highlight.</param>
    /// <param name="selectedPiece">The "main" piece. If null, all pieces in the list are highlighted as Candidates (Yellow).</param>
    public void HighlightMultiplePieces(List<Transform> pieces, Transform selectedPiece)
    {
        ClearPieceHighlights();

        foreach (Transform piece in pieces)
        {
            // If selectedPiece is null (Ambiguity case), isSelected is always false -> Yellow Highlight
            bool isSelected = (selectedPiece != null) && (piece == selectedPiece);
            HighlightPiece(piece, isSelected);
        }
    }

    void CreateLegalMoveIndicator(Square square, Transform board)
    {
        if (board == null) return;

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "LegalMoveIndicator_" + square;

        Vector3 position = CalculateSquarePosition(square, board);
        indicator.transform.position = position + board.up * legalMoveIndicatorHeight;
        indicator.transform.rotation = Quaternion.LookRotation(board.up);

        float cellSize = GetCellSize(board);
        indicator.transform.localScale = new Vector3(
            cellSize * legalMoveIndicatorScale, 
            legalMoveIndicatorHeight, 
            cellSize * legalMoveIndicatorScale
        );

        Renderer renderer = indicator.GetComponent<Renderer>();
        if (legalMoveMaterial != null)
        {
            renderer.material = legalMoveMaterial;
        }
        else
        {
            Material mat = CreateTransparentMaterial(legalMoveColor);
            renderer.material = mat;
        }

        Destroy(indicator.GetComponent<Collider>());
        moveIndicators.Add(indicator);
        originalScales[indicator] = indicator.transform.localScale;
    }

    void CreateMoveIndicator(Square square, Transform board)
    {
        if (board == null) return;

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "MoveIndicator_" + square;
        
        Vector3 position = CalculateSquarePosition(square, board);
        float cellSize = GetCellSize(board);

        indicator.transform.position = position + board.up * indicatorHeight;
        indicator.transform.rotation = Quaternion.LookRotation(board.up);
        indicator.transform.localScale = new Vector3(cellSize * 0.3f, indicatorHeight, cellSize * 0.3f);

        Renderer renderer = indicator.GetComponent<Renderer>();
        if (highlightMaterial != null)
        {
            renderer.material = highlightMaterial;
        }
        else
        {
            Material mat = CreateTransparentMaterial(indicatorColor);
            renderer.material = mat;
        }

        Destroy(indicator.GetComponent<Collider>());
        moveIndicators.Add(indicator);
    }

    // --- Helpers ---

    Vector3 CalculateSquarePosition(Square square, Transform board)
    {
        MeshRenderer mr = board.GetComponent<MeshRenderer>();
        Bounds bounds = mr != null ? mr.bounds : board.GetComponent<Collider>().bounds;
        Vector3 localSize = board.InverseTransformVector(bounds.size);
        float boardWidth = Mathf.Abs(localSize.x);
        float cellSize = boardWidth / 8f;
        float originX = -boardWidth * 0.5f;
        float originZ = -boardWidth * 0.5f; // Assuming square board

        int c = square.File - 1;
        int r = square.Rank - 1;
        float cx = originX + (c + 0.5f) * cellSize;
        float cz = originZ + (r + 0.5f) * cellSize;

        return board.TransformPoint(new Vector3(cx, 0f, cz));
    }

    float GetCellSize(Transform board)
    {
        MeshRenderer mr = board.GetComponent<MeshRenderer>();
        Bounds bounds = mr != null ? mr.bounds : board.GetComponent<Collider>().bounds;
        Vector3 localSize = board.InverseTransformVector(bounds.size);
        float boardWidth = Mathf.Abs(localSize.x);
        return boardWidth / 8f;
    }

    Material CreateTransparentMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }

    public void ClearMoveIndicators()
    {
        foreach (GameObject indicator in moveIndicators)
        {
            if (indicator != null)
            {
                originalScales.Remove(indicator);
                Destroy(indicator);
            }
        }
        moveIndicators.Clear();
    }

    public void ClearPieceHighlights()
    {
        foreach (GameObject highlight in pieceHighlights)
        {
            if (highlight != null)
            {
                originalScales.Remove(highlight);
                Destroy(highlight);
            }
        }
        pieceHighlights.Clear();
    }

    public void ClearAllHighlights()
    {
        ClearMoveIndicators();
        ClearPieceHighlights();
    }

    void OnDisable()
    {
        ClearAllHighlights();
    }
}