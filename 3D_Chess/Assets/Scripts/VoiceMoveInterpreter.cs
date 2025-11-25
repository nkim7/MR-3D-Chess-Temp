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

    [Header("Listening Settings")]
    [SerializeField] private float relistenDelay = 0.8f;
    [SerializeField] private float forceReactivateDelay = 2.0f;
    [SerializeField] private bool debugMode = true;

    private bool isProcessing = false;
    private int moveCount = 0;

    void Awake()
    {
        if (voiceService == null)
            voiceService = FindObjectOfType<AppVoiceExperience>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (popup == null)
            popup = FindObjectOfType<FloatingTranscriptPopup>();
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
        CancelInvoke();
    }

    void OnBoardPlaced()
    {
        DebugLog("[VoiceMoveInterpreter] Board placed. Starting voice recognition.");
        if (popup != null)
            popup.SetText("Say your move, e.g. \"knight to f3\" or \"e2 to e4\".");

        StartListeningDelayed(0.5f);
    }

    void StartListeningDelayed(float delay)
    {
        CancelInvoke(nameof(ActivateVoiceService));
        Invoke(nameof(ActivateVoiceService), delay);
        DebugLog($"[VoiceMoveInterpreter] Scheduled activation in {delay}s");
    }

    void ActivateVoiceService()
    {
        if (voiceService == null)
        {
            Debug.LogError("[VoiceMoveInterpreter] VoiceService is null!");
            return;
        }

        if (isProcessing)
        {
            DebugLog("[VoiceMoveInterpreter] Still processing, skipping activation");
            return;
        }

        DebugLog("[VoiceMoveInterpreter] >>> ACTIVATING VOICE SERVICE <<<");
        voiceService.Activate();
        
        // Safety: Force reactivate if nothing happens
        CancelInvoke(nameof(ForceReactivate));
        Invoke(nameof(ForceReactivate), forceReactivateDelay);
    }

    void ForceReactivate()
    {
        if (isProcessing)
        {
            DebugLog("[VoiceMoveInterpreter] Processing, skipping force reactivate");
            return;
        }

        DebugLog("[VoiceMoveInterpreter] !!! FORCE REACTIVATE (safety mechanism) !!!");
        voiceService.Deactivate();
        Invoke(nameof(ActivateVoiceService), 0.3f);
    }

    void OnPartialTranscription(string text)
    {
        DebugLog($"[VoiceMoveInterpreter] Partial: '{text}'");
        CancelInvoke(nameof(ForceReactivate)); // User is speaking
    }

    void OnFullTranscription(string text)
    {
        DebugLog($"[VoiceMoveInterpreter] ====== FULL TRANSCRIPTION ====== '{text}'");
        
        CancelInvoke(nameof(ForceReactivate));
        isProcessing = true;

        if (string.IsNullOrWhiteSpace(text))
        {
            if (popup != null)
                popup.SetText("I didn't hear anything. Try again.");
            FinishProcessingAndRelisten();
            return;
        }

        if (gameManager == null)
        {
            Debug.LogError("[VoiceMoveInterpreter] GameManager not found.");
            if (popup != null)
                popup.SetText("Game is not ready.");
            FinishProcessingAndRelisten();
            return;
        }

        // Try to parse the move
        if (!TryParseMoveCommand(text, out Square from, out Square to, out string parseError))
        {
            if (popup != null)
                popup.SetText($"Couldn't understand \"{text}\".\n{parseError}");
            FinishProcessingAndRelisten();
            return;
        }

        // Execute the move
        bool success = gameManager.TryMoveByVoice(from, to, out string error);

        if (!success)
        {
            DebugLog($"[VoiceMoveInterpreter] Move failed: {error}");
            if (popup != null)
                popup.SetText($"{error}\nTry again.");
            FinishProcessingAndRelisten();
            return;
        }

        // Success!
        moveCount++;
        DebugLog($"[VoiceMoveInterpreter] ✓ Move #{moveCount} success: {from} → {to}");
        if (popup != null)
            popup.SetText($"Moved {from} to {to}.\nSay next move.");

        FinishProcessingAndRelisten();
    }

    void FinishProcessingAndRelisten()
    {
        DebugLog("[VoiceMoveInterpreter] Finishing processing, scheduling relisten...");
        isProcessing = false;
        
        // Deactivate first
        if (voiceService != null)
        {
            voiceService.Deactivate();
            DebugLog("[VoiceMoveInterpreter] Deactivated voice service");
        }
        
        // Then reactivate after delay
        StartListeningDelayed(relistenDelay);
    }

    void OnError(string error, string message)
    {
        Debug.LogWarning($"[VoiceMoveInterpreter] *** ERROR: {error} - {message}");
        if (popup != null)
            popup.SetText("Voice error. Restarting...");
        
        isProcessing = false;
        StartListeningDelayed(1.0f);
    }

    void OnAborted()
    {
        DebugLog("[VoiceMoveInterpreter] *** ABORTED");
        isProcessing = false;
        StartListeningDelayed(0.5f);
    }

    void OnStoppedListeningTimeout()
    {
        DebugLog("[VoiceMoveInterpreter] *** Stopped listening (timeout)");
        if (popup != null)
            popup.SetText("I didn't catch that. Say your move.");
        
        isProcessing = false;
        StartListeningDelayed(0.5f);
    }

    void OnStoppedListeningInactivity()
    {
        DebugLog("[VoiceMoveInterpreter] *** Stopped listening (inactivity)");
        if (popup != null)
            popup.SetText("Say your move, e.g. \"knight to f3\".");
        
        isProcessing = false;
        StartListeningDelayed(0.5f);
    }

    void OnStoppedListeningDeactivation()
    {
        DebugLog("[VoiceMoveInterpreter] *** Stopped listening (deactivation)");
    }

    void OnRequestCompleted()
    {
        DebugLog("[VoiceMoveInterpreter] >>> OnRequestCompleted");
    }

    // ========== ENHANCED NLP PARSING ==========

    bool TryParseMoveCommand(string input, out Square from, out Square to, out string error)
    {
        from = default;
        to = default;
        error = "Say a move like \"knight to f3\" or \"e2 to e4\".";

        string normalized = NormalizeInput(input);
        DebugLog($"[Parse] Normalized: '{normalized}'");

        // Try piece name + destination format (e.g., "knight f3", "bishop to e5")
        if (TryParsePieceMove(normalized, out from, out to))
        {
            DebugLog($"[Parse] Parsed as piece move: {from} → {to}");
            error = null;
            return true;
        }

        // Try square to square format (e.g., "e2 e4", "e2 to e4")
        if (TryParseSquareToSquare(normalized, out from, out to))
        {
            DebugLog($"[Parse] Parsed as square-to-square: {from} → {to}");
            error = null;
            return true;
        }

        DebugLog($"[Parse] Failed to parse: '{input}'");
        return false;
    }

    bool TryParsePieceMove(string input, out Square from, out Square to)
    {
        from = default;
        to = default;

        // Extract piece type
        string pieceType = ExtractPieceType(input);
        if (string.IsNullOrEmpty(pieceType))
            return false;

        // Extract destination square
        Match destMatch = Regex.Match(input, @"[a-h]\s*[1-8]");
        if (!destMatch.Success)
            return false;

        string destStr = destMatch.Value.Replace(" ", "");
        to = new Square(destStr);

        // Find all pieces of this type that can move to destination
        List<Square> candidates = FindPiecesOfType(pieceType, to);

        if (candidates.Count == 0)
        {
            DebugLog($"[Parse] No {pieceType} found on board");
            return false;
        }

        if (candidates.Count == 1)
        {
            from = candidates[0];
            DebugLog($"[Parse] Found unique {pieceType} at {from} → {to}");
            return true;
        }

        // Multiple candidates - try to disambiguate by file or rank in input
        Match fileMatch = Regex.Match(input, @"\b[a-h]\b(?!\s*[1-8])");
        if (fileMatch.Success)
        {
            char file = fileMatch.Value[0];
            var filtered = candidates.Where(sq => sq.ToString()[0] == file).ToList();
            if (filtered.Count == 1)
            {
                from = filtered[0];
                DebugLog($"[Parse] Disambiguated by file: {from}");
                return true;
            }
        }

        // Try disambiguate by rank
        Match rankMatch = Regex.Match(input, @"(?<![a-h])\b[1-8]\b");
        if (rankMatch.Success)
        {
            char rank = rankMatch.Value[0];
            var filtered = candidates.Where(sq => sq.ToString()[1] == rank).ToList();
            if (filtered.Count == 1)
            {
                from = filtered[0];
                DebugLog($"[Parse] Disambiguated by rank: {from}");
                return true;
            }
        }

        // Default to first candidate
        from = candidates[0];
        DebugLog($"[Parse] Multiple {pieceType}s, using first: {from}");
        return true;
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
        if (Regex.IsMatch(input, @"\bpawn\b")) return "pawn";
        if (Regex.IsMatch(input, @"\bknight\b")) return "knight";
        if (Regex.IsMatch(input, @"\bbishop\b")) return "bishop";
        if (Regex.IsMatch(input, @"\brook\b")) return "rook";
        if (Regex.IsMatch(input, @"\bqueen\b")) return "queen";
        if (Regex.IsMatch(input, @"\bking\b")) return "king";
        return null;
    }

    List<Square> FindPiecesOfType(string pieceType, Square destination)
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

        // Replace common separators
        s = s.Replace("-", " to ");
        s = s.Replace(",", " ");
        s = s.Replace(".", " ");

        // Number words to digits
        s = ReplaceWordBoundary(s, "one", "1");
        s = ReplaceWordBoundary(s, "two", "2");
        s = ReplaceWordBoundary(s, "three", "3");
        s = ReplaceWordBoundary(s, "four", "4");
        s = ReplaceWordBoundary(s, "for", "4");
        s = ReplaceWordBoundary(s, "five", "5");
        s = ReplaceWordBoundary(s, "six", "6");
        s = ReplaceWordBoundary(s, "seven", "7");
        s = ReplaceWordBoundary(s, "eight", "8");

        // Common speech recognition errors
        s = Regex.Replace(s, @"\beat\b", "e");
        s = Regex.Replace(s, @"\bate\b", "8");
        s = Regex.Replace(s, @"\bsea\b", "c");
        s = Regex.Replace(s, @"\bbee\b", "b");
        s = Regex.Replace(s, @"\btoo\b", "2");

        // Remove filler words (but keep piece names!)
        s = Regex.Replace(s, @"\bmove\b", " ");
        s = Regex.Replace(s, @"\bfrom\b", " ");
        s = Regex.Replace(s, @"\bto\b", " ");
        s = Regex.Replace(s, @"\bon\b", " ");
        s = Regex.Replace(s, @"\bmy\b", " ");
        s = Regex.Replace(s, @"\byour\b", " ");
        s = Regex.Replace(s, @"\bthe\b", " ");
        s = Regex.Replace(s, @"\bpiece\b", " ");

        // Collapse multiple spaces
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