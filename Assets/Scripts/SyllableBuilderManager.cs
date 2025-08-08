using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // usa isso aqui para embaralhar a ordemda lisat

public class SyllableBuilderManager : MonoBehaviour
{
    #region Data Structures for Rounds
    [System.Serializable]
    public class SyllableButtonData
    {
        public Sprite syllableImage; // A IMAGEM da silaba
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

    #region Public Variables (Inspector Settings)
    [Header("Configuração das Rodadas")]
    public List<RoundData> allRounds;

    [Header("Referências da UI")]
    public Transform syllableButtonParent;
    public GameObject syllableButtonPrefab;
    public Image finalImageDisplay;
    public Button pauseButton; 

    [Header("Controles de Tempo")]
    public float delayAfterRoundWin = 3.0f;

    private AudioManager audioManager;
    #endregion

    #region Private Variables
    private int currentRoundIndex = -1;
    private int nextSyllableIndexToClick = 0;
    private List<GameObject> currentButtons = new List<GameObject>();
    #endregion

    private void Awake()
    {

        audioManager = FindObjectOfType<AudioManager>();
    }
    
    private void Start()
    {
        finalImageDisplay.gameObject.SetActive(false);
        
        
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(() => GameManager.Instance.OpenPauseMenuLvl1());
        }
        
        StartNextRound();
    }

    private void StartNextRound()
    {
        currentRoundIndex++;

        // >>> INTEGRAÇÃO: Verifica se o jogo acabou <<<
        if (currentRoundIndex >= allRounds.Count)
        {
            EndGame();
            return;
        }

        ClearCurrentButtons();
        finalImageDisplay.gameObject.SetActive(false);
        
        nextSyllableIndexToClick = 0;
        RoundData currentRound = allRounds[currentRoundIndex];

        List<SyllableButtonData> shuffledSyllables = currentRound.syllablesInOrder.OrderBy(a => Random.value).ToList();

        foreach (var syllableData in shuffledSyllables)
        {
            GameObject buttonGO = Instantiate(syllableButtonPrefab, syllableButtonParent);
            buttonGO.GetComponent<Image>().sprite = syllableData.syllableImage;
            
            SyllableButton buttonComponent = buttonGO.AddComponent<SyllableButton>();
            buttonComponent.Setup(syllableData, this);

            currentButtons.Add(buttonGO);
        }
    }

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
            button.GetComponent<Button>().interactable = false; // desativa o botao correto

            if (nextSyllableIndexToClick >= currentRound.syllablesInOrder.Count)
            {
                StartCoroutine(RoundWinSequence());
            }
        }
        //obs: não vai aontecer nada quando clicar no errado.
    }

    private IEnumerator RoundWinSequence()
    {
        RoundData currentRound = allRounds[currentRoundIndex];

        // integracao: chamando o gamemanager para adicionar ponto
        GameManager.Instance.AddScore(50); // Adiciona 50 pontos por palavra formada
        
        // Toca o som da palavra completa
        if (audioManager != null && currentRound.finalWordAudio != null)
        {
            audioManager.PlaySFX(currentRound.finalWordAudio);
        }
        
        finalImageDisplay.sprite = currentRound.finalWordImage;
        finalImageDisplay.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(delayAfterRoundWin);

        StartNextRound();
    }

    private void ClearCurrentButtons()
    {
        foreach (GameObject button in currentButtons)
        {
            Destroy(button);
        }
        currentButtons.Clear();
    }

    private void EndGame()
    {
        Debug.Log("FIM DE JOGO! PARABÉNS!");
        //  chamando o GameManager para finalizar o jogo 
        GameManager.Instance.ShowEndPhasePanel();
    }
}