using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SyllableBuilderManager : MonoBehaviour
{
    #region Data Structures for Rounds
    [System.Serializable]
    public class SyllableButtonData
    {
        [Tooltip("A imagem da sílaba escrita (Ex: BO.png)")]
        public Sprite syllableTextImage;
        
        [Tooltip("A imagem do desenho que representa a sílaba (Ex: bola.png)")]
        public Sprite syllableDrawingImage;
        
        public AudioClip syllableAudio;
    }

    [System.Serializable]
    public class RoundData
    {
        [Header("Palavra a ser formada")]
        public string targetWord;
        public AudioClip finalWordAudio;
        public Sprite finalWordImage;
        public Sprite silabaJuntaFinal;
        
        [Header("Sílabas da Rodada (em ordem correta)")]
        public List<SyllableButtonData> syllablesInOrder;
    }
    #endregion

    #region Public Variables
    [Header("Configuração das Rodadas")]
    public List<RoundData> allRounds;

    [Header("Referências da UI")]
    public Transform syllableButtonParent;
    public GameObject syllableButtonPrefab;
    public Image finalImageDisplay;
    public Image silabaJuntaFinalDisplay;
    public Button pauseButton;

    [Header("Controles de Tempo e Efeitos")]
    public float delayAfterRoundWin = 3.0f;
    public float fadeDuration = 0.5f;
    [Tooltip("Pausa após o último acerto, antes de revelar a imagem final.")]
    public float delayBeforeFinalReveal = 1.0f;

    private AudioManager audioManager;
    #endregion

    #region Private Variables
    private int currentRoundIndex = -1;
    private int nextSyllableIndexToClick = 0;
    private List<SyllableButton> currentButtons = new List<SyllableButton>();
    private CanvasGroup finalImageCanvasGroup;
    private CanvasGroup silabaJuntaFinalCanvasGroup;
    #endregion

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
        
        finalImageCanvasGroup = finalImageDisplay.GetComponent<CanvasGroup>();
        if (finalImageCanvasGroup == null)
            finalImageCanvasGroup = finalImageDisplay.gameObject.AddComponent<CanvasGroup>();
            
        silabaJuntaFinalCanvasGroup = silabaJuntaFinalDisplay.GetComponent<CanvasGroup>();
        if (silabaJuntaFinalCanvasGroup == null)
            silabaJuntaFinalCanvasGroup = silabaJuntaFinalDisplay.gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        finalImageDisplay.gameObject.SetActive(false); 
        finalImageCanvasGroup.alpha = 0f;
        silabaJuntaFinalDisplay.gameObject.SetActive(false);
        silabaJuntaFinalCanvasGroup.alpha = 0f;

        if (pauseButton != null && GameManager.Instance != null)
        {
            pauseButton.onClick.AddListener(() => GameManager.Instance.OpenPauseMenuLvl1());
        }
        
        StartNextRound();
    }
    
    private void OnDestroy()
    {
        Screen.orientation = ScreenOrientation.Portrait;
    }

    private void StartNextRound()
    {
        currentRoundIndex++;
        if (currentRoundIndex >= allRounds.Count)
        {
            EndGame();
            return;
        }
        StartCoroutine(ClearAndCreateButtons());
    }

    private IEnumerator ClearAndCreateButtons()
    {
        foreach (SyllableButton button in currentButtons)
        {
            if (button != null) StartCoroutine(button.Fade(false, fadeDuration));
        }
        
        StartCoroutine(FadeCanvasGroup(finalImageCanvasGroup, 0f, fadeDuration));
        StartCoroutine(FadeCanvasGroup(silabaJuntaFinalCanvasGroup, 0f, fadeDuration));
        
        yield return new WaitForSeconds(fadeDuration);

        foreach (SyllableButton button in currentButtons)
        {
            if (button != null) Destroy(button.gameObject);
        }
        currentButtons.Clear();

        CreateNewButtons();
    }

    private void CreateNewButtons()
{
    nextSyllableIndexToClick = 0;
    RoundData currentRound = allRounds[currentRoundIndex];
    
    // A linha que embaralhava as sílabas foi REMOVIDA.
    // O loop 'foreach' agora usa a lista 'syllablesInOrder' diretamente,
    // garantindo que os botões sejam criados na sequência correta.
    foreach (var syllableData in currentRound.syllablesInOrder)
    {
        GameObject buttonGO = Instantiate(syllableButtonPrefab, syllableButtonParent);
        SyllableButton buttonComponent = buttonGO.GetComponent<SyllableButton>();
        
        // O Setup agora cuida de atribuir as imagens e esconder o texto
        buttonComponent.Setup(syllableData, this);
        currentButtons.Add(buttonComponent);
    }
}

    public void OnSyllableClicked(SyllableButtonData clickedSyllable, SyllableButton button)
    {
        RoundData currentRound = allRounds[currentRoundIndex];
        if (clickedSyllable == currentRound.syllablesInOrder[nextSyllableIndexToClick])
        {
            if (audioManager != null && clickedSyllable.syllableAudio != null)
                audioManager.PlaySFX(clickedSyllable.syllableAudio);
            
            nextSyllableIndexToClick++;

            // MANDA O BOTÃO REVELAR A IMAGEM DE TEXTO
            StartCoroutine(button.RevealTextImage());
            button.GetComponent<Button>().interactable = false;
            
            if (nextSyllableIndexToClick >= currentRound.syllablesInOrder.Count)
            {
                StartCoroutine(RoundWinSequence());
            }
        }
    }

    private IEnumerator RoundWinSequence()
    {
        // --- NOVA PAUSA ADICIONADA AQUI ---
        // Espera um pouco depois do último acerto, antes de mostrar qualquer coisa.
        yield return new WaitForSeconds(delayBeforeFinalReveal);

        RoundData currentRound = allRounds[currentRoundIndex];
        if(GameManager.Instance != null)
            GameManager.Instance.AddScore(50);
        
        // ETAPA 1: Prepara as duas imagens finais
        finalImageDisplay.sprite = currentRound.finalWordImage;
        silabaJuntaFinalDisplay.sprite = currentRound.silabaJuntaFinal;
        
        finalImageDisplay.gameObject.SetActive(true);
        silabaJuntaFinalDisplay.gameObject.SetActive(true);

        // ETAPA 2: Faz o fade-in de ambas ao mesmo tempo
        StartCoroutine(FadeCanvasGroup(finalImageCanvasGroup, 1f, fadeDuration));
        StartCoroutine(FadeCanvasGroup(silabaJuntaFinalCanvasGroup, 1f, fadeDuration));
        
        yield return new WaitForSeconds(fadeDuration);
        
        // ETAPA 3: Toca o som da palavra completa
        if (audioManager != null && currentRound.finalWordAudio != null)
        {
            audioManager.PlaySFX(currentRound.finalWordAudio);
        }
        
        // ETAPA 4: Espera o tempo final antes de ir para a próxima rodada.
        yield return new WaitForSeconds(delayAfterRoundWin);
        StartNextRound();
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        if (cg == null) yield break;
        float time = 0f;
        float startAlpha = cg.alpha;
        
        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cg.alpha = targetAlpha;

        if (targetAlpha == 0f)
            cg.gameObject.SetActive(false);
    }

    private void EndGame() { StartCoroutine(EndGameSequence()); }

    private IEnumerator EndGameSequence()
    {
        foreach (SyllableButton button in currentButtons)
            if (button != null) StartCoroutine(button.Fade(false, fadeDuration));
        
        StartCoroutine(FadeCanvasGroup(finalImageCanvasGroup, 0f, fadeDuration));
        StartCoroutine(FadeCanvasGroup(silabaJuntaFinalCanvasGroup, 0f, fadeDuration));

        yield return new WaitForSeconds(fadeDuration);

        if(GameManager.Instance != null)
            GameManager.Instance.ShowEndPhasePanel();
    }
}