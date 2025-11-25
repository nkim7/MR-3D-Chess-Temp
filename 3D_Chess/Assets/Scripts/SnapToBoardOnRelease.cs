using System.Collections;
using System.Reflection;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityChess;

[RequireComponent(typeof(Rigidbody))]
public class SnapToBoardOnRelease : MonoBehaviour
{
    [Header("Board Setup")]
    public Transform board;
    public int rows = 8;
    public int cols = 8;

    [Header("Snap Options")]
    public float heightOffset = 0;
    public bool smoothSnap = true;
    public float snapSpeed = 10f;

    private Rigidbody rb;
    private Grabbable grabbable;
    private HandGrabInteractable handGrabInteractable;
    private bool wasGrabbed;

    private float boardWidth;
    private float boardHeight;
    private float cellSize;

    private ChessPiece chessPiece;
    private Square currSquare;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        handGrabInteractable = GetComponent<HandGrabInteractable>();
        chessPiece = GetComponent<ChessPiece>();

        if (chessPiece != null)
        {
            int file = chessPiece.startFile;
            int rank = chessPiece.startRank;
            currSquare = new Square(file, rank);
        }

        if (board == null)
        {
            Debug.LogError("Board reference is not set on SnapToBoardOnRelease.");
        }
        else
        {
            ComputeBoardSize();
        }
    }

    void ComputeBoardSize()
    {
        MeshRenderer mr = board.GetComponent<MeshRenderer>();
        Collider col = board.GetComponent<Collider>();
        Bounds bounds;

        if (mr != null)
            bounds = mr.bounds;
        else if (col != null)
            bounds = col.bounds;
        else
            bounds = new Bounds(Vector3.zero, new Vector3(1, 0, 1));

        Vector3 localSize = board.InverseTransformVector(bounds.size);
        boardWidth = Mathf.Abs(localSize.x);
        boardHeight = Mathf.Abs(localSize.z);

        cellSize = boardWidth / cols;
    }

    void Update()
    {
        if (chessPiece != null)
        {
            int file = chessPiece.logicalSquare.File;
            int rank = chessPiece.logicalSquare.Rank;
            currSquare = new Square(file, rank);
        }

        bool grabbedNow = IsGrabbedOculus();
        if (wasGrabbed && !grabbedNow)
            SnapToNearestCell();

        wasGrabbed = grabbedNow;
    }

    bool IsGrabbedOculus()
    {
        if (handGrabInteractable != null)
        {
            if (TryGetInt(handGrabInteractable, "SelectingPointsCount", out int nHG))
                return nHG > 0;
        }

        if (grabbable != null)
        {
            if (TryGetInt(grabbable, "SelectingPointsCount", out int nG))
                return nG > 0;
        }

        if (handGrabInteractable != null && TryGetInt(handGrabInteractable, "SelectingInteractorsCount", out int nHG2))
            return nHG2 > 0;

        if (grabbable != null && TryGetInt(grabbable, "SelectingInteractorsCount", out int nG2))
            return nG2 > 0;

        return false;
    }

    static bool TryGetInt(object obj, string name, out int value)
    {
        value = 0;
        if (obj == null) return false;

        var t = obj.GetType();
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(int))
        {
            value = (int)p.GetValue(obj);
            return true;
        }

        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(int))
        {
            value = (int)f.GetValue(obj);
            return true;
        }

        return false;
    }

    void SnapToNearestCell()
    {
        if (board == null) return;

        Vector3 up = board.up;
        Plane plane = new Plane(up, board.position);

        Ray ray = new Ray(transform.position + up * 5f, -up);
        if (!plane.Raycast(ray, out float enter)) return;
        Vector3 projected = ray.GetPoint(enter);

        Vector3 local = board.InverseTransformPoint(projected);

        float originX = -boardWidth * 0.5f;
        float originZ = -boardHeight * 0.5f;

        int c = Mathf.Clamp(Mathf.FloorToInt((local.x - originX) / cellSize), 0, cols - 1);
        int r = Mathf.Clamp(Mathf.FloorToInt((local.z - originZ) / cellSize), 0, rows - 1);

        if (chessPiece != null)
        {
            if (currSquare == new Square(c + 1, r + 1))
            {
                c = currSquare.File - 1;
                r = currSquare.Rank - 1;
                Debug.Log($"Piece already on square. file: {c + 1}, rank: {r + 1}");
            }
            else
            {
                Debug.Log($"Piece to move to square with file: {c + 1}, rank: {r + 1}");
                bool tryLogicalMove = chessPiece.SetSquare(new Square(c + 1, r + 1));
                if (!tryLogicalMove)
                {
                    Debug.Log($"Invalid move, not snapping piece. file: {c + 1}, rank: {r + 1}");
                    c = currSquare.File - 1;
                    r = currSquare.Rank - 1;
                }
            }
        }
        else
        {
            Debug.Log("chessPiece is null");
        }

        float cx = originX + (c + 0.5f) * cellSize;
        float cz = originZ + (r + 0.5f) * cellSize;

        Vector3 worldTarget = board.TransformPoint(new Vector3(cx, 0f, cz)) + up * heightOffset;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (!smoothSnap)
        {
            transform.position = worldTarget;
            transform.rotation = Quaternion.LookRotation(board.forward, board.up);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(SmoothMove(worldTarget));
        }

        currSquare = new Square(c + 1, r + 1);
    }

    public void MoveToSquareInstant(Square targetSquare)
    {
        if (board == null)
        {
            Debug.LogError("Board reference missing on SnapToBoardOnRelease.");
            return;
        }

        Vector3 up = board.up;

        float originX = -boardWidth * 0.5f;
        float originZ = -boardHeight * 0.5f;

        int c = Mathf.Clamp(targetSquare.File - 1, 0, cols - 1);
        int r = Mathf.Clamp(targetSquare.Rank - 1, 0, rows - 1);

        float cx = originX + (c + 0.5f) * cellSize;
        float cz = originZ + (r + 0.5f) * cellSize;

        Vector3 worldTarget = board.TransformPoint(new Vector3(cx, 0f, cz)) + up * heightOffset;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (!smoothSnap)
        {
            transform.position = worldTarget;
            transform.rotation = Quaternion.LookRotation(board.forward, board.up);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(SmoothMove(worldTarget));
        }

        currSquare = targetSquare;
    }

    IEnumerator SmoothMove(Vector3 target)
    {
        while ((transform.position - target).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * snapSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(board.forward, board.up), Time.deltaTime * snapSpeed);
            yield return null;
        }

        transform.position = target;
        transform.rotation = Quaternion.LookRotation(board.forward, board.up);
    }
}
