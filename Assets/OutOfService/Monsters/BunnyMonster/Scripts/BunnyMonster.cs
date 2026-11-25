// Bunny mascot monster: wanders, screams when it spots the target, chases, attacks.
// Leave "Target" empty to drive it by keyboard (WASD/arrows, Shift = run, Space = attack, R = scream).
using UnityEngine;
using UnityEngine.Events;

namespace BunnyMonsterKit
{
    [RequireComponent(typeof(Animator))]
    public class BunnyMonster : MonoBehaviour
    {
        [Header("Target (player). Empty = keyboard test mode")]
        public Transform target;

        [Header("Movement (m/s) - matched to the clips so the feet don't slide")]
        public float walkSpeed = 0.9f;
        public float runSpeed = 2.6f;
        public float turnSpeed = 300f;
        public float gravity = -20f;

        [Header("AI")]
        public float detectRange = 10f;
        public float loseRange = 16f;
        [Range(0f, 180f)] public float viewHalfAngle = 75f;
        public LayerMask sightBlockers = ~0;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.6f;
        public float wanderRadius = 6f;
        public bool screamWhenSpotting = true;

        [Header("Events")]
        public UnityEvent onSpotted = new UnityEvent();
        public UnityEvent onAttackHit = new UnityEvent();
        [Tooltip("Seconds after the attack starts when the claws land")]
        public float attackHitDelay = 0.5f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int ScreamId = Animator.StringToHash("Scream");
        const float AttackLength = 1.4f;
        const float ScreamLength = 2.0f;

        Animator anim;
        CharacterController controller;
        Vector3 home;
        Vector3 wanderPoint;
        float waitUntil;
        float lockUntil;
        float nextAttack;
        float hitAt = -1f;
        float verticalSpeed;
        bool chasing;

        public bool IsChasing { get { return chasing; } }

        void Awake()
        {
            anim = GetComponent<Animator>();
            controller = GetComponent<CharacterController>();
            anim.applyRootMotion = false;
            home = transform.position;
            PickWanderPoint();
        }

        void Update()
        {
            Vector3 velocity;
            if (Time.time < lockUntil)
            {
                velocity = Vector3.zero;
                if (target != null) FaceTowards(target.position);
            }
            else if (target != null)
            {
                velocity = ThinkAI();
            }
            else
            {
                velocity = KeyboardTest();
            }

            Move(velocity);

            if (hitAt > 0f && Time.time >= hitAt)
            {
                hitAt = -1f;
                if (target != null && FlatDistance(target.position) <= attackRange + 0.5f)
                    onAttackHit.Invoke();
            }
        }

        // ------------------------------------------------------------------ public controls
        public void Attack()
        {
            nextAttack = Time.time + attackCooldown;
            hitAt = Time.time + attackHitDelay;
            PlayLocked(AttackId, AttackLength);
        }

        public void Scream()
        {
            PlayLocked(ScreamId, ScreamLength);
        }

        // ------------------------------------------------------------------ AI
        Vector3 ThinkAI()
        {
            float distance = FlatDistance(target.position);

            if (chasing && distance > loseRange)
            {
                chasing = false;
                PickWanderPoint();
            }

            if (!chasing && distance < detectRange && CanSeeTarget())
            {
                chasing = true;
                onSpotted.Invoke();
                if (screamWhenSpotting)
                {
                    Scream();
                    return Vector3.zero;
                }
            }

            if (chasing)
            {
                if (distance <= attackRange)
                {
                    FaceTowards(target.position);
                    if (Time.time >= nextAttack) Attack();
                    return Vector3.zero;
                }
                return DirectionTo(target.position) * runSpeed;
            }

            // wander around the spawn point, pausing between points
            if (Time.time < waitUntil) return Vector3.zero;
            if (FlatDistance(wanderPoint) < 0.4f)
            {
                waitUntil = Time.time + Random.Range(1.5f, 4f);
                PickWanderPoint();
                return Vector3.zero;
            }
            return DirectionTo(wanderPoint) * walkSpeed;
        }

        bool CanSeeTarget()
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, toTarget) > viewHalfAngle)
                return false;

            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 targetPoint = target.position + Vector3.up * 1.0f;
            RaycastHit hit;
            if (Physics.Linecast(eye, targetPoint, out hit, sightBlockers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != target && !hit.transform.IsChildOf(target) && !hit.transform.IsChildOf(transform))
                    return false;
            }
            return true;
        }

        void PickWanderPoint()
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;
            wanderPoint = home + new Vector3(r.x, 0f, r.y);
        }

        // ------------------------------------------------------------------ keyboard test
        Vector3 KeyboardTest()
        {
            if (BunnyInput.AttackDown()) { Attack(); return Vector3.zero; }
            if (BunnyInput.ScreamDown()) { Scream(); return Vector3.zero; }

            Vector2 m = BunnyInput.Move();
            Vector3 dir = new Vector3(m.x, 0f, m.y);
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
                Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
                dir = f * m.y + r * m.x;
            }
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            return dir * (BunnyInput.Run() ? runSpeed : walkSpeed);
        }

        // ------------------------------------------------------------------ helpers
        void PlayLocked(int trigger, float seconds)
        {
            anim.SetTrigger(trigger);
            lockUntil = Time.time + seconds;
        }

        void Move(Vector3 velocity)
        {
            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            if (speed > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(new Vector3(velocity.x, 0f, velocity.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }

            Vector3 step = transform.forward * speed;
            if (controller != null && controller.enabled)
            {
                verticalSpeed = controller.isGrounded ? -1f : verticalSpeed + gravity * Time.deltaTime;
                controller.Move((step + Vector3.up * verticalSpeed) * Time.deltaTime);
            }
            else
            {
                transform.position += step * Time.deltaTime;
            }

            anim.SetFloat(SpeedId, speed, 0.12f, Time.deltaTime);
        }

        void FaceTowards(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), turnSpeed * Time.deltaTime);
        }

        Vector3 DirectionTo(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.zero;
        }

        float FlatDistance(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
