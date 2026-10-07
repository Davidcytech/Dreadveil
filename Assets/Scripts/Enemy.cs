using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private Animator anim;

    public NavMeshAgent agent;

    public Transform player;

    public LayerMask whatIsGround;
    public LayerMask whatIsPlayer;


    // Movimento de patrulha
    public Transform[] patrolPoints;
    private int currentPoint = 0;

    // Ataque
    public float attackCooldown;
    public bool alreadyAttacked;
    public GameObject projectile;

    //Estados do inimigo e deteção
    public float sightRange;
    public float attackRange;
    public bool playerInSightRange;
    public bool playerInAttackRange;
    public LayerMask visionMask;
    public float angle = 90f;
    public float crochSight = 0.5f;

    private void Awake()
    {
       player = GameObject.Find("vBasicController_character").transform;
       agent = GetComponent<NavMeshAgent>();

       anim = GetComponent<Animator>();

    }

    private void Update()
    {
        if (anim != null && agent != null)
    {
        float currentSpeed = agent.velocity.magnitude;
        anim.SetBool("IsMoving", currentSpeed > 0.1f);   // Se usares a variável bool "IsMoving"
    }
    
        // verifica se o jogador esta na area de vi
        playerInSightRange = EnemyWallDetection();
        playerInAttackRange = playerInSightRange && Vector3.Distance(transform.position, player.position) <= attackRange;

        if(!playerInSightRange && !playerInAttackRange) Patrolling();
        if(playerInSightRange && !playerInAttackRange) ChasePlayer();
        if(playerInSightRange && playerInAttackRange) AttackPlayer();
    }

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

    private void Patrolling()
    {
       if(patrolPoints.Length == 0)
       {
        return;
       }

        agent.SetDestination(patrolPoints[currentPoint].position);
        if(!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
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

    private void AttackPlayer()
    {
        agent.SetDestination(transform.position);

       // transform.LookAt(player);
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if(direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation,targetRotation,Time.deltaTime * 10f);
        }

        if(!alreadyAttacked)
        {
            GameObject bullet = Instantiate( projectile, transform.position + transform.forward * 1.2f + Vector3.up, Quaternion.LookRotation(direction));
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

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }




}


       /* if(!alreadyAttacked)
        {
            Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
            rb.AddForce(transform.up * 8f, ForceMode.Impulse);

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), attackCooldown);

        }*/