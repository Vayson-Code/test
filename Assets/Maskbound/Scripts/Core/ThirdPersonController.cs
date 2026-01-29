using Maskbound.Core;
using UnityEngine;
using UnityEngine.InputSystem;


    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float sprintSpeed = 8.5f;
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float accelerationRate = 10f;
        [SerializeField] private float decelerationRate = 15f;
        [SerializeField] private float maxWalkSpeedBonus = 2f; // Max +2 for walking
        [SerializeField] private float maxRunSpeedBonus = 3f; // Max +3 for running
        [SerializeField] private float maxSprintSpeedBonus = 4f; // Max +4 for sprinting
        [SerializeField] private float speedBonusAccumulationRate = 0.2f; // How fast the speed bonus increases
        [SerializeField] private float sprintBonusAccumulationRate = 0.7f; // Faster accumulation when sprinting (+0.2 + 0.5)

        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedGravity = -2f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayers;

        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float cameraRotationSpeed = 2f;

        // Components
        [SerializeField]private CharacterController characterController;
        [SerializeField]private Animator animator;
        [SerializeField]private CombatSystem combatSystem;
        [SerializeField]private MaskManager maskManager;
        [SerializeField]private PlayerInput playerInput;

        // Movement state
        private Vector2 moveInput;
        private Vector3 moveDirection;
        private Vector3 lastMoveDirection; // Keep track of last input direction for deceleration
        private Vector3 velocity;
        private float currentSpeed;
        private float targetSpeed;
        private float speedBonus = 0f; // Acceleration bonus up to +2 (walk) or +3 (run)

        // Jump state
        private bool isGrounded;
        private bool wasGrounded;
        private float lastGroundedTime;
        private float lastJumpPressTime;
        private bool jumpRequested;

        // Animation IDs
        private int animIDSpeed;
        private int animIDGrounded;
        private int animIDJump;
        private int animIDFreeFall;
        private int animIDMotionSpeed;
        private int animIDMoveX;
        private int animIDMoveY;

        // States
        public bool IsSprinting { get; private set; }
        public bool IsSliding { get; private set; }
        public bool InCombat => combatSystem != null && combatSystem.InCombat;

        private void Awake()
        {
            // Ensure PlayerInput is properly configured
            if (playerInput != null)
            {
                // Verify that the input actions asset is assigned
                if (playerInput.actions == null)
                {
                    Debug.LogError("PlayerInput component has no InputActionAsset assigned!");
                }
                else
                {
                    // Enable the input system if it's not already enabled
                    playerInput.actions.Enable();
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
            CheckGrounded();
            HandleJumpBuffer();
            HandleGravity();
            HandleMovement();
            UpdateAnimations();

            // Fallback: Read sprint input directly if callback system fails
            if (playerInput?.actions != null)
            {
                var sprintAction = playerInput.actions.FindAction("Sprint");
                if (sprintAction != null)
                {
                    bool isSprintPressed = sprintAction.IsPressed();
                    if (isSprintPressed != IsSprinting)
                    {
                        IsSprinting = isSprintPressed;
                        Debug.Log($"Fallback: IsSprinting set to {IsSprinting}");
                    }
                }
            }
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

            Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position;
            isGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
            if (isGrounded && !wasGrounded)
            {
                OnLanded();
            }

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }
        }

        private void HandleJumpBuffer()
        {
            // Jump buffering - remember jump press for a short time
            if (jumpRequested)
            {
                if (Time.time - lastJumpPressTime <= jumpBufferTime)
                {
                    // Coyote time - can jump shortly after leaving ground
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
        }

        private void HandleGravity()
        {
            if (isGrounded)
            {
                velocity.y = groundedGravity;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        private void HandleMovement()
        {
            // Don't move during certain states
            if (combatSystem != null && combatSystem.IsInAction)
            {
                return;
            }

            // Calculate target speed based on input and sprint state
            float inputMagnitude = moveInput.magnitude;
            
            if (inputMagnitude > 0f)
            {
                Debug.Log(IsSprinting ? "Sprinting" : "Flying");
                targetSpeed = IsSprinting ? sprintSpeed : (inputMagnitude > 0.5f ? runSpeed : walkSpeed);
                
                // Determine max bonus and accumulation rate based on movement type
                float maxBonus;
                float accumulationRate;
                
                if (IsSprinting)
                {
                    // When sprinting: use sprint values
                    maxBonus = maxSprintSpeedBonus; // +4 when sprinting
                    accumulationRate = sprintBonusAccumulationRate; // Faster accumulation (0.7)
                }
                else
                {
                    // When running/walking: use normal values
                    maxBonus = inputMagnitude > 0.5f ? maxRunSpeedBonus : maxWalkSpeedBonus; // +3 for run, +2 for walk
                    accumulationRate = speedBonusAccumulationRate; // Normal accumulation (0.2)
                }
                
                // If speed bonus exceeds the current max, decay it gradually
                if (speedBonus > maxBonus)
                {
                    speedBonus = Mathf.Max(speedBonus - decelerationRate * Time.deltaTime, maxBonus);
                }
                // Otherwise, continuously increase speed bonus while moving, up to max
                else if (speedBonus < maxBonus)
                {
                    speedBonus = Mathf.Min(speedBonus + accumulationRate * Time.deltaTime, maxBonus);
                }
                
                // Update move direction relative to camera
                if (cameraTransform != null)
                {
                    Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                    
                    // Only rotate the character if moving forward or sideways, not backward
                    // Check if forward input (Z) is positive - if negative, character is moving backward
                    if (moveInput.y >= 0f)
                    {
                        // Moving forward or sideways - rotate to face input direction
                        float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                        float currentYaw = transform.eulerAngles.y;
                        
                        // Smooth rotation with lerp
                        float rotation = Mathf.LerpAngle(currentYaw, targetRotation, rotationSpeed * Time.deltaTime);
                        transform.rotation = Quaternion.Euler(0f, rotation, 0f);
                        
                        // Use the full inputDirection to allow left/right movement
                        moveDirection = Quaternion.Euler(0f, rotation, 0f) * inputDirection;
                    }
                    else
                    {
                        // Moving backward - keep current facing direction, but move in input direction
                        moveDirection = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * inputDirection;
                    }
                    
                    lastMoveDirection = moveDirection; // Store the direction while player is actively moving
                }
            }
            else
            {
                // Decelerate speed bonus first, then target speed
                speedBonus = Mathf.Max(speedBonus - decelerationRate * Time.deltaTime, 0f);
                if(speedBonus <= 0f)
                    targetSpeed = Mathf.Max(targetSpeed - decelerationRate * Time.deltaTime, 0f);
                
                // During deceleration, continue moving in the last direction
                moveDirection = lastMoveDirection;
            }

            // Current speed = base speed + accumulated bonus
            currentSpeed = targetSpeed + speedBonus;

            // Apply movement
            Vector3 movement = moveDirection * currentSpeed + Vector3.up * velocity.y;
            characterController.Move(movement * Time.deltaTime);
        }

        private void Jump()
        {
            if (!isGrounded) return;

            // Calculate jump velocity to reach desired height
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            animator.SetTrigger(animIDJump);
            jumpRequested = false;
        }

        private void OnLanded()
        {
            // Handle landing logic
            animator.ResetTrigger(animIDJump);
        }

        private void UpdateAnimations()
        {
            Debug.Log(currentSpeed);
            animator.SetBool(animIDGrounded, isGrounded);
            animator.SetFloat(animIDSpeed, currentSpeed);
            animator.SetFloat(animIDMotionSpeed, 1f);

            // Relative movement for blend tree - follow currentSpeed and lastMoveDirection during deceleration
            if (currentSpeed > 0.1f) // Character is actually moving (either actively or decelerating)
            {
                Vector3 localMove = transform.InverseTransformDirection(moveDirection);
                animator.SetFloat(animIDMoveX, localMove.x * currentSpeed);
                animator.SetFloat(animIDMoveY, localMove.z * currentSpeed);
            }
            else
            {
                animator.SetFloat(animIDMoveX, 0f);
                animator.SetFloat(animIDMoveY, 0f);
            }

            // Falling animation
            if (!isGrounded && velocity.y < -2f)
            {
                animator.SetBool(animIDFreeFall, true);
            }
            else
            {
                animator.SetBool(animIDFreeFall, false);
            }
        }

        #region Input Handlers
        public void OnMove(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            moveInput = input;
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
            // Sprint only while holding the button
            IsSprinting = value.isPressed;
            Debug.Log($"OnSprint called: IsSprinting = {IsSprinting}");
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
        public float GetCurrentSpeed() => currentSpeed;
        public bool IsMoving() => moveInput.magnitude > 0.1f;
        #endregion
    }
