using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
/* ===========================================
Referências
=============================================*/

    private Animator anim;
    private Player playerScript;

    public NavMeshAgent agent;
    public Transform player;


/* ===========================================
Patrulha
=============================================*/

    public Transform[] patrolPoints;    // pontos para marcar rota do inimigo
    private int currentPoint = 0;       // ponto de destino atual

/* ===========================================
Visão e deteção
=============================================*/
    public float sightRange;            // distância máxima de visão
    public float angle = 90f;           // ângulo do cone de visão
    public float crochSight = 0.5f;     // multiplicador do alcance quando o player está agachado
    public LayerMask visionMask;        // layers que o raycast de visão pode atingir
    public bool playerInSightRange;     // se está a ver o player 
    private bool wasPlayerInSightRange; 

/* ===========================================
Perseguição após perder de vista
=============================================*/

    public float loseSightTime = 5f;    // tempo a perseguir depois de perder o player
    private float lostSightTimer;       // temporizador de perda
    private Vector3 lastSeenPosition;   // última posição onde o player foi visto

/* ===========================================
Audição
=============================================*/

    public float searchTime = 4f;       // tempo a procurar quando chega ao local do som
    private bool investigating;         // está a ir investigar um som t/f
    private bool reachedSound;          // já chegou ao local do som t/f
    private Vector3 soundPosition;      // local do som
    private float searchTimer;          // temporizador da procura

    // Silêncio global: enquanto ativo, nenhum inimigo deste tipo ouve sons
    private static float silenceEndTime;

    public static void ActivateSilence(float duration)
    {
        silenceEndTime = Mathf.Max(silenceEndTime, Time.time + duration);
    }

/* ===========================================
Susto
=============================================*/

    public float scaredTime = 1.5f;     // tempo parado ao ver o player pela primeira vez
    private bool isScared;
    private float scaredTimer;

/* ===========================================
Ataque
=============================================*/

    public float attackRange;           // distância a que para e dispara
    public bool playerInAttackRange;
    public float attackCooldown;        // tempo entre disparos
    public bool alreadyAttacked;        // true = em cooldown
    public GameObject projectile;       // prefab da bala

/* ===========================================
Inibição de energia
=============================================*/

    public bool inhibitsPlayerEnergy;
    public float inhibitionRadius = 8f;             // área onde o player é inibido
    private const float inhibitionRefresh = 0.25f;  // duração renovada a cada frame na área

/* ===========================================
Alerta em cadeia
=============================================*/

    public bool alertsOthers = true;    // avisa os outros inimigos?
    public float alertDelay = 2f;       // tempo a ver o player antes de avisar
    public float alertRadius = 20f;     // quem estiver dentro deste raio é avisado
    private float alertTimer;
    private bool alertSent;             // já avisou neste avistamento?

    

    private void Awake()
    {
        player = GameObject.Find("vBasicController_character").transform;
        agent = GetComponent<NavMeshAgent>();

        anim = GetComponent<Animator>();
        playerScript = player.GetComponent<Player>();

    }

    private void Update()
    {
        // Se o agent não está ativo ou não está no NavMesh (ex.: inimigo abatido), não faz nada
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (anim != null && agent != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            anim.SetBool("IsMoving", currentSpeed > 0.1f);
        }


        // Inibe o player enquanto estiver na área; pára quando o inimigo morre
        if (inhibitsPlayerEnergy && playerScript != null &&
            Vector3.Distance(transform.position, player.position) <= inhibitionRadius)
        {
            playerScript.InhibitEnergy(inhibitionRefresh);
        }

        playerInSightRange = EnemyWallDetection();

        bool justSpotted = playerInSightRange && !wasPlayerInSightRange;

        if (justSpotted)
        {
            StartScare();
        }

        HandleAlert();

        wasPlayerInSightRange = playerInSightRange;

        playerInAttackRange = playerInSightRange && Vector3.Distance(transform.position, player.position) <= attackRange;

        // Máquina de estados:
        // 1. vê o player -> susto / ataque / perseguição
        // 2. perdeu de vista -> vai à última posição
        // 3. ouviu um som -> investiga
        // 4. nada -> patrulha

        if (playerInSightRange)
        {
            investigating = false;
            lostSightTimer = loseSightTime;
            lastSeenPosition = player.position;

            if (isScared)
            {
                Scared();
            }
            else if (playerInAttackRange)
            {
                SetShooting(true);
                AttackPlayer();
            }
            else
            {
                SetShooting(false);
                ChasePlayer();
            }
        }
        else
        {
            // perdeu o player de vista: cancela o susto/disparo
            if (isScared) EndScare();
            SetShooting(false);

            if (lostSightTimer > 0f)
            {
                lostSightTimer -= Time.deltaTime;
                agent.isStopped = false;
                agent.SetDestination(lastSeenPosition);
            }

            else if (investigating) Investigate();
            else Patrolling();
        }
    }

/* ===========================================
Audição
=============================================*/

    // Chamado pelo alarme (ou por qualquer outro som)
    public void HearSound(Vector3 position)
    {
        if (Time.time < silenceEndTime)
        return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (playerInSightRange) return;

        // raio pequeno, para não saltar para outra zona
        if (!NavMesh.SamplePosition(position, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
            return;

        // só aceita se existir um caminho completo até lá
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(navHit.position, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning(name + ": sem caminho completo até ao alarme", this);
            return;
        }

        soundPosition = navHit.position;
        investigating = true;
        searchTimer = searchTime;
        agent.isStopped = false;
        reachedSound = false;
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
                reachedSound = false;
                agent.isStopped = false;

                if (patrolPoints.Length > 0)
                    agent.SetDestination(patrolPoints[currentPoint].position);
            }

            return;
        }

        agent.isStopped = false;
        agent.SetDestination(soundPosition);

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <= agent.stoppingDistance + 2f)
        {
            reachedSound = true;
            searchTimer = searchTime;
            agent.isStopped = true;
        }
    }

/* ===========================================
Visão
=============================================*/

    private bool EnemyWallDetection()
    {
        vCrouchController crouchScript = player.GetComponent<vCrouchController>();
        bool isCrouching = crouchScript.isCrouching;
        float currentSightRange;

        if (isCrouching)
        {
            currentSightRange = sightRange * crochSight;
        }
        else
        {
            currentSightRange = sightRange;
        }

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * 1f;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance > currentSightRange)
        {
            return false;
        }

        float viewAngle = Vector3.Angle(transform.forward, direction);

        if (viewAngle > angle / 2f)
        {
            return false;
        }

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, visionMask))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                return true;
            }
        }

        return false;
    }

/* ===========================================
Patrulha e perseguição
=============================================*/

    private void Patrolling()
    {
        if (patrolPoints.Length == 0)
        {
            return;
        }

        agent.SetDestination(patrolPoints[currentPoint].position);
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            currentPoint++;
            if (currentPoint >= patrolPoints.Length)
            {
                currentPoint = 0;
            }

        }
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);

    }

/* ===========================================
Ataque
=============================================*/

    private void AttackPlayer()
    {
        agent.SetDestination(transform.position); //para

        // olha para o player
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
 
        // dispara se não estiver em cooldown
        if (!alreadyAttacked)
        {
            GameObject bullet = Instantiate(projectile, transform.position + transform.forward * 1.2f + Vector3.up, Quaternion.LookRotation(direction));
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 shootDirection = (player.position - bullet.transform.position).normalized;
                rb.AddForce(transform.forward * 10f, ForceMode.Impulse);
            }

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), attackCooldown);
        }
    }

/* ===========================================
Susto
=============================================*/

    private void StartScare()
    {
        isScared = true;
        scaredTimer = scaredTime;
        alreadyAttacked = true;                 // impede o tiro durante o susto
        CancelInvoke(nameof(ResetAttack));

        agent.isStopped = true;
        agent.ResetPath();

        if (anim != null)
        {
            anim.SetBool("IsShooting", false);
            anim.SetBool("IsScared", true);
        }
    }

    private void Scared()
    {
        // fica parado, mas vira-se para o player
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        scaredTimer -= Time.deltaTime;

        if (scaredTimer <= 0f)
        {
            EndScare();
            alreadyAttacked = false;           // agora já pode disparar
        }
    }

    private void EndScare()
    {
        isScared = false;
        alreadyAttacked = false; 
        agent.isStopped = false;

        if (anim != null)
            anim.SetBool("IsScared", false);
    }

    private void SetShooting(bool value)
    {
        if (anim != null)
            anim.SetBool("IsShooting", value);
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

/* ===========================================
Alerta em cadeia
=============================================*/
    
    
    // Depois de alertDelay segundos a ver o player, avisa os outros
    private void HandleAlert()
    {
        if (!alertsOthers) return;

        // perdeu o player reinicia
        if (!playerInSightRange){
            alertTimer = 0f;
            alertSent = false;  
            return;
        }

        if (alertSent) return;
        alertTimer += Time.deltaTime;
        if (alertTimer >= alertDelay)
        {
            alertSent = true;
            AlertNearbyEnemies();
        }
    }

    // Manda os inimigos dentro de alertRadius investigar onde o player está
    private void AlertNearbyEnemies()
    {
        Vector3 lastKnown = player.position;

        foreach (Enemy other in FindObjectsOfType<Enemy>())
        {
            if (other == null || other == this) continue;
            if (Vector3.Distance(transform.position, other.transform.position) > alertRadius) continue;

            other.HearSound(lastKnown);
        }

        foreach (EnemyMelee melee in FindObjectsOfType<EnemyMelee>())
        {
            if (melee == null) continue;
            if (Vector3.Distance(transform.position, melee.transform.position) > alertRadius) continue;

            melee.HearSound(lastKnown);
        }
    }



}

