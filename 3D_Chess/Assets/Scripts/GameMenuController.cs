using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenuController : MonoBehaviour
{
    public static GameMenuController Instance { get; private set; }

    [Header("Menu Root (CanvasRoot)")]
    [SerializeField] private GameObject menuRoot;

    [Header("Buttons (drag working buttons here)")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    [Header("Optional")]
    [SerializeField] private bool pauseGameTime = false;

    [Header("Menu Position")]
    [SerializeField] private float heightAboveBoard = 0.3f;
    [SerializeField] private bool anchorToBoard = true;

    private bool isOpen = false;

    private Camera mainCam;
    private RectTransform menuRectTransform;
    private Transform boardTransform;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCam = Camera.main ?? FindObjectOfType<Camera>();

        if (menuRoot == null)
        {
            Debug.LogError("[Menu] menuRoot is not assigned on GameMenuController.");
        }
        else
        {
            // We expect menuRoot to be CanvasRoot (has RectTransform)
            menuRectTransform = menuRoot.GetComponent<RectTransform>();
            if (menuRectTransform == null)
            {
                Debug.LogWarning("[Menu] menuRoot has no RectTransform; positioning may not work.");
            }

            // Start hidden
            menuRoot.SetActive(false);
        }

        // Wire buttons (no need to set OnClick in Inspector)
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeButton);
        else
            Debug.LogWarning("[Menu] Resume button is not assigned.");

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButton);
        else
            Debug.LogWarning("[Menu] Restart button is not assigned.");

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButton);
        else
            Debug.LogWarning("[Menu] Exit button is not assigned.");
    }

    private void Start()
    {
        FindChessBoard();
        SetMenuVisible(false);
    }

    private void FindChessBoard()
    {
        ChessBoard board = FindObjectOfType<ChessBoard>();
        if (board != null)
        {
            boardTransform = board.transform;
            Debug.Log("[Menu] Found chessboard for anchoring");
        }
        else
        {
            Debug.LogWarning("[Menu] Chessboard not found – menu will not anchor.");
        }
    }

    private void LateUpdate()
    {
        if (!isOpen || menuRectTransform == null)
            return;

        UpdateMenuPosition();
    }

    private void UpdateMenuPosition()
    {
        if (anchorToBoard && boardTransform != null)
        {
            // Place menu above board
            Vector3 boardCenter = boardTransform.position;
            Vector3 boardUp = boardTransform.up;

            menuRectTransform.position = boardCenter + boardUp * heightAboveBoard;

            // Rotate to face the camera while keeping "up" aligned with boardUp
            if (mainCam != null)
            {
                Vector3 dirToCamera = mainCam.transform.position - menuRectTransform.position;
                dirToCamera = Vector3.ProjectOnPlane(dirToCamera, boardUp);

                if (dirToCamera.sqrMagnitude > 0.001f)
                {
                    menuRectTransform.rotation =
                        Quaternion.LookRotation(-dirToCamera.normalized, boardUp);
                }
            }
        }
        else if (mainCam != null)
        {
            // Fallback: fixed distance in front of camera
            menuRectTransform.position =
                mainCam.transform.position + mainCam.transform.forward * 1.5f;
            menuRectTransform.rotation =
                Quaternion.LookRotation(mainCam.transform.forward, mainCam.transform.up);
        }
    }

    // ------------ Public API (for voice etc.) ------------

    public void ShowMenu()
    {
        Debug.Log("[Menu] ShowMenu");
        SetMenuVisible(true);
    }

    public void HideMenu()
    {
        Debug.Log("[Menu] HideMenu");
        SetMenuVisible(false);
    }

    public void ToggleMenu()
    {
        Debug.Log("[Menu] ToggleMenu");
        SetMenuVisible(!isOpen);
    }

    // ------------ Button callbacks ------------

    private void OnResumeButton()
    {
        Debug.Log("[Menu] Resume pressed");
        HideMenu();

        var voice = FindObjectOfType<VoiceMoveInterpreter>();
        if (voice != null)
        {
            voice.ResumeVoiceListening();
        }
    }

    private void OnRestartButton()
    {
        Debug.Log("[Menu] Restart pressed");

        if (pauseGameTime)
            Time.timeScale = 1f;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    private void OnExitButton()
    {
        Debug.Log("[Menu] Exit pressed");

        if (pauseGameTime)
            Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------ Internal ------------

    private void SetMenuVisible(bool visible)
    {
        isOpen = visible;

        if (menuRoot != null)
            menuRoot.SetActive(visible);

        if (pauseGameTime)
            Time.timeScale = visible ? 0f : 1f;

        Debug.Log("[Menu] " + (visible ? "OPEN" : "CLOSED"));
    }
}
