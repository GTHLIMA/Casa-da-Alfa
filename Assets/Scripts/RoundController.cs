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
            Debug.Log("Fim dos rounds!");
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

        // small delay then enable
        yield return new WaitForSeconds(0.18f);
        inputLocked = false;
        foreach (var b in currentOptionButtons) b.SetInteractable(true);
    }

    void GenerateOptions(SyllableData correctData)
{
    // build pool and remove any option that matches by name or sprite (evita duplicatas mesmo que instâncias sejam diferentes)
    List<OptionData> pool = new List<OptionData>(allOptions);
    pool.RemoveAll(o => o == null ? true :
        (o.optionName == correctData.correctOption.optionName || o.optionImage == correctData.correctOption.optionImage)
    );

    // pick 3 distractors
    List<OptionData> chosen = new List<OptionData>();
    int pickCount = Mathf.Min(3, pool.Count);
    for (int i = 0; i < pickCount; i++)
    {
        int r = Random.Range(0, pool.Count);
        chosen.Add(pool[r]);
        pool.RemoveAt(r);
    }

    // fallback: if not enough distractors, pick from allOptions excluding items that match the correct option by name/image
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

    // ensure 3 distractors (worst-case, duplicate the first distractor to avoid crash — but ideally your allOptions has 17 unique)
    while (chosen.Count < 3)
    {
        if (allOptions.Count > 0) chosen.Add(allOptions[0]);
        else break;
    }

    // Now get the correct OPTION instance from allOptions if exists (match by name or image), otherwise use correctData.correctOption
    OptionData correctInstance = allOptions.Find(o => o != null &&
        (o.optionName == correctData.correctOption.optionName || o.optionImage == correctData.correctOption.optionImage)
    );
    if (correctInstance == null) correctInstance = correctData.correctOption;

    // add the correct and shuffle
    chosen.Add(correctInstance);
    Shuffle(chosen);

    // DEBUG: lista de escolhidos (ajuda a ver duplicatas)
    string debugNames = "";
    foreach (var c in chosen) debugNames += (c != null ? c.optionName : "NULL") + ", ";
    Debug.Log($"Round {currentRound} options: {debugNames}");

    // instantiate and setup
    foreach (var opt in chosen)
    {
        GameObject go = Instantiate(optionPrefab, optionsParent);
        // ensure CanvasGroup present for fade
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

        // local copy to avoid closure issue
        OptionData localOpt = opt;
        OptionButton localOb = ob;
        localOb.Setup(localOpt.optionImage, localOpt.syllableSprite, () => OnOptionPressed(localOb, localOpt));
        localOb.SetInteractable(false);

        // fade-in the button's CanvasGroup
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

    button.ShowSyllable();

    bool isCorrect = (clicked != null && currentSyllable != null &&
        (clicked.optionName == currentSyllable.correctOption.optionName ||
         clicked.optionImage == currentSyllable.correctOption.optionImage));

    if (isCorrect)
    {
        button.ShowFeedback(true, checkSprite);

        // toca som do desenho + som de acerto
        if (gm != null)
        {
            if (clicked.optionAudio != null)
                gm.PlayOption(clicked.optionAudio); // som da figura
            gm.PlayCorrect(); // efeito de acerto
        }

        float waitTime = button.feedbackDuration + button.fadeDuration + 0.25f;
        yield return new WaitForSeconds(waitTime);

        currentRound++;
        StartCoroutine(StartRoundCoroutine());
    }
    else
    {
        button.ShowFeedback(false, wrongSprite);

        if (gm != null)
        {
            gm.ShakeCamera(0.35f, 12f);
            gm.PlayWrong(); // efeito sonoro opcional de erro
            // toca novamente o som da sílaba atual
            if (currentSyllable != null && currentSyllable.syllableAudio != null)
                gm.PlaySyllable(currentSyllable.syllableAudio);
        }

        float waitTime = button.feedbackDuration + button.fadeDuration + 0.25f;
        yield return new WaitForSeconds(waitTime);

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

}
