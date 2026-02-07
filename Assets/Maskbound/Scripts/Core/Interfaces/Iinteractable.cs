using UnityEngine;

namespace Maskbound.Scripts.Core.Interfaces
{
    // Interface for objects that can be interacted with by the player or other systems
    public interface Iinteractable
    {
        // Called when an interaction is triggered
        void Interact(GameObject interactor);
    }
}