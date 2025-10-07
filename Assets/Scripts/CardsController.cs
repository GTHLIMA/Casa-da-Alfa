using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class CardsController : MonoBehaviour
{
    [Header("Prefab / Grid")]
    [SerializeField] Card cardPrefab;
    [SerializeField] Transform gridTransform;
    [SerializeField] GridLayoutGroup gridLayout;

    [Header("Assets (sprite[i] <-> cardAudios[i])")]
    [SerializeField] Sprite[] sprites;
    [SerializeField] AudioClip[] cardAudios;

    [Header("Config de rounds (pares por round)")]
    [SerializeField] int[] pairsPerRound = { 3, 4, 5, 5 };

    [Header("Sons extras")]
    [SerializeField] private AudioClip roundTransitionAudio;
    [SerializeField] private AudioClip roundCompleteAudio;
    [SerializeField] private AudioClip endGameAudio;

    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject endPhasePanel;

    [Header("UI Texts")]
    [SerializeField] private Text scoreHUD;
    [SerializeField] private Text scorePause;
    [SerializeField] private Text scoreEndPhase;

    [Header("Confetti FX")]
    [SerializeField] private ParticleSystem endOfLevelConfetti;

    [Header("Imagem de transição")]
    [SerializeField] private Image roundOverlayImage;
    
    // --- NOVO HEADER PARA ÁUDIO SFX ---
    [Header("--- ÁUDIO (SFX Controller) ---")]
    [Tooltip("AudioSource dedicado para tocar efeitos sonoros (SFX).")]
    [SerializeField] private AudioSource SFXSource;
    [Tooltip("Define o volume inicial para os efeitos sonoros (SFX) nesta cena.")]
    [SerializeField] private float initialSFXVolume = 1.0f; 
    // ---------------------------------

    private AudioSource audioSource; // Áudio principal (eventos e música de fundo se for o caso)
    private List<Sprite> spritePairs;
    private List<AudioClip> audioPairs;

    private Card firstSelected;
    private Card secondSelected;

    private int matchCounts;
    private int currentRound = 0;
    private bool canSelect = true;

    private int score = 0;

    private void Start()
    {
        // Cria e anexa o AudioSource principal (para eventos)
        audioSource = gameObject.AddComponent<AudioSource>();
        
        // Aplica a configuração inicial de volume SFX
        SetSFXVolume(initialSFXVolume);
        
        StartRound();
        UpdateAllScoreDisplays();
    }

    // ... (Métodos StartRound, PreviewCardsCoroutine, ClearGrid, PrepareSpritesForRound, CreateCards, ShufflePairs permanecem inalterados)
    
    private void StartRound()
    {
        matchCounts = 0;
        firstSelected = null;
        secondSelected = null;
        canSelect = false;

        ClearGrid();

        if (gridLayout != null)
            gridLayout.enabled = true;

        PrepareSpritesForRound();
        CreateCards();

        if (gridLayout != null)
            gridLayout.enabled = false;

        StartCoroutine(PreviewCardsCoroutine());
    }

    private IEnumerator PreviewCardsCoroutine()
    {
        foreach (Transform child in gridTransform)
        {
            Card c = child.GetComponent<Card>();
            c.Show();
        }

        yield return new WaitForSeconds(3f);

        foreach (Transform child in gridTransform)
        {
            Card c = child.GetComponent<Card>();
            c.Hide();
        }

        canSelect = true;
    }

    private void ClearGrid()
    {
        foreach (Transform child in gridTransform)
        {
            Destroy(child.gameObject);
        }
    }

    private void PrepareSpritesForRound()
    {
        spritePairs = new List<Sprite>();
        audioPairs = new List<AudioClip>();

        int pairsThisRound = pairsPerRound[Mathf.Clamp(currentRound, 0, pairsPerRound.Length - 1)];
        int startIndex = 0;
        for (int r = 0; r < currentRound; r++) startIndex += pairsPerRound[r];

        for (int i = 0; i < pairsThisRound; i++)
        {
            int idx = startIndex + i;
            if (idx >= sprites.Length)
            {
                Debug.LogWarning($"CardsController: sprite faltando para round {currentRound + 1} no índice {idx}.");
                break;
            }

            spritePairs.Add(sprites[idx]);
            spritePairs.Add(sprites[idx]);

            AudioClip a = (cardAudios != null && idx < cardAudios.Length) ? cardAudios[idx] : null;
            audioPairs.Add(a);
            audioPairs.Add(a);
        }

        ShufflePairs(spritePairs, audioPairs);
    }

    private void CreateCards()
    {
        for (int i = 0; i < spritePairs.Count; i++)
        {
            Card card = Instantiate(cardPrefab, gridTransform);
            card.Initialize(spritePairs[i], (audioPairs != null && i < audioPairs.Count) ? audioPairs[i] : null, this);
        }
    }

    public void SetSelected(Card card)
    {
        if (!canSelect || !card.isSelected) return;
        if (firstSelected == card) return;

        canSelect = false;
        card.Show();

        card.PlayAudio(() =>
        {
            if (firstSelected == null)
            {
                firstSelected = card;
                canSelect = true;
            }
            else if (secondSelected == null)
            {
                secondSelected = card;
                StartCoroutine(CheckMatching(firstSelected, secondSelected));
            }
        });
    }

    private IEnumerator CheckMatching(Card a, Card b)
    {
        yield return new WaitForSeconds(0.3f);

        if (a.iconSprite == b.iconSprite)
        {
            a.CorrectMatch();
            b.CorrectMatch();
            matchCounts++;

            AddScore(10);

            if (matchCounts >= spritePairs.Count / 2)
            {
                yield return StartCoroutine(HandleRoundComplete());
            }
        }
        else
        {
            a.Hide();
            b.Hide();
        }

        firstSelected = null;
        secondSelected = null;
        canSelect = true;
    }

    private IEnumerator HandleRoundComplete()
    {
        if (roundCompleteAudio != null) audioSource.PlayOneShot(roundCompleteAudio);

        if (roundOverlayImage != null)
        {
            roundOverlayImage.gameObject.SetActive(true);
            roundOverlayImage.canvasRenderer.SetAlpha(0f);
            roundOverlayImage.CrossFadeAlpha(1f, 0.5f, false);

            yield return new WaitForSeconds(2f);

            roundOverlayImage.CrossFadeAlpha(0f, 0.5f, false);
            yield return new WaitForSeconds(0.5f);
            roundOverlayImage.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        currentRound++;
        if (currentRound < pairsPerRound.Length)
        {
            StartRound();
        }
        else
        {
            ShowEndPhasePanel();
        }
    }

    private void ShufflePairs(List<Sprite> spritesList, List<AudioClip> audiosList)
    {
        for (int i = spritesList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            Sprite temp = spritesList[i];
            spritesList[i] = spritesList[randomIndex];
            spritesList[randomIndex] = temp;

            if (audiosList != null && audiosList.Count == spritesList.Count)
            {
                AudioClip ta = audiosList[i];
                audiosList[i] = audiosList[randomIndex];
                audiosList[randomIndex] = ta;
            }
        }
    }
    
    // ==== FUNÇÕES DE ÁUDIO MIGRARADAS ====
    
    // Função para tocar SFX (usa o SFXSource dedicado)
    public void PlaySFX(AudioClip clip)
    {
        if (SFXSource != null && clip != null)
        {
            SFXSource.PlayOneShot(clip);
        }
    }
    
    // Função para definir o volume do SFX
    public void SetSFXVolume(float volume)
    {
        if (SFXSource != null)
        {
            SFXSource.volume = Mathf.Clamp01(volume);
        }
    }

    // Função para definir o volume do áudio principal (background/eventos)
    public void SetBackgroundVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    // ==== SCORE & UI ====
    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        UpdateAllScoreDisplays();
    }

    public void ShowEndPhasePanel()
    {
        Debug.Log("== [ShowEndPhasePanel] - FIM DE JOGO! ==");
        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        if (endGameAudio != null) audioSource.PlayOneShot(endGameAudio);
        if (endOfLevelConfetti != null) endOfLevelConfetti.Play();
        UpdateAllScoreDisplays();
    }
    
    private void UpdateAllScoreDisplays()
    {
        string formattedScore = score.ToString("000");
        if (scoreHUD != null) scoreHUD.text = formattedScore;
        if (scorePause != null) scorePause.text = "Score: " + formattedScore;
        if (scoreEndPhase != null) scoreEndPhase.text = formattedScore;
    }

    public void OpenPauseMenu()
    {
        if (scorePause != null) scorePause.text = "Score: " + score.ToString();
        if (pauseMenu != null) pauseMenu.SetActive(true);
        audioSource.Pause();
        Time.timeScale = 0;
    }

    public void ClosePauseMenu()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        audioSource.UnPause();
    }
}