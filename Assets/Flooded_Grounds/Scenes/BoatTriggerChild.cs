using UnityEngine;

public class BoatTriggerChild : MonoBehaviour
{
    private BoatMovement boat;

    void Awake()
    {
        boat = GetComponentInParent<BoatMovement>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (boat != null)
        {
            boat.TriggerBoat(collision.gameObject);
        }
    }
}