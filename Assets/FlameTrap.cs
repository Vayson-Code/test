using System.Collections;
using UnityEngine;

public class FlameTrap : MonoBehaviour
{
    [Header("Cycle")]
    public float activeTime = 2f;
    public float offTime = 5f;

    public float damagePerSecond = 20f;


    public GameObject vfxRoot; // drag your VFX object here (can be this gameObject)
    

    private bool isActive = true;

    void Start()
    {
        if (vfxRoot == null) vfxRoot = gameObject;
        StartCoroutine(Cycle());
    }

    IEnumerator Cycle()
    {
        while (true)
        {
            // ON
            isActive = true;
            vfxRoot.SetActive(true);
            yield return new WaitForSeconds(activeTime);

            // OFF
            isActive = false;
            vfxRoot.SetActive(false);
            yield return new WaitForSeconds(offTime);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!isActive) return;
        Debug.Log("Flame trap is active and something is in it.");
        if (other.CompareTag("Player"))
        {
            other.GetComponent<ThirdPersonController>().TakeDamage(1f * Time.deltaTime);

        }
    }
}