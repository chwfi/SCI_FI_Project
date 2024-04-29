using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    protected float speed;
    protected float animationBlend;
    protected float targetRotation = 0.0f;
    protected float rotationVelocity;
    protected float verticalVelocity;
    protected float terminalVelocity = 53.0f;

    protected float jumpTimeoutDelta;
    protected float fallTimeoutDelta;
    protected float rollTimeoutDelta;

    public PlayerGroundedState(Entity owner, StateMachine stateMachine) : base(owner, stateMachine)
    {
    }

    public override void EnterState()
    {
        jumpTimeoutDelta = _player.JumpTimeout;
        fallTimeoutDelta = _player.FallTimeout;
        rollTimeoutDelta = _player.RollTimeout;
    }

    private void JumpAndGravity()
    {
        if (_player.Grounded)
        {
            fallTimeoutDelta = _player.FallTimeout;

            _player.Animator.SetBool(_player.AnimIDJump, false);
            _player.Animator.SetBool(_player.AnimIDFreeFall, false);

            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            // Jump
            if (_player.InputReader.jump && jumpTimeoutDelta <= 0.0f)
            {
                verticalVelocity = Mathf.Sqrt(_player.JumpHeight * -2f * _player.Gravity);

                _player.Animator.SetBool(_player.AnimIDJump, true);
            }

            if (jumpTimeoutDelta >= 0.0f)
            {
                jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            jumpTimeoutDelta = _player.JumpTimeout;

            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _player.Animator.SetBool(_player.AnimIDFreeFall, true);
            }

            _player.InputReader.jump = false;
        }

        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity += _player.Gravity * Time.deltaTime;
        }
    }
    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(_player.transform.position.x, _player.transform.position.y - _player.GroundedOffset,
            _player.transform.position.z);
        _player.Grounded = Physics.CheckSphere(spherePosition, _player.GroundedRadius, _player.GroundLayers,
            QueryTriggerInteraction.Ignore);

        _player.Animator.SetBool(_player.AnimIDGrounded, _player.Grounded);
    }

    private void Move()
    {
        if (!_player.CanMove) return;

        float targetSpeed = _player.InputReader.sprint ? _player.SprintSpeed : _player.MoveSpeed;
        if (_player.InputReader.move == Vector2.zero) targetSpeed = 0.0f;
        float currentHorizontalSpeed =
            new Vector3(_player.CharController.velocity.x, 0.0f, _player.CharController.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _player.InputReader.analogMovement ? _player.InputReader.move.magnitude : 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * _player.SpeedChangeRate);

            speed = Mathf.Round(speed * 1000f) / 1000f;
        }
        else
        {
            speed = targetSpeed;
        }

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * _player.SpeedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;

        Vector3 inputDirection = new Vector3(_player.InputReader.move.x, 0.0f, _player.InputReader.move.y).normalized;

        if (_player.InputReader.move != Vector2.zero)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _player.MainCam.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(_player.transform.eulerAngles.y, targetRotation, ref rotationVelocity,
                _player.RotationSmoothTime);

            _player.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        _player.CharController.Move(targetDirection.normalized * (speed * Time.deltaTime) +
                         new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);

        _player.Animator.SetFloat(_player.AnimIDSpeed, animationBlend);
        _player.Animator.SetFloat(_player.AnimIDMotionSpeed, inputMagnitude);
    }

    public override void UpdateState()
    {
        Debug.Log("¤·¤©¤·¤¤");
        Move();
        JumpAndGravity();
        GroundedCheck();
    }

    public override void ExitState()
    {
    }
}
