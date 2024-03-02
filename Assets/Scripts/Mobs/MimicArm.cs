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
    public PolygonCollider2D clawCollider;
    public LayerMask playerLayers;
    public Transform claw1, claw2;
    public Vector3 clawTargetPosition;
    public float clawTargetAngle;
    public float clawSpeed;
    public Vector3 idlePosition;
    public float clawIdleAngle;
    public float clawOpenAmount;  // 0 = closed, 1 = fully open

    [SerializeField] private LineRenderer line1, line2;  // line1: body to joint, link2: joint to claw

    public float limbLength;  // Length of limb

    public float chargeTime, coolDownTime;
    public float clawDamage;
    public float clawSize;
    public float rageCoolDownMultiplier;
    
    public IdleState idleState;
    public AttackState attackState;
    public DeadState deadState;

    private MimicAudio mimicAudio;


    private float timeSinceSightExit;
    void Start()
    {
        mimicAI = GetComponent<MimicAI>();
        mimicAudio = GetComponent<MimicAudio>();
        
        state = MimicArmState.Idle;

        idleState = new IdleState(this, idlePosition, clawIdleAngle, clawSpeed);
        attackState = new AttackState(arm: this, charge: chargeTime, damage: clawDamage, snapSpeed: clawSpeed * 3f, snapDuration: coolDownTime, 
            chargeClawPosition: idlePosition * 0.75f, chargeClawSpeed: clawSpeed * 2f, clawSize);
        deadState = new DeadState(this);
        
        stateMachine = new StateMachine();
        stateMachine.ChangeState(idleState);
        
        SetClawOpenAmount(1f);

        timeSinceSightExit = 0f;
    }

    void DetermineState()
    {
        if (stateMachine.CompareType(deadState))
            return;
        
        
        if (mimicAI.PlayerInSight)
        {
            timeSinceSightExit = 0f;
        }
        else
        {
            timeSinceSightExit += Time.fixedDeltaTime;
        }
        
        
        if (Vector2.Distance(mimicAI.ProbablePlayerPos, transform.position) <= limbLength * 2f && mimicAI.PlayerInSight)
        {
            stateMachine.ChangeStateIfNot(attackState);
        }
        else if(timeSinceSightExit >= 1f)
        {
            stateMachine.ChangeStateIfNot(idleState);
        }
    }

    void SetClawOpenAmount(float amount)
    {
        claw1.localPosition = new Vector3(0f, amount * 0.5f + (1 - amount) * 0.25f);
        claw2.localPosition = new Vector3(0f, -amount * 0.5f + -(1 - amount) * 0.25f);
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
                // Debug.LogWarning("Limb fucked. Using midpoint to claw position");
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

    public void Die()
    {
        stateMachine.ChangeState(deadState);
    }
    
    public class IdleState : IState
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

    public class AttackState : IState
    {
        private MimicArm arm;
        private MimicAI ai;
        private float charge, chargeTimer;
        private bool clawAttackFlag;
        private float damage;
        private float snapSpeed;
        private float snapDuration, snapTimer;

        private Vector3 playerPos;
        
        private readonly Vector3 chargeClawPosition;
        private readonly float clawTargetAngle;
        private float chargeClawSpeed;

        private float clawSize;
        
        public AttackState(MimicArm arm, float charge, float damage, float snapSpeed, float snapDuration,
            Vector3 chargeClawPosition, float chargeClawSpeed, float clawSize)
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
            this.chargeClawSpeed = chargeClawSpeed;
            this.clawSize = clawSize;
        }

        public void Enter()
        {
            chargeTimer = charge;
            snapTimer = snapDuration;
            arm.SetClawSpeed(chargeClawSpeed);
            clawAttackFlag = false;
            arm.mimicAudio.PlayWarningSFX();
        }

        private void ChargeUpdate()
        {
            arm.SetClawOpenAmount(1f);
            Vector3 currentClawTargetPosition = arm.mimicAI.FacingRight ? chargeClawPosition : new Vector3(-chargeClawPosition.x, chargeClawPosition.y);

            var position = arm.transform.position;
            RaycastHit2D clawPositionRay = Physics2D.Raycast(position, currentClawTargetPosition,
                currentClawTargetPosition.magnitude, arm.mimicAI.wallLayers);

            if (clawPositionRay)
                currentClawTargetPosition = clawPositionRay.point - (Vector2)position;
            
            arm.clawTargetPosition = currentClawTargetPosition;
            arm.clawTargetAngle = Mathf.Atan2((playerPos - position).y, (playerPos - position).x);
            
            chargeTimer -= Time.fixedDeltaTime;
            if (chargeTimer <= 0f && ai.PlayerInSight && Vector3.Distance(arm.transform.position, playerPos) < arm.limbLength * 2)
            {
                // Switch to "Snap" mode
                arm.clawCollider.enabled = true;
                arm.clawTargetPosition = playerPos - arm.transform.position;
                arm.SetClawSpeed(snapSpeed);
                arm.mimicAudio.PlaySlashSFX(arm.clawTransform);
            }
        }

        private void SnapUpdate()
        {
            // arm.clawTargetAngle = arm.mimicAI.FacingRight ? clawTargetAngle : Mathf.PI - clawTargetAngle;
            arm.SetClawOpenAmount(0f);
            snapTimer -= Time.fixedDeltaTime;
            
            bool moving = !(Vector3.Distance(arm.clawTransform.localPosition, arm.clawTargetPosition) < clawSize * 1f);
            if (!moving && !clawAttackFlag)
            {
                var filter = new ContactFilter2D();
                filter.SetLayerMask(arm.playerLayers);
                filter.useLayerMask = true;
                var results = new Collider2D[1];
                Physics2D.OverlapCollider(arm.clawCollider, filter, results);
                if (results[0] != null) // Player collider in results[] -> Player collided with claw
                {
                    ClawAttack();
                    clawAttackFlag = true;
                }
                else
                {
                    clawAttackFlag = Vector3.Distance(arm.clawTransform.localPosition, arm.clawTargetPosition) < clawSize * 0.25f;
                }
            }
                
            if (snapTimer <= 0f)
            {
                snapTimer = snapDuration;
                chargeTimer = charge;
                arm.SetClawSpeed(chargeClawSpeed);
                clawAttackFlag = false;
                
                arm.mimicAudio.PlayReleaseSFX();
            }
        }

        private void ClawAttack()
        {
            var player = GM.PlayerInstance;
            player.GetComponent<PlayerStats>().TakeDamage(damage);
            player.GetComponent<PlayerStats>().ApplyDebuff(PlayerStats.debuffs.slowed, 1f);
            player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            //player.GetComponent<PlayerMovement>().walkSpeed *= 0.5f;
        }

        public void Rage()
        {
            charge *= arm.rageCoolDownMultiplier;
            snapDuration *= arm.rageCoolDownMultiplier;
        }
        
        public void Update()
        {
            playerPos = GM.GetPlayerPosition();
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
            arm.SetClawOpenAmount(1f);
        }
    }

    public class DeadState : IState
    {
        private MimicArm arm;
        public DeadState(MimicArm arm)
        {
            this.arm = arm;
        }
        
        public void Enter()
        {
            var rb = arm.clawTransform.GetComponent<Rigidbody2D>();
            arm.clawTransform.GetComponent<PolygonCollider2D>().enabled = true;
        }

        public void Update()
        {
            
        }

        public void Exit()
        {
            
        }
    }
}
