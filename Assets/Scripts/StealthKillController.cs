using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StealthKillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private CharacterController characterController;

    [Header("Takedown Settings")]
    [SerializeField] private float takedownRange = 2.0f;
    [SerializeField] private float enemySearchRadius = 2.5f;
    [SerializeField] private float animationDuration = 2.5f;

    [Header("Enemy Detection")]
    [SerializeField] private LayerMask enemyLayer;

    private bool isDoingTakedown = false;

    private GameObject currentEnemy;

    private Rigidbody enemyRigidbody;
    private NavMeshAgent enemyNavMeshAgent;
    private Animator enemyAnimator;

    private Transform takedownPoint;

    private void Awake()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (isDoingTakedown)
            return;

        // Botão direito do rato
        if (Input.GetMouseButtonDown(1))
        {
            TryStealthKill();
        }
    }

    private void TryStealthKill()
    {
        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            enemySearchRadius,
            enemyLayer
        );

        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in enemies)
        {
            GameObject enemy = col.transform.root.gameObject;

            if (enemy == gameObject)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy == null)
        {
            Debug.Log("Nenhum inimigo encontrado para Stealth Kill.");
            return;
        }

        if (closestDistance > takedownRange)
        {
            Debug.Log("Inimigo demasiado longe.");
            return;
        }

        StealthKillTarget target =
            closestEnemy.GetComponent<StealthKillTarget>();

        if (target == null)
        {
            Debug.LogWarning(
                "O inimigo encontrado não tem StealthKillTarget!"
            );

            return;
        }

        StartCoroutine(PerformTakedown(target));
    }

    private IEnumerator PerformTakedown(StealthKillTarget target)
    {
        isDoingTakedown = true;

        currentEnemy = target.gameObject;

        enemyAnimator = target.GetComponent<Animator>();
        enemyRigidbody = target.GetComponent<Rigidbody>();
        enemyNavMeshAgent = target.GetComponent<NavMeshAgent>();

        takedownPoint = target.TakedownPoint;

        if (enemyAnimator == null)
        {
            Debug.LogError("O inimigo não tem Animator!");
            isDoingTakedown = false;
            yield break;
        }

        if (takedownPoint == null)
        {
            Debug.LogError("TakedownPoint não está atribuído!");
            isDoingTakedown = false;
            yield break;
        }

        // ==============================
        // 1. DESATIVAR MOVIMENTO DO PLAYER
        // ==============================

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // ==============================
        // 2. PARAR INIMIGO
        // ==============================

        if (enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.isStopped = true;
            enemyNavMeshAgent.updatePosition = false;
            enemyNavMeshAgent.updateRotation = false;
        }

        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = true;
        }

        // ==============================
        // 3. POSICIONAR PLAYER
        // ==============================

        transform.position = takedownPoint.position;

        // O Player fica virado para o inimigo
        Vector3 direction =
            currentEnemy.transform.position - transform.position;

        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        // ==============================
        // 4. TOCAR ANIMAÇÕES
        // ==============================

        playerAnimator.SetTrigger("BrutalTake");

        enemyAnimator.SetTrigger("Knocked");

        Debug.Log("STEALTH TAKEDOWN!");

        // ==============================
        // 5. ESPERAR ANIMAÇÃO
        // ==============================

        yield return new WaitForSeconds(animationDuration);

        // ==============================
        // 6. FINALIZAR
        // ==============================

        FinishTakedown();
    }

    private void FinishTakedown()
    {
        // Reativar CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // Reativar NavMeshAgent
        if (enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.updatePosition = true;
            enemyNavMeshAgent.updateRotation = true;
            enemyNavMeshAgent.isStopped = false;
        }

        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = false;
        }

        currentEnemy = null;
        enemyAnimator = null;
        enemyRigidbody = null;
        enemyNavMeshAgent = null;
        takedownPoint = null;

        isDoingTakedown = false;

        Debug.Log("Stealth Takedown terminado.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            enemySearchRadius
        );
    }
}