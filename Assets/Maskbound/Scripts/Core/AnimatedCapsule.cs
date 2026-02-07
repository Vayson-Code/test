using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class AnimatedCapsule : MonoBehaviour
{
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Animator animator;

    private float initialHeight; 
    private Vector3 initialCenter;

    void Awake()
    {

        initialHeight = capsule.height;
        initialCenter = capsule.center;
    }

    void FixedUpdate()
    {
        bool isGrounded = animator.GetBool("IsGrounded");

        if (isGrounded)
        {
            capsule.height = initialHeight;
            capsule.center = initialCenter;
            return;
        }

        float h = animator.GetFloat("capsuleHeight");
        float c = animator.GetFloat("capsuleCenterY");
        
        // Debug to see what you're actually getting
        Debug.Log($"Curve value: {h}, Radius: {capsule.radius}");

        // Clamp to minimum valid capsule height (must be at least 2x radius)
        float minHeight = capsule.radius * 2f;
        capsule.height = Mathf.Max(initialHeight * h, minHeight);
    
        capsule.center = new Vector3(
            initialCenter.x,
            initialCenter.y * c,
            initialCenter.z
        );
    }
}