using UnityEngine;
using UnityEngine.AI;

public class StealthKillTarget : MonoBehaviour
{
    [Header("Stealth Kill Settings")]
    [SerializeField] private Transform takedownPoint;

    private bool isKilled = false;

    public Transform TakedownPoint => takedownPoint;
    public bool CanBeKilled => !isKilled;

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    private MonoBehaviour enemyAIScript;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyAIScript = GetComponent("Enemy") as MonoBehaviour;
    }

    public void StartTakedown()
    {
        isKilled = true;

        if (enemyAIScript != null)
            enemyAIScript.enabled = false;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public void FinishTakedown()
    {
        // Congela o inimigo no chão na pose final da animação
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Converte o collider em Trigger para não bloquear a passagem do player
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (takedownPoint == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(takedownPoint.position, 0.25f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(takedownPoint.position, takedownPoint.forward * 0.8f);
    }
}