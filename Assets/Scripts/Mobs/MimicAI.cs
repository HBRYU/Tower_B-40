using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class MimicLeg
{
    public readonly bool isSmall;
    public bool disabled, dead;
    public Transform transform;
    public Transform bodyTransform;
    public LineRenderer lineRenderer;
    public Vector3 Position => transform.position;
    public Vector3 Direction => (Position - bodyTransform.position).normalized;
    public bool connected = false;
    public Vector3 target;
    public readonly float speed;
    public readonly float force;

    public float deadCableLength;
    
    public MimicLeg(bool isSmall, Transform transform, Transform bodyTransform, float speed, float force)
    {
        this.isSmall = isSmall;
        this.transform = transform;
        this.bodyTransform = bodyTransform;
        this.speed = speed;
        this.force = force;
        lineRenderer = transform.GetComponent<LineRenderer>();
    }

    public void MoveToTargetPoint()
    {
        /*
        Vector3 moveVector = (target - transform.position).normalized * speed;
        if ((target - Position).sqrMagnitude < moveVector.sqrMagnitude)
            moveVector = target - Position;
        transform.Translate(moveVector);
        */

        transform.position = Vector3.Lerp(Position, target, speed);
    }

    public void DrawCableToBody()
    {
        int verticesPerUnitLength = 3;
        Vector3 p = Position, q = bodyTransform.position;

        
        
        float dist = Vector3.Distance(Position, q);
        
        int vertexCount = Mathf.RoundToInt(dist * verticesPerUnitLength);
        float sagging = 2f / (dist*dist + 0.1f);
        Vector3[] vertices = new Vector3[vertexCount];
        
        // Going for a parabola connecting the two points (leg and body) with first coefficient fixed to alpha
        // alpha is determined by sagging
        // y = alpha*x^2 + beta*x + gamma

        float alpha = sagging;
        float beta = (alpha * (q.x * q.x - p.x * p.x) + p.y - q.y) / (p.x - q.x + 0.01f);
        float gamma = p.y - alpha * p.x * p.x - beta * p.x;

        if (Mathf.Abs(p.x - q.x) < 0.05f)
        {
            float lowPointX = -beta / (2 * alpha);
            if ((lowPointX < q.x && lowPointX > p.x) || (lowPointX > q.x && lowPointX < p.x))
            {
                lineRenderer.positionCount = 3;
                lineRenderer.SetPosition(0, p);
                
                lineRenderer.SetPosition(1, 
                    new Vector3(lowPointX, alpha * lowPointX * lowPointX + beta * lowPointX + gamma));
                
                lineRenderer.SetPosition(2, q);
            }
            else
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, p);
                lineRenderer.SetPosition(1, q);
            }
            return;
        }
        
        
        float deltaX = (p.x - q.x) / (vertexCount < 2 ? 1 : vertexCount-1);
        for (int i = 0; i < vertexCount; i++)
        {
            float x = q.x + deltaX * i;
            vertices[i] = new Vector3(x, alpha * x * x + beta * x + gamma);
        }

        // Render line
        lineRenderer.positionCount = vertexCount;
        lineRenderer.SetPositions(vertices);
    }
}


public enum MimicState
{
    Sleep,
    Idle,
    Search,
    Chase,
    Flee,
    Dead
}


[BurstCompile]
public class MimicAI : MonoBehaviour
{
    private Rigidbody2D rb;

    private MobStats stats;
    private MimicAudio mimicAudio;
    
    private PFManager pfManager;
    private Transform playerTransform;

    public bool FacingRight { get; private set; }

    public Transform gizmo;

    [Header("Head")]
    [SerializeField] private Transform head;
    [SerializeField] private SpriteRenderer headSprite;
    [SerializeField] private Transform eye, pupil;
    
    [Header("Legs")]
    public List<MimicLeg> legs = new ();
    [SerializeField] private int numLegs;
    [SerializeField] private GameObject smallLegObj, legObj;
    [SerializeField][Range(0f, 1f)] private float smallLegChance;
    [SerializeField][Tooltip("Random.Range(-nSA, +nSA) for X&Y")] private float legSpawnArea;
    [SerializeField] private float smallLegForce, legForce;
    public LayerMask wallLayers;

    [Header("\nAI")] 
    public PFGrid grid;
    public MimicState state;
    public StateMachine stateMachine = new StateMachine();

    public IdleState idleState;
    public ChaseState chaseState;
    public SearchState searchState;
    public DeadState deadState;


    public Vector3 destination;
    private Vector3 target;
    private Queue<Vector3> pathQueue;
    
    [SerializeField] private LayerMask sensorLayers;
    public bool PlayerInSight { get; private set; }
    public Vector3 ProbablePlayerPos { get; private set; }

    void CreateLegs(int n)
    {
        for (int i = 0; i < n; i++)
        {
            MimicLeg thisNode;
            Vector3 pos = transform.position + new Vector3(Random.Range(-legSpawnArea, legSpawnArea), Random.Range(-legSpawnArea, legSpawnArea));
            if (Random.Range(0f, 1f) < smallLegChance)
            {
                /* Small Node
                - Small max tension/push
                - Small range
                */
                GameObject thisNodeObj = Instantiate(smallLegObj, pos, Quaternion.identity);
                thisNode = new MimicLeg(true, thisNodeObj.transform, transform, 0.25f, smallLegForce);
            }
            else
            {
                /* Medium Node
                - Medium max tension/push
                - Medium range
                */
                GameObject thisNodeObj = Instantiate(legObj, pos, Quaternion.identity);
                thisNode = new MimicLeg(false, thisNodeObj.transform, transform, 0.05f, legForce);
            }
            legs.Add(thisNode);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<MobStatsInterface>().stats;
        mimicAudio = GetComponent<MimicAudio>();
        stats.deathAction = Die;
        stats.takeDamageAction = TakeDamage;
        pfManager = GM.GetPFManager();
        grid = new ("mimic", GM.Instance.standardAStarTilemap);
        CreateLegs(numLegs);
        playerTransform = GM.PlayerInstance.transform;
        pathQueue = new Queue<Vector3>();
        SetLegTargetsStay();

        moveVars.Reset();
        
        //idleState = new IdleState(this, 3f, 1f, 5f, true);
        //chaseState = new ChaseState(this, 5f);
        // searchState = new SearchState(this);
        
        idleState.Initialize(this);
        chaseState.Initialize(this);
        searchState.Initialize(this);
        deadState = new DeadState(this);
        
        
        stateMachine.ChangeState(idleState);
    }

    void FixedUpdate()
    {
        stateMachine.Update();
        LegsBehaviour();
        
        if(stats.Dead)
            return;
        HeadBehaviour();
        EyeBehaviour();
        Sight();
        gizmo.position = destination;
    }

    void PlayerSightEnter()
    {
        PlayerInSight = true;
        ProbablePlayerPos = playerTransform.position;
        
        // Change STATE to Chase
        if(!stateMachine.CompareType(chaseState))
            stateMachine.ChangeState(chaseState);
    }

    void PlayerSightExit()
    {
        PlayerInSight = false;
    }
    
    void Sight()
    {
        // Theta disabled for now

        var eyePosition = eye.position;
        var playerPosition = playerTransform.position;
        // Vector3 eyeDir = eyePosition.normalized;
        Vector3 playerDir = (playerPosition - eyePosition).normalized;
        
        
        // float theta = Mathf.Acos(Vector3.Dot(eyeDir, playerDir));

        // float maxTheta = 2 * Mathf.PI / 3f;
        float maxDist = 15f;
        float dist = Vector3.Distance(eyePosition, playerPosition);
        RaycastHit2D hit = Physics2D.Raycast(eyePosition, playerDir, dist, wallLayers);
        // print((theta >= maxTheta) + " " + (dist > maxDist) + " " + (hit.collider != null));
        // print(theta);
        if (dist > maxDist || hit.collider != null) // theta >= maxTheta ||  
        {
            if (PlayerInSight)
            {
                PlayerInSight = false;
                PlayerSightExit();
            }
        }
        else if(!PlayerInSight)
        {
            PlayerInSight = true;
            PlayerSightEnter();
        }
    }
    
    void HeadBehaviour()
    {
        Vector3 headLookAt;
        if (PlayerInSight)
            headLookAt = ProbablePlayerPos;
        else if (!pathQueue.TryPeek(out headLookAt))
            headLookAt = destination;

        headLookAt = Vector3.Lerp(destination, headLookAt, 0.5f);

        float neck = 0.75f, headSpeed = 0.05f;
        
        Vector3 headTargetPos = (headLookAt - transform.position).normalized * neck;

        head.localPosition = Vector3.Lerp(head.localPosition, headTargetPos, headSpeed);

        FacingRight = headLookAt.x > head.position.x;
        headSprite.flipX = !FacingRight;
    }

    void EyeBehaviour()
    {
        // For eye + pupil
        Vector3 distanceScale = new Vector3(0.15f, 0.2f);
        Vector3 offset = new Vector3(0, 0);

        Vector3 eyeLookAt;
        if (PlayerInSight)
            eyeLookAt = ProbablePlayerPos;
        else if (!pathQueue.TryPeek(out eyeLookAt))
            eyeLookAt = destination;
        Vector3 eyeTargetPosRaw = (eyeLookAt - head.position).normalized;
        
        // Applied distanceScale & offset
        Vector3 eyeTargetPos = new Vector3(eyeTargetPosRaw.x * distanceScale.x, eyeTargetPosRaw.y * distanceScale.y) + offset;

        float eyeSpeed = 0.1f;
        eye.localPosition = Vector3.Lerp(eye.localPosition, eyeTargetPos, eyeSpeed);
    }

    void LegsBehaviour()
    {
        foreach (var leg in legs)
        {
            leg.DrawCableToBody();
            if (leg.disabled)
            {
                // #0. Leg disabled
                leg.target = transform.position;
                leg.MoveToTargetPoint();
                leg.connected = false;
                
                continue;
            }

            if (leg.dead)
            {
                leg.connected = false;
                float d = Vector3.Distance(leg.Position, transform.position);
                if (d < leg.deadCableLength)
                    continue;

                float frictionCoeff = 0.5f;
                
                Vector2 tension = leg.Direction * (leg.force * (d - leg.deadCableLength));
                rb.AddForce(tension * frictionCoeff);
                leg.transform.GetComponent<Rigidbody2D>().AddForce(-tension * frictionCoeff);
                continue;
            }
            
            if (leg.connected)
            {
                // #1. Leg connected to surface
                Vector2 force = leg.Direction * leg.force;

                force *= Vector2.Dot(force, target - transform.position) > 0f
                    ? 1f
                    : -1f;
                
                rb.AddForce(force);
                continue;
            }
            
            // #2. Leg not connected to surface
            leg.MoveToTargetPoint();
            float connectionThreshold = 0.1f;  // for Vec3.sqrMagnitude
            if (Vector3.SqrMagnitude(leg.target - leg.Position) < connectionThreshold)
            {
                leg.connected = true;
                mimicAudio.PlayLegSFX(leg.Position, random: true);
            }
            
        }
    }

    /// <summary>
    /// Resets the target of the least optimal leg
    /// Return codes are as follows:
    /// true: successful
    /// false: failed
    /// </summary>
    bool SetLegTargetsMove(Vector3 target)
    {
        Vector3 targetDirection = (target - transform.position).normalized;
        Vector3 gravityForce = (rb.mass * rb.gravityScale * Physics2D.gravity);
        Vector3 netForce = gravityForce;
        legs.ForEach(leg => netForce += leg.connected ? (leg.Direction * leg.force) : Vector3.zero);


        float dotProduct = Vector3.Dot(targetDirection,netForce.normalized);

        const float minDot = 1.7f / 2f;  // roughly cos(30deg)
        
        if (Mathf.Abs(dotProduct) > minDot)
        {
            // #1. netForce somewhat leads to target
            // return true;
        }
        
        // #2. netForce leads away from target
        MimicLeg leastOptimalLeg = legs[0];
        float leastOptimalDot = 1f;
        foreach (var leg in legs)
        {
            float thisDot = Vector3.Dot(targetDirection, leg.Direction);
            if (Mathf.Abs(leastOptimalDot) > Mathf.Abs(thisDot))
            {
                leastOptimalLeg = leg;
                leastOptimalDot = thisDot;
            }
        }
        
        // Set new target for least optimal leg
        float thetaInterval = Mathf.PI / 32f;
        float theta = 0f;
        float thetaDir = 1f;

        RaycastHit2D hit = new RaycastHit2D();

        do
        {
            // (netForce + F).normalized = targetDirection
            Vector3 newLegDir = 
                Random.value > 0.5f
                ? -targetDirection
                : targetDirection;

            float newLegDirRad = Mathf.Atan2(newLegDir.y, newLegDir.x) + theta;
            newLegDir.x = Mathf.Cos(newLegDirRad);
            newLegDir.y = Mathf.Sin(newLegDirRad);
            
            hit = Physics2D.Raycast(transform.position, newLegDir, 10f, wallLayers);
            theta = (Mathf.Abs(theta) + thetaInterval) * thetaDir;
            thetaDir *= -1f;
        } while (hit.collider == null && Mathf.Abs(theta) < Mathf.PI);

        if (hit.collider == null)
        {
            // target point not found
            return false;
        }
        
        mimicAudio.PlayLegMoveSFX(leastOptimalLeg.Position, random: true);

        leastOptimalLeg.target = hit.point;
        leastOptimalLeg.connected = false;
        return true;
    }

    private Vector3 stasisPosition;
    void SetLegTargetsStay()
    {
        Vector3[] connectionPoints = new Vector3[numLegs];
        target = stasisPosition;
        float theta = 0f;
        for (int i = 0; i < numLegs; i++)
        {
            float alphaInterval = 3.14f / 16f;
            float alpha = 0f;
            float alphaDir = 1f;

            RaycastHit2D hit = new RaycastHit2D();

            while (hit.collider == null && Mathf.Abs(alpha) < Mathf.PI * 2f)
            {
                Vector2 dir = new Vector2(Mathf.Cos(theta + alpha), Mathf.Sin(theta + alpha));
                hit = Physics2D.Raycast(transform.position, dir, 10f, wallLayers);

                alpha = (Mathf.Abs(alpha) + alphaInterval) * alphaDir;
                alphaDir *= -1f;
            }

            if (hit.collider == null)
            {
                legs[i].disabled = true;
                continue;
            }

            legs[i].target = hit.point;

            theta += Mathf.PI * 2f / numLegs;
        }
    }

    void EnqueuePath(Vector3 target, bool clear=false)
    {
        if(clear)
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
                pfManager.pfGraph.GetDijkstraPath(nearestNode, nearestDestinationNode, debug:true).Select(node => node.position).ToArray();

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
    // ------------------------------------------------------------------------------------------------
    // Variables for STATE: Search
    [System.Serializable]
    public struct MimicMoveVariables
    {
        public float refreshPathClock;
        [HideInInspector] public float refreshPathClockTimer;

        public float setLegTargetClock;
        [HideInInspector] public float setLegTargetClockTimer;

        public void Reset()  // I can apparently add functions to structs in C# lmfao
        {
            refreshPathClockTimer = refreshPathClock;
            setLegTargetClockTimer = setLegTargetClock;
        }
    }

    [Header("Variables for moving from point A to B")]
    public MimicMoveVariables moveVars;
    
    float MoveToDestination(Vector3 thisDestination)  // Returns distance to destination
    {
        if (moveVars.refreshPathClockTimer <= 0f)
        {
            moveVars.refreshPathClockTimer = moveVars.refreshPathClock;
            EnqueuePath(thisDestination, true);
        }

        target = thisDestination;
        if(pathQueue.TryPeek(out target))
        {
            Vector3 direction = target - transform.position;
            // rb.velocity = direction.normalized * 4f; // Placeholder

            if (moveVars.setLegTargetClockTimer <= 0f)
            {
                moveVars.setLegTargetClockTimer = moveVars.setLegTargetClock;
                SetLegTargetsMove(target);
            }
        
            if (direction.sqrMagnitude < 0.5f)
            {
                pathQueue.Dequeue();
            }
        }
   
        // Update moveVars
        moveVars.refreshPathClockTimer -= Time.fixedDeltaTime;
        moveVars.setLegTargetClockTimer -= Time.fixedDeltaTime;

        return Vector3.Distance(thisDestination, transform.position);
    }
    // ------------------------------------------------------------------------------------------------
    [Serializable]
    public class IdleState : IState
    {
        private MimicAI ai;
        public float stasisClockMax, stasisClockMin;
        private float stasisClock;
        public float timeOut;
        private float timeOutTimer;

        private Vector3 currentDestination;

        private bool isStatic;

        public IdleState(MimicAI ai, float stasisClockMax, float stasisClockMin, float timeOut, bool isStatic)
        {
            this.ai = ai;
            this.stasisClockMax = stasisClockMax;
            this.stasisClockMin = stasisClockMin;
            this.timeOut = timeOut;
            this.isStatic = isStatic;
        }

        public void Initialize(MimicAI ai)
        {
            this.ai = ai;
        }

        public void Enter()
        {
            Reset();
            ai.state = MimicState.Idle;
        }
        
        public void Reset()
        {
            stasisClock = Random.Range(stasisClockMin, stasisClockMax);
            timeOutTimer = timeOut;
            currentDestination = Vector3.zero;
            isStatic = false;
        }
        
        Vector3 GetRandomDestination()
        {
            float pi = Mathf.PI;

            float rayLength = 10f;
            
            int rayCount = Random.Range(3, 7);
            Vector3[] candidates = new Vector3[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                float angle = Random.Range(-pi / 3, pi / 3) + (Random.value >= 0.5f ? 1f : 0f) * pi;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
                RaycastHit2D hit = Physics2D.Raycast(ai.transform.position, dir, rayLength, ai.wallLayers);
                
                candidates[i] += hit.collider != null
                    ? hit.point + ((Vector2)ai.transform.position - hit.point).normalized * 1f
                    : (Vector3)hit.point;
            }
    
            return candidates[Random.Range(0, rayCount)];
        }

        public void Update()
        {
            if (stasisClock > 0f && !isStatic)
            {
                ai.stasisPosition = ai.transform.position;
                ai.SetLegTargetsStay();
                isStatic = true;
            }
            else if (stasisClock <= 0f && isStatic)
            {
                isStatic = false;
                currentDestination = GetRandomDestination();
                ai.destination = currentDestination;
            }
            else if (stasisClock <= 0f && !isStatic)
            {
                float dist = ai.MoveToDestination(currentDestination);
                if (dist <= 2f)
                {
                    stasisClock = Random.Range(stasisClockMin, stasisClockMax);
                }
            }
            else
            {
                stasisClock -= Time.fixedDeltaTime;
            }

            timeOutTimer -= Time.fixedDeltaTime;
            if (timeOutTimer <= 0f && !isStatic)
            {
                Reset();
            }
        }
        
        public void Exit() {}
    }
    
    // ------------------------------------------------------------------------------------------------
    // Chase
    [System.Serializable]

    public class ChaseState : IState
    {
        private MimicAI ai;
        
        public float timeOut;

        public Queue<Vector3> playerPosHistoryQueue;
        public List<Vector3> playerPosHistory;
        public float timeSinceSightExit;
        public Vector3 finalPlayerSighting;

        public ChaseState(MimicAI ai, float timeOut)
        {
            this.ai = ai;
            this.timeOut = timeOut;
            ai.state = MimicState.Chase;
        }
        
        public void Initialize(MimicAI ai)
        {
            this.ai = ai;
        }

        public void Reset()
        {
            var playerPos = GM.GetPlayerPosition();
            int cap = 60;
            playerPosHistory = new List<Vector3>(cap);
            for (int i = 0; i < cap; i++)
                playerPosHistory.Add(playerPos);
            playerPosHistoryQueue = new Queue<Vector3>(playerPosHistory);
            timeSinceSightExit = 0f;
            finalPlayerSighting = playerPos;
        }

        public void UpdatePlayerPosHistory(Vector3 playerPosition)
        {
            // playerPosHistory.RemoveAt(0);
            // playerPosHistory.Add(GM.GetPlayerPosition());
            playerPosHistoryQueue.Dequeue();
            playerPosHistoryQueue.Enqueue(playerPosition);
        }
        
        public void UpdatePlayerPosHistoryList()
        {
            playerPosHistory = new List<Vector3>(playerPosHistoryQueue);
        }

        public void Enter()
        {
            Reset();
            ai.mimicAudio.PlayAlertSFX();
            // Shit programming
            ai.pupil.GetChild(0).GetComponent<SpriteRenderer>().color = Color.red;
        }

        public void Update()
        {
            Vector3 GuessPlayerPosition()
            {
                Vector3 generalDir = playerPosHistory.Last() - playerPosHistory.First();
                float t = timeSinceSightExit;
                float fadeT = Mathf.Sqrt(t+1) - 1f;
                float velocityCoeff = 2f;
            
                Vector3 v = generalDir / (Time.fixedDeltaTime * playerPosHistory.Count);

                RaycastHit2D hit = Physics2D.Raycast(finalPlayerSighting, generalDir, 
                    velocityCoeff * fadeT * v.magnitude, ai.wallLayers);
                if (hit.collider == null)
                {
                    return velocityCoeff * fadeT * v + finalPlayerSighting;
                }
            
                return hit.point + ((Vector2)ai.transform.position - hit.point).normalized * 0.25f;
            }
        
        
            if (ai.PlayerInSight)
            {
                var playerPosition = ai.playerTransform.position;
                UpdatePlayerPosHistory(playerPosition);
                ai.ProbablePlayerPos = playerPosition;
                finalPlayerSighting = playerPosition;
                timeSinceSightExit = 0f;

                float attackDistance = 3.5f;
                ai.destination = playerPosition + (ai.transform.position - playerPosition).normalized * attackDistance;
                ai.destination.y = playerPosition.y + 0.5f;
                if(Vector3.Distance(ai.destination, ai.transform.position) < 1f)
                    ai.SetLegTargetsStay();
                else
                    ai.MoveToDestination(ai.destination);
            }
            else
            {
                if (timeSinceSightExit == 0f)
                {
                    // "Just lost sight of player"
                    UpdatePlayerPosHistoryList();
                }
            
                timeSinceSightExit += Time.fixedDeltaTime;
                if (timeSinceSightExit > timeOut)
                {
                    timeSinceSightExit = 0f;
                    ai.stateMachine.ChangeState(ai.searchState);
                }
                ai.ProbablePlayerPos = GuessPlayerPosition();
                ai.destination = ai.ProbablePlayerPos;
        
                ai.MoveToDestination(ai.destination);
            }
        
            
        }

        public void Exit()
        {
            ai.pupil.GetChild(0).GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    // Converted to use StateMachines by GPT-4.
    [System.Serializable]
    public class SearchState : IState
    {
        private MimicAI ai;
        private PFNode currentNode;
        private PFNode nextNode;
        private List<PFNode> searchedNodes;

        public SearchState(MimicAI ai)
        {
            this.ai = ai;
        }
        
        public void Initialize(MimicAI ai)
        {
            this.ai = ai;
        }

        public void Enter()
        {
            Reset();
            ai.state = MimicState.Search;
        }

        private void Reset()
        {
            currentNode = PFManager.GetNearestNode(ai.transform.position);
            nextNode = currentNode.adjacentNodes[Random.Range(0, currentNode.adjacentNodes.Length)];
            searchedNodes = new List<PFNode>();
        }

        public void Update()
        {
            float distToNextNode = Vector3.Distance(ai.transform.position, nextNode.position);
            float distThresh = 1.5f;

            if (distToNextNode <= distThresh)
            {
                searchedNodes.Add(currentNode);
                currentNode = nextNode;
                PFNode[] availableNodes = currentNode.adjacentNodes
                    .Where(n => !searchedNodes.Contains(n)).ToArray();

                nextNode = availableNodes.Length > 0
                    ? availableNodes[Random.Range(0, availableNodes.Length)]
                    : currentNode; // Handle case where all nodes are searched
                
                ai.mimicAudio.PlaySearchSFX();
            }

            ai.destination = nextNode.position;
            ai.ProbablePlayerPos = nextNode.position;
            ai.MoveToDestination(ai.destination);
        }

        public void Exit() {}
    }

    public class DeadState : IState
    {
        private MimicAI ai;
        private AudioSource audioSource;
        private const float rate = 0.8f;

        public DeadState(MimicAI ai)
        {
            this.ai = ai;
        }
        
        public void Enter()
        {
            var legs = ai.legs;
            foreach (var leg in legs)
            {
                leg.dead = true;
                leg.transform.GetComponent<Rigidbody2D>().simulated = true;
                leg.transform.GetComponent<BoxCollider2D>().enabled = true;
                leg.deadCableLength = Vector3.Distance(ai.transform.position, leg.Position);
            }

            ai.pupil.GetChild(0).GetComponent<SpriteRenderer>().color = Color.black;
            audioSource = ai.GetComponent<AudioSource>();
        }

        public void Update()
        {
            if(audioSource.volume >= 0.05f)
                audioSource.volume *= rate;
            else
                audioSource.Stop();
        }

        public void Exit()
        {
            
        }
    }
    

    
    void Die()
    {
        //Destroy(gameObject);
        stateMachine.ChangeState(deadState);
        GetComponent<MimicArm>().Die();
    }

    void TakeDamage()
    {
        GetComponent<Animator>().SetTrigger("TakeDamage");
    }
}
