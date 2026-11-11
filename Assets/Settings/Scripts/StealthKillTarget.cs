using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StealthKillTarget : MonoBehaviour
{
    [Header("Stealth Kill Settings")]
    [SerializeField] private Transform takedownPoint;

    [Header("Knife Kill (longo alcance)")]
    [Tooltip("Estado do Animator do inimigo a tocar")]
    [SerializeField] private string knifeDeathStateName = "RangeTaker";

    private bool isKilled = false;

    public Transform TakedownPoint => takedownPoint;
    public bool CanBeKilled => !isKilled;

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    private MonoBehaviour[] enemyAIScripts;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Suporta os dois tipos de inimigo (Enemy e EnemyMelee).
        enemyAIScripts = new MonoBehaviour[]
        {
            GetComponent<Enemy>(),
            GetComponent<EnemyMelee>()
        };
    }

    public void StartTakedown()
    {
        isKilled = true;

        foreach (MonoBehaviour ai in enemyAIScripts)
        {
            if (ai == null)
                continue;

            ai.CancelInvoke();
            ai.enabled = false;
        }

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

    // Ataque à distância: só o inimigo toca a animação (o player não se mexe).
    public void KillWithKnife()
    {
        KillWithKnife(transform.forward);
    }

    // O parâmetro mantém-se para não partir quem chama este método, mas já não é usado.
    public void KillWithKnife(Vector3 knifeDirection)
    {
        if (isKilled)
            return;

        StartTakedown();

        if (animator != null && !string.IsNullOrEmpty(knifeDeathStateName))
        {
            animator.Play(knifeDeathStateName, 0, 0f);
            StartCoroutine(KnifeKillRoutine());
        }
        else
        {
            FinishTakedown();
        }
    }

    private IEnumerator KnifeKillRoutine()
    {
        // Espera que o Animator entre no estado (máx. 1 s, por segurança)
        float timeout = 1f;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(knifeDeathStateName) && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Espera que o estado acabe
        while (animator.GetCurrentAnimatorStateInfo(0).IsName(knifeDeathStateName) &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        FinishTakedown();
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