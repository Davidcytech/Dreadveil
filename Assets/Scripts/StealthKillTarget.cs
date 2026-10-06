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
        
        // Tenta encontrar o script de IA do inimigo ("Enemy")
        enemyAIScript = GetComponent("Enemy") as MonoBehaviour;
    }

    /// <summary>
    /// Desativa a IA e prepara o inimigo para sofrer o abate.
    /// </summary>
    public void StartTakedown()
    {
        isKilled = true;

        // Desativa o script de IA do inimigo
        if (enemyAIScript != null)
            enemyAIScript.enabled = false;

        // Para e desativa o NavMeshAgent para não tentar andar
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }

        // Bloqueia a física rígida durante a animação
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Congela o inimigo na pose final da animação de morte/desmaio.
    /// </summary>
    public void FinishTakedown()
    {
        // Desativa o Animator no último frame para o corpo não voltar à pose inicial (T-Pose/Idle)
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Converte o collider para Trigger para o jogador não colidir/tropeçar no corpo no chão
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (takedownPoint == null) return;

        // Posição onde o jogador se vai colocar
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(takedownPoint.position, 0.25f);

        // Direção para onde o jogador fica virado
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(takedownPoint.position, takedownPoint.forward * 0.8f);
    }
}