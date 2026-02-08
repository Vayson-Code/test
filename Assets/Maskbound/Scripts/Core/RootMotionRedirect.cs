using UnityEngine;

public class RootMotionRedirect : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Glisse ici le Rigidbody du Parent (le Player)")]
    [SerializeField] private Rigidbody parentRigidbody;
    [SerializeField] private Animator _animator;

    

    // Cette fonction magique est appelée automatiquement par Unity
    // AVANT d'appliquer le Root Motion standard.
    void OnAnimatorMove()
    {
        // 1. On vérifie qu'on a bien les composants nécessaires
        if (_animator == null || parentRigidbody == null) return;

        // 2. On vérifie si on est au sol ou si l'animation doit contrôler le mouvement
        // (Tu peux ajouter des conditions ici, par ex: if(!isGrounded) return;)

        // 3. On applique le mouvement de l'animation au RIGIDBODY du PARENT
        // animator.deltaPosition est le mouvement que l'animation VOUDRAIT faire cette frame
        Vector3 newPosition = parentRigidbody.position + _animator.deltaPosition;
        parentRigidbody.MovePosition(newPosition);

        // 4. On applique aussi la rotation (pour ton attaque spin !)
        Quaternion newRotation = parentRigidbody.rotation * _animator.deltaRotation;
        parentRigidbody.MoveRotation(newRotation);
    }
}