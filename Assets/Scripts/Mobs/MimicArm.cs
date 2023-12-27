using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class MimicArm : MonoBehaviour
{
    public enum MimicArmState
    {
        Idle,
        Aim,
        Strike,
    }
    
    
    public MimicArmState state;
    public StateMachine stateMachine;
    private MimicAI mimicAI;
    
    public Transform clawTransform, jointTransform;
    public Vector3 clawTargetPosition;
    public float clawTargetAngle;
    public float clawSpeed;
    public Vector3 idlePosition;
    public float clawIdleAngle;
    public float clawOpenAmount;  // 0 = closed, 1 = fully open

    [SerializeField] private LineRenderer line1, line2;  // line1: body to joint, link2: joint to claw

    public float limbLength;  // Length of limb

    private IdleState idleState;
    private AttackState attackState;
    void Start()
    {
        mimicAI = GetComponent<MimicAI>();
        
        state = MimicArmState.Idle;

        idleState = new IdleState(this, idlePosition, clawIdleAngle, clawSpeed);
        attackState = new AttackState(arm: this, charge: 1f, damage: 30f, snapSpeed: clawSpeed * 3f, snapDuration: 1.5f, 
            chargeClawPosition: idlePosition * 0.75f, clawTargetAngle: clawTargetAngle, chargeClawSpeed: clawSpeed * 2f);
        stateMachine = new StateMachine();
        stateMachine.ChangeState(idleState);
    }

    void DetermineState()
    {
        if (Vector2.Distance(mimicAI.ProbablePlayerPos, transform.position) <= limbLength * 2f)
        {
            print("Attack State");
            stateMachine.ChangeStateIfNot(attackState);
        }
        else
        {
            print("Idle State");
            stateMachine.ChangeStateIfNot(idleState);
        }
    }
    
    void FixedUpdate()
    {
        DetermineState();
        stateMachine.Update();
        ArmPhysicsBehaviour();
        UpdateLineRenderers();
    }

    void SetClawSpeed(float speed)
    {
        clawSpeed = speed;
    }
    
    void ArmPhysicsBehaviour()
    {
        Vector3 GetJointPosition(Vector3 clawLocalPosition, bool facingRight)
        {
            // Simple vector based single-joint IK
            
            Vector3 midPoint = clawLocalPosition / 2f;
            float clawPositionAngle = Mathf.Atan2(clawLocalPosition.y, clawLocalPosition.x);

            float jointPositionAngle = 0f;

            float directionMultiplier = facingRight ? 1f : -1f;
            
            if (clawPositionAngle <= Mathf.PI * 0.5f || clawPositionAngle > Mathf.PI * 1.5f)
            {
                jointPositionAngle = clawPositionAngle - Mathf.PI * 0.5f * directionMultiplier;
            }
            else
            {
                jointPositionAngle = clawPositionAngle + Mathf.PI * 0.5f * directionMultiplier;
            }

            float clawDistance = clawLocalPosition.magnitude;

            if (limbLength < clawDistance * 0.5f)
            {
                Debug.LogWarning("Limb fucked. Using midpoint to claw position");
                return clawLocalPosition * 0.5f;
            }
            float jointDistanceFromMidpoint = Mathf.Sqrt(limbLength * limbLength - clawDistance * 0.5f * clawDistance * 0.5f);

            Vector3 jointPositionDirection = new Vector3(Mathf.Cos(jointPositionAngle), Mathf.Sin(jointPositionAngle));
            
            Vector3 jointPosition = midPoint + jointPositionDirection * jointDistanceFromMidpoint;

            return jointPosition;
        }

        var localPosition = clawTransform.localPosition;
        localPosition = Vector3.Lerp(localPosition, clawTargetPosition, clawSpeed);
        localPosition += (Vector3)GetComponent<Rigidbody2D>().velocity * (-1f * Time.fixedDeltaTime);
        clawTransform.localPosition = localPosition;
        clawTransform.localEulerAngles = new Vector3(0, 0, clawTargetAngle * Mathf.Rad2Deg);
        jointTransform.localPosition = Vector3.Lerp(jointTransform.localPosition, GetJointPosition(localPosition, mimicAI.FacingRight), clawSpeed);
    }

    void UpdateLineRenderers()
    {
        line1.SetPosition(0, transform.position);
        line1.SetPosition(1, jointTransform.position);
        line2.SetPosition(0, jointTransform.position);
        line2.SetPosition(1, clawTransform.position);
    }
    
    private class IdleState : IState
    {
        private MimicArm arm;
        private readonly Vector3 idleClawPosition;
        private readonly float clawTargetAngle;
        private float idleClawSpeed;

        public IdleState(MimicArm arm, Vector3 idleClawPosition, float clawTargetAngle, float idleClawSpeed)
        {
            this.arm = arm;
            this.idleClawPosition = idleClawPosition;
            this.clawTargetAngle = clawTargetAngle;
            this.idleClawSpeed = idleClawSpeed;
        }
        
        public void Enter()
        {
            arm.SetClawSpeed(idleClawSpeed);
        }

        public void Update()
        {
            Vector3 currentClawTargetPosition = arm.mimicAI.FacingRight ? idleClawPosition : new Vector3(-idleClawPosition.x, idleClawPosition.y);

            var position = arm.transform.position;
            RaycastHit2D clawPositionRay = Physics2D.Raycast(position, currentClawTargetPosition,
                currentClawTargetPosition.magnitude, arm.mimicAI.wallLayers);

            if (clawPositionRay)
                currentClawTargetPosition = clawPositionRay.point - (Vector2)position;
            
            arm.clawTargetPosition = currentClawTargetPosition;
            arm.clawTargetAngle = arm.mimicAI.FacingRight ? clawTargetAngle : Mathf.PI - clawTargetAngle;
        }

        public void Exit()
        {
            
        }
    }

    private class AttackState : IState
    {
        private MimicArm arm;
        private MimicAI ai;
        private float charge, chargeTimer;
        private float damage;
        private float snapSpeed;
        private float snapDuration, snapTimer;

        private Vector3 playerPos;
        
        private readonly Vector3 chargeClawPosition;
        private readonly float clawTargetAngle;
        private float chargeClawSpeed;
        
        public AttackState(MimicArm arm, float charge, float damage, float snapSpeed, float snapDuration,
            Vector3 chargeClawPosition, float clawTargetAngle, float chargeClawSpeed)
        {
            this.arm = arm;
            ai = arm.GetComponent<MimicAI>();
            this.charge = charge;
            chargeTimer = charge;
            this.damage = damage;
            this.snapSpeed = snapSpeed;
            this.snapDuration = snapDuration;
            snapTimer = snapDuration;
            this.chargeClawPosition = chargeClawPosition;
            this.clawTargetAngle = clawTargetAngle;
            this.chargeClawSpeed = chargeClawSpeed;
        }

        public void Enter()
        {
            chargeTimer = charge;
            snapTimer = snapDuration;
            arm.SetClawSpeed(chargeClawSpeed);
        }

        private void ChargeUpdate()
        {
            Vector3 currentClawTargetPosition = arm.mimicAI.FacingRight ? chargeClawPosition : new Vector3(-chargeClawPosition.x, chargeClawPosition.y);

            var position = arm.transform.position;
            RaycastHit2D clawPositionRay = Physics2D.Raycast(position, currentClawTargetPosition,
                currentClawTargetPosition.magnitude, arm.mimicAI.wallLayers);

            if (clawPositionRay)
                currentClawTargetPosition = clawPositionRay.point - (Vector2)position;
            
            arm.clawTargetPosition = currentClawTargetPosition;
            arm.clawTargetAngle = arm.mimicAI.FacingRight ? clawTargetAngle : Mathf.PI - clawTargetAngle;
            
            chargeTimer -= Time.fixedDeltaTime;
            if (chargeTimer <= 0f)
            {
                chargeTimer = charge;
                arm.SetClawSpeed(snapSpeed);
            }
        }

        private void SnapUpdate()
        {
            arm.clawTargetPosition = playerPos - arm.transform.position;
            // arm.clawTargetAngle = arm.mimicAI.FacingRight ? clawTargetAngle : Mathf.PI - clawTargetAngle;
            
            snapTimer -= Time.fixedDeltaTime;
            if (snapTimer <= 0f)
            {
                snapTimer = snapDuration;
                arm.SetClawSpeed(chargeClawSpeed);
            }
        }
        
        public void Update()
        {
            playerPos = ai.ProbablePlayerPos;
            if (chargeTimer >= 0f)
            {
                // #1. Charge State
                ChargeUpdate();
                return;
            }
            
            // #2. Snap (Attack motion)
            SnapUpdate();
        }

        public void Exit()
        {
            
        }
    }
}