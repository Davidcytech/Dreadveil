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

    [Header("Animator State Names (player)")]
    [Tooltip("Têm de ser iguais aos nomes dos estados no Animator do player")]
    [SerializeField] private string brutalPlayerState = "DoBrutalTakedow";
    [SerializeField] private string stealthPlayerState = "DoStealthTakedow";

    [Header("Brutal Takedown (Botão Direito)")]
    [SerializeField] private float brutalHitDelay = 0.5f;
    [SerializeField] private float brutalAnimationDuration = 2.5f;

    [Header("Stealth Takedown (Botão Esquerdo)")]
    [SerializeField] private float stealthHitDelay = 0.3f;
    [SerializeField] private float stealthAnimationDuration = 2.0f;

    [Header("Boss Takedown")]
    [SerializeField] private string bossPlayerStateName = "BossTakedown";
    [Tooltip("Segundos até o boss reagir (sincroniza com o golpe do player)")]
    [SerializeField] private float bossReactDelay = 1.0f;

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

    private enum PromptType { None, Normal, Boss }
    private PromptType currentPrompt = PromptType.None;

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
        {
            currentPrompt = PromptType.None;
            UpdatePrompt();
            return;
        }

        currentPrompt = GetPromptType();
        UpdatePrompt();

        // Botão direito -> brutal (faz barulho)
        if (Input.GetMouseButtonDown(1))
        {
            TryStealthKill(brutalPlayerState, "BrutalTaker", brutalHitDelay, brutalAnimationDuration, true);
        }
        // Botão esquerdo -> furtivo (silencioso)
        else if (Input.GetMouseButtonDown(0))
        {
            TryStealthKill(stealthPlayerState, "StealthTaker", stealthHitDelay, stealthAnimationDuration, false);
        }
    }

    private void OnDisable()
    {
        if (PromptManager.Instance != null)
            PromptManager.Instance.Hide(this);
    }

    // ---------------- PROMPT ----------------

    private PromptType GetPromptType()
    {
        GameObject closest = FindClosestEnemy(out float closestDistance);

        if (closest == null || closestDistance > takedownRange)
            return PromptType.None;

        StealthKillTarget target = closest.GetComponent<StealthKillTarget>();
        if (target == null)
            return PromptType.None;

        Boss boss = closest.GetComponent<Boss>();
        if (boss != null)
        {
            bool ok = target.TakedownPoint != null && boss.CanBeStabbed(transform);
            return ok ? PromptType.Boss : PromptType.None;
        }

        return target.CanBeKilled ? PromptType.Normal : PromptType.None;
    }

    private void UpdatePrompt()
    {
        if (PromptManager.Instance == null)
            return;

        switch (currentPrompt)
        {
            case PromptType.Boss:
                PromptManager.Instance.Show(this, "<color=#FFD54F>LMB / RMB</color>  atacar");
                break;

            case PromptType.Normal:
                PromptManager.Instance.Show(this,
                    "<color=#FFD54F>LMB</color>  abate furtivo\n<color=#FFD54F>RMB</color>  abate brutal");
                break;

            default:
                PromptManager.Instance.Hide(this);
                break;
        }
    }

    // ---------------- DETEÇÃO ----------------

    private GameObject FindClosestEnemy(out float closestDistance)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, enemySearchRadius, enemyLayer);

        GameObject closestEnemy = null;
        closestDistance = Mathf.Infinity;

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

        return closestEnemy;
    }

    private void TryStealthKill(string playerStateName, string enemyStateName, float hitDelay, float animDuration, bool makesNoise)
    {
        GameObject closestEnemy = FindClosestEnemy(out float closestDistance);

        if (closestEnemy == null || closestDistance > takedownRange)
            return;

        StealthKillTarget target = closestEnemy.GetComponent<StealthKillTarget>();

        if (target == null)
            return;

        // Boss: fluxo próprio (tem de vir antes do fluxo dos inimigos normais)
        Boss boss = closestEnemy.GetComponent<Boss>();
        if (boss != null)
        {
            if (target.TakedownPoint != null && boss.CanBeStabbed(transform))
                StartCoroutine(PerformBossTakedown(boss, target.TakedownPoint, target.TakedownEndPoint));

            return;
        }

        if (!target.CanBeKilled)
            return;

        StartCoroutine(PerformTakedown(target, playerStateName, enemyStateName, hitDelay, animDuration, makesNoise));
    }

    // ---------------- BLOQUEIO DO PLAYER ----------------

    private void LockPlayer()
    {
        if (invectorInput != null) invectorInput.enabled = false;
        if (invectorController != null) invectorController.enabled = false;
        if (characterController != null) characterController.enabled = false;
    }

    private void UnlockPlayer()
    {
        if (invectorInput != null) invectorInput.enabled = true;
        if (invectorController != null) invectorController.enabled = true;
        if (characterController != null) characterController.enabled = true;
    }

    private IEnumerator WaitForPlayerStateEnd(string stateName)
    {
        // espera que o Animator entre no estado (máx. 1 s, por segurança)
        float timeout = 1f;
        while (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName) && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // espera que o estado acabe
        while (playerAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
               playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
        {
            yield return null;
        }
    }

    // ---------------- TAKEDOWN NORMAL ----------------

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
        LockPlayer();

        transform.position = target.TakedownPoint.position;
        transform.rotation = target.TakedownPoint.rotation;

        playerAnimator.Play(playerStateName, 0, 0f);

        StartCoroutine(PlayKickSounds(target, makesNoise));

        yield return new WaitForSeconds(hitDelay);

        enemyAnimator.Play(enemyStateName, 0, 0f);

        float remainingTime = animDuration - hitDelay;
        if (remainingTime > 0)
            yield return new WaitForSeconds(remainingTime);

        target.FinishTakedown();

        UnlockPlayer();
        isDoingTakedown = false;
    }

    // ---------------- TAKEDOWN DO BOSS ----------------

    private IEnumerator PerformBossTakedown(Boss boss, Transform point, Transform endPoint)
    {
        isDoingTakedown = true;

        boss.StartStealthHit(bossReactDelay);

        LockPlayer();

        transform.SetPositionAndRotation(point.position, point.rotation);

        bool oldRootMotion = playerAnimator.applyRootMotion;
        playerAnimator.applyRootMotion = true;

        playerAnimator.Play(bossPlayerStateName, 0, 0f);

        yield return StartCoroutine(WaitForPlayerStateEnd(bossPlayerStateName));

        playerAnimator.applyRootMotion = oldRootMotion;

        // garante o ponto final, aconteça o que acontecer ao root motion
        if (endPoint != null)
            transform.SetPositionAndRotation(endPoint.position, endPoint.rotation);

        UnlockPlayer();
        isDoingTakedown = false;
    }

    // ---------------- SOM / RUÍDO ----------------

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
        Collider[] hits = Physics.OverlapSphere(transform.position, soundHearingRadius, enemyLayer);

        HashSet<Enemy> alerted = new HashSet<Enemy>();

        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();

            if (enemy == null || !enemy.enabled)
                continue;

            if (victim != null && enemy.gameObject == victim.gameObject)
                continue;

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