using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class HackingMinigame : MonoBehaviour
{
    public GameObject panel;                     // minigame panel 
    public TextMeshProUGUI[] slotTexts;          // TOdos textmesh para as letras
    public string secretPassword = "GATSTD";     

    // dificuldade
    public float letterInterval = 0.08f;         // tempo de trocas de letras
    public int minSwapsBeforeTarget = 6;         // minimo de letras ate a certa
    public int maxSwapsBeforeTarget = 12;        // maximo de letras ate a certa
    public float correctLetterDuration = 0.45f;  // tempo de reacao
    public bool restartAllOnFail = false;        // restart, é opcial, podemos desativar

    //cores
    public Color normalColor = new Color32(0, 229, 255, 255);     // Ciano brilhante para as letras a rodar
    public Color correctColor = new Color32(0, 255, 102, 255);    // Verde-fósforo néon intenso
    public Color errorColor = new Color32(255, 51, 85, 255);      // Vermelho erro cyberpunk
    public Color pendingColor = new Color32(70, 80, 100, 255);
    public bool pauseGame = false;               // true = Time.timeScale = 0
    public UnityEvent onWin;                

    private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private int currentSlot;
    private bool active;
    private bool windowOpen;                    
    private bool waitingForError;
    private Coroutine spinRoutine;

    public CanvasGroup canvasGroup; 
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

   
    public void StartMinigame()
    {
        if (active) return;
        active = true;
        panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseGame) Time.timeScale = 0f;

        currentSlot = 0;
        ResetSlots();
        StartCoroutine(FadeIn());
        spinRoutine = StartCoroutine(SpinSlot());
    }

    private void Update()
    {
        if (!active || waitingForError) return;

        if (Input.GetKeyDown(KeyCode.Space)){
            TryLockLetter();
        }
    }
    private IEnumerator FadeIn()
{
    float timer = 0f;
    canvasGroup.alpha = 0f;

    while (timer < fadeDuration)
    {
        timer += Time.unscaledDeltaTime; // Usa unscaled se o jogo estiver pausado
        canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
        yield return null;
    }
    canvasGroup.alpha = 1f;
}

// Corrotina de Fade Out (desaparecer suavemente antes de fechar)
private IEnumerator FadeOutAndClose()
{
    float timer = 0f;
    while (timer < fadeDuration)
    {
        timer += Time.unscaledDeltaTime;
        canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
        yield return null;
    }
    canvasGroup.alpha = 0f;

    // Executa o fecho real depois do fade acabar
    CloseDirect();
}

    private void ResetSlots()
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            slotTexts[i].text = "_";
            slotTexts[i].color = pendingColor;
        }
    }

    // slot machine
    private IEnumerator SpinSlot()
    {
        TextMeshProUGUI text = slotTexts[currentSlot];
        char correct = secretPassword[currentSlot];

        while (true)
        {
            windowOpen = false;
            text.color = normalColor;

            int swaps = Random.Range(minSwapsBeforeTarget, maxSwapsBeforeTarget + 1);

            for (int i = 0; i < swaps; i++)
            {
                char c;
                do { c = alphabet[Random.Range(0, alphabet.Length)]; }
                while (c == correct);                     // percorre o numero de trocas e nunca pde ser a correta

                text.text = c.ToString();
                yield return new WaitForSecondsRealtime(letterInterval);
            }

            // letras correta
            text.text = correct.ToString();
            text.color = correctColor;
            windowOpen = true;

            yield return new WaitForSecondsRealtime(correctLetterDuration);
        }
    }

    private void TryLockLetter()
    {
        if (spinRoutine != null) StopCoroutine(spinRoutine);

        if (windowOpen)
        {
            // hit: the letter stays locked
            slotTexts[currentSlot].color = correctColor;
            currentSlot++;

            if (currentSlot >= slotTexts.Length)
            {
                Win();
                return;
            }

            spinRoutine = StartCoroutine(SpinSlot());
        }
        else
        {
            StartCoroutine(Fail());
        }
    }

    private IEnumerator Fail()
    {
        waitingForError = true;
        windowOpen = false;

        slotTexts[currentSlot].color = errorColor;
        yield return new WaitForSecondsRealtime(0.5f);

        if (restartAllOnFail)
        {
            currentSlot = 0;
            ResetSlots();
        }
        else
        {
            slotTexts[currentSlot].text = "_";
            slotTexts[currentSlot].color = pendingColor;
        }

        waitingForError = false;
        spinRoutine = StartCoroutine(SpinSlot());
    }

    private void Win()
    {
        StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        waitingForError = true;                              // blocks clicks
        yield return new WaitForSecondsRealtime(0.8f);       // um tempo para o player conseguir ver a pss

        Close();
        onWin.Invoke();
    }

    //fechar o jogo
    public void Close()
    {
        active = false;
        waitingForError = false;

        if (spinRoutine != null) StopCoroutine(spinRoutine);

        panel.SetActive(false);

        if (pauseGame) Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void CloseDirect()
    {
        panel.SetActive(false);

        if (pauseGame) Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}