using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Android;
using Meta.XR.MRUtilityKit;
using System; // for Action

public class ChessBoard : MonoBehaviour
{
    public GameObject handleA;
    public GameObject handleB;

    public bool do_snap_down = true;

    OVRSpatialAnchor anchorA;
    OVRSpatialAnchor anchorB;

    HandGrabInteractable grabbableA;
    HandGrabInteractable grabbableB;
    bool was_grabbedA = false;
    bool was_grabbedB = false;

    float original_distance;
    Vector3 original_scale;

    bool update_chessboard_pos = true;

    // Raised once when board placement is finished
    public static event Action BoardPlaced;

    void set_handle_active(bool active)
    {
        handleA.SetActive(active);
        handleB.SetActive(active);
    }

    void set_pieces_grabbable(bool grab_enabled)
    {
        foreach (HandGrabInteractable grab in gameObject.GetComponentsInChildren<HandGrabInteractable>())
        {
            grab.enabled = grab_enabled;
        }
    }

    void Start()
    {
        anchorA = handleA.GetComponent<OVRSpatialAnchor>();
        anchorB = handleB.GetComponent<OVRSpatialAnchor>();

        grabbableA = handleA.GetComponent<HandGrabInteractable>();
        grabbableB = handleB.GetComponent<HandGrabInteractable>();

        original_scale = transform.localScale;
        original_distance = (handleA.transform.position - handleB.transform.position).magnitude;

        set_handle_active(true);
        set_pieces_grabbable(false);
        PlaceChessBoard();
    }

    void Update()
    {
        if (handleA != null && handleB != null && update_chessboard_pos)
        {
            if (grabbableA.State == InteractableState.Select || grabbableB.State == InteractableState.Select)
            {
                PlaceChessBoard();

                if (grabbableA.State == InteractableState.Select)
                    was_grabbedA = true;
                else
                    was_grabbedB = true;
            }
            else if (was_grabbedA || was_grabbedB)
            {
                if (do_snap_down)
                {
                    if (was_grabbedA && grabbableA.State != InteractableState.Select)
                    {
                        handleA.transform.position = snap_down(handleA.transform.position);
                    }

                    if (was_grabbedB && grabbableB.State != InteractableState.Select)
                    {
                        handleB.transform.position = snap_down(handleB.transform.position);
                    }

                    if (!was_grabbedA)
                    {
                        Vector3 new_pos = handleA.transform.position;
                        new_pos.y = handleB.transform.position.y;
                        handleA.transform.position = new_pos;
                    }

                    if (!was_grabbedB)
                    {
                        Vector3 new_pos = handleB.transform.position;
                        new_pos.y = handleA.transform.position.y;
                        handleB.transform.position = new_pos;
                    }
                }
                PlaceChessBoard();

                Debug.Log("Release");
                if (was_grabbedA && grabbableA.State != InteractableState.Select)
                {
                    Debug.LogWarning("Release A");
                    was_grabbedA = false;
                }
                if (was_grabbedB && grabbableB.State != InteractableState.Select)
                {
                    Debug.LogWarning("Release B");
                    was_grabbedB = false;
                }
            }
        }
    }

    Vector3 snap_down(Vector3 pos)
    {
        Ray down = new Ray(pos, Vector3.down);

        var hit = new RaycastHit();
        MRUKAnchor anchorHit = null;
        MRUK.Instance?.GetCurrentRoom()?.Raycast(down, Mathf.Infinity, out hit, out anchorHit);

        bool isCeiling = Vector3.Dot(hit.normal, Vector3.down) > 0.7f;
        if (isCeiling)
        {
            Debug.LogError("Hit ceiling");
            return pos;
        }

        return hit.point;
    }

    void PlaceChessBoard()
    {
        Vector3 a = handleA.transform.position;
        Vector3 b = handleB.transform.position;

        Vector3 center = (a + b) / 2f;
        Vector3 direction = (b - a).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.SetPositionAndRotation(center, rotation);
        transform.localScale = original_scale / original_distance * (handleA.transform.position - handleB.transform.position).magnitude;
    }

    public void finish_placement()
    {
        anchorA.gameObject.SetActive(false);
        anchorB.gameObject.SetActive(false);
        update_chessboard_pos = false;
        set_pieces_grabbable(true);

        Debug.Log("[ChessBoard] Board placement finished.");
        BoardPlaced?.Invoke();
    }
}
