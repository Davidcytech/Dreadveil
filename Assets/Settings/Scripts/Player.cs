
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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
    public float stepInterval = 0.45f;
    public float walkingVolume = 0.3f;
    public float runningVolume = 1f;

    [Header("Alcance Sonoro")]
    public float walkingSoundRange = 3f;
    public float runningSoundRange = 8f;

    private float stepTimer;
    private bool footstepsBlocked;
    private Invector.vCharacterController.vThirdPersonController playerController;

    private void Start()
    {
        playerController = GetComponent<Invector.vCharacterController.vThirdPersonController>();
        footstepsBlocked = false;
    }

    private void Update()
    {
        if (dead || footstepsBlocked)
        {
            if (footstepsAudio != null)
            {
                footstepsAudio.Stop();
                footstepsAudio.time = 0f;
            }

            return;
        }

        HandleFootsteps();

        if (energy >= maxEnergy)
        {
            energy = maxEnergy;
            regenerationTimer = 0f;
            return;
        }

        regenerationTimer += Time.deltaTime;

        if (regenerationTimer >= regenerationInterval)
        {
            energy = Mathf.Min(maxEnergy, energy + energyPerRegeneration);
            regenerationTimer = 0f;
        }
    }

    private void HandleFootsteps()
    {
        if (footstepsAudio == null || playerController == null)
            return;

        vCrouchController crouchController = GetComponent<vCrouchController>();

        bool isCrouching = crouchController != null && crouchController.isCrouching;
        bool isMoving = playerController.input.magnitude > 0.1f;
        bool isGrounded = playerController.isGrounded;
        bool isRunning = playerController.isSprinting;

        if (isCrouching || !isMoving || !isGrounded)
        {
            stepTimer = 0f;

            if (footstepsAudio.isPlaying)
            {
                footstepsAudio.Stop();
                footstepsAudio.time = 0f;
            }

            return;
        }

        footstepsAudio.volume = isRunning ? runningVolume : walkingVolume;

        stepTimer += Time.deltaTime;

        if (stepTimer >= stepInterval)
        {
            if (footstepsAudio.clip != null && !footstepsAudio.isPlaying)
                footstepsAudio.Play();

            float soundRange = isRunning
                ? runningSoundRange
                : walkingSoundRange;

            AlertEnemies(soundRange);

            stepTimer = 0f;
        }
    }

    private void AlertEnemies(float soundRange)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
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
        if (energy < amount)
            return false;

        energy -= amount;
        return true;
    }

    public void TakeDamage(float damage)
    {
        if (dead) return;

        health -= damage;

        if (health <= 0f)
        {
            health = 0f;
            dead = true;
            footstepsBlocked = true;

            if (footstepsAudio != null)
            {
                footstepsAudio.Stop();
                footstepsAudio.time = 0f;
            }

            StartCoroutine(RestartGame());
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Destroy(gameObject);
        }
    }

    // Redução de energia
    private bool energyInhibited;
    private Coroutine inhibitionCoroutine;

    public void InhibitEnergy(float duration)
    {
        if (inhibitionCoroutine != null)
            StopCoroutine(inhibitionCoroutine);

        inhibitionCoroutine = StartCoroutine(EnergyInhibition(duration));
    }

    private IEnumerator EnergyInhibition(float duration)
    {
        float savedEnergy = energy;

        energyInhibited = true;
        energy = 0f;

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

    /*
    private void OnGUI()
    {
        if (!dead) return;

        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 64;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.red;

        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Capturado", style);
    }
    */
}
