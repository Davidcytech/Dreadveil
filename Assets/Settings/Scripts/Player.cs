
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

    // Ataque com faca
    [Header("Ataque com Faca")]
    public GameObject knifePrefab;
    public Transform knifeSpawnPoint;
    public float knifeCooldown = 2f;

    private float knifeTimer;

    // coletaveis para desbloquear faca
    public int collectabelsOwend = 0;
    public int collectabelsNeeded = 5;
    public bool knifeUnlocked = false;

    // Implementação dos power ups
    private float timerEpoweup;
    public bool energyEpowerup => timerEpoweup > 0f;

    private bool hasShieldPowerup = false;
    public bool HasShieldPowerup => hasShieldPowerup;

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
        if (timerEpoweup > 0f)
        {
            timerEpoweup -= Time.deltaTime;
            energy = maxEnergy;
        }

        if (knifeTimer > 0f)
            knifeTimer -= Time.deltaTime;

        if (!dead && Input.GetKeyDown(KeyCode.R)){
            if(knifeUnlocked){
                ThrowKnife();
            }
        }
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
            energy = Mathf.Min(maxEnergy, energy + energyPerRegeneration);
            regenerationTimer = 0f;
        }
    }

    private void ThrowKnife()
    {
        if (knifePrefab == null || knifeSpawnPoint == null)
            return;

        if (knifeTimer > 0f)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint = ray.GetPoint(100f);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            targetPoint = hit.point;

        Vector3 direction = (targetPoint - knifeSpawnPoint.position).normalized;

        GameObject knife = Instantiate(
            knifePrefab,
            knifeSpawnPoint.position,
            Quaternion.LookRotation(direction)
        );

        KnifeProjectile projectile = knife.GetComponent<KnifeProjectile>();

        if (projectile != null)
            projectile.Launch(direction, transform);

        knifeTimer = knifeCooldown;
    }

    private void HandleFootsteps()
    {
        if (footstepsAudio == null || playerController == null)
            return;

        bool isCrouching =
            crouchController != null &&
            crouchController.isCrouching;

        bool hasMovementInput = playerController.input.magnitude > 0.1f;

        bool isGrounded = playerController.isGrounded;
        bool isRunning = playerController.isSprinting;

        bool isMoving = hasMovementInput && isGrounded && !isCrouching;

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

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= soundRange)
                enemy.HearSound(transform.position);
        }
    }

    public bool TryUseEnergy(float amount)
    {
        if (energyEpowerup)
            return true;

        if (energyInhibited || energy < amount)
            return false;

        energy -= amount;
        return true;
    }

    public void TakeDamage(float damage)
    {
        if (dead)
            return;
        // comentario super sexy, meti aqui a cena do power up de escudo
        if (hasShieldPowerup)
        {
            hasShieldPowerup = false;
            return; 
        }

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

    // Redução de energia
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

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // implementacao do power up de energia
    public void EnergyPowerup(float duration)
    {
        if (inhibitionCoroutine != null)
        {
            StopCoroutine(inhibitionCoroutine);
            inhibitionCoroutine = null;
        }

        energyInhibited = false;
        timerEpoweup = duration;
        energy = maxEnergy;
    }

    // Implementação do power up de escuro

    public void ShieldPowerup()
    {
        hasShieldPowerup = true;
    }

    // IMPLEMENTEI A COISA DE APANHAR COLETAVEIS
    public void AddCollecatbel()
    {
        collectabelsOwend++;
        if (collectabelsOwend >= collectabelsNeeded && !knifeUnlocked)
        {
            knifeUnlocked = true;
            Debug.Log("Faca Desbloqueada!");

        }
    }
    



}
