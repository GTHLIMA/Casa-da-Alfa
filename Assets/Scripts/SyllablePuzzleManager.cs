using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SyllablePuzzleManager : MonoBehaviour
{
    #region Data Structures
    [System.Serializable]
    public class SourceWordData
    {
        public Sprite sourceDrawing;
        public Sprite firstSyllableImage;
        public AudioClip firstSyllableAudio;
    }

    [System.Serializable]
    public class PuzzleData
    {
        public string targetWord;
        public List<Sprite> targetSyllableImages;
        public Sprite finalDrawingImage;
        public Sprite finalWordImage;
        public AudioClip finalWordAudio;
        public List<SourceWordData> sourceWords;
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
    public float delayBetweenSlotReveals = 0.5f;
    public float delayAfterWin = 2.0f;
    #endregion

    #region Private State
    private List<SyllableSourceButton> activeSourceButtons = new List<SyllableSourceButton>();
    private List<Image> activeAnswerSlots = new List<Image>();
    
    private PuzzleData currentPuzzle;
    private int clicksMade = 0;
    private AudioManager audioManager;
    private int currentPuzzleIndex = 0;
    private bool isReviewing = false;
    #endregion

    private void Start()
    {
        audioManager = FindObjectOfType<AudioManager>();
        LoadPuzzle(currentPuzzleIndex);
    }

    /// <summary>
    /// Esta é a função de "REINICIALIZAÇÃO". Ela limpa tudo e carrega os novos dados.
    /// </summary>
    void LoadPuzzle(int puzzleIndex)
    {
        // 1. LIMPEZA COMPLETA DO ESTADO ANTERIOR
        ClearBoard();
        
        if (puzzleIndex >= allPuzzles.Count)
        {
            Debug.Log("FIM DE JOGO!");
            finalImageDisplay.gameObject.SetActive(false);
            return;
        }

        // 2. CARREGA AS NOVAS INFORMAÇÕES
        currentPuzzle = allPuzzles[puzzleIndex];
        
        // 3. MONTA A NOVA TELA
        finalImageDisplay.gameObject.SetActive(true);
        finalImageDisplay.sprite = questionMarkSprite;

        for (int i = 0; i < currentPuzzle.targetSyllableImages.Count; i++)
        {
            GameObject slotGO = Instantiate(answerSlotPrefab, answerSlotParent);
            activeAnswerSlots.Add(slotGO.GetComponent<Image>());
        }

        foreach (var sourceWord in currentPuzzle.sourceWords)
        {
            GameObject buttonGO = Instantiate(sourceButtonPrefab, sourceButtonParent);
            SyllableSourceButton buttonScript = buttonGO.GetComponent<SyllableSourceButton>();
            buttonScript.Setup(sourceWord, this);
            activeSourceButtons.Add(buttonScript);
        }
    }

    public void PlaySyllableAudio(AudioClip clip)
    {
        if (audioManager != null && clip != null)
        {
            audioManager.PlaySFX(clip);
        }
    }

    public void OnSourceButtonClicked(SourceWordData clickedSource, SyllableSourceButton button)
    {
        if (isReviewing) return;

        clicksMade++;

        if (clicksMade >= activeSourceButtons.Count)
        {
            isReviewing = true;
            // Passa os dados do puzzle ATUAL para a rotina de vitória
            StartCoroutine(ReviewAndWinSequence(currentPuzzle));
        }
    }


    private IEnumerator ReviewAndWinSequence(PuzzleData completedPuzzle)
    {
        yield return new WaitForSeconds(1.0f); 

        // Usa os dados do "completedPuzzle" para garantir que as sílabas são as corretas
        for (int i = 0; i < completedPuzzle.targetSyllableImages.Count; i++)
        {
            Image slotImage = activeAnswerSlots[i];
            Sprite correctSyllableSprite = completedPuzzle.targetSyllableImages[i];
            slotImage.sprite = correctSyllableSprite;
            
            var sourceWordData = completedPuzzle.sourceWords.Find(w => w.firstSyllableImage == correctSyllableSprite);
            if(sourceWordData != null) 
            {
                PlaySyllableAudio(sourceWordData.firstSyllableAudio);
            }
            
            StartCoroutine(BounceAnimation(slotImage.rectTransform));
            yield return new WaitForSeconds(delayBetweenSlotReveals);
        }

        // Animação de Vitória
        ClearAnswerSlots();
        finalWordDisplay.sprite = completedPuzzle.finalWordImage;
        finalWordDisplay.gameObject.SetActive(true);
        StartCoroutine(BounceAnimation(finalWordDisplay.rectTransform));
        
        finalImageDisplay.sprite = completedPuzzle.finalDrawingImage;
        StartCoroutine(BounceAnimation(finalImageDisplay.rectTransform));

        if (audioManager != null) audioManager.PlaySFX(completedPuzzle.finalWordAudio);
        
        yield return new WaitForSeconds(delayAfterWin);

    
        currentPuzzleIndex++;
        LoadPuzzle(currentPuzzleIndex);
    }
    
    private IEnumerator BounceAnimation(RectTransform targetRect)
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
}