using Maskbound.Core;
using Maskbound.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 2f;
    
    [Header("Acceleration/Deceleration")]
    [SerializeField] private float accelerationTime = 0.3f;
    [SerializeField] private float decelerationTime = 0.2f;
    [SerializeField] private float directionChangeSharpness = 8f;
    
    [Header("Speed Bonus System")]
    [SerializeField] private float maxWalkSpeedBonus = 1f;
    [SerializeField] private float maxRunSpeedBonus = 5f;
    [SerializeField] private float maxSprintSpeedBonus = 4f;
    [SerializeField] private float bonusAccumulationTime = 3f;
    [SerializeField] private float bonusDecayTime = 1f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float airControlMultiplier = 0.5f;// 50% of ground speed while in air
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private CombatSystem combatSystem;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerSkillsManager playerSkillsManager;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Vector3 smoothMoveDirection;
    
    private float currentBaseSpeed;
    private float targetBaseSpeed;
    private float speedBonus;
    private float timeMoving;
    private float timeStopped;

    private bool isGrounded;
    private bool wasGrounded;
    private float lastGroundedTime;
    private float lastJumpPressTime;
    private bool jumpRequested;

    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;
    private int animIDMoveX;
    private int animIDMoveY;

    // Cached values
    private float cachedDeltaTime;
    private float cachedCameraYaw;
    private const float MIN_MOVEMENT_THRESHOLD = 0.01f;
    private const float INPUT_DEAD_ZONE = 0.01f;
    private const float RUN_THRESHOLD = 0.5f;
    private const float SPEED_SNAP_THRESHOLD = 0.01f;
    private const float DIRECTION_SNAP_THRESHOLD = 0.01f;
    private const float FALLING_THRESHOLD = -2f;
    private const float BONUS_LERP_SPEED = 5f;
    private const float DIRECTION_DECAY_SPEED = 10f;

    private InputAction sprintAction;

    public bool IsSprinting { get; private set; }
    public bool IsSliding { get; private set; }
    public bool InCombat => combatSystem != null && combatSystem.InCombat;

    private void Awake()
    {
        if (playerInput != null)
        {
            if (playerInput.actions == null)
            {
                Debug.LogError("PlayerInput component has no InputActionAsset assigned!");
            }
            else
            {
                playerInput.actions.Enable();
                sprintAction = playerInput.actions.FindAction("Sprint");
            }
        }
        else
        {
            Debug.LogError("PlayerInput component is missing on " + gameObject.name);
        }

        AssignAnimationIDs();
        
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        cachedDeltaTime = Time.deltaTime;
        
        CheckGrounded();
        HandleJumpBuffer();
        UpdateAnimations();
        
        // Update sprint state from input action
        if (sprintAction != null)
        {
            IsSprinting = sprintAction.IsPressed();
        }
    }

    private void FixedUpdate()
    {
        // Physics calculations should be in FixedUpdate
        HandleMovement();
        HandleGravity();
    }

    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("speed");
        animIDGrounded = Animator.StringToHash("IsGrounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        animIDMoveX = Animator.StringToHash("MoveX");
        animIDMoveY = Animator.StringToHash("MoveY");
    }

    private void CheckGrounded()
    {
        wasGrounded = isGrounded;
        
        // Use groundCheck position just like the working CharacterController version
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;
        
        // Use CheckSphere like the original working version instead of SphereCast
        isGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
        
        if (isGrounded)
        {
            if (!wasGrounded)
            {
                OnLanded();
            }
            lastGroundedTime = Time.time;
        }
    }

    private void HandleJumpBuffer()
    {
        if (!jumpRequested) return;

        if (Time.time - lastJumpPressTime <= jumpBufferTime)
        {
            if (Time.time - lastGroundedTime <= coyoteTime)
            {
                Jump();
            }
        }
        else
        {
            jumpRequested = false;
        }
    }

    private void HandleGravity()
    {
        if (!isGrounded)
        {
            // Apply custom gravity
            rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);
        }
        else
        {
            // When grounded, apply a small downward force to keep character stuck to ground
            // This helps on slopes and prevents bouncing
            if (rb.linearVelocity.y < 0)
            {
                rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);
            }
        }
    }

    private void HandleMovement()
    {
        if (combatSystem != null && combatSystem.IsInAction) return;

        float inputMagnitude = moveInput.magnitude;
        bool isMoving = inputMagnitude > INPUT_DEAD_ZONE;

        // Determine target speed
        if (isMoving)
        {
            targetBaseSpeed = IsSprinting ? sprintSpeed : 
                             (inputMagnitude > RUN_THRESHOLD ? runSpeed : walkSpeed);
            
            timeMoving += Time.fixedDeltaTime;
            timeStopped = 0f;
        }
        else
        {
            targetBaseSpeed = 0f;
            timeStopped += Time.fixedDeltaTime;
            timeMoving = 0f;
        }

        // Smooth speed changes
        float smoothingTime = isMoving ? accelerationTime : decelerationTime;
        float speedDiff = targetBaseSpeed - currentBaseSpeed;
        float smoothFactor = Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(smoothingTime, SPEED_SNAP_THRESHOLD));
        currentBaseSpeed += speedDiff * (1f - smoothFactor);

        if (Mathf.Abs(currentBaseSpeed) < SPEED_SNAP_THRESHOLD && !isMoving)
        {
            currentBaseSpeed = 0f;
        }

        // Calculate speed bonus
        float maxBonus = IsSprinting ? maxSprintSpeedBonus : 
                        (inputMagnitude > RUN_THRESHOLD ? maxRunSpeedBonus : 
                        (inputMagnitude > INPUT_DEAD_ZONE ? maxWalkSpeedBonus : 0f));

        if (isMoving)
        {
            float bonusProgress = Mathf.Clamp01(timeMoving / bonusAccumulationTime);
            bonusProgress = bonusProgress * bonusProgress * (3f - 2f * bonusProgress);
            float targetBonus = maxBonus * bonusProgress;
            speedBonus = Mathf.Lerp(speedBonus, targetBonus, Time.fixedDeltaTime * BONUS_LERP_SPEED);
        }
        else
        {
            float decayProgress = Mathf.Clamp01(timeStopped / bonusDecayTime);
            speedBonus = Mathf.Lerp(speedBonus, 0f, decayProgress);
        }

        // Calculate movement direction
        if (isMoving && cameraTransform != null)
        {
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            
            cachedCameraYaw = cameraTransform.eulerAngles.y;
            Vector3 targetMoveDirection = Quaternion.Euler(0f, cachedCameraYaw, 0f) * inputDirection;
            
            if (smoothMoveDirection.sqrMagnitude < DIRECTION_SNAP_THRESHOLD)
            {
                smoothMoveDirection = targetMoveDirection;
            }
            else
            {
                smoothMoveDirection = Vector3.Slerp(
                    smoothMoveDirection, 
                    targetMoveDirection, 
                    directionChangeSharpness * Time.fixedDeltaTime
                ).normalized;
            }

            moveDirection = smoothMoveDirection;

            // Rotate character
            if (moveInput.y >= 0f && moveDirection.sqrMagnitude > DIRECTION_SNAP_THRESHOLD)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                rb.MoveRotation(Quaternion.Slerp(
                    rb.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.fixedDeltaTime
                ));
            }
        }
        else if (!isMoving)
        {
            smoothMoveDirection = Vector3.Lerp(smoothMoveDirection, Vector3.zero, Time.fixedDeltaTime * DIRECTION_DECAY_SPEED);
            if (smoothMoveDirection.sqrMagnitude < DIRECTION_SNAP_THRESHOLD)
            {
                smoothMoveDirection = Vector3.zero;
                moveDirection = Vector3.zero;
            }
        }

        // Apply movement
        float totalSpeed = currentBaseSpeed + speedBonus;
        
        if (isGrounded)
        {
            // Simple ground movement
            Vector3 targetVelocity = moveDirection * totalSpeed;
            Vector3 currentVelocityXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 velocityChange = targetVelocity - currentVelocityXZ;
            
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        else
        {
            // Air control (reduced speed but same application method)
            Vector3 targetVelocity = moveDirection * totalSpeed * airControlMultiplier;
            Vector3 currentVelocityXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 velocityChange = targetVelocity - currentVelocityXZ;
            
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }

    private void Jump()
    {
        if (!isGrounded) return;

        // Calculate jump velocity
        float jumpVelocity = Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(gravity));

        // Reset Y velocity and apply jump
        Vector3 vel = rb.linearVelocity;
        vel.y = jumpVelocity;
        rb.linearVelocity = vel;
        

        if (animator != null)
        {
            animator.SetTrigger(animIDJump);
        }

        jumpRequested = false;
        isGrounded = false; // Immediately set to false to prevent double jumps
    }

    private void OnLanded()
    {
        if (animator != null)
        {
            animator.ResetTrigger(animIDJump);
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        float totalSpeed = currentBaseSpeed + speedBonus;
        
        animator.SetBool(animIDGrounded, isGrounded);
        animator.SetFloat(animIDSpeed, totalSpeed);
        animator.SetFloat(animIDMotionSpeed, 1f);

        if (totalSpeed > MIN_MOVEMENT_THRESHOLD)
        {
            Vector3 localMove = transform.InverseTransformDirection(moveDirection);
            animator.SetFloat(animIDMoveX, localMove.x * totalSpeed);
            animator.SetFloat(animIDMoveY, localMove.z * totalSpeed);
        }
        else
        {
            animator.SetFloat(animIDMoveX, 0f);
            animator.SetFloat(animIDMoveY, 0f);
        }

        animator.SetBool(animIDFreeFall, !isGrounded && rb.linearVelocity.y < FALLING_THRESHOLD);
    }

    #region Input Handlers
    
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            lastJumpPressTime = Time.time;
            jumpRequested = true;
        }
    }

    public void OnSprint(InputValue value)
    {
        IsSprinting = value.isPressed;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && combatSystem != null)
        {
            FindFirstObjectByType<Enemy>().TakeDamage(10); 
            combatSystem.PerformAttack();
        }
    }

    public void OnAbility(InputValue value)
    {
        if (value.isPressed && playerSkillsManager != null && playerSkillsManager.HasCurrentMaskAbility(0))
        {
           playerSkillsManager.GetSkillsArray()[0].ApplyEffect(gameObject);
        }
    }
    // Additional input handlers for skills can be added here, following the same pattern
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    #region Public API
    
    public void SetMovementEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void AddImpulse(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }

    public Vector3 GetMoveDirection() => moveDirection;
    public float GetCurrentSpeed() => currentBaseSpeed + speedBonus;
    public bool IsMoving() => moveInput.magnitude > 0.1f;
    
    #endregion
}
