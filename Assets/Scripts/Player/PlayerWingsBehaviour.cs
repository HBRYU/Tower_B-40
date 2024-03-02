using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class PlayerWing
{
    public enum PlayerWingState
    {
        idle,
        followMouse,
        cooldown,
    }


    public readonly bool right;
    public float damage, velocityDamageMultiplier, maxVelocityDamage;
    public PlayerWingState state;
    public int inputMouseButton;
    private Material material;
    private Material cooldownMaterial;
    public Vector3 position;
    public Vector3 idlePosition;
    private Vector3 offsetIdlePosition;
    private Vector3 offsetIdleVelocity;
    public Vector3 targetPosition;
    private Transform playerTransform;
    private PlayerMovement playerMovement;
    private Rigidbody2D playerRb;
    public float speed;
    public float range;
    private float travelDistanceCoeff;

    private bool coolingDown;
    private float cooldown;
    private float cooldownTimer;

    private LayerMask collisionLayers;
    
    private GameObject meshObject;
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private List<Vector3> vertices;
    
    private List<Vector3> positionHistory, directionHistory;
    // Why do we keep position & direction history?
    // => Draw wings based on movement => must perform calculations based on time => must keep history

    private Vector3 mousePosition;
    public TrailRenderer trailRenderer;

    public float Velocity { get; private set; }

    private PlayerWingsAudio wingsAudio;

    private bool hidden = false;
    public bool overrideWing = false;
    public float width = 0.1f;

    private bool forceIdle = false;
    
    public PlayerWing(bool right, float damage, GameObject meshObject, Material material, Material cooldownMaterial, Vector3 idlePosition,
        float speed, float range, float travelDistanceCoeff, float cooldown, LayerMask collisionLayers)
    {
        this.right = right;
        this.state = PlayerWingState.idle;

        this.damage = damage;
        
        this.meshObject = meshObject;
        
        this.material = material;
        this.cooldownMaterial = cooldownMaterial;
        
        this.idlePosition = idlePosition;
        this.speed = speed;
        this.range = range;
        this.travelDistanceCoeff = travelDistanceCoeff;
        playerTransform = GM.PlayerInstance.transform;
        playerRb = playerTransform.GetComponent<Rigidbody2D>();
        playerMovement = playerTransform.GetComponent<PlayerMovement>();
        wingsAudio = playerTransform.GetComponent<PlayerWingsAudio>();

        var cap = 60;
        positionHistory = new List<Vector3>(cap);
        directionHistory = new List<Vector3>(cap);
        for (int i = 0; i < cap; i++)
        {
            positionHistory.Add(position);
            directionHistory.Add(Vector3.right);
        }
        
        position = idlePosition;
        offsetIdlePosition = idlePosition;

        mesh = new Mesh();
        meshFilter = meshObject.GetComponent<MeshFilter>();
        meshRenderer = meshObject.GetComponent<MeshRenderer>();
        vertices = new List<Vector3>();
        for (int i = 0; i < 3; i++)
        {
            vertices.Add(idlePosition + new Vector3(-i*0.05f, 0f));
        }

        this.cooldown = cooldown;
        cooldownTimer = cooldown;

        this.collisionLayers = collisionLayers;
        
        state = PlayerWingState.idle;
    }

    public void FollowMouse()
    {
        
        targetPosition = Vector3.Lerp(Vector3.zero, mousePosition - playerTransform.position, travelDistanceCoeff);
        position = Vector3.Lerp(position, targetPosition, speed);

        if (Vector3.SqrMagnitude(position) > range * range)
        {
            position = position.normalized * range;
        }
        
        float minLength1 = 0.3f, minLength2 = 0.5f;
        int delay1 = 2, delay2 = 3; 
        
        vertices[0] = position;
        
        int c = 10;
        var dir = new Vector3();
        for (int i = directionHistory.Count - c; i < directionHistory.Count; i++)
        {
            dir += directionHistory[i]/c;
        }
        
        vertices[1] = (
            positionHistory[^delay1] + dir * -minLength1
            );
        vertices[2] = (
            positionHistory[^delay2] + dir * -minLength2
        );
        
    }
    
    public void Idle()
    {
        int xMultiplier = playerMovement.facingRight ? 1 : -1;

        float offsetAccelerationRange = 0.0005f;
        var offsetAcceleration =
            new Vector3(Random.Range(-offsetAccelerationRange, offsetAccelerationRange),
                        Random.Range(-offsetAccelerationRange, offsetAccelerationRange));

        var d = idlePosition - offsetIdlePosition;
        var wingPullAccel = d.normalized * (d.sqrMagnitude * 0.05f);
        offsetAcceleration += wingPullAccel;
        
        offsetIdleVelocity += offsetAcceleration;
        
        // Damping
        float maxOffsetVelocity = 0.2f * Time.fixedDeltaTime;
        offsetIdleVelocity = offsetIdleVelocity.sqrMagnitude > maxOffsetVelocity * maxOffsetVelocity
            ? offsetIdleVelocity.normalized * maxOffsetVelocity
            : offsetIdleVelocity;
        
        offsetIdlePosition += offsetIdleVelocity;

        float offsetMaxDist = 0.5f;
        if (Vector3.SqrMagnitude(idlePosition - offsetIdlePosition) > offsetMaxDist * offsetMaxDist)
            offsetIdlePosition = idlePosition + (offsetIdlePosition - idlePosition).normalized * offsetMaxDist;
        
        var correctIdlePosition = offsetIdlePosition;
        correctIdlePosition.x *= xMultiplier;  // "correct" as in corrected for facing right or left
        position = correctIdlePosition;
        
        Vector3[] vertexTargets =
        {
            position,
            position + new Vector3(-0.1f * xMultiplier, 0.2f, 0f),
            position + new Vector3(-0.3f * xMultiplier, 0.5f, 0f),
        };

        float[] vertexSpeeds =
        {
            speed,
            speed * 0.75f,
            speed * 0.5f,
        };
        
        // I know this is stupid code but this line is essential for smoothly following the player
        Vector3 playerDS = playerRb.velocity * Time.fixedDeltaTime;
        
        for (int i = 0; i < 3; i++)
        {
            vertices[i] -= playerDS;
            vertices[i] = Vector3.Lerp(vertices[i], vertexTargets[i], vertexSpeeds[i]);
        }
    }

    public void Cooldown()
    {
        Idle();
        cooldownTimer -= Time.fixedDeltaTime;
        if (cooldownTimer <= 0f)
        {
            coolingDown = false;
            cooldownTimer = cooldown;
            state = PlayerWingState.idle;
            GM.InGameUIInstance.SetWingColor(right, Color.cyan);
        }
    }

    public void DrawMesh()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        positionHistory.RemoveAt(0);
        positionHistory.Add(position);
        
        float velocity = Vector3.Magnitude(position - positionHistory[^2])/Time.fixedDeltaTime;
        Velocity = velocity;
        // Calculate velocity
        // -> 1. If fast, just use position history to draw wing
        // -> 2. If reasonably slow, use position history + default lengths to draw wing to prevent it from disappearing
        
        directionHistory.RemoveAt(0);
        directionHistory.Add(
            velocity >= 1f 
                ? (position - positionHistory[^2]).normalized 
                : (mousePosition - playerTransform.position + position).normalized);
        
        Vector3 pos1, pos2, pos3;
        
        pos1 = vertices[2];
        pos2 = vertices[1];
        pos3 = vertices[0];

        Vector3 v1 = pos1 - pos2;
        Vector3 v2 = pos3 - pos2;

        if (Vector3.Cross(v1, v2) == Vector3.zero)
            v1 += new Vector3(0, -0.005f);
        Vector3 pos2A; // = ((pos1 - pos2).normalized + (pos3 - pos2).normalized).normalized * width + pos2;
        Vector3 pos2B; // = -((pos1 - pos2).normalized + (pos3 - pos2).normalized).normalized * width + pos2;
        
        pos2A = ((v1.normalized) + (v2.normalized)).normalized * width + pos2;
        pos2B = -((v1.normalized) + (v2.normalized)).normalized * width + pos2;

        
        mesh.vertices = new Vector3[]
        {
            pos1,
            pos2A,
            pos3,
            pos2B,
        };

        mesh.triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
        };

        meshFilter.mesh = mesh;
        meshRenderer.material = coolingDown ? cooldownMaterial : material;
    }

    public void WingCollision()
    {
        
        int c = positionHistory.Count;

        Vector3 deltaPos = position - positionHistory[c - 2];
        Vector3 playerPos = playerTransform.position;
        RaycastHit2D hit = Physics2D.Raycast( playerPos + positionHistory[c - 2], deltaPos, 
            Vector3.Distance(positionHistory[c - 2], position), collisionLayers);

        MobStatsInterface mobStatsInterface;
        
        if (!hit.collider)
            return;

        HitboxInterface hbInterface = hit.collider.GetComponent<HitboxInterface>();
        if(!hbInterface)
            return;
        
        GameObject targetObj = hbInterface.masterObject;
        mobStatsInterface = targetObj.GetComponent<MobStatsInterface>();
        if (mobStatsInterface != null)
        {
            float additionalDamage = Mathf.Clamp(deltaPos.magnitude * velocityDamageMultiplier, 0f, maxVelocityDamage);
            mobStatsInterface.stats.TakeDamage(damage + additionalDamage);
            //Debug.Log(damage + additionalDamage);
            coolingDown = true;
            state = PlayerWingState.cooldown;
            
            wingsAudio.PlayWingHitSFX(position);
        }

        Rigidbody2D targetRb = targetObj.GetComponent<Rigidbody2D>();
        var v = deltaPos / Time.fixedDeltaTime;
        var m = 25f;
        Vector3 impulse = m * v;
        if(targetRb)
            targetRb.AddForce(impulse);
    }

    void HandleUI()
    {
        if(coolingDown)
            GM.InGameUIInstance.SetWingFill(right, (cooldown - cooldownTimer) / cooldown);
        else
            GM.InGameUIInstance.SetWingFill(right, 1);
    }

    public void Hide(bool value)
    {
        hidden = value;
    }

    public void SetVertices(List<Vector3> vertices)
    {
        this.vertices = vertices;
    }

    public void Disable(bool value)
    {
        Hide(value);
        overrideWing = value;
        if (value)
        {
            SetVertices(new List<Vector3>(){ Vector3.zero, Vector3.zero, Vector3.zero});
        }
    }

    public void ForceIdle(bool value)
    {
        forceIdle = value;
    }

    public void Update()
    {
        if (overrideWing)
        {
            return;
        }
        
        if (state != PlayerWingState.cooldown)
        {
            state = Input.GetMouseButton(inputMouseButton) && !forceIdle ? PlayerWingState.followMouse : PlayerWingState.idle;
        }
        
        HandleUI();
    }

    public void FixedUpdate()
    {
        if (overrideWing)
        {
            DrawMesh();
            return;
        }
        
        trailRenderer.enabled = false;
        
        switch (state)
        {
            case PlayerWingState.idle:
                Idle();
                break;
            case PlayerWingState.followMouse:
                FollowMouse();
                WingCollision();

                trailRenderer.enabled = true;
                trailRenderer.transform.localPosition = vertices[2];
                
                break;
            case PlayerWingState.cooldown:
                Cooldown();
                break;
        }
        
        DrawMesh();
    }
}

public class PlayerWingsBehaviour : MonoBehaviour
{
    public GameObject wing1meshObject, wing2meshObject;
    
    public PlayerWing wing1, wing2;

    public Material wingMaterial, wingCooldownMaterial;

    public Vector3 wing1Offset = new (-0.5f, 0.5f);
    public Vector3 wing2Offset = new (-0.25f, 0.5f);

    public float damage, cooldown, velocityDamageMultiplier, maxVelocityDamage, range;

    public LayerMask collisionLayers;
    public TrailRenderer wing1TrailRenderer, wing2TrailRenderer;

    private bool dead;
    
    void Start()
    {
        wing1 = new PlayerWing(false, damage, wing1meshObject, wingMaterial, wingCooldownMaterial, wing1Offset, 0.5f, 7f, range, cooldown, collisionLayers);
        wing2 = new PlayerWing(true, damage, wing2meshObject, wingMaterial, wingCooldownMaterial, wing2Offset, 0.5f, 7f, range, cooldown, collisionLayers);

        wing1.inputMouseButton = 0;
        wing2.inputMouseButton = 1;

        wing1.trailRenderer = wing1TrailRenderer;
        wing2.trailRenderer = wing2TrailRenderer;

        wing1.velocityDamageMultiplier = velocityDamageMultiplier;
        wing2.velocityDamageMultiplier = velocityDamageMultiplier;
        wing1.maxVelocityDamage = maxVelocityDamage;
        wing2.maxVelocityDamage = maxVelocityDamage;
        
        var playerWingsAudio = GetComponent<PlayerWingsAudio>();
        
        playerWingsAudio.wings.Add(wing1);
        playerWingsAudio.wings.Add(wing2);
        playerWingsAudio.Setup();
    }
    
    private void FixedUpdate()
    {
        wing1.FixedUpdate();
        wing2.FixedUpdate();
    }

    private void Update()
    {
        wing1.Update();
        wing2.Update();

        // Just in case
        if (dead)
        {
            wing1.Disable(true);
            wing2.Disable(true);
        }
    }

    public void Die()
    {
        dead = true;
    }
}
