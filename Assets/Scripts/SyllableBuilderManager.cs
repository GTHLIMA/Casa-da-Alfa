using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SyllableBuilderManager : MonoBehaviour
{
    #region Data Structures for Rounds
    [System.Serializable]
    public class SyllableButtonData
    {
        [Tooltip("A imagem da sílaba escrita (Ex: BO.png), que será o próprio botão.")]
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
    public Button pauseButton;

    [Header("Controles de Tempo e Efeitos")]
    public float delayAfterRoundWin = 3.0f;
    public float fadeDuration = 0.5f;

    private AudioManager audioManager;
    #endregion

    #region Private Variables
    private int currentRoundIndex = -1;
    private int nextSyllableIndexToClick = 0;
    private List<SyllableButton> currentButtons = new List<SyllableButton>();
    private CanvasGroup finalImageCanvasGroup;
    #endregion

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
        
        finalImageCanvasGroup = finalImageDisplay.GetComponent<CanvasGroup>();
        if (finalImageCanvasGroup == null)
        {
            finalImageCanvasGroup = finalImageDisplay.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // CORREÇÃO: Garante que a imagem final comece completamente invisível e desativada.
        finalImageDisplay.gameObject.SetActive(false); 
        finalImageCanvasGroup.alpha = 0f;

        if (pauseButton != null && GameManager.Instance != null)
        {
            pauseButton.onClick.AddListener(() => GameManager.Instance.OpenPauseMenuLvl1());
        }
        
        StartNextRound();
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
        // Faz o fade-out dos botões existentes
        foreach (SyllableButton button in currentButtons)
        {
            if (button != null) StartCoroutine(button.Fade(false, fadeDuration));
        }
        
        // CORREÇÃO: Garante que a imagem final da rodada anterior também suma.
        StartCoroutine(FadeCanvasGroup(finalImageCanvasGroup, 0f, fadeDuration));
        
        yield return new WaitForSeconds(fadeDuration);

        // Destrói os objetos dos botões antigos
        foreach (SyllableButton button in currentButtons)
        {
            if (button != null) Destroy(button.gameObject);
        }
        currentButtons.Clear();

        // Agora, cria os novos botões
        CreateNewButtons();
    }

    private void CreateNewButtons()
{
    nextSyllableIndexToClick = 0;
    RoundData currentRound = allRounds[currentRoundIndex];
    
    foreach (var syllableData in currentRound.syllablesInOrder)
    {
        GameObject buttonGO = Instantiate(syllableButtonPrefab, syllableButtonParent);
        
        // --- LÓGICA ATUALIZADA ---

        // 1. Define a imagem do botão principal (a sílaba escrita)
        Image mainButtonImage = buttonGO.GetComponent<Image>();
        if (mainButtonImage != null)
        {
            mainButtonImage.sprite = syllableData.syllableTextImage;
        }
        else
        {
            Debug.LogError("O prefab do botão não tem um componente Image no seu objeto raiz!");
        }

        // 2. Encontra o objeto filho e define a imagem do desenho
        Image drawingImageComponent = buttonGO.transform.Find("ImagemDesenho")?.GetComponent<Image>();
        if (drawingImageComponent != null)
        {
            drawingImageComponent.sprite = syllableData.syllableDrawingImage;
        }
        else
        {
            Debug.LogError("Não foi possível encontrar o GameObject filho 'ImagemDesenho' no prefab do botão!");
        }
        
        // O resto da configuração continua igual
        SyllableButton buttonComponent = buttonGO.GetComponent<SyllableButton>();
        buttonComponent.Setup(syllableData, this);

        currentButtons.Add(buttonComponent);
    }
}

    // --- MÉTODO ALTERADO ---
    public void OnSyllableClicked(SyllableButtonData clickedSyllable, SyllableButton button)
    {
        RoundData currentRound = allRounds[currentRoundIndex];
        
        if (clickedSyllable == currentRound.syllablesInOrder[nextSyllableIndexToClick])
        {
            if (audioManager != null && clickedSyllable.syllableAudio != null)
            {
                audioManager.PlaySFX(clickedSyllable.syllableAudio);
            }

            nextSyllableIndexToClick++;

            // MUDANÇA: O botão não some, apenas fica semitransparente e desativado.
            Button uiButton = button.GetComponent<Button>();
            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            
            uiButton.interactable = false; // Desativa o clique
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.5f; // Deixa com 50% de opacidade
            }
            
            if (nextSyllableIndexToClick >= currentRound.syllablesInOrder.Count)
            {
                StartCoroutine(RoundWinSequence());
            }
        }
    }

    private IEnumerator RoundWinSequence()
{
    RoundData currentRound = allRounds[currentRoundIndex];
    
    if(GameManager.Instance != null)
        GameManager.Instance.AddScore(50);
    
    finalImageDisplay.sprite = currentRound.finalWordImage;
    finalImageDisplay.gameObject.SetActive(true);
    yield return StartCoroutine(FadeCanvasGroup(finalImageCanvasGroup, 1f, fadeDuration));
    
    if (audioManager != null && currentRound.finalWordAudio != null)
    {
        audioManager.PlaySFX(currentRound.finalWordAudio);
    }
    
    yield return new WaitForSeconds(delayAfterRoundWin);

    StartNextRound();
}
    
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        float time = 0f;
        float startAlpha = cg.alpha;
        cg.blocksRaycasts = targetAlpha > 0;

        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cg.alpha = targetAlpha;

        // Se o fade for para sumir, desativa o objeto no final para garantir.
        if (targetAlpha == 0f)
        {
            cg.gameObject.SetActive(false);
        }
    }

    private void EndGame()
    {
        Debug.Log("FIM DE JOGO! PARABÉNS!");
        if(GameManager.Instance != null)
            GameManager.Instance.ShowEndPhasePanel();
    }
}