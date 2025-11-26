using UnityEngine;

public class FlyingPiecesOrbit : MonoBehaviour
{
    public float ringRadius = 1.4f;
    public float height = 0f;
    public float rotationSpeed = 15f;
    public float bobAmplitude = 0.1f;
    public float bobFrequency = 1.0f;
    public float pieceSpinSpeed = 25f;

    private Transform[] pieces;
    private float[] phaseOffsets;
    private Vector3[] basePositions;

    void Start()
    {
        int count = transform.childCount;
        pieces = new Transform[count];
        phaseOffsets = new float[count];
        basePositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            pieces[i] = child;

            float angle = (Mathf.PI * 2f) * (i / (float)count);
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * ringRadius,
                height,
                Mathf.Sin(angle) * ringRadius
            );

            child.localPosition = pos;
            child.LookAt(transform.position);
            child.rotation = Quaternion.Euler(0f, child.rotation.eulerAngles.y + 180f, 0f);

            basePositions[i] = pos;
            phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void LateUpdate()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

        float t = Time.time;
        float dt = Time.deltaTime;

        for (int i = 0; i < pieces.Length; i++)
        {
            Transform piece = pieces[i];
            if (piece == null) continue;

            float bob = Mathf.Sin(t * bobFrequency + phaseOffsets[i]) * bobAmplitude;
            Vector3 basePos = basePositions[i];
            piece.localPosition = new Vector3(basePos.x, basePos.y + bob, basePos.z);

            if (pieceSpinSpeed != 0f)
                piece.Rotate(0f, pieceSpinSpeed * dt, 0f, Space.Self);
        }
    }
}
