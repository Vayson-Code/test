using UnityEngine;

public class RockMovement : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveDirection = Vector3.right;
    public float distance = 5f;

    [Header("Speed")]
    public float forwardSpeed = 2f;   // mid-range speed
    public float returnSpeed = 6f;    // fast return speed

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool goingForward = true;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + moveDirection.normalized * distance;
    }

    void Update()
    {
        float speed = goingForward ? forwardSpeed : returnSpeed;

        transform.position = Vector3.MoveTowards(
            transform.position,
            goingForward ? targetPosition : startPosition,
            speed * Time.deltaTime
        );

        // Switch direction when destination reached
        if (Vector3.Distance(transform.position,
                goingForward ? targetPosition : startPosition) < 0.01f)
        {
            goingForward = !goingForward;
        }
    }
}