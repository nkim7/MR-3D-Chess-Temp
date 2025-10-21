using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    public GameObject handleA;
    public GameObject handleB;

    OVRSpatialAnchor anchorA;
    OVRSpatialAnchor anchorB;

    HandGrabInteractable grabbableA;
    HandGrabInteractable grabbableB;
    bool was_grabbed = false;

    float original_distance;
    Vector3 original_scale;

    // Start is called before the first frame update
    void Start()
    {
        anchorA = handleA.GetComponent<OVRSpatialAnchor>();
        anchorB = handleB.GetComponent<OVRSpatialAnchor>();

        grabbableA = handleA.GetComponent<HandGrabInteractable>();
        grabbableB = handleB.GetComponent<HandGrabInteractable>();

        original_scale = transform.localScale;
        original_distance = (handleA.transform.position - handleB.transform.position).magnitude;
    }

    // Update is called once per frame
    void Update()
    {
        if (handleA != null && handleB != null)
        {

            if (grabbableA.State == InteractableState.Select || grabbableB.State == InteractableState.Select) {
                PlaceChessBoard();

                was_grabbed = true;
                //anchorA.enabled = false;
                //anchorB.enabled = false;
            }
            else if (was_grabbed)
            {
                Debug.Log("Release");
                was_grabbed = false;
                //anchorA.enabled = true;
                //anchorB.enabled = true;
            }
        }
    }

    void PlaceChessBoard()
    {
        Vector3 a = handleA.transform.position;
        Vector3 b = handleB.transform.position;

        // Midpoint between anchors
        Vector3 center = (a + b) / 2f;

        // Direction from A to B
        Vector3 direction = (b - a).normalized;

        // Rotation aligning X-axis of the board with the anchor line
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.SetPositionAndRotation(center, rotation);

        transform.localScale = original_scale / original_distance * (handleA.transform.position - handleB.transform.position).magnitude; 
    }
}
