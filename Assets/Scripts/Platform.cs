using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public enum MoveDirection
    {
        UpDown,
        LeftRight,
        ForwardBackward
    }

    [Header("Activation")]
    public bool isActive = true;

    [Header("Movement")]
    public MoveDirection direction;
    public float distance = 3f;
    public float speed = 2f;

    private Vector3 startPos;
    private float moveTime = 0f;

    void Start()
    {
        startPos = transform.position;
    }

    void FixedUpdate()
    {
        if (!isActive)
            return;

        moveTime += Time.fixedDeltaTime;

        float movement = Mathf.PingPong(moveTime * speed, distance);

        switch (direction)
        {
            case MoveDirection.UpDown:
                transform.position = startPos + Vector3.up * movement;
                break;

            case MoveDirection.LeftRight:
                transform.position = startPos + Vector3.right * movement;
                break;

            case MoveDirection.ForwardBackward:
                transform.position = startPos + Vector3.forward * movement;
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isActive) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    public void Activate()
    {
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public void Toggle()
    {
        isActive = !isActive;
    }
}
