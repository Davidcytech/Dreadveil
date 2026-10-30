
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Invector.vCharacterController;

public class Player : MonoBehaviour
{
    public float health = 100f;
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float energyPerRegeneration = 25f;
    public float regenerationInterval = 5f;

    private bool dead;
    private float regenerationTimer;

    [Header("Passos")]
    public AudioSource footstepsAudio;
    public float initialStepDelay = 0.25f;
    public float stepInterval = 0.6f;
    public float runningStepInterval = 0.4f;
    public float walkingVolume = 0.3f;
    public float runningVolume = 1f;
    public float runningPitch = 1.2f;

    [Header("Alcance Sonoro")]
    public float walkingSoundRange = 3f;
    public float runningSoundRange = 8f;

    private float stepTimer;
    private bool footstepsBlocked;
    private bool wasMoving;

    private vThirdPersonController playerController;
    private vCrouchController crouchController;

    private void Start()
    {
        playerController = GetComponent<vThirdPersonController>();
        crouchController = GetComponent<vCrouchController>();

        footstepsBlocked = false;
        stepTimer = 0f;
        wasMoving = false;

        if (footstepsAudio != null)
        {
            footstepsAudio.playOnAwake = false;
            footstepsAudio.loop = false;
            footstepsAudio.Stop();
        }
    }

    private void Update()
    {
        if (dead || footstepsBlocked)
        {
            if (footstepsAudio != null && footstepsAudio.isPlaying)
                footstepsAudio.Stop();

            return;
        }

        HandleFootsteps();

        if (energy >= maxEnergy)
        {
            energy = maxEnergy;
            regenerationTimer = 0f;
            return;
        }

        if (energyInhibited)
            return;

        regenerationTimer += Time.deltaTime;

        if (regenerationTimer >= regenerationInterval)
        {
            energy = Mathf.Min(
                maxEnergy,
                energy + energyPerRegeneration
            );

            regenerationTimer = 0f;
        }
    }

    private void HandleFootsteps()
    {
        if (footstepsAudio == null || playerController == null)
            return;

        bool isCrouching =
            crouchController != null &&
            crouchController.isCrouching;

        bool hasMovementInput =
            playerController.input.magnitude > 0.1f;

        bool isGrounded = playerController.isGrounded;
        bool isRunning = playerController.isSprinting;

        bool isMoving =
            hasMovementInput &&
            isGrounded &&
            !isCrouching;

        if (!isMoving)
        {
            stepTimer = 0f;
            wasMoving = false;

            if (footstepsAudio.isPlaying)
                footstepsAudio.Stop();

            return;
        }

        if (!wasMoving)
        {
            stepTimer = 0f;
            wasMoving = true;
        }

        float currentInterval = isRunning
            ? runningStepInterval
            : stepInterval;

        footstepsAudio.volume = isRunning
            ? runningVolume
            : walkingVolume;

        footstepsAudio.pitch = isRunning
            ? runningPitch
            : 1f;

        stepTimer += Time.deltaTime;

        float requiredDelay = initialStepDelay + currentInterval;

        if (stepTimer < requiredDelay)
            return;

        if (footstepsAudio.isPlaying)
            return;

        if (footstepsAudio.clip == null)
            return;

        footstepsAudio.Play();

        float soundRange = isRunning
            ? runningSoundRange
            : walkingSoundRange;

        AlertEnemies(soundRange);

        stepTimer = initialStepDelay;
    }

    private void AlertEnemies(float soundRange)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance <= soundRange)
            {
                enemy.HearSound(transform.position);
            }
        }
    }

    public bool TryUseEnergy(float amount)
    {
        if (energyInhibited || energy < amount)
            return false;

        energy -= amount;
        return true;
    }

    public void TakeDamage(float damage)
    {
        if (dead)
            return;

        health -= damage;

        if (health <= 0f)
        {
            health = 0f;
            dead = true;
            footstepsBlocked = true;

            if (footstepsAudio != null)
                footstepsAudio.Stop();

            StartCoroutine(RestartGame());

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    [Header("Redução de Energia")]
    private bool energyInhibited;
    private Coroutine inhibitionCoroutine;

    public void InhibitEnergy(float duration)
    {
        if (inhibitionCoroutine != null)
            StopCoroutine(inhibitionCoroutine);

        inhibitionCoroutine = StartCoroutine(
            EnergyInhibition(duration)
        );
    }

    private IEnumerator EnergyInhibition(float duration)
    {
        float savedEnergy = energy;

        energyInhibited = true;
        energy = 0f;
        regenerationTimer = 0f;

        yield return new WaitForSeconds(duration);

        energy = savedEnergy;
        energyInhibited = false;
        inhibitionCoroutine = null;
    }

    private IEnumerator RestartGame()
    {
        yield return new WaitForSecondsRealtime(3f);

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}
