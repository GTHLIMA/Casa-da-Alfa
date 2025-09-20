<<<<<<< Updated upstream
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SyllablePuzzleManager : MonoBehaviour
{
    #region Data Structures
    [System.Serializable]
    public class OnScreenWord
    {
        public Sprite drawingImage;
        public Sprite syllableImage;
        public AudioClip syllableAudio;
    }

    [System.Serializable]
    public class AnswerSyllable
    {
        public Sprite syllableImage;
        public AudioClip syllableAudio;
    }

    [System.Serializable]
    public class PuzzleData
    {
        public string targetWord;
        public Sprite finalDrawingImage;
        public Sprite finalWordImage;
        public AudioClip finalWordAudio;
        public List<OnScreenWord> onScreenWords;
        public List<AnswerSyllable> answerSequence;
    }
    #endregion

    [Header("Configuração dos Quebra-Cabeças")]
    public List<PuzzleData> allPuzzles;

    #region Public References
    [Header("Prefabs e Imagens Iniciais")]
    public GameObject sourceButtonPrefab;
    public GameObject answerSlotPrefab;
    public Sprite questionMarkSprite;

    [Header("Parent Panels")]
    public Transform sourceButtonParent;
    public Transform answerSlotParent;
    public Image fundoImageDisplay;

    [Header("Final Reveal Displays")]
    public Image finalImageDisplay;
    public Image finalWordDisplay;
    

    [Header("Configurações de Animação e Tempo")]
    public float bounceDuration = 0.5f;
    public AnimationCurve bounceCurve;
    public float delayAfterClick = 0.3f;
    public float delayBeforeReview = 1.0f;
    public float delayBetweenSlotReveals = 0.7f;
    public float delayBeforeUnification = 1.0f;
    public float delayAfterWin = 2.0f;
    #endregion

    #region Private State
    private List<SyllableSourceButton> activeSourceButtons = new List<SyllableSourceButton>();
    private List<AnswerSlotController> activeAnswerSlots = new List<AnswerSlotController>();
    private PuzzleData currentPuzzle;
    private int nextClickIndex = 0; 
    private AudioManager audioManager;
    private int currentPuzzleIndex = 0;
    private bool isReviewing = false;

    private ButtonFloatEffect currentFloatEffect;
    #endregion

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
    }

    private void Start()
    {
        LoadPuzzle(currentPuzzleIndex);
    }

    void LoadPuzzle(int puzzleIndex)
    {
        ClearBoard();
        if (puzzleIndex >= allPuzzles.Count)
        {
            EndGame(); 
            return;
        }
        currentPuzzle = allPuzzles[puzzleIndex];
        finalImageDisplay.gameObject.SetActive(true);
        finalImageDisplay.sprite = questionMarkSprite;

        for (int i = 0; i < currentPuzzle.answerSequence.Count; i++)
        {
            GameObject slotGO = Instantiate(answerSlotPrefab, answerSlotParent);
            AnswerSlotController slotController = slotGO.GetComponent<AnswerSlotController>();
            slotController.Setup(this, questionMarkSprite);
            activeAnswerSlots.Add(slotController);
        }
        
        // CORRIGIDO: Garante que os botões sejam criados na ordem definida em 'onScreenWords'
        foreach (var sourceWord in currentPuzzle.onScreenWords)
        {
            GameObject buttonGO = Instantiate(sourceButtonPrefab, sourceButtonParent);
            SyllableSourceButton buttonScript = buttonGO.GetComponent<SyllableSourceButton>();
            buttonScript.Setup(sourceWord, this);
            activeSourceButtons.Add(buttonScript);
        }

        // NOVO: Aplica destaque no primeiro botão
        HighlightNextButton();
    }

    public void PlayAudio(AudioClip clip)
    {
        if (audioManager != null && clip != null) audioManager.PlaySFX(clip);
    }

    public void OnSourceButtonClicked(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        if (isReviewing) return;

        if (button != activeSourceButtons[nextClickIndex])
        {
            Debug.Log("Clique fora de ordem!");
            return;
        }

        StartCoroutine(SourceButtonClickSequence(clickedWord, button));
    }

    private IEnumerator SourceButtonClickSequence(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        button.SetUsed(true);

        // NOVO: Para o efeito no botão atual
        StopHighlightOn(button);

        nextClickIndex++; 
        
        yield return new WaitForSeconds(delayAfterClick);

        button.RevealLocalSyllable();
        PlayAudio(clickedWord.syllableAudio);

        // NOVO: Destaca o próximo botão
        HighlightNextButton();

        if (nextClickIndex >= activeSourceButtons.Count)
        {
            isReviewing = true;
            StartCoroutine(ReviewAndWinSequence());
        }
    }

    private IEnumerator ReviewAndWinSequence()
    {
        yield return new WaitForSeconds(delayBeforeReview);

        for (int i = 0; i < currentPuzzle.answerSequence.Count; i++)
        {
            AnswerSyllable answerSyllable = currentPuzzle.answerSequence[i];
            activeAnswerSlots[i].RevealSyllable(answerSyllable.syllableImage, answerSyllable.syllableAudio);
            yield return new WaitForSeconds(delayBetweenSlotReveals);
        }
        
        yield return new WaitForSeconds(delayBeforeUnification);

        ClearAnswerSlots();
        finalWordDisplay.sprite = currentPuzzle.finalWordImage;
        finalWordDisplay.gameObject.SetActive(true);
        StartCoroutine(BounceAnimation(finalWordDisplay.rectTransform));
        
        finalImageDisplay.sprite = currentPuzzle.finalDrawingImage;
        StartCoroutine(BounceAnimation(finalImageDisplay.rectTransform));

        PlayAudio(currentPuzzle.finalWordAudio);
        yield return new WaitForSeconds(delayAfterWin);
        currentPuzzleIndex++;
        LoadPuzzle(currentPuzzleIndex);
    }
    
    public IEnumerator BounceAnimation(RectTransform targetRect)
    {
        float timer = 0f;
        Vector3 originalScale = Vector3.one;
        while (timer < bounceDuration)
        {
            float scaleValue = bounceCurve.Evaluate(timer / bounceDuration);
            targetRect.localScale = originalScale * scaleValue;
            timer += Time.deltaTime;
            yield return null;
        }
        targetRect.localScale = originalScale;
    }
    
    private void ClearBoard()
    {
        foreach (var button in activeSourceButtons) 
            if(button != null) Destroy(button.gameObject);
        activeSourceButtons.Clear();
        ClearAnswerSlots();
        finalWordDisplay.gameObject.SetActive(false);
        nextClickIndex = 0;
        isReviewing = false;
    }
    
    private void ClearAnswerSlots()
    {
        foreach (var slot in activeAnswerSlots) 
            if(slot != null) Destroy(slot.gameObject);
        activeAnswerSlots.Clear();
    }
    
    private void EndGame()
    {
        Debug.Log("FIM DE JOGO! Chamando GameManager...");

        finalWordDisplay.gameObject.SetActive(false);
        sourceButtonParent.gameObject.SetActive(false);
        answerSlotParent.gameObject.SetActive(false);
        finalImageDisplay.gameObject.SetActive(false);
        fundoImageDisplay.gameObject.SetActive(false);
        

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowEndPhasePanel();
        }
        else
        {
            Debug.LogError("GameManager.Instance não encontrado! O painel de fim de fase não será mostrado.");
        }
    }

    private void HighlightNextButton()
    {
    if (nextClickIndex < activeSourceButtons.Count)
    {
        var button = activeSourceButtons[nextClickIndex];
        if (button.GetComponent<ButtonFloatEffect>() == null)
        {
            currentFloatEffect = button.gameObject.AddComponent<ButtonFloatEffect>();
            currentFloatEffect.floatSpeed = 2f;
            currentFloatEffect.floatHeight = 15f;
        }
    }
    }

    private void StopHighlightOn(SyllableSourceButton button)
    {
    var effect = button.GetComponent<ButtonFloatEffect>();
    if (effect != null) Destroy(effect);
    }
}
=======
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SyllablePuzzleManager : MonoBehaviour
{
    #region Data Structures
    [System.Serializable]
    public class OnScreenWord
    {
        public Sprite drawingImage;
        public Sprite syllableImage;
        public AudioClip syllableAudio;
    }

    [System.Serializable]
    public class AnswerSyllable
    {
        public Sprite syllableImage;
        public AudioClip syllableAudio;
    }

    [System.Serializable]
    public class PuzzleData
    {
        public string targetWord;
        public Sprite finalDrawingImage;
        public Sprite finalWordImage;
        public AudioClip finalWordAudio;
        public List<OnScreenWord> onScreenWords;
        public List<AnswerSyllable> answerSequence;
    }
    #endregion

    [Header("Configuração dos Quebra-Cabeças")]
    public List<PuzzleData> allPuzzles;

    #region Public References
    [Header("Prefabs e Imagens Iniciais")]
    public GameObject sourceButtonPrefab;
    public GameObject answerSlotPrefab;
    public Sprite questionMarkSprite;

    [Header("Parent Panels")]
    public Transform sourceButtonParent;
    public Transform answerSlotParent;
    public Image fundoImageDisplay;

    [Header("Final Reveal Displays")]
    public Image finalImageDisplay;
    public Image finalWordDisplay;
    

    [Header("Configurações de Animação e Tempo")]
    public float bounceDuration = 0.5f;
    public AnimationCurve bounceCurve;
    public float delayAfterClick = 0.3f;
    public float delayBeforeReview = 1.0f;
    public float delayBetweenSlotReveals = 0.7f;
    public float delayBeforeUnification = 1.0f;
    public float delayAfterWin = 2.0f;
    #endregion

    #region Private State
    private List<SyllableSourceButton> activeSourceButtons = new List<SyllableSourceButton>();
    private List<AnswerSlotController> activeAnswerSlots = new List<AnswerSlotController>();
    private PuzzleData currentPuzzle;
    private int nextClickIndex = 0; 
    private AudioManager audioManager;
    private int currentPuzzleIndex = 0;
    private bool isReviewing = false;

    private ButtonFloatEffect currentFloatEffect;
    #endregion

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
    }

    private void Start()
    {
        LoadPuzzle(currentPuzzleIndex);
    }

    void LoadPuzzle(int puzzleIndex)
    {
        ClearBoard();
        if (puzzleIndex >= allPuzzles.Count)
        {
            EndGame(); 
            return;
        }
        currentPuzzle = allPuzzles[puzzleIndex];
        finalImageDisplay.gameObject.SetActive(true);
        finalImageDisplay.sprite = questionMarkSprite;

        for (int i = 0; i < currentPuzzle.answerSequence.Count; i++)
        {
            GameObject slotGO = Instantiate(answerSlotPrefab, answerSlotParent);
            AnswerSlotController slotController = slotGO.GetComponent<AnswerSlotController>();
            slotController.Setup(this, questionMarkSprite);
            activeAnswerSlots.Add(slotController);
        }
        
        // CORRIGIDO: Garante que os botões sejam criados na ordem definida em 'onScreenWords'
        foreach (var sourceWord in currentPuzzle.onScreenWords)
        {
            GameObject buttonGO = Instantiate(sourceButtonPrefab, sourceButtonParent);
            SyllableSourceButton buttonScript = buttonGO.GetComponent<SyllableSourceButton>();
            buttonScript.Setup(sourceWord, this);
            activeSourceButtons.Add(buttonScript);
        }

        // NOVO: Aplica destaque no primeiro botão
        HighlightNextButton();
    }

    public void PlayAudio(AudioClip clip)
    {
        if (audioManager != null && clip != null) audioManager.PlaySFX(clip);
    }

    public void OnSourceButtonClicked(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        if (isReviewing) return;

        if (button != activeSourceButtons[nextClickIndex])
        {
            Debug.Log("Clique fora de ordem!");
            return;
        }

        StartCoroutine(SourceButtonClickSequence(clickedWord, button));
    }

    private IEnumerator SourceButtonClickSequence(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        button.SetUsed(true);

        // NOVO: Para o efeito no botão atual
        StopHighlightOn(button);

        nextClickIndex++; 
        
        yield return new WaitForSeconds(delayAfterClick);

        button.RevealLocalSyllable();
        PlayAudio(clickedWord.syllableAudio);

        // NOVO: Destaca o próximo botão
        HighlightNextButton();

        if (nextClickIndex >= activeSourceButtons.Count)
        {
            isReviewing = true;
            StartCoroutine(ReviewAndWinSequence());
        }
    }

    private IEnumerator ReviewAndWinSequence()
    {
        yield return new WaitForSeconds(delayBeforeReview);

        for (int i = 0; i < currentPuzzle.answerSequence.Count; i++)
        {
            AnswerSyllable answerSyllable = currentPuzzle.answerSequence[i];
            activeAnswerSlots[i].RevealSyllable(answerSyllable.syllableImage, answerSyllable.syllableAudio);
            yield return new WaitForSeconds(delayBetweenSlotReveals);
        }
        
        yield return new WaitForSeconds(delayBeforeUnification);

        ClearAnswerSlots();
        finalWordDisplay.sprite = currentPuzzle.finalWordImage;
        finalWordDisplay.gameObject.SetActive(true);
        StartCoroutine(BounceAnimation(finalWordDisplay.rectTransform));
        
        finalImageDisplay.sprite = currentPuzzle.finalDrawingImage;
        StartCoroutine(BounceAnimation(finalImageDisplay.rectTransform));

        PlayAudio(currentPuzzle.finalWordAudio);
        yield return new WaitForSeconds(delayAfterWin);
        currentPuzzleIndex++;
        LoadPuzzle(currentPuzzleIndex);
    }
    
    public IEnumerator BounceAnimation(RectTransform targetRect)
    {
        float timer = 0f;
        Vector3 originalScale = Vector3.one;
        while (timer < bounceDuration)
        {
            float scaleValue = bounceCurve.Evaluate(timer / bounceDuration);
            targetRect.localScale = originalScale * scaleValue;
            timer += Time.deltaTime;
            yield return null;
        }
        targetRect.localScale = originalScale;
    }
    
    private void ClearBoard()
    {
        foreach (var button in activeSourceButtons) 
            if(button != null) Destroy(button.gameObject);
        activeSourceButtons.Clear();
        ClearAnswerSlots();
        finalWordDisplay.gameObject.SetActive(false);
        nextClickIndex = 0;
        isReviewing = false;
    }
    
    private void ClearAnswerSlots()
    {
        foreach (var slot in activeAnswerSlots) 
            if(slot != null) Destroy(slot.gameObject);
        activeAnswerSlots.Clear();
    }
    
    private void EndGame()
    {
        Debug.Log("FIM DE JOGO! Chamando GameManager...");

        finalWordDisplay.gameObject.SetActive(false);
        sourceButtonParent.gameObject.SetActive(false);
        answerSlotParent.gameObject.SetActive(false);
        finalImageDisplay.gameObject.SetActive(false);
        fundoImageDisplay.gameObject.SetActive(false);
        

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowEndPhasePanel();
        }
        else
        {
            Debug.LogError("GameManager.Instance não encontrado! O painel de fim de fase não será mostrado.");
        }
    }

    private void HighlightNextButton()
    {
    if (nextClickIndex < activeSourceButtons.Count)
    {
        var button = activeSourceButtons[nextClickIndex];
        if (button.GetComponent<ButtonFloatEffect>() == null)
        {
            currentFloatEffect = button.gameObject.AddComponent<ButtonFloatEffect>();
            currentFloatEffect.floatSpeed = 5f;
            currentFloatEffect.floatHeight = 10f;
        }
    }
    }

    private void StopHighlightOn(SyllableSourceButton button)
    {
    var effect = button.GetComponent<ButtonFloatEffect>();
    if (effect != null) Destroy(effect);
    }
}
>>>>>>> Stashed changes
