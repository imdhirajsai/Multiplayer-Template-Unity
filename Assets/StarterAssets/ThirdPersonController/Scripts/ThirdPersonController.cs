using UnityEngine;
using Photon.Pun;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using System.Collections;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    [RequireComponent(typeof(PhotonView))] // Require PhotonView component
    [RequireComponent(typeof(PhotonAnimatorView))] // Require PhotonAnimatorView component
    public class ThirdPersonController : MonoBehaviourPunCallbacks
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Flying")]
        public float FlySpeed = 10.0f; // Speed while flying
        public bool IsFlying = false; // Track if the player is currently flying

        [Tooltip("Flying speed of the character in m/s")]
        public float FlyingSpeed = 5.0f; // You can adjust this value as needed

        [Header("First Person Mode Camera")]
        public GameObject FirstPersonCamera; // Toggle for first-person mode
        public bool isFirstPerson = false;

        // Camera rotation
        private float yawRotation; // Horizontal rotation for first-person mode


        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float rotationSpeed = 5.0f;



        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDFlyingKiss;
        private int _animIDIsFlying;
        private int _animIDIsDancing;
        private int _animIDIsDancing1;
        private int _animIDIsDancing2;
        private int _animIDIsSad;
        private int _animIDHello;
        private int _animIDNamasteMale;
        private int _animIDNamasteFemale;

        private int currentEmoteHash = 0;
        private bool canPlayEmote = true;
        private float emoteCooldownTime = 0.5f; // Half a second cooldown


#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private PhotonView _photonView; // Declare PhotonView
        private PhotonAnimatorView _photonAnimatorView; // Declare PhotonAnimatorView
        private const float AnimationCompleteThreshold = 1.0f;


        private const float _threshold = 0.01f;

        private bool _hasAnimator;
      

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _photonAnimatorView = GetComponent<PhotonAnimatorView>();

            // Get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            if (!_photonView.IsMine)
            {
                // Disable input and camera control for remote players
                enabled = false;
                return;
            }


            // Assign animation IDs and reset timeouts
            AssignAnimationIDs();
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            if (!_photonView.IsMine)
            {
                return; // Do not update if this is not the local player's view
            }


            // If flying, handle flying movement
            if (IsFlying)
            {
                Fly();
                GetComponentInChildren<TeleportOnClickWithGizmo>(false);
            }

            else
            {
                // Jump, Grounded check, and Move functions
                JumpAndGravity();
                GroundedCheck();
                Move();
                GetComponentInChildren<TeleportOnClickWithGizmo>(true);
            }

            if (_hasAnimator && currentEmoteHash != 0)
            {
                AnimatorStateInfo currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                // Debug: Check the current state hash
                Debug.Log($"Current animation state: {currentStateInfo.shortNameHash}, Expected emote hash: {currentEmoteHash}");

                if (currentStateInfo.shortNameHash == currentEmoteHash && currentStateInfo.normalizedTime >= 1.0f)
                {
                    // Reset emote when done
                    ResetEmote();
                }
                else if (_input.sprint || _input.move != Vector2.zero || !Grounded)
                {
                    // Reset if player is moving or jumping
                    ResetEmote();
                }
            }
        }

        private void ResetEmote()
        {
            if (_hasAnimator && currentEmoteHash != 0)
            {
                _animator.SetBool(currentEmoteHash, false);
                Debug.Log($"{currentEmoteHash} animation reset due to movement/jumping or completion.");
                currentEmoteHash = 0; // Clear the current emote hash
            }
        }

        private void LateUpdate()
        {
            //if (!_photonView.IsMine)
            //{
            //    return; // Skip camera rotation if this is not the local player
            //}

            // Camera rotation
            CameraRotation();
        }

        private void LogCurrentAnimatorState()
        {
            // Log all layer states
            for (int i = 0; i < _animator.layerCount; i++)
            {
                AnimatorStateInfo layerStateInfo = _animator.GetCurrentAnimatorStateInfo(i);
                Debug.Log($"Layer {i} - Current animation state: {layerStateInfo.shortNameHash}, Normalized Time: {layerStateInfo.normalizedTime}");
            }

            // Log the current state info for the base layer
            AnimatorStateInfo currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Base Layer - Current animation state: {currentStateInfo.shortNameHash}, Normalized Time: {currentStateInfo.normalizedTime}");

            // Log parameters
            foreach (var parameter in _animator.parameters)
            {
                Debug.Log($"Parameter: {parameter.name}, Type: {parameter.type}, Value: {GetParameterValue(parameter)}");
            }
        }

        // Utility method to get the value of a parameter
        private object GetParameterValue(AnimatorControllerParameter parameter)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    return _animator.GetBool(parameter.name);
                case AnimatorControllerParameterType.Float:
                    return _animator.GetFloat(parameter.name);
                case AnimatorControllerParameterType.Int:
                    return _animator.GetInteger(parameter.name);
                case AnimatorControllerParameterType.Trigger:
                    return "Trigger";
                default:
                    return null;
            }
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDFlyingKiss = Animator.StringToHash("FlyingKiss");
            _animIDIsFlying = Animator.StringToHash("IsFlying");
            _animIDIsDancing = Animator.StringToHash("IsDancing");
            _animIDIsDancing1 = Animator.StringToHash("IsDancing1");
            _animIDIsDancing2 = Animator.StringToHash("IsDancing2");
            _animIDIsSad = Animator.StringToHash("IsSad");
            _animIDHello = Animator.StringToHash("Hello");
            _animIDNamasteMale = Animator.StringToHash("NamasteMale");
            _animIDNamasteFemale = Animator.StringToHash("NamasteFemale");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
                // Check if the player is in first-person mode
                if (!isFirstPerson)
                {
                  FirstPersonCamera.SetActive(false);
                // Check if cursor is locked
                  bool isCursorLocked = Cursor.lockState == CursorLockMode.Locked;

                    // If there is an input and camera position is not fixed
                    if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
                    {
                        // Handle rotation based on cursor state
                        if (isCursorLocked)
                        {
                            // When cursor is locked, rotate normally without holding any button
                            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
                        }
                        else if (Input.GetMouseButton(1)) // Right mouse button is button index 1
                        {
                            // When cursor is not locked, rotate only when right mouse button is held down
                            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
                        }
                    }

                    // Clamp our rotations so our values are limited to 360 degrees
                    _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
                    _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                    // Cinemachine will follow this target
                    CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                        _cinemachineTargetPitch + CameraAngleOverride,
                        _cinemachineTargetYaw,
                        0.0f
                    );
                }
                else
                {
                    // Handle first-person camera rotation
                    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                    // Rotate based on input
                    _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                    _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;

                    // Clamp the first-person pitch and yaw
                    _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
                    _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                    // Rotate the player GameObject (this)
                    Vector3 playerRotation = new Vector3(0f, _cinemachineTargetYaw, 0f);
                    transform.rotation = Quaternion.Euler(playerRotation); // 'this' refers to PlayerGameObject

                    FirstPersonCamera.SetActive(true);
                }
            
        }

        public void FPSMode()
        {
            isFirstPerson = !isFirstPerson; 
        }


        public void playFlyingKissEmote()
        {
            EmoteManager(_animIDFlyingKiss);
        }
        
        public void playHelloEmote()
        {
            EmoteManager(_animIDHello);
        }
        
        public void playIsSadEmote()
        {
            EmoteManager(_animIDIsSad);
        } 
        
        public void playNamasteMale()
        {
            EmoteManager(_animIDNamasteMale);
        }
        public void playNamasteFemale()
        {
            EmoteManager(_animIDNamasteFemale);
        }
        

        public void PlayRandomDanceEmote()
        {
           int[] danceAnimations = new int[] { _animIDIsDancing, _animIDIsDancing1, _animIDIsDancing2};

            // Select a random index from the array
            int randomIndex = Random.Range(0, danceAnimations.Length);
            int randomDanceID = danceAnimations[randomIndex];

            Debug.Log($"Randomly selected dance ID: {randomDanceID}");

            if (randomDanceID == 0)
            {
                Debug.LogError("Random dance ID is 0, which might indicate it's not assigned correctly.");
                return;
            }
           EmoteManager(randomDanceID);
        }


        public void ToggleFlying()
        {
            IsFlying = !IsFlying; // Toggle flying state

            if (_hasAnimator)
            {
                // Set the flying animation state in the animator
                _animator.SetBool(_animIDIsFlying, IsFlying);
            }
        }

        private void Fly()
        {
            // Get horizontal input for lateral movement (A/D keys)
            float horizontalInput = _input.move.x; // A/D for lateral movement
            float verticalInput = _input.move.y; // W/S for moving forward/backward

            // Get the camera's forward direction and ignore vertical movement
            Vector3 cameraForward = _mainCamera.transform.forward;
            cameraForward.y = 0; // Ignore the vertical component for forward/backward movement
            cameraForward.Normalize(); // Normalize to maintain consistent speed

            // Get the right direction based on the camera's right direction
            Vector3 cameraRight = _mainCamera.transform.right;

            // Calculate the target direction for flying based on camera orientation
            Vector3 targetDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;

            // Apply lateral movement
            _controller.Move(targetDirection * FlyingSpeed * Time.deltaTime);

            // Ascend or descend based on the camera's pitch and inverted vertical input (W/S keys)
            if (verticalInput != 0) // Only adjust vertical movement if there is vertical input
            {
                // Get the camera's pitch angle
                float cameraPitch = _mainCamera.transform.eulerAngles.x;

                // Convert pitch from degrees to radians for calculations
                float pitchRadians = cameraPitch * Mathf.Deg2Rad;

                // Calculate the vertical component based on inverted vertical input
                float verticalMovement = -verticalInput * Mathf.Sin(pitchRadians) * FlyingSpeed * Time.deltaTime;

                // Apply the vertical movement
                _controller.Move(new Vector3(0, verticalMovement, 0));
            }

            // Rotate player towards camera looking direction
            if (targetDirection != Vector3.zero) // Only rotate if there's movement input
            {
                // Calculate the target rotation based on the camera's forward direction
                _targetRotation = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
        }


        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character

            if (_hasAnimator && _photonView.IsMine) // Sync animations only if this is the local player
            {
                // Update Animator parameters
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

                // Sync animator parameters across the network
                _photonAnimatorView.SetParameterSynchronized("Speed", PhotonAnimatorView.ParameterType.Float, PhotonAnimatorView.SynchronizeType.Discrete);
                _photonAnimatorView.SetParameterSynchronized("MotionSpeed", PhotonAnimatorView.ParameterType.Float, PhotonAnimatorView.SynchronizeType.Discrete);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator && _photonView.IsMine) // Sync animations only if this is the local player
                    {
                        // Update animator for jumping and falling
                        _animator.SetBool(_animIDJump, _input.jump);
                        _animator.SetBool(_animIDFreeFall, !Grounded);

                        // Sync jump and fall animations across the network
                        _photonAnimatorView.SetParameterSynchronized("Jump", PhotonAnimatorView.ParameterType.Bool, PhotonAnimatorView.SynchronizeType.Discrete);
                        _photonAnimatorView.SetParameterSynchronized("FreeFall", PhotonAnimatorView.ParameterType.Bool, PhotonAnimatorView.SynchronizeType.Discrete);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        public void EmoteManager(int emoteHash)
        {
            if (canPlayEmote && _input.move == Vector2.zero && Grounded)
            {
                if (_hasAnimator && currentEmoteHash != emoteHash)
                {
                    if (currentEmoteHash != 0)
                    {
                        _animator.SetBool(currentEmoteHash, false); // Reset previous emote
                        Debug.Log($"{currentEmoteHash} animation reset.");
                    }

                    currentEmoteHash = emoteHash;
                    _animator.SetBool(currentEmoteHash, true); // Trigger new emote
                    Debug.Log($"{currentEmoteHash} animation triggered.");

                    // Start cooldown timer
                    StartCoroutine(EmoteCooldown());
                }
            }
            else if (_input.move != Vector2.zero || !Grounded)
            {
                if (_hasAnimator && currentEmoteHash != 0)
                {
                    _animator.SetBool(currentEmoteHash, false); // Stop emote on movement
                    Debug.Log($"Movement or jumping detected, resetting emote {currentEmoteHash}.");
                    currentEmoteHash = 0;
                }
            }
        }

        private IEnumerator EmoteCooldown()
        {
            canPlayEmote = false;
            yield return new WaitForSeconds(emoteCooldownTime);
            canPlayEmote = true;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        
    }
}