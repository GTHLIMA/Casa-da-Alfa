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

    [Header("Final Reveal Displays")]
    public Image finalImageDisplay;
    public Image finalWordDisplay;

    [Header("Configurações de Animação e Tempo")]
    public float bounceDuration = 0.5f;
    public AnimationCurve bounceCurve;
    
    [Tooltip("Pausa após o clique no desenho, ANTES de revelar a sílaba filha.")]
    public float delayAfterClick = 0.3f;
    [Tooltip("Pausa após todos os desenhos serem clicados, ANTES das interrogações começarem a ser substituídas.")]
    public float delayBeforeReview = 1.0f;
    [Tooltip("Pausa entre a revelação de cada sílaba na sequência de revisão.")]
    public float delayBetweenSlotReveals = 0.7f;
    [Tooltip("Pausa após a última sílaba ser revelada, ANTES de se unirem na palavra final.")]
    public float delayBeforeUnification = 1.0f;
    [Tooltip("Pausa final após a vitória, ANTES de carregar o próximo quebra-cabeça.")]
    public float delayAfterWin = 2.0f;
    #endregion

    #region Private State
    private List<SyllableSourceButton> activeSourceButtons = new List<SyllableSourceButton>();
    private List<AnswerSlotController> activeAnswerSlots = new List<AnswerSlotController>();
    private PuzzleData currentPuzzle;
    private int clicksMade = 0;
    private AudioManager audioManager;
    private int currentPuzzleIndex = 0;
    private bool isReviewing = false;
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
            // --- LÓGICA DE FIM DE JOGO ATUALIZADA ---
            EndGame(); // Chama o novo método de finalização
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
        foreach (var sourceWord in currentPuzzle.onScreenWords)
        {
            GameObject buttonGO = Instantiate(sourceButtonPrefab, sourceButtonParent);
            SyllableSourceButton buttonScript = buttonGO.GetComponent<SyllableSourceButton>();
            buttonScript.Setup(sourceWord, this);
            activeSourceButtons.Add(buttonScript);
        }
    }

    public void PlayAudio(AudioClip clip)
    {
        if (audioManager != null && clip != null) audioManager.PlaySFX(clip);
    }

    public void OnSourceButtonClicked(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        if (isReviewing) return;
        StartCoroutine(SourceButtonClickSequence(clickedWord, button));
    }

    private IEnumerator SourceButtonClickSequence(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        button.SetUsed(true);
        
        yield return new WaitForSeconds(delayAfterClick);

        button.RevealLocalSyllable();
        PlayAudio(clickedWord.syllableAudio);

        clicksMade++;
        if (clicksMade >= activeSourceButtons.Count)
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
        clicksMade = 0;
        isReviewing = false;
    }
    
    private void ClearAnswerSlots()
    {
        foreach (var slot in activeAnswerSlots) 
            if(slot != null) Destroy(slot.gameObject);
        activeAnswerSlots.Clear();
    }
    
    // --- NOVO MÉTODO PARA FINALIZAR O JOGO ---
    private void EndGame()
    {
        Debug.Log("FIM DE JOGO! Chamando GameManager...");

        // Esconde os painéis de interação
        finalWordDisplay.gameObject.SetActive(false);
        sourceButtonParent.gameObject.SetActive(false);
        answerSlotParent.gameObject.SetActive(false);
        finalImageDisplay.gameObject.SetActive(false);

        // Verifica se a instância do GameManager existe antes de chamá-la
        if (GameManager.Instance != null)
        {
            // Usa o método ShowEndPhasePanel que você já tem no seu GameManager
            GameManager.Instance.ShowEndPhasePanel();
        }
        else
        {
            Debug.LogError("GameManager.Instance não encontrado! O painel de fim de fase não será mostrado.");
        }
    }
}