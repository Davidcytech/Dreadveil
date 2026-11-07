using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMelee : MonoBehaviour
{
    private Animator anim;
    private NavMeshAgent agent;
    private Transform player;
    private vCrouchController crouchScript;
    private Collider playerCollider;
    private Player playerScript;

    // Patrulha
    public Transform[] patrolPoints;
    private int currentPoint;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    // Visão
    public float sightRange = 10f;
    public float angle = 90f;
    public float crochSight = 0.5f;
    public float eyeHeight = 1.5f;
    public LayerMask visionMask;
    public bool playerInSightRange;

    // Ataque
    public float catchDistance = 1.5f;
    public float damage = 100f;
    public float attackCooldown = 1f;
    private float nextAttackTime;

    // Audição
    public float searchTime = 4f;
    private bool investigating;
    private bool reachedSound;
    private Vector3 soundPosition;
    private float searchTimer;

    // Susto
    public float scaredTime = 2f;
    private bool isScared;
    private float scaredTimer;
    private bool wasPlayerInSightRange;

    private void Awake()
    {
        player = GameObject.Find("vBasicController_character").transform;
        crouchScript = player.GetComponent<vCrouchController>();
        playerCollider = player.GetComponentInChildren<Collider>();
        playerScript = player.GetComponentInParent<Player>();

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        agent.speed = patrolSpeed;
    }

    private void Update()
{
    if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        return;

    if (anim != null)
        anim.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);

    playerInSightRange = CanSeePlayer();

    if (playerInSightRange && !wasPlayerInSightRange)
        StartScare();

    wasPlayerInSightRange = playerInSightRange;

    if (playerInSightRange)
    {
        investigating = false;

        if (isScared)
        {
            Scared();
        }
        else
        {
            ChaseAndKick();
        }
    }
    else
    {
        // perdeu o player de vista
        if (isScared) EndScare();
        SetRunning(false);

        agent.speed = patrolSpeed;

        if (investigating) Investigate();
        else Patrol();
    }
}

    public void HearSound(Vector3 position)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (playerInSightRange) return;

        if (!NavMesh.SamplePosition(position, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
            return;

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(navHit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return;

        soundPosition = navHit.position;
        investigating = true;
        reachedSound = false;
        agent.isStopped = false;
    }

    private void Investigate()
    {
        if (reachedSound)
        {
            searchTimer -= Time.deltaTime;

            if (searchTimer <= 0f)
            {
                investigating = false;
                agent.isStopped = false;
            }
            return;
        }

        agent.SetDestination(soundPosition);

        if (!agent.pathPending && agent.hasPath &&
            agent.remainingDistance <= agent.stoppingDistance + 2f)
        {
            reachedSound = true;
            searchTimer = searchTime;
            agent.isStopped = true;
        }
    }

    private bool CanSeePlayer()
    {
        float range = crouchScript.isCrouching ? sightRange * crochSight : sightRange;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 direction = playerCollider.bounds.center - origin;
        float distance = direction.magnitude;

        if (distance > range || Vector3.Angle(transform.forward, direction) > angle / 2f)
            return false;

        // RaycastAll para ignorar o próprio inimigo
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, visionMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
                continue;

            return hit.transform.IsChildOf(player);
        }

        return false;
    }

    private void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPoint].position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
    }

    private void StartScare()
    {
        isScared = true;
        scaredTimer = scaredTime;

        agent.isStopped = true;
        agent.ResetPath();

        if (anim != null)
        {
            anim.SetBool("IsRunning", false);
            anim.SetBool("IsScared", true);
        }
    }

    private void Scared()
    {
        // fica parado, mas vira-se para o player
        FacePlayer();

        scaredTimer -= Time.deltaTime;

        if (scaredTimer <= 0f)
        {
            EndScare();
            SetRunning(true);   // no mesmo frame, para não haver "buracos" no Animator
        }
    }

    private void EndScare()
    {
        isScared = false;
        agent.isStopped = false;

        if (anim != null)
            anim.SetBool("IsScared", false);
    }

    private void ChaseAndKick()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= catchDistance)
        {
            // perto: pára e dá o pontapé
            agent.isStopped = true;
            FacePlayer();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                if (anim != null) anim.SetTrigger("Kick");
            }
        }
        else
        {
            // longe: corre atrás do player
            SetRunning(true);
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
    }

    public void DealKickDamage()
{
    if (isScared || playerScript == null) return;

    if (Vector3.Distance(transform.position, player.position) <= catchDistance + 0.5f)
        playerScript.TakeDamage(damage);
}

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void SetRunning(bool value)
    {
        if (anim != null)
            anim.SetBool("IsRunning", value);
    }
}