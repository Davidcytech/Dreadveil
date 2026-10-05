using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
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

    //Estados do inimigo
    public float sightRange;
    public float attackRange;
    public bool playerInSightRange;
    public bool playerInAttackRange;

    private void Awake()
    {
       player = GameObject.Find("Ch45").transform;
       agent = GetComponent<NavMeshAgent>();

    }

    private void Update()
    {
        // verifica se o jogador esta na area de vi
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if(!playerInSightRange && !playerInAttackRange) Patrolling();
        if(playerInSightRange && !playerInAttackRange) ChasePlayer();
        if(playerInSightRange && playerInAttackRange) AttackPlayer();
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

       /* if(!alreadyAttacked)
        {
            Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
            rb.AddForce(transform.up * 8f, ForceMode.Impulse);

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), attackCooldown);

        }*/

    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

}
