using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StealthKillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private AudioSource audioSource;

    [Header("Takedown Settings")]
    [SerializeField] private float takedownRange = 2.0f;
    [SerializeField] private float enemySearchRadius = 2.5f;

    [Header("Brutal Takedown (Botão Direito)")]
    [SerializeField] private float brutalHitDelay = 0.5f;
    [SerializeField] private float brutalAnimationDuration = 2.5f;

    [Header("Stealth Takedown (Botão Esquerdo)")]
    [SerializeField] private float stealthHitDelay = 0.3f;
    [SerializeField] private float stealthAnimationDuration = 2.0f;

    [Header("Kick Sound")]
    [SerializeField] private AudioClip kickSound;
    [Range(0f, 1f)][SerializeField] private float kickVolume = 1f;
    [Tooltip("Segundos desde o início da animação até ao primeiro chuto")]
    [SerializeField] private float firstKickDelay = 1.5f;
    [Tooltip("Segundos entre o primeiro e o segundo chuto")]
    [SerializeField] private float delayBetweenKicks = 2f;

    [Header("Noise (só no Brutal)")]
    [Tooltip("Raio em que outros inimigos ouvem cada chuto")]
    [SerializeField] private float soundHearingRadius = 15f;

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

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

        // Botão direito -> brutal (faz barulho)
        if (Input.GetMouseButtonDown(1))
        {
            TryStealthKill("DoBrutalTakedow", "BrutalTaker", brutalHitDelay, brutalAnimationDuration, true);
        }
        // Botão esquerdo -> furtivo (silencioso)
        else if (Input.GetMouseButtonDown(0))
        {
            TryStealthKill("DoStealthTakedow", "StealthTaker", stealthHitDelay, stealthAnimationDuration, false);
        }
    }

    private void TryStealthKill(string playerStateName, string enemyStateName, float hitDelay, float animDuration, bool makesNoise)
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

        StartCoroutine(PerformTakedown(target, playerStateName, enemyStateName, hitDelay, animDuration, makesNoise));
    }

    private IEnumerator PerformTakedown(StealthKillTarget target, string playerStateName, string enemyStateName, float hitDelay, float animDuration, bool makesNoise)
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

        target.StartTakedown();

        if (invectorInput != null) invectorInput.enabled = false;
        if (invectorController != null) invectorController.enabled = false;
        if (characterController != null) characterController.enabled = false;

        transform.position = target.TakedownPoint.position;
        transform.rotation = target.TakedownPoint.rotation;

        playerAnimator.Play(playerStateName, 0, 0f);

        StartCoroutine(PlayKickSounds(target, makesNoise));

        yield return new WaitForSeconds(hitDelay);

        enemyAnimator.Play(enemyStateName, 0, 0f);

        float remainingTime = animDuration - hitDelay;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        target.FinishTakedown();

        if (invectorInput != null) invectorInput.enabled = true;
        if (invectorController != null) invectorController.enabled = true;
        if (characterController != null) characterController.enabled = true;

        isDoingTakedown = false;
    }

    private IEnumerator PlayKickSounds(StealthKillTarget victim, bool makesNoise)
    {
        if (kickSound == null || audioSource == null)
        {
            Debug.LogWarning("Falta o kickSound ou o AudioSource no Player!");
            yield break;
        }

        yield return new WaitForSeconds(firstKickDelay);
        audioSource.PlayOneShot(kickSound, kickVolume);
        if (makesNoise) AlertNearbyEnemies(victim);

        yield return new WaitForSeconds(delayBetweenKicks);
        audioSource.PlayOneShot(kickSound, kickVolume);
        if (makesNoise) AlertNearbyEnemies(victim);
    }

    private void AlertNearbyEnemies(StealthKillTarget victim)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            soundHearingRadius,
            enemyLayer
        );

        HashSet<Enemy> alerted = new HashSet<Enemy>();

        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();

            if (enemy == null || !enemy.enabled)
                continue;

            // Ignora o inimigo que está a ser abatido
            if (victim != null && enemy.gameObject == victim.gameObject)
                continue;

            // Evita chamar o mesmo inimigo várias vezes (tem vários colliders)
            if (!alerted.Add(enemy))
                continue;

            enemy.HearSound(transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemySearchRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, soundHearingRadius);
    }
}