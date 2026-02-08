using UnityEngine;
using System.Collections;

public class BoatMovement : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveOffset = new Vector3(-5f, 0f, 0f);
    public float moveDuration = 2f;
    public float waitBeforeReturn = 1f;

    private Vector3 startPosition;
    private bool isMoving;

    void Awake()
    {
        startPosition = transform.position;
    }

    // Called by child colliders
    public void TriggerBoat(GameObject other)
    {
        if (isMoving) return;
        if (!other.CompareTag("Player")) return;

        StartCoroutine(MoveBoat());
    }

    private IEnumerator MoveBoat()
    {
        isMoving = true;

        Vector3 targetPosition = startPosition + moveOffset;

        yield return MoveOverTime(startPosition, targetPosition, moveDuration);
        yield return new WaitForSeconds(waitBeforeReturn);
        yield return MoveOverTime(targetPosition, startPosition, moveDuration);

        isMoving = false;
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
    }
}