using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FlyAI : MonoBehaviour
{
    public float speed;
    public float moveInterval;
    private float moveIntervalTimer;

    public float hoveringJitter;  // Use idle movement logic from PlayerWingsBehaviour
    public int burstBulletCount;
    private int bulletCounter;
    public float fireSpeed, reloadTime;
    private float reloadTimer;

    public float damage, range, minFireDistance, sight, retainPlayerSightTime;
    private float retainPlayerSightTimer;  // Too lazy to make probable player position logic again. A timer will make-do

    public LayerMask wallLayers;

    private GameObject player;
    
    private PFGrid grid;
    
    private StateMachine stateMachine;
    public IdleState idleState;

    public Transform eyeTransform, pupilTransform;
    private SpriteRenderer pupilSpriteRenderer;
    
    
    void Start()
    {
        SetupVariables();
    }

    void FixedUpdate()
    {
        stateMachine.Update();
    }

    void SetupVariables()
    {
        player = GM.PlayerInstance;
        pupilSpriteRenderer = pupilTransform.GetComponent<SpriteRenderer>();
        
        stateMachine = new StateMachine();
        
        idleState.Initialize(this);
        stateMachine.ChangeState(idleState);
    }

    [Serializable]
    public class IdleState : IState
    {
        private FlyAI ai;
        private Transform transform;
        private Rigidbody2D rb;
        private SpriteRenderer sr;

        
        public float idleSpeed;
        public float idealHeight, minHeight, maxHeight;
        private const float HeightRayLength = 999f;
        public float maxThrust;

        private float currentHeight;

        public float minStayTime, maxStayTime;
        private float stayTimer;

        public float nextPositionMaxDistance;
        private Vector2 targetPosition;

        private LayerMask wallLayers;
        // Doesn't need a constructor since it can be instantiated in editor
        
        public void Initialize(FlyAI ai)
        {
            this.ai = ai;
            transform = ai.transform;
            wallLayers = ai.wallLayers;
            rb = ai.GetComponent<Rigidbody2D>();
            sr = ai.GetComponent<SpriteRenderer>();
        }
        
        public void Enter()
        {
            stayTimer = Random.Range(minStayTime, maxStayTime);
        }

        public void Update()
        {
            currentHeight = GetHeight(transform.position);
            HandleTargetPosition();
            HandleMovement();
            ai.eyeTransform.position = targetPosition;
        }

        public void Exit()
        {
            
        }

        float GetHeight(Vector2 position)
        {
            RaycastHit2D downwardRayHit = Physics2D.Raycast(position, Vector2.down, HeightRayLength, wallLayers);
            return !downwardRayHit ? HeightRayLength : position.y - downwardRayHit.point.y;
        }

        Vector2 GetNextTargetPosition()
        {
            var position = transform.position;
            //float theta = Random.Range(0f, Mathf.PI * (2f/3f));
            //if (theta > Mathf.PI * (1f / 3f)) theta += Mathf.PI * (1f / 3f);
            float theta = Random.Range(0f, 2 * Mathf.PI);
            
            
            Vector2 dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            
            RaycastHit2D dirRayHit = Physics2D.Raycast(position,dir, nextPositionMaxDistance, wallLayers);
            float cusion = 0.5f;
            float maxD = dirRayHit ? Vector2.Distance(position, dirRayHit.point) - cusion : nextPositionMaxDistance;
            if (maxD < 0f) maxD = 0f;
            float d = Random.Range(0f, maxD);

            Vector2 basePosition = (Vector2)position + dir * d;
            
            float positionHeight = GetHeight(basePosition);

            float nextHeight = positionHeight < idealHeight ? idealHeight : positionHeight > maxHeight ? maxHeight : positionHeight;
            
            Vector2 nextTargetPos =  new Vector2(basePosition.x, basePosition.y + nextHeight - positionHeight);
            if(Physics2D.OverlapCircle(nextTargetPos, 0.2f, wallLayers))
                nextTargetPos = (Vector2)position + dir * d;

            if (nextTargetPos.x > transform.position.x)
                sr.flipX = false;
            else
                sr.flipX = true;

            return nextTargetPos;
        }

        void HandleTargetPosition()
        {
            stayTimer -= Time.fixedDeltaTime;
            if (stayTimer <= 0f)
            {
                targetPosition = GetNextTargetPosition();
                stayTimer = Random.Range(minStayTime, maxStayTime);
            }
        }

        void HandleMovement()
        {
            Vector2 targetDir = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocityDir = rb.velocity.normalized;

            Vector2 deltaDir = targetDir - velocityDir;
            
            float maxDeltaDir = 0.1f;
            //if (Vector3.Cross(targetDir, velocityDir).magnitude < maxDeltaDir)
            //    return;
            
            rb.AddForce((targetDir + deltaDir) * maxThrust);
        }
    }

    [Serializable]
    public class ChaseState : IState
    {
        private FlyAI ai;
        private Transform transform;
        private Rigidbody2D rb;
        private SpriteRenderer sr;
        public LayerMask wallLayers;
        private PFGrid grid;
        
        public void Initialize(FlyAI ai)
        {
            this.ai = ai;
            transform = ai.transform;
            rb = transform.GetComponent<Rigidbody2D>();
            sr = transform.GetComponent<SpriteRenderer>();
            wallLayers = ai.wallLayers;
            
            grid = new PFGrid("based", GM.Instance.standardAStarTilemap);
        }
        
        
        public void Enter()
        {
            
        }

        public void Update()
        {
            
        }

        public void Exit()
        {
            
        }
    }
}
