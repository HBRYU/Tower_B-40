using System.Collections.Generic;
using UnityEngine;

namespace Player.Skills.Pierce
{
    [CreateAssetMenu(fileName = "New Pierce Skill", menuName = "Skills/Pierce")]
    public class SkillPierce : Skill
    {
        private bool right;
        public GameObject animationObject;
        private GameObject animationObjectInstance;
        private Animator animator;
        public float distance, speed;
        public float activationTime;
        private float activationTimeTimer;
        public LayerMask wallLayers, targetHitboxLayers, phaseThroughLayers;
        public float damage;
        private bool damageDealt = false;

        private Vector2 direction;
        private Rigidbody2D rb;
        private PlayerSkillManager playerSkillManager;
        private PlayerMovement playerMovement;
        private PlayerAnimation playerAnimation;
        private SpriteRenderer sr;

        private Transform transform;
        private Vector2 startPoint, endPoint;

        private CapsuleCollider2D playerCollider;
        public Vector2 setPlayerColliderSize;
        private Vector2 initialColliderSize;

        private List<PlayerWing> wings;
        private float initialWingsWidth;
    
        public override bool ActivationPattern(List<int> indexes)
        {
            if (indexes.Count != 2)
                return false;
            if (indexes[1] - indexes[0] == 2)
            {
                // Right
                right = true;
                return true;
            }
            if (indexes[1] - indexes[0] == -2)
            {
                // Left
                right = false;
                return true;
            }
            // Else
            return false;
        }

        public override void Activate(GameObject playerObject)
        {
            InitializeVariables(playerObject);
            
            rb.velocity = Vector2.zero;
            playerMovement.overrideMovement = true;
            playerMovement.FaceDirection(right);
            animationObjectInstance.GetComponent<SpriteRenderer>().flipX = !right;
            
            direction = right ? Vector2.right : Vector2.left;

            startPoint = transform.position;
            
            RaycastHit2D ray = Physics2D.Raycast(startPoint, direction, distance, wallLayers);
            if (ray)
                endPoint = ray.point - direction * 0.5f;
            else
                endPoint = (Vector2)transform.position + direction * distance;

            rb.gravityScale = 0f;
            sr.enabled = false;
            playerCollider.size = setPlayerColliderSize;
            SetPhaseLayers(true);

            activationTimeTimer = activationTime;
            endFlag = false;
        }
        
        public override void Update()
        {
            if (activationTimeTimer <= 0f)
            {
                animator.SetTrigger("Move");
                Move();
            }
            else
            {
                activationTimeTimer -= Time.deltaTime;
            }
        }

        public override void Deactivate()
        {
            playerMovement.overrideMovement = false;
            rb.gravityScale = 1f;
            sr.enabled = true;
            damageDealt = false;
            activationTimeTimer = activationTime;
            SetPhaseLayers(false);
            Destroy(animationObjectInstance);
            playerAnimation.RequestAnimation("Walk", false);
            playerSkillManager.DeactivateSkill(this);
            endFlag = false;
        }

        void InitializeVariables(GameObject playerObject)
        {
            rb = playerObject.GetComponent<Rigidbody2D>();
            transform = playerObject.transform;
            playerSkillManager = playerObject.GetComponent<PlayerSkillManager>();
            playerMovement = playerObject.GetComponent<PlayerMovement>();
            playerAnimation = playerObject.GetComponent<PlayerAnimation>();
            sr = GameObject.FindGameObjectWithTag("PlayerSprite").GetComponent<SpriteRenderer>();
            animationObjectInstance = Instantiate(animationObject, transform.position, Quaternion.identity, transform);
            animator = animationObjectInstance.GetComponent<Animator>();
            animationObjectInstance.GetComponent<AnimationObjectInterface>().endAction = Deactivate;
            playerCollider = playerObject.GetComponent<CapsuleCollider2D>();
            initialColliderSize = playerCollider.size;
            var wingsBehavioour = playerObject.GetComponent<PlayerWingsBehaviour>();
            wings = new List<PlayerWing>(2) { wingsBehavioour.wing1, wingsBehavioour.wing2 };
            initialWingsWidth = wings[0].width;
            
            foreach (var wing in wings)
            {
                wing.overrideWing = true;
                var vertices = new List<Vector3>(3)
                {
                    new (-1f, 0f, 0f),
                    Vector3.zero,
                    new (1f, 0f, 0f)
                };
                wing.SetVertices(vertices);
            }

            moveClock = 0f;
        }

        private float moveClock;
        private bool endFlag;
        void Move()
        {
            Debug.Log(1);
            moveClock += Time.deltaTime;
            if ((speed * Time.deltaTime > Mathf.Abs(endPoint.x - transform.position.x) || speed * moveClock > distance) && !endFlag)
            {
                endFlag = true;
                rb.MovePosition(endPoint);
                playerCollider.size = initialColliderSize;
                if(!damageDealt)
                    HandleDamage();
                animator.SetTrigger("End");
                rb.velocity = Vector2.zero;
                
                foreach (var wing in wings)
                {
                    wing.width = initialWingsWidth;
                    Debug.Log(initialWingsWidth);
                    wing.overrideWing = false;
                }
            }
            else if (!endFlag)
            {
                foreach (var wing in wings)
                {
                    wing.width = 0.3f;
                    var vertices = new List<Vector3>(3)
                    {
                        new (-2f, 0f, 0f),
                        Vector3.zero,
                        new (2f, 0f, 0f)
                    };
                    wing.SetVertices(vertices);
                }

                rb.velocity = speed * direction;
            }
        }

        void HandleDamage()
        {
            RaycastHit2D[] hitboxRay = Physics2D.RaycastAll(startPoint, direction, Vector2.Distance(startPoint , endPoint), targetHitboxLayers);
            foreach (var hit in hitboxRay)
            {
                var hbInterface = hit.collider.GetComponent<HitboxInterface>();
                if (hbInterface)
                {
                    Debug.Log(damage);
                    var msInterface = hbInterface.masterObject.GetComponent<MobStatsInterface>();
                    if(msInterface)
                        msInterface.stats.TakeDamage(damage);
                }
            }

            damageDealt = true;
        }

        void SetPhaseLayers(bool value)
        {
            // GPT 4
            for (int i = 0; i < 32; i++) // There are 32 layers in Unity (0-31)
            {
                if (((1 << i) & phaseThroughLayers) != 0)
                {
                    // This layer is in the LayerMask, so ignore collision with player layer
                    Physics2D.IgnoreLayerCollision(GM.GetPlayer().layer, i, value);
                }
            }
        }
    }
}