using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : Entity
{
    [Header("Player")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;
    public float RollSpeed = 1.0f;

    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;
    public float RollTimeout = 0.60f;

    [Header("Player Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;
    public bool LockCameraPosition = false;

    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;
    private float _rollTimeoutDelta;

    private int _dirInt;

    public int AnimIDSpeed;
    public int AnimIDGrounded;
    public int AnimIDJump;
    public int AnimIDFreeFall;
    public int AnimIDRollLeft;
    public int AnimIDRollRight;
    public int AnimIDMotionSpeed;

    private Coroutine _rollCoroutine;

    public Animator Animator { get; private set; }
    public CharacterController CharController { get; private set; }
    public GameObject MainCam { get; private set; }

    [SerializeField] private InputReader _inputReader;
    public InputReader InputReader => _inputReader;

    public StateMachine StateMachine { get; private set; }

    private const float _threshold = 0.01f;

    public bool CanMove = true;
    public bool CanRoll = true;

    private void Awake()
    {
        if (MainCam == null)
        {
            MainCam = GameObject.FindGameObjectWithTag("MainCamera");
        }

        StateMachine = new StateMachine();

        foreach (PlayerStateType state in Enum.GetValues(typeof(PlayerStateType)))
        {
            Type t = Type.GetType($"Player{state}State");
            State newState = Activator.CreateInstance(t, this, StateMachine) as State;
            StateMachine.AddState(state, newState);
        }
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        Animator = transform.Find("Visual").GetComponent<Animator>();
        CharController = GetComponent<CharacterController>();

        AssignAnimationIDs();

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        _rollTimeoutDelta = RollTimeout;

        StateMachine.Init(PlayerStateType.Idle);
    }

    private void Update()
    {
        StateMachine.CurrentState.UpdateState();
        //JumpAndGravity();
        //GroundedCheck();
        //Move();
        Roll();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        AnimIDSpeed = Animator.StringToHash("Speed");
        AnimIDGrounded = Animator.StringToHash("Grounded");
        AnimIDJump = Animator.StringToHash("Jump");
        AnimIDFreeFall = Animator.StringToHash("FreeFall");
        AnimIDRollLeft = Animator.StringToHash("RollLeft");
        AnimIDRollRight = Animator.StringToHash("RollRight");
        AnimIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        Animator.SetBool(AnimIDGrounded, Grounded);
    }

    private void CameraRotation()
    {
        if (_inputReader.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            float deltaTimeMultiplier = 1.0f;
            _cinemachineTargetYaw += _inputReader.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _inputReader.look.y * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        if (!CanMove) return;

        float targetSpeed = _inputReader.sprint ? SprintSpeed : MoveSpeed;

        if (_inputReader.move == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(CharController.velocity.x, 0.0f, CharController.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _inputReader.analogMovement ? _inputReader.move.magnitude : 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);

            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection = new Vector3(_inputReader.move.x, 0.0f, _inputReader.move.y).normalized;

        if (_inputReader.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              MainCam.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        CharController.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        Animator.SetFloat(AnimIDSpeed, _animationBlend);
        Animator.SetFloat(AnimIDMotionSpeed, inputMagnitude);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            Animator.SetBool(AnimIDJump, false);
            Animator.SetBool(AnimIDFreeFall, false);

            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_inputReader.jump && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                Animator.SetBool(AnimIDJump, true);
            }

            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                Animator.SetBool(AnimIDFreeFall, true);
            }

            _inputReader.jump = false;
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private void Roll()
    {
        if (Grounded && _inputReader.sprint && CanRoll)
        {
            if (_inputReader.rollLeft)
            {
                _rollTimeoutDelta = RollTimeout;

                _dirInt = -1;
                Animator.SetBool(AnimIDRollRight, false);
                Animator.SetBool(AnimIDRollLeft, true);

                if (_rollCoroutine != null)
                    StopCoroutine(RollCoroutine(_dirInt));

                _rollCoroutine = StartCoroutine(RollCoroutine(_dirInt));
            }

            if (_inputReader.rollRight)
            {
                _rollTimeoutDelta = RollTimeout;

                _dirInt = 1;
                Animator.SetBool(AnimIDRollLeft, false);
                Animator.SetBool(AnimIDRollRight, true);

                if (_rollCoroutine != null)
                    StopCoroutine(RollCoroutine(_dirInt));

                _rollCoroutine = StartCoroutine(RollCoroutine(_dirInt));
            }

            if (_rollTimeoutDelta > 0.0f)
            {
                _rollTimeoutDelta -= Time.deltaTime;
            }

            if (_rollTimeoutDelta <= 0.0f)
            {
                Animator.SetBool(AnimIDRollLeft, false);
                Animator.SetBool(AnimIDRollRight, false);
            }
        }
    }

    private IEnumerator RollCoroutine(int dir)
    {
        float duration = 0.2f;
        float startTime = Time.time;

        Vector3 direction = dir == -1 ? -transform.right : transform.right;

        while (Time.time < startTime + duration)
        {
            CharController.Move(RollSpeed * Time.deltaTime * direction);
            yield return null;
        }
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
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(CharController.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(CharController.center), FootstepAudioVolume);
        }
    }
}