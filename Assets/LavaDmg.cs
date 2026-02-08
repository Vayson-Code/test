using UnityEngine;

public class LavaDmg : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

        void OnTriggerStay(Collider other)
    {
        
        Debug.Log("player is in lava.");
        if (other.CompareTag("Player"))
        {
            other.GetComponent<ThirdPersonController>().TakeDamage(1f * Time.deltaTime);

        }
    }
}
