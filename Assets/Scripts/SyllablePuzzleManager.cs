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
    
    [Header("--- CONTROLE DE CENA E ÁUDIO ---")]
    [Tooltip("Painel da UI de Pausa.")]
    public GameObject PauseMenu;
    [Tooltip("Painel da UI de Fim de Fase.")]
    [SerializeField] private GameObject endPhasePanel;

    [Tooltip("Efeito de Confete a ser ativado no final.")]
    [SerializeField] private ParticleSystem confettiEffect;
    
    [Tooltip("AudioSource principal para música/voz de fundo.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("AudioSource dedicado para tocar efeitos sonoros (SFX).")]
    [SerializeField] private AudioSource SFXSource;
    [Tooltip("Clip de áudio para a música de fundo/ambiente.")]
    [SerializeField] private AudioClip backgroundMusicClip; 
    [Tooltip("Clip de áudio para o som de Fim de Fase (end2 no AudioManager antigo).")]
    [SerializeField] private AudioClip endPhaseSFXClip; 
    
    [Tooltip("Define o volume inicial para os efeitos sonoros (SFX) nesta cena.")]
    [SerializeField] private float initialSFXVolume = 1.0f; 

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
    private int currentPuzzleIndex = 0;
    private bool isReviewing = false;

    private ButtonFloatEffect currentFloatEffect;

    private float savedTime; 
    
    private SyllableGameLogger gameLogger; //Firebase
    #endregion

    private void Awake()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (SFXSource == null) SFXSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        gameLogger = FindObjectOfType<SyllableGameLogger>();//firebase

        SetSFXVolume(initialSFXVolume);
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
        
        foreach (var sourceWord in currentPuzzle.onScreenWords)
        {
            GameObject buttonGO = Instantiate(sourceButtonPrefab, sourceButtonParent);
            SyllableSourceButton buttonScript = buttonGO.GetComponent<SyllableSourceButton>();
            buttonScript.Setup(sourceWord, this);
            activeSourceButtons.Add(buttonScript);
        }

        HighlightNextButton();
    }

    public void PauseAudio(AudioClip clip)
    {
        if (audioSource != null && audioSource.clip == clip)
        {
            savedTime = audioSource.time;
            audioSource.Stop();
        }
    }

    // Retoma o áudio a partir do tempo salvo
    public void ResumeAudio(AudioClip clip)
    {
        if (audioSource != null && audioSource.clip == clip)
        {
            audioSource.time = savedTime;
            audioSource.Play();
        }
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (SFXSource != null && clip != null)
        {
            SFXSource.PlayOneShot(clip);
        }
    }
    
    public void PlayAudio(AudioClip clip)
    {
        PlaySFX(clip); 
    }
    
    // Função para definir o volume do SFX
    public void SetSFXVolume(float volume)
    {
        if (SFXSource != null)
        {
            SFXSource.volume = Mathf.Clamp01(volume);
        }
    }

    
    public void OpenPauseMenuLvl1()
    {
    

        // Ativa o painel de pausa
        if (PauseMenu != null)
        {
            PauseMenu.SetActive(true);

            CanvasGroup cg = PauseMenu.GetComponent<CanvasGroup>();
            if (cg == null) cg = PauseMenu.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

       
        if (audioSource != null && backgroundMusicClip != null)
            PauseAudio(backgroundMusicClip);

      
        Time.timeScale = 0f;

        AudioListener.pause = true;


        Debug.Log("Jogo pausado: tempo parado e painel ativo.");
    }

    public void ClosePauseMenuLvl1()
    {
        // Retoma o tempo do jogo
        Time.timeScale = 1f;

        AudioListener.pause = false;
        if (audioSource != null && backgroundMusicClip != null)
            ResumeAudio(backgroundMusicClip);

      
        if (PauseMenu != null)
            PauseMenu.SetActive(false);

        Debug.Log("Jogo retomado.");
    }
    

    
    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    private IEnumerator ShowEndPhasePanelCoroutine()
    {
        yield return new WaitForSeconds(0.5f);


        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        
        if (confettiEffect != null)
    {
        confettiEffect.Play();
        Debug.Log("Efeito de confete ativado!");
    }

        Debug.Log("Lógica de spawner de balões e confete foi removida.");

        if (audioSource != null && SFXSource != null)
        {

            PauseAudio(backgroundMusicClip);
            
            PlaySFX(endPhaseSFXClip);
        }

    }


    public void OnSourceButtonClicked(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        if (isReviewing) return;

        // CORREÇÃO: Verifica se é o próximo botão na sequência, não a sílaba
        if (button != activeSourceButtons[nextClickIndex])
        {
            // LOG DE ERRO FIREBASE
            if (gameLogger != null)
            {
                string expectedName = activeSourceButtons[nextClickIndex].GetWordName();
                string receivedName = clickedWord.syllableImage != null ? clickedWord.syllableImage.name : "unknown";
                gameLogger.LogWrongClick(expectedName, receivedName, currentPuzzleIndex, nextClickIndex);
            }

            Debug.Log($"Clique fora de ordem! Esperava o botão na posição {nextClickIndex}");
            return;
        }

        // LOG DE ACERTO FIREBASE
        if (gameLogger != null)
        {
            string wordName = clickedWord.syllableImage != null ? clickedWord.syllableImage.name : "unknown";
            gameLogger.LogCorrectClick(wordName, currentPuzzleIndex, nextClickIndex);
        }

        StartCoroutine(SourceButtonClickSequence(clickedWord, button));
    }
    private IEnumerator SourceButtonClickSequence(OnScreenWord clickedWord, SyllableSourceButton button)
    {
        button.SetUsed(true);

        StopHighlightOn(button);

        nextClickIndex++; 
        
        yield return new WaitForSeconds(delayAfterClick);

        button.RevealLocalSyllable();
        PlayAudio(clickedWord.syllableAudio);

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

        if (gameLogger != null) // firebase log de puzzle completo
        {
            gameLogger.LogPuzzleCompleted(currentPuzzle.targetWord, currentPuzzleIndex, true);
        }

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
        Debug.Log("FIM DE JOGO! Acionando painel de fim de fase local.");
        
        // Oculta todos os elementos do quebra-cabeça
        finalWordDisplay.gameObject.SetActive(false);
        sourceButtonParent.gameObject.SetActive(false);
        answerSlotParent.gameObject.SetActive(false);
        finalImageDisplay.gameObject.SetActive(false);
        fundoImageDisplay.gameObject.SetActive(false);
        
        // Chama a função local
        ShowEndPhasePanel(); 
    }

    private void HighlightNextButton()
    {
    if (nextClickIndex < activeSourceButtons.Count)
        {
        // var btnImage = button.GetSyllableImage(); //firebase
        var button = activeSourceButtons[nextClickIndex];
        if (button.GetComponent<ButtonFloatEffect>() == null)
        {
            currentFloatEffect = button.gameObject.AddComponent<ButtonFloatEffect>();
            currentFloatEffect.floatSpeed = 13f;
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