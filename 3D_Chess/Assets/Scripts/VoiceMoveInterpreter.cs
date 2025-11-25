using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oculus.Voice;
using Meta.WitAi.Events;
using UnityChess;

public class VoiceMoveInterpreter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AppVoiceExperience voiceService;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private FloatingTranscriptPopup popup;
    [SerializeField] private Camera xrCamera;
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("Listening Settings")]
    [SerializeField] private float relistenDelay = 0.8f;
    [SerializeField] private float forceReactivateDelay = 2.0f;
    [SerializeField] private bool debugMode = true;

    [Header("XR Disambiguation")]
    [SerializeField] private float gazeWeight = 3.0f;
    [SerializeField] private float proximityWeight = 1.5f;
    [SerializeField] private float maxGazeAngle = 45f;
    [SerializeField] private float confirmationTimeout = 5f;

    [Header("Visual Feedback")]
    [SerializeField] private Material candidateMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material targetMaterial;

    private bool isProcessing = false;
    private int moveCount = 0;

    private bool awaitingConfirmation = false;
    private Square pendingFrom;
    private Square pendingTo;
    private List<Square> pendingCandidates;
    private Dictionary<Square, GameObject> highlightObjects = new Dictionary<Square, GameObject>();

    void Awake()
    {
        if (voiceService == null)
            voiceService = FindObjectOfType<AppVoiceExperience>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (popup == null)
            popup = FindObjectOfType<FloatingTranscriptPopup>();
        if (xrCamera == null)
            xrCamera = Camera.main;
    }

    void OnEnable()
    {
        if (voiceService != null && voiceService.VoiceEvents != null)
        {
            voiceService.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
            voiceService.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            voiceService.VoiceEvents.OnError.AddListener(OnError);
            voiceService.VoiceEvents.OnAborted.AddListener(OnAborted);
            voiceService.VoiceEvents.OnStoppedListeningDueToTimeout.AddListener(OnStoppedListeningTimeout);
            voiceService.VoiceEvents.OnStoppedListeningDueToInactivity.AddListener(OnStoppedListeningInactivity);
            voiceService.VoiceEvents.OnStoppedListeningDueToDeactivation.AddListener(OnStoppedListeningDeactivation);
            voiceService.VoiceEvents.OnRequestCompleted.AddListener(OnRequestCompleted);
        }

        ChessBoard.BoardPlaced += OnBoardPlaced;
    }

    void OnDisable()
    {
        if (voiceService != null && voiceService.VoiceEvents != null)
        {
            voiceService.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
            voiceService.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            voiceService.VoiceEvents.OnError.RemoveListener(OnError);
            voiceService.VoiceEvents.OnAborted.RemoveListener(OnAborted);
            voiceService.VoiceEvents.OnStoppedListeningDueToTimeout.RemoveListener(OnStoppedListeningTimeout);
            voiceService.VoiceEvents.OnStoppedListeningDueToInactivity.RemoveListener(OnStoppedListeningInactivity);
            voiceService.VoiceEvents.OnStoppedListeningDueToDeactivation.RemoveListener(OnStoppedListeningDeactivation);
            voiceService.VoiceEvents.OnRequestCompleted.RemoveListener(OnRequestCompleted);
        }

        ChessBoard.BoardPlaced -= OnBoardPlaced;
        ClearAllHighlights();
        CancelInvoke();
    }

    void Update()
    {
        if (awaitingConfirmation)
        {
            if (CheckPinchConfirmation())
            {
                ConfirmMove();
            }
        }
    }

    bool CheckPinchConfirmation()
    {
        bool leftPinch = leftHand != null && leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool rightPinch = rightHand != null && rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        
        return leftPinch || rightPinch;
    }

    void OnBoardPlaced()
    {
        DebugLog("[Voice] Board placed");
        if (popup != null)
            popup.SetText("Say: \"knight to f3\" or \"e2 to e4\"");

        StartListeningDelayed(0.5f);
    }

    void StartListeningDelayed(float delay)
    {
        CancelInvoke(nameof(ActivateVoiceService));
        Invoke(nameof(ActivateVoiceService), delay);
        DebugLog($"[Voice] Scheduled in {delay}s");
    }

    void ActivateVoiceService()
    {
        if (voiceService == null || awaitingConfirmation)
            return;

        if (isProcessing)
        {
            DebugLog("[Voice] Still processing");
            return;
        }

        DebugLog("[Voice] >>> ACTIVATING <<<");
        voiceService.Activate();
        
        CancelInvoke(nameof(ForceReactivate));
        Invoke(nameof(ForceReactivate), forceReactivateDelay);
    }

    void ForceReactivate()
    {
        if (isProcessing || awaitingConfirmation)
            return;

        DebugLog("[Voice] FORCE REACTIVATE");
        voiceService.Deactivate();
        Invoke(nameof(ActivateVoiceService), 0.3f);
    }

    void OnPartialTranscription(string text)
    {
        DebugLog($"[Voice] Partial: '{text}'");
        CancelInvoke(nameof(ForceReactivate));
        
        if (popup != null)
            popup.SetText($"Hearing: \"{text}\"");
    }

    void OnFullTranscription(string text)
    {
        DebugLog($"[Voice] === FULL === '{text}'");
        
        CancelInvoke(nameof(ForceReactivate));
        isProcessing = true;

        if (text.ToLower().Contains("menu"))
        {
            HandleMenuCommand();
            return;
        }

        if (awaitingConfirmation && (text.Contains("yes") || text.Contains("confirm")))
        {
            ConfirmMove();
            return;
        }

        if (awaitingConfirmation && (text.Contains("no") || text.Contains("cancel")))
        {
            CancelConfirmation();
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            if (popup != null)
                popup.SetText("Didn't hear. Try again.");
            FinishProcessingAndRelisten();
            return;
        }

        if (gameManager == null)
        {
            Debug.LogError("[Voice] GameManager null");
            if (popup != null)
                popup.SetText("Game not ready");
            FinishProcessingAndRelisten();
            return;
        }

        if (!TryParseMoveCommand(text, out Square from, out Square to, out string parseError))
        {
            if (popup != null)
                popup.SetText($"\"{text}\"\n{parseError}");
            FinishProcessingAndRelisten();
            return;
        }

        pendingFrom = from;
        pendingTo = to;

        bool success = gameManager.TryMoveByVoice(from, to, out string error);

        if (!success)
        {
            DebugLog($"[Voice] Failed: {error}");
            if (popup != null)
                popup.SetText($"{error}\nTry again");
            FinishProcessingAndRelisten();
            return;
        }

        moveCount++;
        DebugLog($"[Voice] ✓ Move #{moveCount}: {from} → {to}");
        if (popup != null)
            popup.SetText($"✓ {from} to {to}\nNext move");

        ClearAllHighlights();
        FinishProcessingAndRelisten();
    }

    void HandleMenuCommand()
    {
        DebugLog("[Voice] Menu command detected");
        
        if (awaitingConfirmation)
        {
            CancelConfirmation();
        }

        PauseVoiceListening();

        if (GameMenuController.Instance != null)
        {
            GameMenuController.Instance.ShowMenu();
        }
        else
        {
            Debug.LogError("[Voice] GameMenuController not found");
            ResumeVoiceListening();
        }
    }

    public void PauseVoiceListening()
    {
        DebugLog("[Voice] Pausing voice listening");
        CancelInvoke();
        
        if (voiceService != null)
        {
            voiceService.Deactivate();
        }

        isProcessing = false;
        
        if (popup != null)
            popup.SetText("Menu open");
    }

    public void ResumeVoiceListening()
    {
        DebugLog("[Voice] Resuming voice listening");
        
        if (popup != null)
            popup.SetText("Say your move");

        StartListeningDelayed(0.5f);
    }

    void ConfirmMove()
    {
        DebugLog("[Voice] Move confirmed");
        awaitingConfirmation = false;
        CancelInvoke(nameof(TimeoutConfirmation));

        bool success = gameManager.TryMoveByVoice(pendingFrom, pendingTo, out string error);

        if (!success)
        {
            if (popup != null)
                popup.SetText($"{error}\nTry again");
        }
        else
        {
            moveCount++;
            DebugLog($"[Voice] ✓ Move #{moveCount}: {pendingFrom} → {pendingTo}");
            if (popup != null)
                popup.SetText($"✓ {pendingFrom} to {pendingTo}\nNext move");
        }

        ClearAllHighlights();
        FinishProcessingAndRelisten();
    }

    void CancelConfirmation()
    {
        DebugLog("[Voice] Move cancelled");
        awaitingConfirmation = false;
        CancelInvoke(nameof(TimeoutConfirmation));
        ClearAllHighlights();
        
        if (popup != null)
            popup.SetText("Cancelled. Say new move");
        
        FinishProcessingAndRelisten();
    }

    void TimeoutConfirmation()
    {
        DebugLog("[Voice] Confirmation timeout");
        CancelConfirmation();
    }

    void RequestConfirmation(Square from, Square to, List<Square> allCandidates)
    {
        awaitingConfirmation = true;
        pendingFrom = from;
        pendingTo = to;
        pendingCandidates = allCandidates;

        ShowConfirmationVisuals(from, to, allCandidates);

        if (popup != null)
            popup.SetText($"Move {from} to {to}?\nPinch to confirm\nSay NO to cancel");

        Invoke(nameof(TimeoutConfirmation), confirmationTimeout);
        isProcessing = false;
    }

    void ShowConfirmationVisuals(Square selected, Square target, List<Square> allCandidates)
    {
        ClearAllHighlights();

        foreach (Square sq in allCandidates)
        {
            if (!gameManager.TryGetPiece(sq, out ChessPiece piece))
                continue;

            bool isSelected = sq == selected;
            GameObject highlight = CreateHighlight(piece, isSelected ? selectedMaterial : candidateMaterial, 0.25f);
            highlightObjects[sq] = highlight;
        }

        CreateTargetHighlight(target);
    }

    GameObject CreateHighlight(ChessPiece piece, Material mat, float scale)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.name = "Highlight";
        highlight.transform.SetParent(piece.transform);
        highlight.transform.localPosition = Vector3.up * 0.2f;
        highlight.transform.localScale = Vector3.one * scale;
        
        Destroy(highlight.GetComponent<Collider>());
        
        Renderer renderer = highlight.GetComponent<Renderer>();
        if (mat != null)
        {
            renderer.material = mat;
        }
        else
        {
            Material defaultMat = new Material(Shader.Find("Unlit/Color"));
            bool isSelected = piece.logicalSquare == pendingFrom;
            defaultMat.color = isSelected ? Color.green : Color.yellow;
            renderer.material = defaultMat;
        }

        return highlight;
    }

    void CreateTargetHighlight(Square target)
    {
        if (!gameManager.TryGetPiece(target, out ChessPiece targetPiece))
        {
            var snap = FindObjectOfType<SnapToBoardOnRelease>();
            if (snap?.board == null) return;

            GameObject targetHighlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetHighlight.name = "TargetHighlight";
            
            Vector3 pos = CalculateBoardPosition(snap.board, target);
            targetHighlight.transform.position = pos;
            targetHighlight.transform.rotation = Quaternion.LookRotation(snap.board.up);
            targetHighlight.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);

            Destroy(targetHighlight.GetComponent<Collider>());
            
            Renderer renderer = targetHighlight.GetComponent<Renderer>();
            Material mat = targetMaterial != null ? targetMaterial : new Material(Shader.Find("Unlit/Color"));
            if (targetMaterial == null) mat.color = Color.cyan;
            renderer.material = mat;

            highlightObjects[target] = targetHighlight;
        }
    }

    Vector3 CalculateBoardPosition(Transform board, Square square)
    {
        MeshRenderer mr = board.GetComponent<MeshRenderer>();
        Bounds bounds = mr != null ? mr.bounds : board.GetComponent<Collider>().bounds;
        Vector3 localSize = board.InverseTransformVector(bounds.size);
        float boardWidth = Mathf.Abs(localSize.x);
        float cellSize = boardWidth / 8f;
        float originX = -boardWidth * 0.5f;
        float originZ = -boardWidth * 0.5f;

        int c = square.File - 1;
        int r = square.Rank - 1;
        float cx = originX + (c + 0.5f) * cellSize;
        float cz = originZ + (r + 0.5f) * cellSize;

        return board.TransformPoint(new Vector3(cx, 0f, cz));
    }

    void ClearAllHighlights()
    {
        foreach (var kvp in highlightObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        highlightObjects.Clear();
    }

    void FinishProcessingAndRelisten()
    {
        DebugLog("[Voice] Finishing...");
        isProcessing = false;
        
        if (voiceService != null)
        {
            voiceService.Deactivate();
            DebugLog("[Voice] Deactivated");
        }
        
        StartListeningDelayed(relistenDelay);
    }

    void OnError(string error, string message)
    {
        Debug.LogWarning($"[Voice] ERROR: {error} - {message}");
        if (popup != null)
            popup.SetText("Voice error. Restarting...");
        
        isProcessing = false;
        StartListeningDelayed(1.0f);
    }

    void OnAborted()
    {
        DebugLog("[Voice] ABORTED");
        isProcessing = false;
        StartListeningDelayed(0.5f);
    }

    void OnStoppedListeningTimeout()
    {
        DebugLog("[Voice] Timeout");
        if (popup != null && !awaitingConfirmation)
            popup.SetText("Timeout. Try again");
        
        isProcessing = false;
        if (!awaitingConfirmation)
            StartListeningDelayed(0.5f);
    }

    void OnStoppedListeningInactivity()
    {
        DebugLog("[Voice] Inactivity");
        if (popup != null && !awaitingConfirmation)
            popup.SetText("Say: \"knight to f3\"");
        
        isProcessing = false;
        if (!awaitingConfirmation)
            StartListeningDelayed(0.5f);
    }

    void OnStoppedListeningDeactivation()
    {
        DebugLog("[Voice] Deactivation");
    }

    void OnRequestCompleted()
    {
        DebugLog("[Voice] Request completed");
    }

    bool TryParseMoveCommand(string input, out Square from, out Square to, out string error)
    {
        from = default;
        to = default;
        error = "Say: \"knight to f3\" or \"e2 to e4\"";

        string normalized = NormalizeInput(input);
        DebugLog($"[Parse] Normalized: '{normalized}'");

        if (normalized.Contains("say"))
        {
            int sayIndex = normalized.IndexOf("say");
            normalized = normalized.Substring(0, sayIndex).Trim();
            DebugLog($"[Parse] Removed 'say' prompt: '{normalized}'");
        }

        if (TryParsePieceMove(normalized, out from, out to))
        {
            DebugLog($"[Parse] Piece move: {from} → {to}");
            error = null;
            return true;
        }

        if (TryParseSquareToSquare(normalized, out from, out to))
        {
            DebugLog($"[Parse] Square move: {from} → {to}");
            error = null;
            return true;
        }

        DebugLog($"[Parse] Failed: '{input}'");
        return false;
    }

    bool TryParsePieceMove(string input, out Square from, out Square to)
    {
        from = default;
        to = default;

        string pieceType = ExtractPieceType(input);
        if (string.IsNullOrEmpty(pieceType))
            return false;

        Match destMatch = Regex.Match(input, @"[a-h]\s*[1-8]");
        if (!destMatch.Success)
            return false;

        string destStr = destMatch.Value.Replace(" ", "");
        to = new Square(destStr);

        List<Square> allCandidates = FindPiecesOfType(pieceType);

        if (allCandidates.Count == 0)
        {
            DebugLog($"[Parse] No {pieceType}");
            return false;
        }

        List<Square> validCandidates = FilterByLegalMove(allCandidates, to);

        if (validCandidates.Count == 0)
        {
            DebugLog($"[Parse] No {pieceType} can reach {to}");
            return false;
        }

        if (validCandidates.Count == 1)
        {
            from = validCandidates[0];
            DebugLog($"[Parse] Only one {pieceType} can reach: {from}");
            return true;
        }

        from = SelectBestPieceXR(validCandidates);
        DebugLog($"[Parse] Multiple {pieceType}, selected: {from}");

        RequestConfirmation(from, to, validCandidates);
        return true;
    }

    List<Square> FilterByLegalMove(List<Square> candidates, Square destination)
    {
        List<Square> valid = new List<Square>();

        foreach (Square candidate in candidates)
        {
            if (!gameManager.TryGetPiece(candidate, out ChessPiece piece))
                continue;

            Square originalSquare = piece.logicalSquare;
            bool canMove = piece.SetSquare(destination);

            if (canMove)
            {
                valid.Add(candidate);
                piece.SetSquare(originalSquare);
            }
        }

        return valid;
    }

    Square SelectBestPieceXR(List<Square> candidates)
    {
        if (xrCamera == null || candidates.Count == 0)
            return candidates[0];

        float bestScore = float.MinValue;
        Square bestSquare = candidates[0];

        foreach (Square sq in candidates)
        {
            if (!gameManager.TryGetPiece(sq, out ChessPiece piece))
                continue;

            float score = CalculatePieceScore(piece);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestSquare = sq;
            }
        }

        return bestSquare;
    }

    float CalculatePieceScore(ChessPiece piece)
    {
        float score = 0f;

        Vector3 piecePos = piece.transform.position;
        Vector3 camPos = xrCamera.transform.position;
        Vector3 camForward = xrCamera.transform.forward;

        Vector3 toPiece = (piecePos - camPos).normalized;
        float angle = Vector3.Angle(camForward, toPiece);

        if (angle < maxGazeAngle)
        {
            float gazeScore = (1f - angle / maxGazeAngle) * gazeWeight;
            score += gazeScore;
        }

        float distance = Vector3.Distance(camPos, piecePos);
        float proximityScore = (1f / (1f + distance)) * proximityWeight;
        score += proximityScore;

        return score;
    }

    bool TryParseSquareToSquare(string input, out Square from, out Square to)
    {
        from = default;
        to = default;

        MatchCollection matches = Regex.Matches(input, @"[a-h]\s*[1-8]");
        if (matches.Count < 2)
            return false;

        string fromStr = matches[0].Value.Replace(" ", "");
        string toStr = matches[1].Value.Replace(" ", "");

        from = new Square(fromStr);
        to = new Square(toStr);
        return true;
    }

    string ExtractPieceType(string input)
    {
        if (Regex.IsMatch(input, @"\b(pawn|pone|porn|pond)\b")) return "pawn";
        if (Regex.IsMatch(input, @"\b(knight|night|nite|knights|horse)\b")) return "knight";
        if (Regex.IsMatch(input, @"\b(bishop|bishops)\b")) return "bishop";
        if (Regex.IsMatch(input, @"\b(rook|rooks|castle|castles|rock)\b")) return "rook";
        if (Regex.IsMatch(input, @"\b(queen|queens)\b")) return "queen";
        if (Regex.IsMatch(input, @"\b(king|kings)\b")) return "king";
        return null;
    }

    List<Square> FindPiecesOfType(string pieceType)
    {
        List<Square> result = new List<Square>();

        if (gameManager?.chessGame == null)
            return result;

        if (!gameManager.chessGame.BoardTimeline.TryGetCurrent(out Board board))
            return result;

        for (int file = 1; file <= 8; file++)
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                Square sq = new Square(file, rank);
                Piece piece = board[sq];

                if (piece == null)
                    continue;

                if (!IsPieceOfType(piece, pieceType))
                    continue;

                result.Add(sq);
            }
        }

        return result;
    }

    bool IsPieceOfType(Piece piece, string typeName)
    {
        string pieceTypeLower = piece.GetType().Name.ToLower();
        return pieceTypeLower == typeName;
    }

    string NormalizeInput(string input)
    {
        string s = input.ToLowerInvariant().Trim();

        if (s.Contains("menu"))
        {
            return s;
        }

        s = s.Replace("-", " ");
        s = s.Replace(",", " ");
        s = s.Replace(".", " ");

        s = ReplaceWordBoundary(s, "one", "1");
        s = ReplaceWordBoundary(s, "two", "2");
        s = ReplaceWordBoundary(s, "three", "3");
        s = ReplaceWordBoundary(s, "four", "4");
        s = ReplaceWordBoundary(s, "for", "4");
        s = ReplaceWordBoundary(s, "five", "5");
        s = ReplaceWordBoundary(s, "six", "6");
        s = ReplaceWordBoundary(s, "seven", "7");
        s = ReplaceWordBoundary(s, "eight", "8");
        s = ReplaceWordBoundary(s, "ate", "8");

        s = Regex.Replace(s, @"\b(gee|ghee|g\.)\s*([1-8])", "g$2");
        s = Regex.Replace(s, @"\b(see|sea|c\.)\s*([1-8])", "c$2");
        s = Regex.Replace(s, @"\b(bee|be|b\.)\s*([1-8])", "b$2");
        s = Regex.Replace(s, @"\b(age|aitch|h\.)\s*([1-8])", "h$2");
        s = Regex.Replace(s, @"\b(eh|a\.)\s*([1-8])", "a$2");
        s = Regex.Replace(s, @"\b(dee|d\.)\s*([1-8])", "d$2");
        s = Regex.Replace(s, @"\b(ee|e\.)\s*([1-8])", "e$2");
        s = Regex.Replace(s, @"\b(ef|eff|f\.)\s*([1-8])", "f$2");

        s = Regex.Replace(s, @"\b(or|ore)\b", "");
        s = Regex.Replace(s, @"\bsay\b", "");
        
        s = Regex.Replace(s, @"\bmove\b", " ");
        s = Regex.Replace(s, @"\bfrom\b", " ");
        s = Regex.Replace(s, @"\bto\b", " ");
        s = Regex.Replace(s, @"\bon\b", " ");
        s = Regex.Replace(s, @"\bmy\b", " ");
        s = Regex.Replace(s, @"\byour\b", " ");
        s = Regex.Replace(s, @"\bthe\b", " ");
        s = Regex.Replace(s, @"\bpiece\b", " ");

        s = Regex.Replace(s, @"\s+", " ").Trim();

        return s;
    }

    string ReplaceWordBoundary(string input, string word, string replacement)
    {
        return Regex.Replace(input, $@"\b{word}\b", replacement);
    }

    void DebugLog(string message)
    {
        if (debugMode)
            Debug.Log(message);
    }
}