using UnityEngine;

public class RotatingTrap : MonoBehaviour
{
    [Header("Rotation")]
    public float maxAngle = 75f;
    public float speed = 2f;

    [Header("Start Offset")]
    [Tooltip("Starting angle offset in degrees (0, 75, 165, etc.)")]
    public float startOffsetAngle = 0f;

    void Update()
    {
        // Convert angle offset to radians for Sin()
        float offsetRad = startOffsetAngle * Mathf.Deg2Rad;

        float angle = Mathf.Sin(Time.time * speed + offsetRad) * maxAngle;
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
