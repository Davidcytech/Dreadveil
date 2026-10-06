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
    
    [Tooltip("Tempo em segundos que o Player demora a dar o golpe após iniciar a animação")]
    [SerializeField] private float hitImpactDelay = 0.5f; 

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

        StartCoroutine(PerformTakedown(target));
    }

    private IEnumerator PerformTakedown(StealthKillTarget target)
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
        playerAnimator.Play("DoBrutalTakedow", 0, 0f);

        // 4. ESPERAR PELO MOMENTO DO IMPACTO
        yield return new WaitForSeconds(hitImpactDelay);

        // 5. INICIAR ANIMAÇÃO DO INIMIGO NO MOMENTO DO GOLPE
        enemyAnimator.Play("BrutalTaker", 0, 0f);

        // 6. ESPERAR O RESTO DA DURAÇÃO TOTAL
        float remainingTime = animationDuration - hitImpactDelay;
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