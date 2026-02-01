using Maskbound.Core;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
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
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Dynamic Collider (Jump Posture Adjustment)")]
    [SerializeField] private bool useHeadFeetForCollider = true;
    [SerializeField] private Transform headPoint;
    [SerializeField] private Transform leftFeetPoint;
    [SerializeField] private Transform rightFeetPoint;
    [SerializeField] private float minColliderHeight = 0.9f;
    [SerializeField] private float maxColliderHeight = 2.0f;
    [SerializeField] private float colliderAdjustSpeed = 8f;
    [SerializeField] private float colliderCenterAdjustSpeed = 8f;
    [SerializeField] private float shrinkPadding = 0.05f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    [SerializeField] private CombatSystem combatSystem;
    [SerializeField] private MaskManager maskManager;
    [SerializeField] private PlayerInput playerInput;

    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Vector3 smoothMoveDirection;
    private Vector3 velocity;
    
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

    // Collider adjustment caching
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    // Cached values to reduce per-frame allocations and calculations
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
                // Cache sprint action reference for performance
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

        // Cache original collider dimensions for dynamic adjustment
        if (characterController != null)
        {
            originalColliderHeight = characterController.height;
            originalColliderCenter = characterController.center;
            // Ensure max doesn't go below original
            maxColliderHeight = Mathf.Max(maxColliderHeight, originalColliderHeight);
        }
    }

    private void Update()
    {
        // Cache deltaTime once per frame
        cachedDeltaTime = Time.deltaTime;
        
        CheckGrounded();
        HandleJumpBuffer();
        HandleGravity();
        HandleMovement();
        UpdateAnimations();
        UpdateColliderForPosture();
    }

    // Poll sprint state after all input callbacks have fired
    // This ensures we catch the release even if OnSprint callback fails
    private void LateUpdate()
    {
        if (sprintAction != null)
        {
            IsSprinting = sprintAction.IsPressed();
        }
    }

    // Pre-cache animation parameter hashes to avoid string lookups
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
        Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;
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

    // Jump buffering allows jump input slightly before landing
    // Coyote time allows jump slightly after leaving ground
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
        // ALWAYS apply gravity unless we're grounded AND moving downward
        // This ensures gravity works even immediately after jumping
        if (isGrounded && velocity.y <= 0f)
        {
            velocity.y = groundedGravity;
        }
        else
        {
            // Apply gravity continuously when in air OR when jumping upward
            velocity.y += gravity * cachedDeltaTime;
            
            // Clamp to terminal velocity
            if (velocity.y < gravity)
            {
                velocity.y = gravity;
            }
        }
    }

    private void HandleMovement()
    {
        if (combatSystem != null && combatSystem.IsInAction) return;

        float inputMagnitude = moveInput.magnitude;
        bool isMoving = inputMagnitude > INPUT_DEAD_ZONE;

        // Determine target speed based on input state
        if (isMoving)
        {
            targetBaseSpeed = IsSprinting ? sprintSpeed : 
                             (inputMagnitude > RUN_THRESHOLD ? runSpeed : walkSpeed);
            
            timeMoving += cachedDeltaTime;
            timeStopped = 0f;
        }
        else
        {
            targetBaseSpeed = 0f;
            timeStopped += cachedDeltaTime;
            timeMoving = 0f;
        }

        // Exponential smoothing creates natural acceleration curves
        // Speed changes quickly at first, then gradually slows as it approaches target
        float smoothingTime = isMoving ? accelerationTime : decelerationTime;
        float speedDiff = targetBaseSpeed - currentBaseSpeed;
        float smoothFactor = Mathf.Exp(-cachedDeltaTime / Mathf.Max(smoothingTime, SPEED_SNAP_THRESHOLD));
        currentBaseSpeed += speedDiff * (1f - smoothFactor);

        // Snap to zero to prevent floating point drift
        if (Mathf.Abs(currentBaseSpeed) < SPEED_SNAP_THRESHOLD && !isMoving)
        {
            currentBaseSpeed = 0f;
        }

        // Calculate max bonus allowed for current movement type
        float maxBonus = IsSprinting ? maxSprintSpeedBonus : 
                        (inputMagnitude > RUN_THRESHOLD ? maxRunSpeedBonus : 
                        (inputMagnitude > INPUT_DEAD_ZONE ? maxWalkSpeedBonus : 0f));

        // Bonus accumulates while moving, creating a skill-based reward
        if (isMoving)
        {
            // Normalize time to 0-1 range
            float bonusProgress = Mathf.Clamp01(timeMoving / bonusAccumulationTime);
            // Apply smoothstep curve for ease-in-out effect
            bonusProgress = bonusProgress * bonusProgress * (3f - 2f * bonusProgress);
            float targetBonus = maxBonus * bonusProgress;
            speedBonus = Mathf.Lerp(speedBonus, targetBonus, cachedDeltaTime * BONUS_LERP_SPEED);
        }
        else
        {
            float decayProgress = Mathf.Clamp01(timeStopped / bonusDecayTime);
            speedBonus = Mathf.Lerp(speedBonus, 0f, decayProgress);
        }

        // Handle movement direction with camera-relative input
        if (isMoving && cameraTransform != null)
        {
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            
            // Convert input to world space based on camera orientation
            cachedCameraYaw = cameraTransform.eulerAngles.y;
            Vector3 targetMoveDirection = Quaternion.Euler(0f, cachedCameraYaw, 0f) * inputDirection;
            
            // Spherical interpolation prevents abrupt direction changes
            if (smoothMoveDirection.sqrMagnitude < DIRECTION_SNAP_THRESHOLD)
            {
                smoothMoveDirection = targetMoveDirection;
            }
            else
            {
                smoothMoveDirection = Vector3.Slerp(
                    smoothMoveDirection, 
                    targetMoveDirection, 
                    directionChangeSharpness * cachedDeltaTime
                ).normalized;
            }

            moveDirection = smoothMoveDirection;

            // Rotate character to face movement direction (forward movement only)
            if (moveInput.y >= 0f && moveDirection.sqrMagnitude > DIRECTION_SNAP_THRESHOLD)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * cachedDeltaTime
                );
            }
        }
        else if (!isMoving)
        {
            // Gradual direction decay prevents instant stops
            smoothMoveDirection = Vector3.Lerp(smoothMoveDirection, Vector3.zero, cachedDeltaTime * DIRECTION_DECAY_SPEED);
            if (smoothMoveDirection.sqrMagnitude < DIRECTION_SNAP_THRESHOLD)
            {
                smoothMoveDirection = Vector3.zero;
                moveDirection = Vector3.zero;
            }
        }

        // Apply final movement with combined base speed and bonus
        float totalSpeed = currentBaseSpeed + speedBonus;
        Vector3 movement = moveDirection * totalSpeed + Vector3.up * velocity.y;
        characterController.Move(movement * cachedDeltaTime);
    }

    private void Jump()
    {
        if (!isGrounded) return;

        // Use absolute value of gravity for the calculation
        velocity.y = Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(gravity));

        // Trigger jump animation
        if (animator != null)
        {
            animator.SetTrigger(animIDJump);
        }

        jumpRequested = false;
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

        // Convert world movement to local space for blend tree
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

        // Trigger falling animation when appropriate
        animator.SetBool(animIDFreeFall, !isGrounded && velocity.y < FALLING_THRESHOLD);
    }

    private void UpdateColliderForPosture()
    {
        if (!useHeadFeetForCollider || characterController == null) return;

        // Determine desired height: default to original unless we can measure head/feet while airborne
        float desiredHeight = originalColliderHeight;

        if (!isGrounded && headPoint != null && leftFeetPoint != null && rightFeetPoint != null)
        {
            // Calculate average position of both feet
            Vector3 averageFeetPosition = (leftFeetPoint.position + rightFeetPoint.position) * 0.5f;
            
            // Measure distance between head and average feet position in world space and subtract a small padding
            float measured = Vector3.Distance(headPoint.position, averageFeetPosition) - shrinkPadding;
            desiredHeight = Mathf.Clamp(measured, minColliderHeight, maxColliderHeight);
        }

        // Preserve the world-space bottom point of the capsule so the character doesn't sink into ground.
        Vector3 currentCenterWorld = transform.position + characterController.center;
        Vector3 bottomWorld = currentCenterWorld + Vector3.down * (characterController.height * 0.5f);

        // Target center world Y such that bottom stays the same with new height
        float targetCenterWorldY = bottomWorld.y + desiredHeight * 0.5f;
        float targetCenterLocalY = targetCenterWorldY - transform.position.y;

        // Smoothly interpolate height and center.y
        float newHeight = Mathf.Lerp(characterController.height, desiredHeight, cachedDeltaTime * colliderAdjustSpeed);

        Vector3 newCenter = characterController.center;
        newCenter.y = Mathf.Lerp(characterController.center.y, targetCenterLocalY, cachedDeltaTime * colliderCenterAdjustSpeed);

        // Apply
        characterController.height = newHeight;
        characterController.center = newCenter;
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
            combatSystem.PerformAttack();
        }
    }

    public void OnAbility(InputValue value)
    {
        if (value.isPressed && maskManager != null)
        {
            maskManager.UseCurrentMaskAbility();
        }
    }

    public void OnSwitchMask(InputValue value)
    {
        if (value.isPressed && maskManager != null)
        {
            maskManager.CycleToNextMask();
        }
    }
    
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
    }

    public void AddImpulse(Vector3 force)
    {
        velocity += force;
    }

    public Vector3 GetMoveDirection() => moveDirection;
    public float GetCurrentSpeed() => currentBaseSpeed + speedBonus;
    public bool IsMoving() => moveInput.magnitude > 0.1f;
    
    #endregion
}