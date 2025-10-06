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
    public Transform optionsParent; // GridLayoutGroup aqui
    public GameObject optionPrefab; // prefab com OptionButton

    [Header("Data")]
    public List<SyllableData> syllables;
    public List<OptionData> allOptions;

    [Header("Feedback Sprites")]
    public Sprite checkSprite;
    public Sprite wrongSprite;

    [Header("End Phase Settings")]
    public GameObject endPhasePanel;     // painel de fim de fase
    public Text scoreEndPhase;           // texto de pontuação final (se quiser)
    public ParticleSystem confettiEffect; // partículas de comemoração
    public Transform spawnPoint;         // se houver spawner de balões
    public int score = 0;                // contador de acertos

    private List<OptionButton> currentOptionButtons = new List<OptionButton>();
    private int currentRound = 0;
    private SyllableData currentSyllable;
    private GameManager5 gm;
    private bool inputLocked = true;

    void Start()
    {
        gm = FindObjectOfType<GameManager5>();
        StartCoroutine(StartRoundCoroutine());
    }

    IEnumerator StartRoundCoroutine()
    {
        ClearOptions();

        if (currentRound >= syllables.Count)
        {
            ShowEndPhasePanel();
            yield break;
        }

        currentSyllable = syllables[currentRound];

        // atualiza a imagem da sílaba
        if (syllablePanel != null) syllablePanel.sprite = currentSyllable.syllableImage;

        // animação de entrada da sílaba (fade in)
        Tween.Alpha(syllablePanel, 0f, 0f); // garante 0 antes
        Tween.Alpha(syllablePanel, 1f, 0.45f, Ease.OutQuad);

        // toca sílaba e espera
        if (gm != null && currentSyllable.syllableAudio != null)
        {
            gm.PlaySyllable(currentSyllable.syllableAudio);
            yield return new WaitWhile(() => gm.IsSyllablePlaying());
        }

        // gera opções
        GenerateOptions(currentSyllable);

        // pequeno delay e libera input
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
    foreach (var b in currentOptionButtons) b.SetInteractable(false);

    // Mostra a sílaba do botão clicado
    button.ShowSyllable();

    bool isCorrect = (clicked != null && currentSyllable != null &&
        (clicked.optionName == currentSyllable.correctOption.optionName ||
         clicked.optionImage == currentSyllable.correctOption.optionImage));

    if (isCorrect)
    {
        // 🔊 Toca o som do desenho correto
        if (gm != null && clicked.optionAudio != null)
        {
            gm.PlayOption(clicked.optionAudio);
            yield return new WaitWhile(() => gm.IsOptionPlaying());
        }

        // ✅ Feedback de acerto visual e som
        button.ShowFeedback(true, checkSprite);
        if (gm != null) gm.PlayCorrect();

        // Aguarda o feedback e avança pro próximo round
        float waitTime = button.feedbackDuration + button.fadeDuration + 0.25f;
        yield return new WaitForSeconds(waitTime);

        currentRound++;
        StartCoroutine(StartRoundCoroutine());
    }
    else
    {
        // ❌ Feedback de erro visual
        button.ShowFeedback(false, wrongSprite);

        // Efeito de tremer a tela
        if (gm != null) gm.ShakeCamera(0.35f, 12f);

        // Espera o efeito de erro aparecer
        yield return new WaitForSeconds(0.25f);

        // 🔁 Repete o som da sílaba (pergunta)
        if (gm != null && currentSyllable != null && currentSyllable.syllableAudio != null)
        {
            gm.PlaySyllable(currentSyllable.syllableAudio);
            yield return new WaitWhile(() => gm.IsSyllablePlaying());
        }

        // Libera novamente os botões pra tentar de novo
        inputLocked = false;
        foreach (var b in currentOptionButtons) b.SetInteractable(true);
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

        Debug.Log("Fim dos rounds! Painel final exibido.");

        // Pausar música e tocar som de fim
        if (gm != null)
        {
            if (gm.musicSource != null) gm.musicSource.Stop();
            if (gm.sfxSource != null && gm.correctSFX != null)
            {
                gm.sfxSource.PlayOneShot(gm.correctSFX, gm.sfxVolume);
            }
        }

        if (ScoreTransfer.Instance != null)
            ScoreTransfer.Instance.SetScore(score);
    }
}
