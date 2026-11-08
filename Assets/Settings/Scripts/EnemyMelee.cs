using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMelee : MonoBehaviour
{
    /* ===========================================
Referências
=============================================*/
    private Animator anim;
    private NavMeshAgent agent;
    private Transform player;
    private vCrouchController crouchScript;
    private Collider playerCollider;        // usado para apontar ao centro do player
    private Player playerScript;

/* ===========================================
Patrulha
=============================================*/
    public Transform[] patrolPoints;        // pontos por onde o inimigo anda
    private int currentPoint;               // ponto de destino atual
    public float patrolSpeed = 2f;          // velocidade a patrulhar / investigar
    public float chaseSpeed = 5f;           // velocidade a perseguir

/* ===========================================
Visão e deteção
=============================================*/
    public float sightRange = 10f;          // distância máxima de visão
    public float angle = 90f;               // ângulo do cone de visão
    public float crochSight = 0.5f;         // multiplicador do alcance quando o player está agachado
    public float eyeHeight = 1.5f;          // altura dos olhos
    public LayerMask visionMask;            // layers que o raycast de visão pode atingir
    public bool playerInSightRange;         // está a ver o player?
    private bool wasPlayerInSightRange;     // estado no frame anterior (para detetar o 1º avistamento)

/* ===========================================
Audição
=============================================*/
    public float searchTime = 4f;           // tempo a procurar quando chega ao local do som
    private bool investigating;             // está a ir investigar um som?
    private bool reachedSound;              // já chegou ao local do som?
    private Vector3 soundPosition;          // local do som
    private float searchTimer;              // temporizador da procura

/* ===========================================
Susto
=============================================*/
    public float scaredTime = 2f;           // tempo parado ao ver o player pela 1ª vez
    private bool isScared;
    private float scaredTimer;

/* ===========================================
Ataque
=============================================*/
    public float catchDistance = 1.5f;      // distância a que pára e dá o pontapé
    public float damage = 100f;             // dano do pontapé
    public float attackCooldown = 1f;       // tempo entre pontapés
    private float nextAttackTime;           // momento em que pode voltar a atacar


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
     // Agent inativo ou fora do NavMesh (ex.: inimigo abatido): não faz nada
    if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        return;

     // Animação de andar
    if (anim != null)
        anim.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);

    // Deteção
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

/* ===========================================
Visão
=============================================*/

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

/* ===========================================
Audição
=============================================*/
    // Chamado pelo alarme (ou por qualquer outro som)
    public void HearSound(Vector3 position)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (playerInSightRange) return;
        
        // raio pequeno, para não saltar para outra zona
        if (!NavMesh.SamplePosition(position, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
            return;

        // só aceita se existir um caminho completo até lá
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(navHit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return;

        soundPosition = navHit.position;
        investigating = true;
        reachedSound = false;
        agent.isStopped = false;
    }

    // Vai ao local do som, espera searchTime e volta à patrulha
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


/* ===========================================
Patrulha
=============================================*/

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

/* ===========================================
Susto
=============================================*/

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

/* ===========================================
Perseguição e ataque
=============================================*/
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