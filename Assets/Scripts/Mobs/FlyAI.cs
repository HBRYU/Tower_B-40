using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class FlyAI : MonoBehaviour
{
    private MobStats stats;

    public float sight, retainPlayerSightTime;
    private float retainPlayerSightTimer;  // Too lazy to make probable player position logic again. A timer will make-do

    public LayerMask wallLayers;

    private GameObject player;
    
    private PFGrid grid;
    
    private StateMachine stateMachine;
    public IdleState idleState;
    public ChaseState chaseState;

    public Transform eyeTransform, pupilTransform;
    private SpriteRenderer sr, pupilSpriteRenderer;
    private Rigidbody2D rb;

    public AudioClip[] flyNoises;
    public AudioClip deathSFX;
    
    public bool PlayerInSight { get; private set; }


    void Start()
    {
        SetupVariables();
    }

    void FixedUpdate()
    {
        if(stats.Dead)
            return;
        
        PlayerInSight = CheckPlayerInSight();
        DetermineState();
        stateMachine.Update();
        eyeTransform.localPosition = new Vector3(sr.flipX ? -3f / 16f : 3f / 16f, 0f, 0f);
        PupilBehaviour();
    }

    void SetupVariables()
    {
        stats = GetComponent<MobStatsInterface>().stats;
        stats.deathAction = Die;
        stats.takeDamageAction = TakeDamage;
        player = GM.PlayerInstance;
        pupilSpriteRenderer = pupilTransform.GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        retainPlayerSightTimer = retainPlayerSightTime;
        
        stateMachine = new StateMachine();
        
        idleState.Initialize(this);
        chaseState.Initialize(this);
        stateMachine.ChangeState(idleState);
    }

    void DetermineState()
    {
        if (PlayerInSight || retainPlayerSightTimer > 0f)
        {
            stateMachine.ChangeStateIfNot(chaseState);
        }
        else
        {
            stateMachine.ChangeStateIfNot(idleState);
        }
    }

    bool CheckPlayerInSight()
    {
        var position = transform.position;
        var position1 = player.transform.position;
        var d = Vector2.Distance(position, position1);
        RaycastHit2D hit = Physics2D.Raycast(position, position1 - position, d,
            wallLayers);
        retainPlayerSightTimer = !hit ? retainPlayerSightTime : retainPlayerSightTimer - Time.fixedDeltaTime;
        return !hit && d <= sight;
    }

    void PupilBehaviour()
    {
        if (!PlayerInSight)
        {
            pupilTransform.localPosition = Vector3.zero;
            pupilSpriteRenderer.color = Color.white;
            return;
        }

        pupilSpriteRenderer.color = new Color(0f, 0.75f, 1f);

        float d = 1f/16f, t = 0.5f;
        Vector3 targetPos = (player.transform.position - transform.position).normalized * d;
        pupilTransform.localPosition = Vector3.Lerp(pupilTransform.localPosition, targetPos, t);
    }

    void TakeDamage()
    {
        retainPlayerSightTimer = retainPlayerSightTime;
        stateMachine.ChangeStateIfNot(chaseState);
    }

    void Die()
    {
        pupilSpriteRenderer.color = Color.black;
        rb.freezeRotation = false;
        rb.gravityScale = 1f;
        rb.sharedMaterial = null;
        GM.GetAudioManager().Request(deathSFX, () => transform.position, null,
            volume: 0.5f, spatialBlend: 0.7f, priority: 10);
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
            HandleTargetPosition();
            HandleMovement();
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
            
            //float maxDeltaDir = 0.1f;
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
        private Queue<Vector3> pathQueue;
        public float pathRefreshClock;
        private float pathRefreshClockTimer;
        private Transform playerTransform;
        private Vector3 nextTargetPos;
        
        public float idealHeight, minHeight, maxHeight;
        private const float HeightRayLength = 999f;
        public float maxThrust;

        private bool PlayerInSight;
        
        public void Initialize(FlyAI ai)
        {
            this.ai = ai;
            transform = ai.transform;
            rb = transform.GetComponent<Rigidbody2D>();
            sr = transform.GetComponent<SpriteRenderer>();
            wallLayers = ai.wallLayers;
            playerTransform = GM.PlayerInstance.transform;
            
            grid = new PFGrid("based", GM.Instance.standardAStarTilemap);
            pathQueue = new Queue<Vector3>();
        }
        
        
        public void Enter()
        {
            GM.GetAudioManager().Request(ai.flyNoises[Random.Range(0, ai.flyNoises.Length)], () => transform.position,
                null, volume: 0.7f, spatialBlend: 0.7f, priority: 50);
        }

        public void Update()
        {
            PlayerInSight = ai.PlayerInSight;
            if (PlayerInSight)
            {
                var playerPos = playerTransform.position;
                float strikeDistance = 3f;
                nextTargetPos = playerPos + (transform.position - playerPos).normalized * strikeDistance;
                float h = GetHeight(nextTargetPos);
                if (h < minHeight && !CheckCeiling(transform.position))
                    nextTargetPos.y += idealHeight - h;
            }
            else
            {
                HandlePathQueue();
                if(Vector2.Distance(transform.position, nextTargetPos) < 0.5f)
                    pathQueue.TryDequeue(out nextTargetPos);
            }
            HandleMovement();
            HandleSprite();
        }

        public void Exit()
        {
            
        }
        
        float GetHeight(Vector2 position)
        {
            RaycastHit2D downwardRayHit = Physics2D.Raycast(position, Vector2.down, HeightRayLength, wallLayers);
            return !downwardRayHit ? HeightRayLength : position.y - downwardRayHit.point.y;
        }

        bool CheckCeiling(Vector2 position)
        {
            float d = 0.5f;
            RaycastHit2D upwardRayHit = Physics2D.Raycast(position, Vector2.up, d, wallLayers);
            return upwardRayHit;
        }

        void HandlePathQueue()
        {
            pathRefreshClockTimer -= Time.fixedDeltaTime;
            if (pathRefreshClockTimer <= 0f)
            {
                EnqueuePath(playerTransform.position);
                pathRefreshClockTimer = pathRefreshClock;
            }
        }

        void EnqueuePath(Vector3 target, bool clearCache = true)
        {
            if(clearCache)
                pathQueue.Clear();
        
            List<Vector3> path = new List<Vector3>();
            var position = transform.position;
            PFNode nearestNode = PFManager.GetNearestNode(position);
            PFNode nearestDestinationNode = PFManager.GetNearestNode(target);
            // Head straight to target with A* if it's visible and within range
            float closeEnoughRange = 20f;
            float d = Vector2.Distance(position, target);
            if (d <= closeEnoughRange && Physics2D.Raycast(position, target - position, d, wallLayers).collider ==
                null)
            {
                // #0. target visible from current position
                path = grid.GetAStarPath(position, target, wCost: 3).ToList();
            }
            else if(nearestNode != nearestDestinationNode)
            {
                // #1. start node != end node
                Vector2[] nodePath = 
                    GM.GetPFManager().pfGraph.GetDijkstraPath(nearestNode, nearestDestinationNode, debug:true).Select(node => node.position).ToArray();

                if (nodePath.Length == 0)
                {
                    Debug.LogWarning("Error in graph pathfinding :(");
                    return;
                }
                
                if (Vector3.Distance(nodePath[0], nodePath[1]) >
                    Vector3.Distance(transform.position, nodePath[1]))
                    nodePath = nodePath.Skip(1).ToArray();
                
                path.AddRange(grid.GetAStarPath(transform.position, nodePath[0], wCost:3));
                
                for (int i = 0; i < nodePath.Length - 1; i++)
                {
                    Vector3[] pathChunk = grid.GetAStarPath(nodePath[i], nodePath[i + 1],
                        wCost: 10);
                
                    path.AddRange(pathChunk);
                }
                
                path.AddRange(grid.GetAStarPath(path.Last(), target));
            }
            else
            {
                // #2. start node == end node
                path = grid.GetAStarPath(transform.position, target).ToList();
            }
            
            // Process path to only include key tiles around turns instead of the entire path
            /*
             *  --------
             *          \
             *           \
             *            \
             *             ------->
             *  would now be
             *
             *  -       -
             *           \
             *
             *             \
             *              -    ->
             */

            if (path.Count == 0)
            {
                // Don't do enqueues if path is empty
                return;
            }
            
            List<Vector3> pathProcessed = new List<Vector3> {path[0]};
            for (int i = 1; i < path.Count-1; i++)
            {
                var delta1 = path[i] - path[i - 1];
                var delta2 = path[i + 1] - path[i];

                if ((delta2 - delta1).sqrMagnitude > 0.01f)
                {
                    pathProcessed.Add(path[i]);
                }
            }
            pathProcessed.Add(path.Last());
            // pathProcessed.ForEach(tile => print(tile));

           
            
            pathProcessed.ForEach(tile => pathQueue.Enqueue(tile));
        }
        
        void HandleMovement()
        {
            Vector3 targetDir = (nextTargetPos - transform.position).normalized;
            Vector3 velocityDir = rb.velocity.normalized;

            Vector3 deltaDir = targetDir - velocityDir;
            
            // float maxDeltaDir = 0.1f;
            // if (Vector3.Cross(targetDir, velocityDir).magnitude < maxDeltaDir)
            //    return;
            
            rb.AddForce((targetDir + deltaDir) * maxThrust);
        }

        void HandleSprite()
        {
            if (PlayerInSight)
            {
                sr.flipX = playerTransform.position.x < transform.position.x;
                return;
            }
            
            if (Vector3.Distance(transform.position, nextTargetPos) < 0.2f)
                return;
            if (nextTargetPos.x > transform.position.x)
                sr.flipX = false;
            else
                sr.flipX = true;
        }
    }
}
