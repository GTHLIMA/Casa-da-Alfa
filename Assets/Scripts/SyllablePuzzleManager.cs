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
        public List<OnScreenWord> onScreenWords; // Define quais botões aparecem e em que ordem
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
    private int nextClickIndex = 0; // CORRIGIDO: Substitui 'clicksMade' por um índice de sequência
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
    }

    public void PlayAudio(AudioClip clip)
    {
        if (audioManager != null && clip != null) audioManager.PlaySFX(clip);
    }

    // --- LÓGICA DE CLIQUE COMPLETAMENTE CORRIGIDA PARA VERIFICAR ORDEM ---
    public void OnSourceButtonClicked(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        if (isReviewing) return;

        // Verifica se o botão clicado é o próximo na sequência esperada.
        // A sequência é a ordem dos botões na lista 'activeSourceButtons'.
        if (button != activeSourceButtons[nextClickIndex])
        {
            Debug.Log("Clique fora de ordem!");
            // Pode adicionar um som de erro aqui.
            return;
        }

        // Se chegou aqui, o clique foi CERTO e na ORDEM CORRETA
        StartCoroutine(SourceButtonClickSequence(clickedWord, button));
    }

    private IEnumerator SourceButtonClickSequence(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        button.SetUsed(true);
        nextClickIndex++; // Avança para o próximo botão esperado
        
        yield return new WaitForSeconds(delayAfterClick);

        button.RevealLocalSyllable();
        PlayAudio(clickedWord.syllableAudio);

        // Verifica se todos os botões na sequência foram clicados
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
        nextClickIndex = 0; // Reseta o índice de cliques para a nova rodada
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

        // Esconde os painéis de interação
        finalWordDisplay.gameObject.SetActive(false);
        sourceButtonParent.gameObject.SetActive(false);
        answerSlotParent.gameObject.SetActive(false);
        finalImageDisplay.gameObject.SetActive(false);
        fundoImageDisplay.gameObject.SetActive(false);
        

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