using UnityEngine;

// Interagir com o PC abre o minigame; ao ganhar, fecha passado closeDelay segundos.
public class LockPC : MonoBehaviour
{
    [Header("Checkpoint")]                                   // NOVO
    public string id = "LockPC_Level1";                      // NOVO: único em cada PC
    public Checkpoint checkpointAoVencer;                    // NOVO: opcional, checkpoint a ativar ao ganhar

    [Header("Jogador")]
    public GameObject player;
    public float interactDistance = 2f;

    [Header("Minigame")]
    [Tooltip("Objeto que contém todo o minigame (Canvas ou Panel). Pode começar desligado.")]
    public GameObject minigamePanel;
    public CircuitPuzzle puzzle;

    [Tooltip("Scripts a desligar enquanto o minigame está aberto (movimento, câmara, etc.).")]
    public MonoBehaviour[] disableWhilePlaying;

    public KeyCode interactKey = KeyCode.F;
    public KeyCode closeKey = KeyCode.Escape;

    [Tooltip("Segundos até o minigame fechar depois de ganhar.")]
    public float closeDelay = 4f;

    [Header("Opcional: luz / material (ficam verdes ao ganhar)")]
    public Light ThingyLight;
    public Material material;

    private Color originalBaseColor;
    private Color originalEmissionColor;
    private Color originalLightColor;

    private bool hasBaseColor;
    private bool hasEmissionColor;

    private bool isPlaying;

    public bool IsCompleted { get; private set; }

    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    void Start()
    {
        if (material != null)
        {
            hasBaseColor = material.HasProperty("_BaseColor");

            if (hasBaseColor)
                originalBaseColor = material.GetColor("_BaseColor");

            hasEmissionColor = material.HasProperty("_EmissionColor");

            if (hasEmissionColor)
            {
                originalEmissionColor = material.GetColor("_EmissionColor");
                material.EnableKeyword("_EMISSION");
            }
        }

        if (ThingyLight != null)
            originalLightColor = ThingyLight.color;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (puzzle != null)
            puzzle.OnSolved += OnPuzzleSolved;
        else
            Debug.LogError(name + ": falta atribuir o CircuitPuzzle!");

        // NOVO: se já foi resolvido antes do último checkpoint, aparece logo como concluído
        if (CheckpointData.IsDone(id))
        {
            IsCompleted = true;
            SetCompletedVisuals();
        }
    }

    void OnDestroy()
    {
        if (puzzle != null)
            puzzle.OnSolved -= OnPuzzleSolved;
    }

    void Update()
    {
        if (player == null || IsCompleted)
            return;

        if (isPlaying)
        {
            if (Input.GetKeyDown(closeKey))
                CloseMinigame();

            return;
        }

        float distance = Vector3.Distance(
            player.transform.position,
            transform.position
        );

        if (distance <= interactDistance && Input.GetKeyDown(interactKey))
            OpenMinigame();
    }

    void OpenMinigame()
    {
        if (minigamePanel == null)
            return;

        isPlaying = true;
        minigamePanel.SetActive(true);

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayerScriptsEnabled(false);
    }

    void CloseMinigame()
    {
        isPlaying = false;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        SetPlayerScriptsEnabled(true);
    }

    void SetPlayerScriptsEnabled(bool value)
    {
        if (disableWhilePlaying == null)
            return;

        foreach (MonoBehaviour script in disableWhilePlaying)
        {
            if (script != null)
                script.enabled = value;
        }
    }

    void OnPuzzleSolved()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;

        CheckpointData.MarkDone(id);                          // NOVO: regista que este PC foi resolvido

        SetCompletedVisuals();                                // ALTERADO: o código das cores passou para este método

        Invoke(nameof(CloseMinigame), closeDelay);

        // NOVO: guarda o checkpoint depois de o minigame fechar
        if (checkpointAoVencer != null)
            Invoke(nameof(SaveCheckpointAfterWin), closeDelay + 0.1f);
    }

    // NOVO
    void SaveCheckpointAfterWin()
    {
        Player p = player != null ? player.GetComponentInParent<Player>() : null;

        if (p != null)
            checkpointAoVencer.Activate(p);
    }

    // NOVO: luz e material verdes (antes estava dentro do OnPuzzleSolved)
    void SetCompletedVisuals()
    {
        if (ThingyLight != null)
            ThingyLight.color = Color.green;

        if (material != null)
        {
            if (hasBaseColor)
                material.SetColor("_BaseColor", Color.green);

            if (hasEmissionColor)
                material.SetColor("_EmissionColor", Color.green);
        }
    }

    void OnApplicationQuit()
    {
        RestoreOriginalColors();
    }

    void RestoreOriginalColors()
    {
        if (material != null)
        {
            if (hasBaseColor)
                material.SetColor("_BaseColor", originalBaseColor);

            if (hasEmissionColor)
                material.SetColor("_EmissionColor", originalEmissionColor);
        }

        if (ThingyLight != null)
            ThingyLight.color = originalLightColor;
    }
}