using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StealthKillTarget : MonoBehaviour
{
    [Header("Stealth Kill Settings")]
    [SerializeField] private Transform takedownPoint;

    [Header("Knife Kill (longo alcance)")]
    [Tooltip("Estado do Animator do inimigo a tocar (o mesmo do ataque brutal)")]
    [SerializeField] private string knifeDeathStateName = "BrutalTaker";
    [Tooltip("Duração da animação antes de o inimigo ficar congelado no chão")]
    [SerializeField] private float knifeAnimationDuration = 2.5f;
    [Tooltip("Estado do Animator em que o inimigo fica deitado (o mesmo de quando morre). Deixa vazio para desativar.")]
    [SerializeField] private string lyingStateName = "";
    [Tooltip("Segundos até o inimigo acabar de ficar deitado antes de congelar")]
    [SerializeField] private float lyingStateDuration = 1.5f;
    [Tooltip("Vira o inimigo na direção da faca (como se o player estivesse atrás dele)")]
    [SerializeField] private bool faceKnifeDirection = true;

    private bool isKilled = false;

    public Transform TakedownPoint => takedownPoint;
    public bool CanBeKilled => !isKilled;
    public AudioSource killSound;

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

            // Cancela Invokes pendentes (ex.: pontapé do EnemyMelee a meio) e desliga a IA.
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

    public void KillWithKnife(Vector3 knifeDirection)
    {
        if (isKilled)
            return;

        StartTakedown();

        if (faceKnifeDirection)
        {
            knifeDirection.y = 0f;

            if (knifeDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(knifeDirection);
        }

        if (killSound != null)
            killSound.Play();

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
        yield return new WaitForSeconds(knifeAnimationDuration);

        // Passa para o estado deitado antes de congelar o inimigo.
        if (animator != null && !string.IsNullOrEmpty(lyingStateName))
        {
            animator.CrossFade(lyingStateName, 0.25f, 0);
            yield return new WaitForSeconds(lyingStateDuration);
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