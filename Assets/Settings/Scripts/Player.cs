using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public float knifeAimDistance = 100f;
    [Tooltip("Quantas cópias da faca são criadas no início (= quantos lançamentos tens)")]
    public int knifeCount = 3;

    private float knifeTimer;

    // Cópias da faca já criadas (desligadas) à espera de serem lançadas.
    private readonly List<GameObject> knifeStock = new List<GameObject>();

    // coletaveis para desbloquear faca
    public int collectabelsOwend = 0;
    public int collectabelsNeeded = 5;
    public bool knifeUnlocked = false;

    // minigames completos
    public int minigamesCompletos = 0;

    // Implementação dos power ups
    private float timerEpoweup;
    public bool energyEpowerup => timerEpoweup > 0f;

    private bool hasShieldPowerup = false;
    public bool HasShieldPowerup => hasShieldPowerup;

    // dinamite
    public bool hasdynamite = false;

    private void Start()
    {
        playerController = GetComponent<vThirdPersonController>();
        crouchController = GetComponent<vCrouchController>();

        footstepsBlocked = false;
        stepTimer = 0f;
        wasMoving = false;

        CreateKnifeStock();

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

        if (!dead && Input.GetKeyDown(KeyCode.R))
        {
            if (knifeUnlocked)
            {
                ThrowKnife();
            }
        }

        if (!dead && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("F carregado. hasdynamite = " + hasdynamite);

            if (hasdynamite)
            {
                Vector3 origin = transform.position + Vector3.up * 1f;

                if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, 3f))
                {
                    Debug.Log("Raycast acertou em: " + hit.collider.name);

                    Destroy parede = hit.collider.GetComponentInParent<Destroy>();
                    if (parede != null)
                    {
                        Debug.Log("Parede encontrada, a explodir");
                        parede.ExplodirParede();
                        hasdynamite = false;
                    }
                    else
                    {
                        Debug.Log("O objeto atingido não tem o script Destroy");
                    }
                }
                else
                {
                    Debug.Log("Raycast não acertou em nada");
                }
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
        {
            Debug.LogWarning("Faca: falta atribuir o Knife Prefab ou o Knife Spawn Point no Player!");
            return;
        }

        if (knifeTimer > 0f)
            return;

        if (knifeStock.Count == 0)
        {
            Debug.Log("Faca: já não tens facas.");
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint = ray.GetPoint(knifeAimDistance);

        // A câmara fica atrás do player, por isso o raio passa primeiro pelo
        // próprio player. Ignora tudo o que está antes do ponto de lançamento,
        // o próprio player (e o modelo dele) e triggers.
        float minDistance = Mathf.Max(
            0f,
            Vector3.Dot(knifeSpawnPoint.position - ray.origin, ray.direction)
        );

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            knifeAimDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.distance < minDistance)
                continue;

            Transform t = hit.collider.transform;

            if (t == transform || t.IsChildOf(transform))
                continue;

            targetPoint = hit.point;
            break;
        }

        Vector3 direction = (targetPoint - knifeSpawnPoint.position).normalized;

        // Tira UMA cópia do stock e lança-a.
        GameObject knife = knifeStock[knifeStock.Count - 1];
        knifeStock.RemoveAt(knifeStock.Count - 1);
        knifeCount = knifeStock.Count;

        if (knife == null)
            return;

        knife.transform.SetPositionAndRotation(
            knifeSpawnPoint.position,
            Quaternion.LookRotation(direction)
        );

        knife.SetActive(true);

        KnifeProjectile projectile = knife.GetComponent<KnifeProjectile>();

        if (projectile == null)
            projectile = knife.AddComponent<KnifeProjectile>();

        projectile.Launch(direction, transform);

        Debug.Log("Faca lançada. Restam " + knifeStock.Count);

        knifeTimer = knifeCooldown;
    }

    // Cria as cópias da faca (uma por lançamento). O original nunca é usado nem destruído.
    private void CreateKnifeStock()
    {
        if (knifePrefab == null)
            return;

        int amount = knifeCount;

        for (int i = 0; i < amount; i++)
            knifeStock.Add(CreateKnifeCopy());

        // Se o original é um objeto da cena com KnifeProjectile, desliga-o
        // (as cópias já foram feitas) para ele não andar nem desaparecer.
        if (knifePrefab.scene.IsValid())
        {
            KnifeProjectile original = knifePrefab.GetComponent<KnifeProjectile>();

            if (original != null)
                original.enabled = false;
        }

        knifeCount = knifeStock.Count;
        Debug.Log("Facas prontas: " + knifeStock.Count);
    }

    private GameObject CreateKnifeCopy()
    {
        GameObject copy = Instantiate(knifePrefab);
        copy.name = knifePrefab.name + "_Copia";
        copy.transform.SetParent(null);

        // Mantém o tamanho que o original tem no mundo.
        copy.transform.localScale = knifePrefab.transform.lossyScale;

        copy.SetActive(false);
        return copy;
    }

    // Para apanhar facas no mapa (opcional).
    public void AddKnives(int amount)
    {
        if (knifePrefab == null)
            return;

        for (int i = 0; i < amount; i++)
            knifeStock.Add(CreateKnifeCopy());

        knifeCount = knifeStock.Count;
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

    // Chamado pelo MinigamePC2 quando o jogador ganha um minigame.
    public void AddMinigameCompleto()
    {
        minigamesCompletos++;
        Debug.Log("Minigames completos: " + minigamesCompletos);

        if (BigDoorManager.Instance != null)
            BigDoorManager.Instance.AtualizarPortas(minigamesCompletos);
        else
            Debug.LogError("BigDoorManager.Instance é NULL!");
    }

    public void ApanharDinamite()
    {
        hasdynamite = true;
        Debug.Log("Dinamite apanhada!");
    }
}