using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Android; // for permission checks

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

    List<NativeArray<Vector3>> vertices;
    List<NativeArray<int>> indices;
    bool scene_layout_loaded = false;

    void set_handle_active(bool active)
    {
        handleA.SetActive(active);
        handleB.SetActive(active);
    }

    // Start is called before the first frame update
    void Start()
    {
        anchorA = handleA.GetComponent<OVRSpatialAnchor>();
        anchorB = handleB.GetComponent<OVRSpatialAnchor>();

        set_handle_active(false);

        grabbableA = handleA.GetComponent<HandGrabInteractable>();
        grabbableB = handleB.GetComponent<HandGrabInteractable>();



        original_scale = transform.localScale;
        original_distance = (handleA.transform.position - handleB.transform.position).magnitude;

        vertices = new List<NativeArray<Vector3>>();
        indices = new List<NativeArray<int>>();

        get_layout();
    }

    async void get_layout()
    {
        bool failed = false;
        
        if (Permission.HasUserAuthorizedPermission("com.oculus.permission.USE_SCENE"))
            Debug.LogWarning("Scene permissions granted");
        else
        {

        }

        var anchors = new List<OVRAnchor>();
        var result = await OVRAnchor.FetchAnchorsAsync(anchors, new OVRAnchor.FetchOptions
        {
            ComponentTypes = new List<System.Type>() { typeof(OVRSemanticLabels), typeof(OVRTriangleMesh) },
        });

        if (!result.Success || anchors.Count == 0)
        {
            // FAILS ON DEVICE
            Debug.LogError("Failed to locate room anchors");
            failed = true;
        }

        // find triangle meshes of table's and floor

        foreach (var room in anchors)
        {
            OVRSemanticLabels labels;
            if (!room.TryGetComponent<OVRSemanticLabels>(out labels))
            {
                Debug.LogError("Failed to get semantic labels");
                failed = true;
            }

            HashSet<OVRSemanticLabels.Classification> classifications = new HashSet<OVRSemanticLabels.Classification>();

            labels.GetClassifications(classifications);

            if (classifications.Contains(OVRSemanticLabels.Classification.Table) || classifications.Contains(OVRSemanticLabels.Classification.Floor))
            {
                OVRTriangleMesh mesh;

                if (!room.TryGetComponent<OVRTriangleMesh>(out mesh))
                {
                    Debug.LogError("Failed to get triangle mesh");
                    failed = true;
                }


                int vert_count;
                int tri_count;

                if (!mesh.TryGetCounts(out vert_count, out tri_count))
                    Debug.LogError("Failed to get mesh count");

                NativeArray<Vector3> vert = new NativeArray<Vector3>(vert_count, Allocator.Persistent); // needs to persist for long time
                NativeArray<int> ind = new NativeArray<int>(tri_count * 3, Allocator.Persistent);
                if (!mesh.TryGetMesh(vert, ind))
                {
                    Debug.LogError("Failed to load mesh of " + string.Concat(classifications));
                    failed = true;
                }
                vertices.Add(vert);
                indices.Add(ind);
            }
        }

        //if (!failed) { 
            scene_layout_loaded = true;
            set_handle_active(true);
        //}
    }

    Vector3 get_closest_valid_point(Vector3 p)
    {
        return p;
    }

    // https://github.com/RenderKit/embree/blob/master/tutorials/common/math/closest_point.h
    Vector3 closestPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0.0f && d2 <= 0.0f) return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0.0f && d4 <= d3) return b;

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0.0f && d5 <= d6) return c;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
        {
            float v = d1 / (d1 - d3);
            return a + v* ab;
        }

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
        {
            float v = d2 / (d2 - d6);
            return a + v * ac;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
        {
            float v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + v * (c - b);
        }

        { 
            float denom = 1.0f / (va + vb + vc);
            float v = vb * denom;
            float w = vc * denom;
            return a + v * ab + w * ac;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!scene_layout_loaded) {
            return;
        }

        if (handleA != null && handleB != null)
        {
            PlaceChessBoard();

            if (grabbableA.State == InteractableState.Select || grabbableB.State == InteractableState.Select) {
                was_grabbed = true;

            }
            else if (was_grabbed)
            {
                Debug.Log("Release");
                was_grabbed = false;

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
