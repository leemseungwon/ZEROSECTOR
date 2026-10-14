
using System.Collections.Generic;
using LSW._02._Code.Common;
using LSW._02._Code.Entity.Players.States;
using LSW._02._Code.Sos;
using LSW._02._Code.Sos.EntityStatDatas;
using UnityEngine;

namespace LSW._02._Code.Entity.Players
{
    public class PlayerController : BaseEntity
    {
        [Header("Input")]
        [field:SerializeField] public InputData inputData;

        [Header("Camera")]
        [field:SerializeField] public Camera PlayerCamera { get; private set; }
        
        [Header("Movement")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        public float CurrentJumpForce { get; private set; }
        public bool IsGrounded { get; private set; }
        public float MoveInput => inputData != null ? inputData.MoveInput : 0f;
        public bool HasJumpRequest => _jumpRequested;

        private Vector3 _initialScale;
        private bool _jumpRequested;
        
        public const string IdleState = "PlayerIdleState";
        public const string MoveState = "PlayerMoveState";
        public const string JumpState = "PlayerJumpState";
        public const string AttackState = "PlayerAttackState";

        protected override void Awake()
        {
            base.Awake();

            if (inputData == null)
            {
                return;
            }

            inputData.InitializeInput();
            inputData.OnJumpPressed += HandleJumpPressed;
            inputData.OnAttackPressed += RequestAttack;
            
            InitStates();
            
            if (Animation != null)
                _initialScale = Animation.transform.localScale;
        }
        
        private void InitStates()
        {
            StateMachine = new EntityStateMachine
            (
                new List<StateData>{
                    new StateData
                    {
                        stateName = IdleState,
                        State = new PlayerIdleState(this)
                    },
                    new StateData
                    {
                        stateName = MoveState,
                        State = new PlayerMoveState(this)
                    },
                    new StateData
                    {
                        stateName = JumpState,
                        State = new PlayerJumpState(this)
                    },
                    new StateData
                    {
                        stateName = AttackState,
                        State = new PlayerAttackState(this)
                    }
                }
            );
        }

        public override void InitStat()
        {
            base.InitStat();
            if(StatData is PlayerStatData playerStatData)
            {
                CurrentJumpForce = playerStatData.initJumpForce;
            }
        }

        public override void ChangeEntityStat(EntityStatType type, MathOperation operation, float value)
        {
            switch (type)
            {
                case EntityStatType.JumpForce:
                    CurrentJumpForce = operation switch
                    {
                        MathOperation.Add => CurrentJumpForce + value,
                        MathOperation.Subtract => CurrentJumpForce - value,
                        _ => CurrentJumpForce
                    };
                    break;
            }
            base.ChangeEntityStat(type, operation, value);
        }

        private void Start()
        {
            StateMachine.Initialize(IdleState);
        }

        private void HandleJumpPressed()
        {
            if (IsGrounded && !IsDead && !IsStunned)
                _jumpRequested = true;
        }

        protected override void Update()
        {
            base.Update();
            CheckGround();
        }

        private void CheckGround()
        {
            if (groundCheck == null)
            {
                IsGrounded = false;
                return;
            }

            IsGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
        }

        public void Move()
        {
            if (IsDead || IsStunned)
                return;

            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.x = MoveInput * CurrentMoveSpeed;
            Rigidbody.linearVelocity = velocity;
            
            if (Animation != null)
            {
                if (MoveInput > 0)
                    Animation.transform.localScale = new Vector3(Mathf.Abs(_initialScale.x),
                        _initialScale.y, _initialScale.z);
                else if (MoveInput < 0)
                    Animation.transform.localScale = new Vector3(-Mathf.Abs(_initialScale.x),
                        _initialScale.y, _initialScale.z);
            }
        }

        public bool Jump()
        {
            if (!_jumpRequested || !IsGrounded || IsDead || IsStunned)
                return false;

            _jumpRequested = false;

            Vector2 velocity = Rigidbody.linearVelocity;
            velocity.y = CurrentJumpForce;
            Rigidbody.linearVelocity = velocity;

            return true;
        }

        public void ConsumeJumpInput()
        {
            _jumpRequested = false;
        }
        
        public void RequestAttack()
        {
            if (IsDead || IsStunned || !IsGrounded)
                return;

            StateMachine.ChangeState(AttackState);
        }

        public override void OnDestroy()
        {
            if (inputData != null)
            {
                inputData.OnJumpPressed -= HandleJumpPressed;
                inputData.OnAttackPressed -= RequestAttack;
            }

            base.OnDestroy();
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position,
                groundCheckRadius);
        }
    }
}