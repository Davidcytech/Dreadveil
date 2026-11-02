
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Boss : MonoBehaviour
{
    private enum State{ Patroling, Chase, Attacking, Cooldown,Dead }
    private State state = State.Patroling;

    private NavMeshAgent agent;
    private Transform player;
    private vCrouchController crouch;
    private BossAttacks attacks;
    public bool IsDead => state == State.Dead;

    //patrulha
    public Transform patrolCenter;
    public float patrolRadius = 15f;
    public float patrolSpeed = 1.5f;
    public float waitTimeAtPoint = 2f;
    public float chaseSpeed = 2f;
    public float loseSightTime = 3f;

    //visao
    public float sightRange = 15f;
    public float angle = 130f;
    public float crouchSight = 0.5f;
    public LayerMask visionMask;

    //Ataques
    public float attackCooldown = 3f;
    public int backstabsToKill = 3;
    public float backstabAngle = 60f;
    public float backstabCooldown = 1.5f;
    public int backstabsTaken;

    public bool playerInSightRange;

    private Vector3 center;
    private Vector3 currentTarget;

    private bool hasTarget;
    private bool waiting;

    private float waitTimer;
    private float lostSightTimer;
    private float cooldownTimer;
    private float nextBackstabTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attacks = GetComponent<BossAttacks>();

        GameObject p = GameObject.Find("vBasicController_character");

        if (p != null)
        {
            player = p.transform;
            crouch = p.GetComponent<vCrouchController>();
        }
    }

    private void Start()
    {
        center = patrolCenter != null? patrolCenter.position: transform.position;

        if (agent != null)
            agent.speed = patrolSpeed;

        if (attacks == null)
            Debug.LogError("Falta adicionar BossAttacks ao Boss!");
    }

    private void Update()
    {
        if (state == State.Dead || player == null || agent == null)
            return;

        playerInSightRange = CanSeePlayer();

        if (state == State.Attacking)
            return;

        if (state == State.Cooldown)
        {
            agent.isStopped = true;
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)        
            {
                agent.isStopped = false;
                state = State.Patroling;
            }

            return;
        }

        if (playerInSightRange)
        {
            lostSightTimer = loseSightTime;
            waiting = false;
            hasTarget = false;

            Vector3 flat = player.position - transform.position;
            flat.y = 0f;

            float dist = flat.magnitude;

            if (attacks == null)
            {
                ChasePlayer();
                return;
            }

            if (dist <= attacks.slamTriggerRange)
            {
                StartCoroutine(RunAttack(attacks.SlamAttack()));
            }
            else if (dist <= attacks.spikeRange)
            {
                StartCoroutine(RunAttack(attacks.SpikeAttack()));
            }
            else
            {
                ChasePlayer();
            }
        }
        else if (lostSightTimer > 0f)
        {
            lostSightTimer -= Time.deltaTime;
            ChasePlayer();
        }
        else
        {
            Patroling();
        }
    }

    private void Patroling()
    {
        state = State.Patroling;
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        if (waiting)
        {
            agent.isStopped = true;
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                waiting = false;
                hasTarget = false;
                agent.isStopped = false;
            }

            return;
        }

        if (!hasTarget)
        {
            if (!PickRandomPoint()){
                return;
            }

            agent.SetDestination(currentTarget);
            hasTarget = true;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            waiting = true;
            waitTimer = waitTimeAtPoint;
        }
    }

    private bool PickRandomPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;

            Vector3 candidate = center +
                new Vector3(r.x, 0f, r.y);

            if (NavMesh.SamplePosition(candidate,out NavMeshHit hit,3f,NavMesh.AllAreas))
            {
                currentTarget = hit.position;
                return true;
            }
        }

        return false;
    }

    private void ChasePlayer()
    {
        state = State.Chase;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
    }

    private IEnumerator RunAttack(IEnumerator attack)
    {
        state = State.Attacking;
        agent.isStopped = true;
        agent.ResetPath();

        yield return StartCoroutine(attack);

        if (state == State.Dead)
        {
            yield break;
        } 
        state = State.Cooldown;
        cooldownTimer = attackCooldown;
    }

    public bool TakeBackstab(Transform attacker)
    {
        if (state == State.Dead ||
            Time.time < nextBackstabTime)
            return false;

        Vector3 toAttacker = attacker.position - transform.position;
        toAttacker.y = 0f;

        if (Vector3.Angle(-transform.forward, toAttacker)> backstabAngle)
        {
            return false;
        }

        RegisterBackstab();
        return true;
    }

    private void RegisterBackstab()
    {
        backstabsTaken++;
        nextBackstabTime = Time.time + backstabCooldown;

        Debug.Log("Boss: ataque pelas costas " + backstabsTaken + "/" + backstabsToKill);

        if (backstabsTaken >= backstabsToKill)
            Die();
    }

   // Apenas para ver se a backstab funciona
    private void TestBackstab()
    {
        if (Application.isPlaying)
            RegisterBackstab();
    }

    private void Die()
    {
        state = State.Dead;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        Debug.Log("Boss derrotado!");
    }

    private bool CanSeePlayer()
    {
        bool crouching = crouch != null && crouch.isCrouching;

        float range = crouching ? sightRange * crouchSight : sightRange;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up;

        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        if (dist > range) {
            return false;
        }

        if (Vector3.Angle(transform.forward, dir) > angle / 2f)
            return false;

        if (Physics.Raycast(origin,dir.normalized,out RaycastHit hit,dist,visionMask))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }
        return false;
    }



    // Apenas um teste :D

    private void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying
            ? center
            : patrolCenter != null
                ? patrolCenter.position
                : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(c, patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (attacks != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position,
                attacks.slamRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(
                transform.position,
                attacks.spikeRange);
        }

        Vector3 o = transform.position + Vector3.up * 1.5f;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            o,
            o + Quaternion.Euler(0, -angle / 2f, 0)
            * transform.forward * sightRange);

        Gizmos.DrawLine(
            o,
            o + Quaternion.Euler(0, angle / 2f, 0)
            * transform.forward * sightRange);

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            o,
            o + Quaternion.Euler(
                0, 180 - backstabAngle, 0)
            * transform.forward * 3f);

        Gizmos.DrawLine(
            o,
            o + Quaternion.Euler(
                0, 180 + backstabAngle, 0)
            * transform.forward * 3f);
    }
}
