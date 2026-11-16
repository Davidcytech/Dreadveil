using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Boss : MonoBehaviour
{
    private enum State { Patroling, Chase, Attacking, Cooldown, Dead }
    private State state = State.Patroling;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private vCrouchController crouch;
    private BossAttacks attacks;
    public bool IsDead => state == State.Dead;

    // Patrulha
    public Transform patrolCenter;
    public float patrolRadius = 15f;
    public float patrolSpeed = 1.5f;
    public float waitTimeAtPoint = 2f;
    public float chaseSpeed = 2f;
    public float loseSightTime = 3f;

    // Visão
    public float sightRange = 15f;
    public float angle = 130f;
    public float crouchSight = 0.5f;
    public LayerMask visionMask;
    public float eyeHeight = 1.5f;                 
    private Collider playerCollider;               

    // Obstáculos na ronda 
    public LayerMask obstacleMask;                 // escolhe só a layer "Obstaculo"
    public float obstacleCheckDistance = 1.5f;     // distância à frente a verificar
    public float obstacleCheckRadius = 0.5f;       // espessura da verificação
    public float blockedTime = 0.4f;               // tempo parado com obstáculo à frente até desistir do ponto
    public float stuckTime = 1.5f;                 // tempo parado sem razão aparente até desistir
    public float maxTimePerPoint = 15f;            // limite de tempo para chegar a cada ponto
    public float minPointDistance = 3f;            // não escolhe pontos colados a ele
    private float stuckTimer;
    private float pointTimer;
    private NavMeshPath pathCheck;

    // Ataques
    public float attackCooldown = 3f;
    public int backstabsToKill = 3;
    public float backstabAngle = 60f;
    public float backstabCooldown = 1.5f;
    public int backstabsTaken;

    public bool playerInSightRange;
    public float blindTime = 5f;               // tempo em que não vê o player depois do ataque
    private bool reacting;
    public string deathStateName = "";
    private float blindTimer;

    private Vector3 center;
    private Vector3 currentTarget;
    private Vector3 lastSeenPosition;

    private bool hasTarget;
    private bool moveDuringAttack;   // true só durante o Spike
    private bool waiting;
    private bool running;               // true quando está em modo perseguição
    private bool chaseDuringCooldown;   // true depois do Spike

    private float waitTimer;
    private float lostSightTimer;
    private float cooldownTimer;
    private float nextBackstabTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        attacks = GetComponent<BossAttacks>();

        GameObject p = GameObject.Find("vBasicController_character");

        if (p != null)
        {
            player = p.transform;
            crouch = p.GetComponent<vCrouchController>();
            playerCollider = p.GetComponentInChildren<Collider>();   // para uma melhor visao do player
        }
    }

    private void Start()
    {
        center = patrolCenter != null ? patrolCenter.position : transform.position;
        lastSeenPosition = transform.position;
        pathCheck = new NavMeshPath();                                // pathtracking

        if (agent != null)
            agent.speed = patrolSpeed;

        if (attacks == null)
            Debug.LogError("Falta adicionar BossAttacks ao Boss!");
    }

    private void Update()
    {
        if (state == State.Dead || player == null || agent == null)
            return;

        if (blindTimer > 0f)
        blindTimer -= Time.deltaTime;

        playerInSightRange = blindTimer <= 0f && CanSeePlayer();

        if (playerInSightRange)
        {
            lastSeenPosition = player.position;
            lostSightTimer = loseSightTime;
        }

        if (state == State.Attacking)
        {
            if (moveDuringAttack)
                ChaseWhileAttacking();
            return;
        }

        if (state == State.Cooldown)
        {
            HandleCooldown();
            return;
        }

        if (playerInSightRange)
        {
            waiting = false;
            hasTarget = false;

            float dist = FlatDistanceToPlayer();

            if (attacks == null)
            {
                ChasePlayer();
                return;
            }

            if (dist <= attacks.slamTriggerRange)
            {
                StartCoroutine(RunAttack(attacks.SlamAttack(), false));
            }
            else if (dist <= attacks.spikeRange)
            {
                // depois do Spike, persegue durante o cooldown
                StartCoroutine(RunAttack(attacks.SpikeAttack(), true));
            }
            else
            {
                ChasePlayer();
            }
        }
        else if (lostSightTimer > 0f)
        {
            lostSightTimer -= Time.deltaTime;
            ChaseLastSeen();                          
        }
        else
        {
            Patroling();
        }
    }

    private void LateUpdate()
    {
        UpdateAnimator();
    }

    // ---------------- ANIMAÇÕES ----------------
    // Parâmetros do Animator: IsMoving (bool), IsRunning (bool), Attack (trigger)
    private void UpdateAnimator()
    {
        if (anim == null || agent == null)
            return;

        bool moving = state != State.Dead &&
              (state != State.Attacking || moveDuringAttack) &&
              !agent.isStopped &&
              agent.velocity.magnitude > 0.1f;

        anim.SetBool("IsMoving", moving);
        anim.SetBool("IsRunning", moving && running);
    }

    // Chamado pelo BossAttacks (uma vez no Slam, uma vez por espinho no Spike)
    public void PlayAttackAnimation()
    {
        if (anim == null) return;

        anim.ResetTrigger("Attack");
        anim.SetTrigger("Attack");
    }

    // ---------------- COOLDOWN ----------------
    private void HandleCooldown()
    {
        cooldownTimer -= Time.deltaTime;

        if (chaseDuringCooldown)
        {
            // Perto o suficiente e à vista: Slam imediato
            if (playerInSightRange && attacks != null &&
                FlatDistanceToPlayer() <= attacks.slamTriggerRange)
            {
                StartCoroutine(RunAttack(attacks.SlamAttack(), false));
                return;
            }

            // Persegue o player se o vê, senão vai à última posição conhecida
            running = true;
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(playerInSightRange ? player.position : lastSeenPosition);
        }
        else
        {
            running = false;
            agent.isStopped = true;
        }

        if (cooldownTimer <= 0f)
        {
            agent.isStopped = false;
            chaseDuringCooldown = false;
            state = State.Patroling;
        }
    }

    // ---------------- PATRULHA / PERSEGUIÇÃO ----------------

    //agora deteta obstáculos e fica preso
    private void Patroling()
    {
        state = State.Patroling;
        running = false;
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
            if (!PickRandomPoint())
                return;

            agent.SetDestination(currentTarget);
            hasTarget = true;
            stuckTimer = 0f;                           // NOVO
            pointTimer = 0f;                           // NOVO
            return;                                    // NOVO
        }

        // chegou ao ponto: espera
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            waiting = true;
            waitTimer = waitTimeAtPoint;
            return;
        }

        // verifica se está preso ou a bater num obstaculo
        if (agent.pathPending)
            return;

        pointTimer += Time.deltaTime;

        bool slow = agent.velocity.magnitude < 0.2f;
        stuckTimer = slow ? stuckTimer + Time.deltaTime : 0f;

        float limit = ObstacleAhead() ? blockedTime : stuckTime;

        if (stuckTimer >= limit || pointTimer >= maxTimePerPoint)
            GiveUpPoint();
    }

    //desiste deste ponto e escolhe outro
    private void GiveUpPoint()
    {
        hasTarget = false;
        stuckTimer = 0f;
        agent.ResetPath();
    }

    // verifca se ha um obstaculo q o impede de andar
    private bool ObstacleAhead()
    {
        Vector3 dir = agent.desiredVelocity;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;

        Vector3 origin = transform.position + Vector3.up * 0.9f;

        return Physics.SphereCast(origin, obstacleCheckRadius, dir.normalized,out RaycastHit hit, obstacleCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(transform);
    }

    private bool PickRandomPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = center + new Vector3(r.x, 0f, r.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                continue;

            // nap escolhe pontos colados a ele
            if (Vector3.Distance(transform.position, hit.position) < minPointDistance)
                continue;

            // apenas aceita se tiver uma caminho completo ate la
            if (!agent.CalculatePath(hit.position, pathCheck) || pathCheck.status != NavMeshPathStatus.PathComplete)
                continue;

            currentTarget = hit.position;
            return true;
        }

        return false;
    }

    private void ChasePlayer()
    {
        state = State.Chase;
        running = true;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
    }

    // vai ao último sítio onde viu o player e nao ate a posição atual do player
    private void ChaseLastSeen()
    {
        state = State.Chase;
        running = true;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(lastSeenPosition);
    }

    public void SetChasingDuringAttack(bool value)
    {
        moveDuringAttack = value;

        if (!value)
        {
            running = false;
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private IEnumerator RunAttack(IEnumerator attack, bool chaseAfter)
    {
        state = State.Attacking;
        moveDuringAttack = false;
        running = false;
        agent.isStopped = true;
        agent.ResetPath();

        yield return StartCoroutine(attack);

        moveDuringAttack = false;

        if (state == State.Dead)
            yield break;

        chaseDuringCooldown = chaseAfter;
        agent.isStopped = !chaseAfter;
        state = State.Cooldown;
        cooldownTimer = attackCooldown;
    }

    private float FlatDistanceToPlayer()
    {
        Vector3 flat = player.position - transform.position;
        flat.y = 0f;
        return flat.magnitude;
    }

    // ---------------- BACKSTAB / MORTE ----------------
    public bool CanBeStabbed(Transform attacker)
    {
        return state != State.Dead && state != State.Attacking && !reacting;
    }

    // Chamado pelo player no início do takedown
    public void StartStealthHit(float reactDelay)
    {
        StartCoroutine(StealthHitRoutine(reactDelay));
    }

    private IEnumerator StealthHitRoutine(float reactDelay)
    {
        reacting = true;
        backstabsTaken++;
        Debug.Log("Boss: golpe furtivo " + backstabsTaken + "/" + backstabsToKill);

        // pára logo, para o player conseguir posicionar-se
        state = State.Attacking;
        moveDuringAttack = false;
        running = false;
        agent.isStopped = true;
        agent.ResetPath();

        yield return new WaitForSeconds(reactDelay);

        // 3.º golpe: morre
        if (backstabsTaken >= backstabsToKill)
        {
            Die();

            if (anim != null && !string.IsNullOrEmpty(deathStateName))
                anim.Play(deathStateName, 0, 0f);

            yield break;
        }

        // reação: BossTaker -> Roar (a transição já existe no Animator)
        if (anim != null)
        {
            anim.Play("BossTaker", 0, 0f);
            yield return StartCoroutine(WaitForStateEnd("BossTaker"));
            yield return StartCoroutine(WaitForStateEnd("Roar"));
        }

        if (state == State.Dead) yield break;

        // durante o ataque ele sabe onde o player está
        lastSeenPosition = player.position;
        yield return StartCoroutine(attacks.SpikeAttack());

        if (state == State.Dead) yield break;

        // depois do ataque: esquece o player
        lostSightTimer = 0f;
        lastSeenPosition = transform.position;
        blindTimer = blindTime;
        hasTarget = false;
        waiting = false;

        chaseDuringCooldown = false;       // fica parado a "procurar"
        agent.isStopped = true;
        state = State.Cooldown;
        cooldownTimer = attackCooldown;
        reacting = false;
    }

    private IEnumerator WaitForStateEnd(string stateName)
    {
        float timeout = 2f;

        while (!anim.GetCurrentAnimatorStateInfo(0).IsName(stateName) && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        while (anim.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
            anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
            yield return null;
    }

    [ContextMenu("Test Stealth Hit")]
    private void TestStealthHit()
    {
        if (Application.isPlaying)
            StartStealthHit(0f);
    }

    private void Die()
    {
        moveDuringAttack = false;
        state = State.Dead;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
            anim.SetBool("IsRunning", false);
        }

        Debug.Log("Boss derrotado!");
    }

    private void ChaseWhileAttacking()
    {
        running = true;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(playerInSightRange ? player.position : lastSeenPosition);
    }

    // ---------------- VISÃO ----------------
    private bool CanSeePlayer()
    {
        bool crouching = crouch != null && crouch.isCrouching;
        float range = crouching ? sightRange * crouchSight : sightRange;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;

        Vector3 bodyCenter = playerCollider != null ? playerCollider.bounds.center : player.position + Vector3.up;
        float top = playerCollider != null ? playerCollider.bounds.extents.y * 0.8f : 0.6f;

        return CanSeePoint(origin, bodyCenter, range) ||
               CanSeePoint(origin, bodyCenter + Vector3.up * top, range);
    }

    //verifica distância, ângulo e linha de vista para um ponto
    private bool CanSeePoint(Vector3 origin, Vector3 point, float range)
    {
        Vector3 dir = point - origin;
        float dist = dir.magnitude;

        if (dist > range)
            return false;

        if (Vector3.Angle(transform.forward, dir) > angle / 2f)
            return false;

        // RaycastAll para ignorar os colliders do próprio boss
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, visionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
                continue;

            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying
            ? center
            : patrolCenter != null ? patrolCenter.position : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(c, patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (attacks != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, attacks.slamRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attacks.spikeRange);
        }

        Vector3 o = transform.position + Vector3.up * 1.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, -angle / 2f, 0) * transform.forward * sightRange);
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, angle / 2f, 0) * transform.forward * sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, 180 - backstabAngle, 0) * transform.forward * 3f);
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, 180 + backstabAngle, 0) * transform.forward * 3f);
    }
}