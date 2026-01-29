using System.Collections;
using Maskbound.Core;
using UnityEngine;

public class ParkourSystem : MonoBehaviour
{
        [Header("Slide Settings")]
        [SerializeField] private float slideSpeed = 10f;
        [SerializeField] private float slideControllerHeight = 0.5f;
        [SerializeField] private float slideDuration = 1.5f;
        [SerializeField] private float slideCooldown = 1f;

        [Header("Wall Run Settings")]
        [SerializeField] private float wallRunSpeed = 8f;
        [SerializeField] private float wallRunDuration = 2f;
        [SerializeField] private float wallCheckDistance = 0.7f;
        [SerializeField] private float wallJumpUpForce = 10f;
        [SerializeField] private float wallJumpSideForce = 12f;
        [SerializeField] private LayerMask wallLayers;

        [Header("Vault Settings")]
        [SerializeField] private float vaultHeight = 1.5f;
        [SerializeField] private float vaultForwardForce = 5f;
        [SerializeField] private float vaultCheckDistance = 1f;

        [Header("Ledge Grab Settings")]
        [SerializeField] private float ledgeGrabHeight = 2f;
        [SerializeField] private float ledgeClimbSpeed = 3f;

        // Components
        private ThirdPersonController controller;
        private CharacterController characterController;
        private Animator animator;

        // State
        private bool isSliding;
        private bool isWallRunning;
        private bool isVaulting;
        private bool isClimbingLedge;
        
        private float slideTimer;
        private float wallRunTimer;
        private float lastSlideTime;
        
        private Vector3 wallNormal;
        private bool isWallRight;

        // Original controller values
        private float originalHeight;

        // Animation hashes
        private int animIDSlide;
        private int animIDWallRun;
        private int animIDVault;
        private int animIDClimb;
        private int animIDWallRunSide;

        public bool IsSliding => isSliding;
        public bool IsWallRunning => isWallRunning;
        public bool IsInParkourAction => isSliding || isWallRunning || isVaulting || isClimbingLedge;

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();

            originalHeight = characterController.height;

            AssignAnimationIDs();
        }

        private void Update()
        {
            if (isSliding)
            {
                UpdateSlide();
            }

            if (isWallRunning)
            {
                UpdateWallRun();
            }

            CheckForParkourOpportunities();
        }

        private void AssignAnimationIDs()
        {
            animIDSlide = Animator.StringToHash("Slide");
            animIDWallRun = Animator.StringToHash("WallRun");
            animIDVault = Animator.StringToHash("Vault");
            animIDClimb = Animator.StringToHash("Climb");
            animIDWallRunSide = Animator.StringToHash("WallRunSide");
        }

        private void CheckForParkourOpportunities()
        {
            if (IsInParkourAction) return;

            // Check for wall run
            if (CanWallRun())
            {
                StartWallRun();
            }

            // Check for vault
            if (CanVault())
            {
                StartVault();
            }

            // Check for ledge grab
            if (CanGrabLedge())
            {
                StartLedgeGrab();
            }
        }

        #region Slide
        public void TryStartSlide()
        {
            if (isSliding) return;
            if (Time.time - lastSlideTime < slideCooldown) return;
            if (!controller.IsMoving()) return;

            StartSlide();
        }

        private void StartSlide()
        {
            isSliding = true;
            slideTimer = 0f;

            // Lower character controller
            characterController.height = slideControllerHeight;
            characterController.center = new Vector3(0f, slideControllerHeight / 2f, 0f);

            animator.SetBool(animIDSlide, true);
        }

        private void UpdateSlide()
        {
            slideTimer += Time.deltaTime;

            // Move forward during slide
            Vector3 slideDirection = transform.forward;
            characterController.Move(slideDirection * (slideSpeed * Time.deltaTime));

            // End slide conditions
            if (slideTimer >= slideDuration || !controller.IsMoving())
            {
                EndSlide();
            }
        }

        private void EndSlide()
        {
            isSliding = false;
            lastSlideTime = Time.time;

            // Restore controller height
            characterController.height = originalHeight;
            characterController.center = new Vector3(0f, originalHeight / 2f, 0f);

            animator.SetBool(animIDSlide, false);
        }
        #endregion

        #region Wall Run
        private bool CanWallRun()
        {
            if (characterController.isGrounded) return false;

            // Check left wall
            RaycastHit leftHit;
            bool leftWall = Physics.Raycast(transform.position, -transform.right, out leftHit, wallCheckDistance, wallLayers);

            // Check right wall
            RaycastHit rightHit;
            bool rightWall = Physics.Raycast(transform.position, transform.right, out rightHit, wallCheckDistance, wallLayers);

            if (leftWall)
            {
                wallNormal = leftHit.normal;
                isWallRight = false;
                return true;
            }
            else if (rightWall)
            {
                wallNormal = rightHit.normal;
                isWallRight = true;
                return true;
            }

            return false;
        }

        private void StartWallRun()
        {
            isWallRunning = true;
            wallRunTimer = 0f;

            animator.SetBool(animIDWallRun, true);
            animator.SetFloat(animIDWallRunSide, isWallRight ? 1f : -1f);
        }

        private void UpdateWallRun()
        {
            wallRunTimer += Time.deltaTime;

            // Calculate wall run direction (along the wall)
            Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
            
            // Determine forward direction based on player input
            if (Vector3.Dot(transform.forward, wallForward) < 0)
            {
                wallForward = -wallForward;
            }

            // Apply wall run movement
            Vector3 movement = wallForward * (wallRunSpeed * Time.deltaTime);
            characterController.Move(movement);

            // Slight tilt toward wall
            float tiltAngle = isWallRight ? 15f : -15f;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, tiltAngle);

            // End conditions
            if (wallRunTimer >= wallRunDuration || !CanWallRun() || characterController.isGrounded)
            {
                EndWallRun();
            }
        }

        private void EndWallRun()
        {
            isWallRunning = false;

            // Reset tilt
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            animator.SetBool(animIDWallRun, false);
        }

        public void WallJump()
        {
            if (!isWallRunning) return;

            // Jump away from wall
            Vector3 jumpDirection = wallNormal * wallJumpSideForce + Vector3.up * wallJumpUpForce;
            controller.AddImpulse(jumpDirection);

            EndWallRun();
        }
        #endregion

        #region Vault
        private bool CanVault()
        {
            if (!controller.IsMoving()) return false;

            // Raycast forward to detect obstacle
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, vaultCheckDistance))
            {
                // Check if obstacle is vaultable height
                float obstacleHeight = hit.collider.bounds.max.y - transform.position.y;
                
                if (obstacleHeight > 0.5f && obstacleHeight < vaultHeight)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartVault()
        {
            StartCoroutine(VaultRoutine());
        }

        private IEnumerator VaultRoutine()
        {
            isVaulting = true;
            controller.SetMovementEnabled(false);

            animator.SetTrigger(animIDVault);

            // Move up and forward
            float vaultTime = 0.5f;
            float timer = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = transform.position + transform.forward * vaultForwardForce + Vector3.up * vaultHeight;

            while (timer < vaultTime)
            {
                timer += Time.deltaTime;
                float t = timer / vaultTime;
                
                // Smooth curve
                float height = Mathf.Sin(t * Mathf.PI);
                Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
                newPos.y = startPos.y + height * vaultHeight;

                characterController.Move(newPos - transform.position);

                yield return null;
            }

            controller.SetMovementEnabled(true);
            isVaulting = false;
        }
        #endregion

        #region Ledge Grab
        private bool CanGrabLedge()
        {
            if (characterController.isGrounded) return false;
            if (controller.GetCurrentSpeed() < 1f) return false;

            // Raycast forward and up to detect ledge
            Vector3 rayStart = transform.position + Vector3.up * ledgeGrabHeight;
            RaycastHit hit;

            if (Physics.Raycast(rayStart, transform.forward, out hit, 1f))
            {
                // Check if there's a ledge to grab
                Vector3 ledgeCheck = hit.point + Vector3.down * 0.1f;
                if (!Physics.Raycast(ledgeCheck, Vector3.down, 0.5f))
                {
                    return true;
                }
            }

            return false;
        }

        private void StartLedgeGrab()
        {
            StartCoroutine(LedgeGrabRoutine());
        }

        private IEnumerator LedgeGrabRoutine()
        {
            isClimbingLedge = true;
            controller.SetMovementEnabled(false);

            animator.SetTrigger(animIDClimb);

            // Climb up animation duration
            float climbTime = 1f;
            float timer = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = transform.position + transform.forward * 1f + Vector3.up * ledgeGrabHeight;

            while (timer < climbTime)
            {
                timer += Time.deltaTime;
                float t = timer / climbTime;

                Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
                characterController.Move(newPos - transform.position);

                yield return null;
            }

            controller.SetMovementEnabled(true);
            isClimbingLedge = false;
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw wall detection rays
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.right * wallCheckDistance);
            Gizmos.DrawRay(transform.position, -transform.right * wallCheckDistance);

            // Draw vault detection
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * vaultCheckDistance);
        }
    }
