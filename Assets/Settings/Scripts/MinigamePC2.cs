using UnityEngine;

public class MinigamePC2 : MonoBehaviour
{
    [Header("Jogador")]
    public GameObject player;
    public float interactDistance = 2f;

    [Header("Minigame")]
    public GameObject minigamePanel;
    public CircuitPuzzle puzzle;

    [Tooltip("Scripts a desligar enquanto o minigame está aberto.")]
    public MonoBehaviour[] disableWhilePlaying;

    public KeyCode interactKey = KeyCode.F;
    public KeyCode closeKey = KeyCode.Escape;

    [Tooltip("Segundos até o minigame fechar depois de ganhar.")]
    public float closeDelay = 4f;

    [Header("Opcional: luz / material")]
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

        if (puzzle == null)
            Debug.LogError(name + ": falta atribuir o CircuitPuzzle!");
        else
            puzzle.OnSolved += OnPuzzleSolved;
    }

    void OnDestroy()
    {
        if (puzzle != null)
            puzzle.OnSolved -= OnPuzzleSolved;
    }

    void Update()
    {
        if (player == null || IsCompleted || puzzle == null)
            return;

        if (isPlaying)
        {
            if (puzzle.IsSolved)
            {
                OnPuzzleSolved();
                return;
            }

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
        if (minigamePanel == null || IsCompleted)
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
        if (!isPlaying)
            return;

        isPlaying = false;

        CancelInvoke(nameof(CloseMinigame));

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
        Debug.Log(name + ": puzzle resolvido!");

        Player p = player != null ? player.GetComponent<Player>() : null;

        if (p != null)
            p.AddMinigameCompleto();
        else
            Debug.LogError(name + ": o objeto Player não tem o script Player!");

        if (ThingyLight != null)
            ThingyLight.color = Color.green;

        if (material != null)
        {
            if (hasBaseColor)
                material.SetColor("_BaseColor", Color.green);

            if (hasEmissionColor)
                material.SetColor("_EmissionColor", Color.green);
        }

        Invoke(nameof(CloseMinigame), closeDelay);
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