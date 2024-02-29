using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class FlyGun : MonoBehaviour
{
    private FlyAI ai;
    private Rigidbody2D rb;
    public float damage;
    public float rateOfFire;
    public float reloadTime;
    public int magazineSize;
    private int magazine;
    [Range(0f, 1f)] public float aimSpeed;
    public float bulletSpeed;

    private float fireTimer, reloadTimer;
    
    public Transform gunTransform;
    public Transform barrelEnd;
    public Transform bodyJoint, link, gunJoint;

    public GameObject bulletObject;
    public LayerMask bulletCollisionLayers;
    
    class FlyBullet
    {
        public bool Active { get; private set; }
        private float damage;
        private GameObject bulletObject;
        private Transform transform;
        private Transform barrelEndTransform;
        private GameObject flyObject;
        private LayerMask collideLayers;
        private PlayerStats playerStats;
        private AudioClip hitSFX;
        
        private TrailRenderer tr;

        private Vector2 velocity;
        private Vector3 prevPos;

        public float lifeSpan = 3f;
        private float lifeSpanTimer;

        public FlyBullet(GameObject bulletObject, GameObject flyObject, Transform barrelEndTransform, LayerMask collideLayers, float damage, AudioClip hitSfx)
        {
            this.bulletObject = bulletObject;
            transform = this.bulletObject.transform;
            this.flyObject = flyObject;
            this.barrelEndTransform = barrelEndTransform;
            this.collideLayers = collideLayers;
            this.damage = damage;
            hitSFX = hitSfx;
            tr = bulletObject.GetComponent<TrailRenderer>();
            playerStats = GM.PlayerInstance.GetComponent<PlayerStats>();
            FreeInstance();
        }

        public void FreeInstance()
        {
            bulletObject.transform.position = flyObject.transform.position;
            bulletObject.SetActive(false);
            Active = false;
        }

        public void Fire(Vector3 initialVelocity)
        {
            Active = true;
            bulletObject.SetActive(true);
            transform.position = barrelEndTransform.position;
            var deg = Mathf.Atan2(initialVelocity.y, initialVelocity.x) * Mathf.Rad2Deg;
            transform.localEulerAngles = new Vector3(0f, 0f, deg);
            velocity = initialVelocity;
            lifeSpanTimer = lifeSpan;
            prevPos = transform.position;
            tr.Clear();
        }

        public void Update()
        {
            lifeSpanTimer -= Time.deltaTime;
            if (lifeSpanTimer <= 0f)
            {
                FreeInstance();
                return;
            }
            
            RaycastHit2D bulletRayHit =
                Physics2D.Raycast(prevPos, velocity, velocity.magnitude * Time.deltaTime, collideLayers);

            prevPos = transform.position;
            
            if (!bulletRayHit)
            {
                transform.position += (Vector3)velocity * Time.deltaTime;
                return;
            }
            
            transform.position = bulletRayHit.point;
            if (bulletRayHit.collider.gameObject == GM.PlayerInstance)
            {
                playerStats.TakeDamage(damage);
            }

            GM.GetAudioManager().Request(hitSFX, () => transform.position, null,
                volume: 0.5f, spatialBlend: 0.7f, priority: 50);
            FreeInstance();
        }
    }

    private MobStats stats;
    
    public int bulletPoolCount = 20;

    public AudioClip fireSFX, hitSFX;
    
    private List<FlyBullet> bullets;

    private bool facingRight;
    private SpriteRenderer sr, gunSr;

    private Transform playerTransform;
    
    void Start()
    {
        stats = GetComponent<MobStatsInterface>().stats;
        SetupVariables();
        InitializeBulletPool();
    }

    void Update()
    {
        UpdateBullets();
        if(stats.Dead)
            return;
        facingRight = !sr.flipX;
    }

    private void FixedUpdate()
    {
        if(stats.Dead)
            return;
        if(ai.PlayerInSight)
            AttackStateUpdate();
        else
            IdleStateUpdate();
        HandleLink();
    }

    void SetupVariables()
    {
        ai = GetComponent<FlyAI>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        gunSr = gunTransform.GetComponent<SpriteRenderer>();
        playerTransform = GM.GetPlayer().transform;
        magazine = magazineSize;
        reloadTimer = reloadTime;
        fireTimer = 1f / rateOfFire;
    }

    void InitializeBulletPool()
    {
        bullets = new List<FlyBullet>(bulletPoolCount);
        for (int i = 0; i < bulletPoolCount; i++)
        {
            bullets.Add(new FlyBullet(Instantiate(bulletObject), gameObject, barrelEnd, bulletCollisionLayers, damage, hitSFX));
        }
    }

    void UpdateBullets()
    {
        foreach (var bullet in bullets)
        {
            if(bullet.Active)
                bullet.Update();
        }
    }

    void SetGunPosition()
    {
        float t = 0.5f;
        Vector3 idlePosition = facingRight ? new Vector3(-0.3f, -0.5f) : new Vector3(0.3f, -0.5f);
        gunTransform.localPosition = Vector3.Lerp(gunTransform.localPosition, idlePosition, t) - (Vector3)rb.velocity * Time.fixedDeltaTime;
    }
    
    void AimGun(Vector2 targetDir)
    {
        var rad = gunTransform.localEulerAngles.z * Mathf.Deg2Rad;
        var targetRad = Mathf.Atan2(targetDir.y, targetDir.x);

        if (Mathf.Abs(rad - targetRad) > Mathf.Abs(rad + 2 * Mathf.PI - targetRad))
            targetRad -= 2 * Mathf.PI;
        if (Mathf.Abs(rad - targetRad) > Mathf.Abs(rad - 2 * Mathf.PI - targetRad))
            targetRad += 2 * Mathf.PI;

        var setRad = targetRad * aimSpeed + rad * (1 - aimSpeed);

        gunTransform.localEulerAngles = new Vector3(0f, 0f, setRad * Mathf.Rad2Deg);
    }

    void HandleLink()
    {
        var position1 = bodyJoint.position;
        var position2 = gunJoint.position;

        if (!facingRight)
            position1.x -= bodyJoint.localPosition.x * 2f;
        
        link.position = Vector3.Lerp(position1, position2, 0.5f);
        link.localScale = new Vector3(Vector3.Distance(position1, position2), 1f / 16f);
        var dir = position1 - position2;
        var deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        link.localEulerAngles = new Vector3(0f, 0f, deg);
    }

    void IdleStateUpdate()
    {
        // In FixedUpdate()
        SetGunPosition();

        var targetDir = facingRight ? Vector2.right : Vector2.left;
        
        AimGun(targetDir);
    }
    
    void AttackStateUpdate()
    {
        gunSr.flipX = false;
        SetGunPosition();
        
        // Aim
        var dir = (playerTransform.position - gunTransform.position).normalized;
        AimGun(dir);

        reloadTimer -= Time.fixedDeltaTime;
        fireTimer -= Time.fixedDeltaTime;
        
        if (reloadTimer <= 0f && fireTimer <= 0f)
        {
            Fire();
            fireTimer = 1f / rateOfFire;
            magazine -= 1;
            if (magazine <= 0)
            {
                reloadTimer = reloadTime;
                magazine = magazineSize;
            }
        }

        void Fire()
        {
            var freeBullet = bullets.Find(b => !b.Active);
            freeBullet.Fire(dir * bulletSpeed);
            GM.GetAudioManager().Request(fireSFX, () => barrelEnd.position, null,
                volume: 0.5f, spatialBlend: 0.8f, priority: 50);
        }
    }
}
