using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Player.Skills
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
        public LayerMask wallLayers, targetHitboxLayers;
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

            rb.simulated = false;
            sr.enabled = false;

            activationTimeTimer = activationTime;

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
            rb.simulated = true;
            sr.enabled = true;
            damageDealt = false;
            activationTimeTimer = activationTime;
            Destroy(animationObjectInstance);
            playerSkillManager.DeactivateSkill(this);
        }

        void InitializeVariables(GameObject playerObject)
        {
            rb = playerObject.GetComponent<Rigidbody2D>();
            transform = playerObject.transform;
            playerSkillManager = playerObject.GetComponent<PlayerSkillManager>();
            playerMovement = playerObject.GetComponent<PlayerMovement>();
            sr = GameObject.FindGameObjectWithTag("PlayerSprite").GetComponent<SpriteRenderer>();
            animationObjectInstance = Instantiate(animationObject, transform.position, Quaternion.identity, transform);
            animator = animationObjectInstance.GetComponent<Animator>();
            animationObjectInstance.GetComponent<AnimationObjectInterface>().endAction = Deactivate;
        }

        void Move()
        {
            if (speed * Time.deltaTime > Mathf.Abs(endPoint.x - transform.position.x))
            {
                transform.position = endPoint;
                if(!damageDealt)
                    HandleDamage();
                animator.SetTrigger("End");
            }
            else
            {
                transform.position += (Vector3)direction * (speed * Time.deltaTime);
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
    }
}