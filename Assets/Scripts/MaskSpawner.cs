using UnityEngine;

public class MaskSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject maskPrefab;
    public Transform spawnPoint;
    public float spawnEvery = 1.2f;

    [Header("Direction")]
    public Vector3 moveDirection = Vector3.forward; // set in inspector

    [Header("Mask Settings")]
    public float maskSpeed = 6f;
    public float maskLifeTime = 6f;

    private float timer;

    void Update()
    {
        if (maskPrefab == null || spawnPoint == null)
            return;

        timer += Time.deltaTime;
        if (timer >= spawnEvery)
        {
            timer = 0f;

            GameObject m = Instantiate(maskPrefab, spawnPoint.position, Quaternion.identity);

Vector3 dir = moveDirection.normalized;
if (dir.sqrMagnitude < 0.001f)
    dir = Vector3.forward;

// set direction for movement WITHOUT using forward
Mask mask = m.GetComponent<Mask>();
if (mask != null)
{
    mask.moveDir = dir;
    mask.speed = maskSpeed;
    mask.lifeTime = maskLifeTime;
}

// now you can rotate the object visually and it will NOT affect movement
m.transform.eulerAngles = new Vector3(-90f, m.transform.eulerAngles.y, 90f);

        }
    }
}
