using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Oculus.Voice;
using Meta.WitAi;
using Meta.WitAi.Events;

public class FloatingTranscriptPopup : MonoBehaviour
{
    [Header("Voice SDK")]
    [SerializeField] private AppVoiceExperience voiceService;

    [Header("Camera")]
    [SerializeField] private Camera cameraOverride;

    [Header("Popup layout")]
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.2f, 0f);
    [SerializeField] private Vector2 panelSize = new Vector2(450f, 180f);

    private Camera mainCam;
    private Canvas popupCanvas;
    private RectTransform popupRect;
    private TextMeshProUGUI transcriptText;
    private bool popupCreated;

    void Awake()
    {
        if (cameraOverride != null)
            mainCam = cameraOverride;
        else
            mainCam = Camera.main;

        if (mainCam == null)
        {
            Debug.LogError("[FloatingTranscriptPopup] Main Camera not found.");
        }

        if (voiceService == null)
            voiceService = FindObjectOfType<AppVoiceExperience>();

        if (voiceService == null)
            Debug.LogError("[FloatingTranscriptPopup] AppVoiceExperience not found.");
    }

    void OnEnable()
    {
        ChessBoard.BoardPlaced += OnBoardPlaced;
        RegisterVoiceEvents(true);
    }

    void OnDisable()
    {
        ChessBoard.BoardPlaced -= OnBoardPlaced;
        RegisterVoiceEvents(false);
    }

    void LateUpdate()
    {
        UpdatePopupTransform();
    }

    void OnBoardPlaced()
    {
        if (!popupCreated)
        {
            CreatePopupUI();
            popupCreated = true;
        }

        SetText("Say your move, e.g. \"e2 to e4\".");
    }

    void CreatePopupUI()
    {
        GameObject canvasGO = new GameObject("FloatingTranscriptCanvas");
        popupCanvas = canvasGO.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.WorldSpace;
        popupCanvas.worldCamera = mainCam;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGO.AddComponent<GraphicRaycaster>();

        popupRect = canvasGO.GetComponent<RectTransform>();
        popupRect.SetParent(transform, false);
        popupRect.sizeDelta = panelSize;
        popupRect.localScale = Vector3.one * 0.0025f;

        GameObject textGO = new GameObject("TranscriptText");
        textGO.transform.SetParent(canvasGO.transform, false);

        transcriptText = textGO.AddComponent<TextMeshProUGUI>();
        transcriptText.text = "";
        transcriptText.fontSize = 32f;
        transcriptText.enableWordWrapping = true;
        transcriptText.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform textRect = transcriptText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 20f);
        textRect.offsetMax = new Vector2(-20f, -20f);
    }

    void UpdatePopupTransform()
    {
        if (mainCam == null || popupRect == null) return;

        Vector3 basePos = mainCam.transform.position + mainCam.transform.forward * distanceFromCamera;
        Vector3 offsetWorld = mainCam.transform.TransformVector(localOffset);

        popupRect.position = basePos + offsetWorld;

        Vector3 lookDir = popupRect.position - mainCam.transform.position;
        if (lookDir.sqrMagnitude > 0.0001f)
            popupRect.rotation = Quaternion.LookRotation(lookDir, mainCam.transform.up);
    }

    void RegisterVoiceEvents(bool subscribe)
    {
        if (voiceService == null || voiceService.VoiceEvents == null)
        {
            Debug.LogWarning("[FloatingTranscriptPopup] VoiceEvents missing.");
            return;
        }

        VoiceEvents events = voiceService.VoiceEvents;

        if (subscribe)
        {
            events.OnPartialTranscription.AddListener(OnPartialTranscription);
            events.OnFullTranscription.AddListener(OnFullTranscription);
        }
        else
        {
            events.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            events.OnFullTranscription.RemoveListener(OnFullTranscription);
        }
    }

    void OnPartialTranscription(string text)
    {
        Debug.Log("[FloatingTranscriptPopup] Partial: " + text);
        if (transcriptText != null)
            transcriptText.text = text;
    }

    void OnFullTranscription(string text)
    {
        Debug.Log("[FloatingTranscriptPopup] Full: " + text);
        // Final user-facing message is handled by VoiceMoveInterpreter.
    }

    public void ClearText()
    {
        if (transcriptText != null)
            transcriptText.text = string.Empty;
    }

    public void SetText(string text)
    {
        if (transcriptText != null)
            transcriptText.text = text;
    }
}
