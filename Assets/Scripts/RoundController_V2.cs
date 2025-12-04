using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class RoundController_V2 : MonoBehaviour
{
    [Header("UI References")]
    public Image soundIcon;             // Ícone de som (substitui syllablePanel)
    public Transform optionsParent;     // GridLayoutGroup das opções
    public GameObject optionPrefab;     // Prefab do OptionButton

    [Header("----- UI e Efeitos -----")]
    public GameObject PauseMenu;         // Painel de pausa
    public Text scoreTextPause;          // Texto de score no pause

    [Header("Data")]
    public List<SyllableData> syllables;
    public List<OptionData> allOptions;

    [Header("Feedback Sprites")]
    public Sprite checkSprite;
    public Sprite wrongSprite;

    [Header("End Phase Settings")]
    public GameObject endPhasePanel;
    public Text scoreEndPhase;
    public ParticleSystem confettiEffect;
    public Transform spawnPoint;
    public int score = 0;

    [Header("--- Configurações de Tempo (Speed) ---")]
    [Tooltip("Tempo de espera após errar antes de liberar o input novamente.")]
    [SerializeField] private float delayAfterWrong = 0.5f; // Era 1.8f fixo

    [Tooltip("Tempo extra após o feedback visual de acerto antes de ir para o próximo round.")]
    [SerializeField] private float bufferAfterCorrect = 0.2f; // Tempo curto para transição

    [Tooltip("Se deve tocar o áudio da sílaba novamente após errar.")]
    [SerializeField] private bool replayAudioOnWrong = true;

    private List<OptionButton> currentOptionButtons = new List<OptionButton>();
    private int currentRound = 0;
    private SyllableData currentSyllable;
    private GameManager5 gm;
    private bool inputLocked = true;
    private bool isPlayingSyllable = false;
    private bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        StartCoroutine(WaitForGameManagerAndStart());
    }

    IEnumerator WaitForGameManagerAndStart()
    {
        while (gm == null)
        {
            Time.timeScale = 1f;
            gm = FindObjectOfType<GameManager5>();
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        if (gm != null)
        {
            gm.PlayMusic(gm.backgroundMusic, true);
        }

        StartCoroutine(StartRoundCoroutine());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // =====================================
    // 💾 CONTROLE DE PAUSA
    // =====================================
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (PauseMenu != null) PauseMenu.SetActive(true);
        isPaused = true;
        Time.timeScale = 0f;
        
        // Pausar música
        if (gm != null) gm.PauseMusic();

        // Atualizar score no painel de pause
        if (scoreTextPause != null)
            scoreTextPause.text = "Score: " + score;
    }

    public void ResumeGame()
    {
        if (PauseMenu != null) PauseMenu.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
        
        // Retomar música
        if (gm != null) gm.ResumeMusic();
    }

    // =====================================
    // 🎮 LÓGICA DO ROUND
    // =====================================
    IEnumerator StartRoundCoroutine()
    {
        ClearOptions();

        if (currentRound >= syllables.Count)
        {
            Debug.Log("Fim dos rounds!");
            yield break;
        }

        currentSyllable = syllables[currentRound];

        // Atualiza som da sílaba (som associado ao ícone)
        if (gm != null && currentSyllable.syllableAudio != null)
        {
            yield return StartCoroutine(PlaySyllableIconEffect());
        }

        GenerateOptions(currentSyllable);
        
        // Pequeno delay para garantir que UI foi criada antes de liberar input
        yield return new WaitForSeconds(0.1f); 

        inputLocked = false;
        foreach (var b in currentOptionButtons)
            b.SetInteractable(true);
    }

    IEnumerator PlaySyllableIconEffect()
    {
        isPlayingSyllable = true;

        if (soundIcon != null)
            Tween.Alpha(soundIcon, 0.5f, 0.2f); // efeito de translucidez enquanto toca

        gm.PlaySyllable(currentSyllable.syllableAudio);
        yield return new WaitWhile(() => gm.IsSyllablePlaying());

        if (soundIcon != null)
            Tween.Alpha(soundIcon, 1f, 0.2f); // volta ao normal

        isPlayingSyllable = false;
    }

    void GenerateOptions(SyllableData correctData)
    {
        List<OptionData> pool = new List<OptionData>(allOptions);
        pool.RemoveAll(o => o == null ? true :
            (o.optionName == correctData.correctOption.optionName || o.optionImage == correctData.correctOption.optionImage)
        );

        List<OptionData> chosen = new List<OptionData>();
        int pickCount = Mathf.Min(3, pool.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int r = Random.Range(0, pool.Count);
            chosen.Add(pool[r]);
            pool.RemoveAt(r);
        }

        OptionData correctInstance = allOptions.Find(o => o != null &&
            (o.optionName == correctData.correctOption.optionName || o.optionImage == correctData.correctOption.optionImage)
        );
        if (correctInstance == null) correctInstance = correctData.correctOption;

        chosen.Add(correctInstance);
        Shuffle(chosen);

        foreach (var opt in chosen)
        {
            GameObject go = Instantiate(optionPrefab, optionsParent);
            CanvasGroup cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            OptionButton ob = go.GetComponent<OptionButton>();
            if (ob == null)
            {
                Debug.LogError("OptionPrefab precisa do componente OptionButton.");
                continue;
            }

            currentOptionButtons.Add(ob);
            OptionData localOpt = opt;
            OptionButton localOb = ob;

            localOb.Setup(localOpt.optionImage, localOpt.syllableSprite, () => OnOptionPressed(localOb, localOpt));
            localOb.SetInteractable(false);
            
            // Tween de entrada um pouco mais rápido (0.3s)
            Tween.Alpha(cg, 1f, 0.3f, Ease.OutQuad);
        }
    }

    private void OnOptionPressed(OptionButton button, OptionData clicked)
    {
        if (inputLocked) return;
        StartCoroutine(HandleSelection(button, clicked));
    }

    IEnumerator HandleSelection(OptionButton button, OptionData clicked)
    {
        inputLocked = true;

        // Desabilita interação imediatamente para evitar cliques duplos
        foreach (var b in currentOptionButtons)
        {
            b.SetInteractable(false);
            b.SetPressedVisual(false);
        }

        button.SetPressedVisual(true);
        button.ShowSyllable();

        bool isCorrect = (clicked != null && currentSyllable != null &&
            (clicked.optionName == currentSyllable.correctOption.optionName ||
             clicked.optionImage == currentSyllable.correctOption.optionImage));

        if (isCorrect)
        {
            // === LÓGICA DE ACERTO ===
            button.ShowFeedback(true, checkSprite);

            // Toca o áudio da opção
            if (gm != null && clicked.optionAudio != null)
            {
                gm.PlayOption(clicked.optionAudio);
                // Espera o áudio, mas se for muito longo e quiser agilizar, 
                // você pode remover esse WaitWhile ou limitar o tempo.
                yield return new WaitWhile(() => gm.IsOptionPlaying());
            }

            // Toca som de "Plim"
            if (gm != null) gm.PlayCorrect();

            // Espera só o tempo necessário da animação de feedback + um buffer pequeno
            float waitTime = Mathf.Max(0.5f, button.feedbackDuration) + bufferAfterCorrect;
            yield return new WaitForSeconds(waitTime);

            currentRound++;
            score++;

            foreach (var b in currentOptionButtons) b.SetPressedVisual(false);

            if (currentRound >= syllables.Count)
                ShowEndPhasePanel();
            else
                StartCoroutine(StartRoundCoroutine());
        }
        else
        {
            // === LÓGICA DE ERRO ===
            button.ShowFeedback(false, wrongSprite);

            if (gm != null)
            {
                gm.ShakeCamera(0.35f, 12f);
                gm.PlayWrong();

                // OTIMIZAÇÃO: Espera reduzida (controlada pelo Inspector)
                yield return new WaitForSeconds(delayAfterWrong);

                // Opcional: Replay da pergunta
                if (replayAudioOnWrong && currentSyllable != null && currentSyllable.syllableAudio != null)
                {
                    gm.PlaySyllable(currentSyllable.syllableAudio);
                    yield return new WaitWhile(() => gm.IsSyllablePlaying());
                }
            }
            else
            {
                // Fallback se não tiver GM
                yield return new WaitForSeconds(delayAfterWrong);
            }

            // Remove o feedback visual (opcional esperar o fade ou cortar)
            // Aqui estamos assumindo que o ShowFeedback já lidou com a animação de saída ou que vamos resetar o estado.
            
            inputLocked = false;
            foreach (var b in currentOptionButtons)
            {
                b.SetInteractable(true);
                b.SetPressedVisual(false);
            }
        }
    }

    void ClearOptions()
    {
        foreach (Transform child in optionsParent)
            Destroy(child.gameObject);
        currentOptionButtons.Clear();
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    public void RepeatQuestion()
    {
        if (gm != null && currentSyllable != null && currentSyllable.syllableAudio != null)
        {
            StartCoroutine(PlaySyllableIconEffect());
        }
    }

    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    IEnumerator ShowEndPhasePanelCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (scoreEndPhase != null)
            scoreEndPhase.text = "Score: " + score.ToString();

        if (endPhasePanel != null)
            endPhasePanel.SetActive(true);

        if (spawnPoint != null)
            spawnPoint.gameObject.SetActive(false);

        if (confettiEffect != null)
            confettiEffect.Play();

        if (gm != null)
        {
            if (gm.sfxSource != null && gm.confettiSfx != null)
                gm.sfxSource.PlayOneShot(gm.confettiSfx, gm.sfxVolume);

            gm.StopMusic();
        }

        Debug.Log("Fim dos rounds! Painel final exibido.");
    }

    public void ForcePlayMusic()
    {
        if (gm != null && gm.backgroundMusic != null)
        {
            gm.PlayMusic(gm.backgroundMusic, true);
        }
    }
}