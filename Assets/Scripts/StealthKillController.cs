using System.Collections;
using UnityEngine;

public class StealthKillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator playerAnimator;

    [Header("Takedown Settings")]
    [SerializeField] private float takedownRange = 2.0f;
    [SerializeField] private float enemySearchRadius = 2.5f;
    [SerializeField] private float animationDuration = 2.5f;

    [Header("Hit Delays (Sincronização)")]
    [Tooltip("Atraso do impacto para o Abate Brutal (Botão Direito)")]
    [SerializeField] private float brutalHitDelay = 0.5f;

    [Tooltip("Atraso do impacto para o Abate Stealth (Botão Esquerdo)")]
    [SerializeField] private float stealthHitDelay = 0.3f;

    [Header("Enemy Detection")]
    [SerializeField] private LayerMask enemyLayer;

    private bool isDoingTakedown = false;

    private MonoBehaviour invectorInput;
    private MonoBehaviour invectorController;
    private CharacterController characterController;

    private void Awake()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        invectorInput = GetComponent("vThirdPersonInput") as MonoBehaviour;
        invectorController = GetComponent("vBasicController") as MonoBehaviour;
        if (invectorController == null)
            invectorController = GetComponent("vThirdPersonMotor") as MonoBehaviour;

        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (isDoingTakedown)
            return;

        // Botão Direito do Rato -> Abate Brutal
        if (Input.GetMouseButtonDown(1))
        {
            TryStealthKill("DoBrutalTakedow", "BrutalTaker", brutalHitDelay);
        }
        // Botão Esquerdo do Rato -> Abate Furtivo
        else if (Input.GetMouseButtonDown(0))
        {
            TryStealthKill("DoStealthTakedow", "StealthTaker", stealthHitDelay);
        }
    }

    private void TryStealthKill(string playerStateName, string enemyStateName, float hitDelay)
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

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy == null || closestDistance > takedownRange)
            return;

        StealthKillTarget target = closestEnemy.GetComponent<StealthKillTarget>();

        if (target == null || !target.CanBeKilled)
            return;

        StartCoroutine(PerformTakedown(target, playerStateName, enemyStateName, hitDelay));
    }

    private IEnumerator PerformTakedown(StealthKillTarget target, string playerStateName, string enemyStateName, float hitDelay)
    {
        isDoingTakedown = true;

        if (target.TakedownPoint == null)
        {
            Debug.LogError("TakedownPoint não atribuído!");
            isDoingTakedown = false;
            yield break;
        }

        Animator enemyAnimator = target.GetComponent<Animator>();
        if (enemyAnimator == null)
        {
            Debug.LogError("Inimigo sem Animator!");
            isDoingTakedown = false;
            yield break;
        }

        // 1. DESATIVAR CONTROLO DE MOVIMENTO E IA
        target.StartTakedown();

        if (invectorInput != null) invectorInput.enabled = false;
        if (invectorController != null) invectorController.enabled = false;
        if (characterController != null) characterController.enabled = false;

        // 2. POSICIONAR E ALINHAR O PLAYER
        transform.position = target.TakedownPoint.position;
        transform.rotation = target.TakedownPoint.rotation;

        // 3. INICIAR ANIMAÇÃO DO PLAYER
        playerAnimator.Play(playerStateName, 0, 0f);

        // 4. ESPERAR PELO MOMENTO DO IMPACTO
        yield return new WaitForSeconds(hitDelay);

        // 5. INICIAR ANIMAÇÃO DO INIMIGO
        enemyAnimator.Play(enemyStateName, 0, 0f);

        // 6. ESPERAR O RESTO DA DURAÇÃO TOTAL
        float remainingTime = animationDuration - hitDelay;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // 7. FINALIZAR
        target.FinishTakedown();

        if (invectorInput != null) invectorInput.enabled = true;
        if (invectorController != null) invectorController.enabled = true;
        if (characterController != null) characterController.enabled = true;

        isDoingTakedown = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemySearchRadius);
    }
}