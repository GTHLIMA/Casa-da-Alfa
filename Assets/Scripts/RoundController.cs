using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

[System.Serializable]
public class OptionData
{
    public string optionName;
    public Sprite optionImage;
    public AudioClip optionAudio;
    public Sprite syllableSprite;
}

[System.Serializable]
public class SyllableData
{
    public string syllableName;
    public Sprite syllableImage;
    public AudioClip syllableAudio;
    public OptionData correctOption;
}

public class RoundController : MonoBehaviour
{
    [Header("UI References")]
    public Image syllablePanel;
    public Transform optionsParent;
    public GameObject optionPrefab;

    [Header("----- UI e Efeitos -----")]
    public GameObject PauseMenu;         // Painel de pausa
    public GameObject endPhasePanel;     // Painel de fim de fase
    public ParticleSystem confettiEffect;
    public Transform spawnPoint;
    public Text scoreTextPause;
    public Text scoreTextEnd;

    [Header("Data")]
    public List<SyllableData> syllables;
    public List<OptionData> allOptions;

    [Header("Feedback Sprites")]
    public Sprite checkSprite;
    public Sprite wrongSprite;

    [Header("End Phase Settings")]
    public Text scoreEndPhase;           // texto de pontuação final
    public int score = 0;                // contador de acertos

    private List<OptionButton> currentOptionButtons = new List<OptionButton>();
    private int currentRound = 0;
    private SyllableData currentSyllable;
    private GameManager5 gm;
    private bool inputLocked = true;
    private bool isPaused = false;

    void Start()
    {
        // 🔥 GARANTIR QUE O JOGO NÃO INICIE PAUSADO
        Time.timeScale = 1f;
        isPaused = false;
        
        StartCoroutine(WaitForGameManagerAndStart());
    }

    IEnumerator WaitForGameManagerAndStart()
    {
        while (gm == null)
        {
            gm = FindObjectOfType<GameManager5>();
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        // 🔥 FORÇAR MÚSICA TOCAR AO INICIAR A CENA
        if (gm != null)
        {
            gm.PlayMusic(gm.backgroundMusic, true);
        }

        StartCoroutine(StartRoundCoroutine());
    }

    void Update()
    {
        // 🔥 CONTROLE DE PAUSA COM TECLA (OPCIONAL)
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

        // atualiza a imagem da sílaba
        if (syllablePanel != null) syllablePanel.sprite = currentSyllable.syllableImage;

        // animação de entrada da sílaba (fade in)
        Tween.Alpha(syllablePanel, 0f, 0f);
        Tween.Alpha(syllablePanel, 1f, 0.45f, Ease.OutQuad);

        // toca sílaba e espera
        if (gm != null && currentSyllable.syllableAudio != null)
        {
            gm.PlaySyllable(currentSyllable.syllableAudio);
            yield return new WaitWhile(() => gm.IsSyllablePlaying());
        }

        // gera opções
        GenerateOptions(currentSyllable);

        // small delay then enable
        yield return new WaitForSeconds(0.18f);
        inputLocked = false;
        foreach (var b in currentOptionButtons) b.SetInteractable(true);
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

        if (chosen.Count < 3)
        {
            List<OptionData> fallback = new List<OptionData>(allOptions);
            fallback.RemoveAll(o => o == null ? true :
                (o.optionName == correctData.correctOption.optionName || o.optionImage == correctData.correctOption.optionImage)
            );
            while (chosen.Count < 3 && fallback.Count > 0)
            {
                int r = Random.Range(0, fallback.Count);
                if (!chosen.Contains(fallback[r])) chosen.Add(fallback[r]);
                fallback.RemoveAt(r);
            }
        }

        while (chosen.Count < 3)
        {
            if (allOptions.Count > 0) chosen.Add(allOptions[0]);
            else break;
        }

        OptionData correctInstance = allOptions.Find(o => o != null &&
            (o.optionName == correctData.correctOption.optionName || o.optionImage == correctData.correctOption.optionImage)
        );
        if (correctInstance == null) correctInstance = correctData.correctOption;

        chosen.Add(correctInstance);
        Shuffle(chosen);

        string debugNames = "";
        foreach (var c in chosen) debugNames += (c != null ? c.optionName : "NULL") + ", ";
        Debug.Log($"Round {currentRound} options: {debugNames}");

        foreach (var opt in chosen)
        {
            GameObject go = Instantiate(optionPrefab, optionsParent);

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
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

            Tween.Alpha(cg, 1f, 0.45f, Ease.OutQuad);
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
            button.ShowFeedback(true, checkSprite);

            if (gm != null && clicked.optionAudio != null)
            {
                gm.PlayOption(clicked.optionAudio);
                yield return new WaitWhile(() => gm.IsOptionPlaying());
            }

            yield return new WaitForSeconds(0.15f);
            if (gm != null) gm.PlayCorrect();

            float waitTime = button.feedbackDuration + button.fadeDuration + 0.25f;
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
            button.ShowFeedback(false, wrongSprite);

            if (gm != null)
            {
                gm.ShakeCamera(0.35f, 12f);
                gm.PlayWrong();
                if (currentSyllable != null && currentSyllable.syllableAudio != null)
                    gm.PlaySyllable(currentSyllable.syllableAudio);
            }

            float waitTime = button.feedbackDuration + button.fadeDuration + 0.25f;
            yield return new WaitForSeconds(waitTime);

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
        foreach (Transform child in optionsParent) Destroy(child.gameObject);
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
            gm.PlaySyllable(currentSyllable.syllableAudio);
        }
    }

    // =====================================
    // 🎉 FIM DE FASE
    // =====================================
    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    private IEnumerator ShowEndPhasePanelCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (scoreEndPhase != null)
            scoreEndPhase.text = "Score: " + score.ToString();

        if (endPhasePanel != null)
            endPhasePanel.SetActive(true);

        if (spawnPoint != null)
            spawnPoint.gameObject.SetActive(false);

        if (confettiEffect != null)
        {
            confettiEffect.Play();
            Debug.Log("Efeito de confete ativado!");
        }

        if (gm != null)
        {
            gm.PlayConfettiSound();
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