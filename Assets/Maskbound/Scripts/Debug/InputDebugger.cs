using UnityEngine;
using UnityEngine.InputSystem;

    /// <summary>
    /// Diagnostic script to help debug input issues.
    /// Attach this to the same GameObject as ThirdPersonController to verify input is working.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class InputDebugger : MonoBehaviour
    {
        private PlayerInput playerInput;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();

            if (playerInput == null)
            {
                Debug.LogError("InputDebugger: PlayerInput component not found!");
                return;
            }

            Debug.Log("=== INPUT DEBUGGER STARTED ===");
            Debug.Log($"PlayerInput found: {playerInput.gameObject.name}");
            Debug.Log($"Actions asset assigned: {(playerInput.actions != null ? "YES" : "NO")}");
            Debug.Log($"Notification behavior: {playerInput.notificationBehavior}");

            if (playerInput.actions != null)
            {
                Debug.Log($"Actions asset name: {playerInput.actions.name}");
                Debug.Log($"Action maps: {playerInput.actions.actionMaps.Count}");
                foreach (var map in playerInput.actions.actionMaps)
                {
                    Debug.Log($"  - {map.name}: {map.actions.Count} actions");
                    foreach (var action in map.actions)
                    {
                        Debug.Log($"    - {action.name} ({action.type})");
                    }
                }

                Debug.Log($"Control schemes: {playerInput.actions.controlSchemes.Count}");
                foreach (var scheme in playerInput.actions.controlSchemes)
                {
                    Debug.Log($"  - {scheme.name}");
                }

                Debug.Log($"Current control scheme: {playerInput.currentControlScheme}");
                Debug.Log($"Default action map: {playerInput.defaultActionMap}");
                
                // Subscribe to actions directly if not using Send Messages
                if (playerInput.notificationBehavior != PlayerNotifications.SendMessages)
                {
                    Debug.Log("PlayerInput is NOT set to 'Send Messages' mode. Subscribing to events directly...");
                    
                    var moveAction = playerInput.actions.FindAction("Move");
                    if (moveAction != null)
                    {
                        moveAction.performed += OnMovePerformed;
                        moveAction.canceled += OnMoveCanceled;
                    }

                    var jumpAction = playerInput.actions.FindAction("Jump");
                    if (jumpAction != null)
                    {
                        jumpAction.performed += OnJumpPerformed;
                        jumpAction.canceled += OnJumpCanceled;
                    }

                    var sprintAction = playerInput.actions.FindAction("Sprint");
                    if (sprintAction != null)
                    {
                        sprintAction.performed += OnSprintPerformed;
                        sprintAction.canceled += OnSprintCanceled;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (playerInput?.actions != null)
            {
                var moveAction = playerInput.actions.FindAction("Move");
                if (moveAction != null)
                {
                    moveAction.performed -= OnMovePerformed;
                    moveAction.canceled -= OnMoveCanceled;
                }

                var jumpAction = playerInput.actions.FindAction("Jump");
                if (jumpAction != null)
                {
                    jumpAction.performed -= OnJumpPerformed;
                    jumpAction.canceled -= OnJumpCanceled;
                }

                var sprintAction = playerInput.actions.FindAction("Sprint");
                if (sprintAction != null)
                {
                    sprintAction.performed -= OnSprintPerformed;
                    sprintAction.canceled -= OnSprintCanceled;
                }
            }
        }

        private void Update()
        {
            if (playerInput == null || playerInput.actions == null)
                return;

            // Try reading Move action directly
            var moveAction = playerInput.actions.FindAction("Move");
            if (moveAction != null)
            {
                Vector2 moveValue = moveAction.ReadValue<Vector2>();
                if (moveValue.magnitude > 0.01f)
                {
                    Debug.Log($"Direct Move input: {moveValue} (magnitude: {moveValue.magnitude})");
                }
            }

            // Try reading other actions
            var jumpAction = playerInput.actions.FindAction("Jump");
            if (jumpAction != null && jumpAction.triggered)
            {
                Debug.Log("Jump action triggered!");
            }
        }

        // Event handlers for direct subscription
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            Debug.Log($"[EVENT] Move performed: {input} (magnitude: {input.magnitude})");
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            Debug.Log($"[EVENT] Move canceled");
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            Debug.Log($"[EVENT] Jump performed!");
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            Debug.Log($"[EVENT] Jump canceled");
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            Debug.Log($"[EVENT] Sprint performed!");
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            Debug.Log($"[EVENT] Sprint canceled");
        }

        // Legacy Send Messages callbacks (for when PlayerInput is set to Send Messages mode)
        public void OnMove(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            Debug.Log($"[SEND MESSAGES] OnMove called with: {input} (magnitude: {input.magnitude})");
        }

        public void OnJump(InputValue value)
        {
            Debug.Log($"[SEND MESSAGES] OnJump called - isPressed: {value.isPressed}");
        }

        public void OnSprint(InputValue value)
        {
            Debug.Log($"[SEND MESSAGES] OnSprint called - isPressed: {value.isPressed}");
        }

        public void OnAttack(InputValue value)
        {
            Debug.Log($"[SEND MESSAGES] OnAttack called - isPressed: {value.isPressed}");
        }

        public void OnAbility(InputValue value)
        {
            Debug.Log($"[SEND MESSAGES] OnAbility called - isPressed: {value.isPressed}");
        }

        public void OnSwitchMask(InputValue value)
        {
            Debug.Log($"[SEND MESSAGES] OnSwitchMask called - isPressed: {value.isPressed}");
        }
    }

