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

    [Header("Config Visual")]
    [Tooltip("Se marcado, inverte a imagem interna quando a carta vira, corrigindo o efeito de espelho causado pela rotação 180.")]
    [SerializeField] private bool fixMirroredCards = true;

    [Header("Sons extras")]
    [SerializeField] private AudioClip roundTransitionAudio;
    [SerializeField] private AudioClip roundCompleteAudio;
    [SerializeField] private AudioClip endGameAudio;
    [SerializeField] private AudioClip matchCorrectAudio;

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
    
    [Header("Game Logger")]
    [SerializeField] private CardGameLogger gameLogger;
    
    [Header("=== SISTEMA DE ÁUDIO ===")]
    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicado para música de fundo")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("AudioSource dedicado para efeitos sonoros (SFX)")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("AudioSource dedicado para sons de sílabas das cartas")]
    [SerializeField] private AudioSource syllableSource;
    
    [Header("Volumes Iniciais")]
    [Range(0f, 1f)]
    [SerializeField] private float initialMusicVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float initialSFXVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float initialSyllableVolume = 1.0f;

    private List<Sprite> spritePairs;
    private List<AudioClip> audioPairs;
    private List<Card> allCards = new List<Card>();

    private Card firstSelected;
    private Card secondSelected;

    private int matchCounts;
    private int currentRound = 0;
    private bool canSelect = true;
    private int score = 0;

    // Variáveis para logging de matches
    private int matchAttempts = 0;
    private float matchStartTime;
    private int totalCardTouches = 0;

    private void Start()
    {
        Debug.Log("=== CARDS CONTROLLER START ===");
        
        InitializeAudioSources();
        
        SetMusicVolume(initialMusicVolume);
        SetSFXVolume(initialSFXVolume);
        SetSyllableVolume(initialSyllableVolume);
        
        if (gameLogger == null)
        {
            gameLogger = GetComponent<CardGameLogger>();
            if (gameLogger == null)
            {
                gameLogger = FindObjectOfType<CardGameLogger>();
                if (gameLogger == null)
                {
                    Debug.LogError("❌ CardGameLogger não encontrado!");
                }
                else
                {
                    Debug.Log("✅ CardGameLogger encontrado via FindObjectOfType");
                }
            }
        }

        StartRound();
        UpdateAllScoreDisplays();
    }

    private void InitializeAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        
        if (syllableSource == null)
        {
            GameObject syllableObj = new GameObject("SyllableSource");
            syllableObj.transform.SetParent(transform);
            syllableSource = syllableObj.AddComponent<AudioSource>();
            syllableSource.playOnAwake = false;
        }
    }

    private void StartRound()
    {
        Debug.Log($"🔄 INICIANDO ROUND {currentRound + 1}");
        
        matchCounts = 0;
        firstSelected = null;
        secondSelected = null;
        canSelect = false;
        matchAttempts = 0;
        totalCardTouches = 0;
        allCards.Clear();

        ClearGrid();

        if (gridLayout != null)
            gridLayout.enabled = true;

        PrepareSpritesForRound();
        CreateCards();

        if (gridLayout != null)
            gridLayout.enabled = false;

        if (gameLogger != null)
        {
            int pairsThisRound = pairsPerRound[Mathf.Clamp(currentRound, 0, pairsPerRound.Length - 1)];
            gameLogger.StartRound(currentRound + 1, pairsThisRound);
        }

        StartCoroutine(PreviewCardsCoroutine());
    }

    private IEnumerator PreviewCardsCoroutine()
    {
        Debug.Log("👀 Mostrando preview das cartas...");
        
        foreach (var card in allCards)
        {
            card.Show();
        }

        yield return new WaitForSeconds(3f);

        foreach (var card in allCards)
        {
            card.Hide();
        }

        canSelect = true;
        matchStartTime = Time.time;
        
        Debug.Log($"🎮 Round {currentRound + 1} pronto para jogar!");
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
            // ATUALIZADO: Passando a configuração de fixMirroredCards
            card.Initialize(spritePairs[i], (audioPairs != null && i < audioPairs.Count) ? audioPairs[i] : null, this, fixMirroredCards);
            allCards.Add(card);
        }
    }

    public void SetSelected(Card card)
    {
        if (!canSelect || !card.isSelected) 
        {
            return;
        }
        
        if (firstSelected == card) 
        {
            return;
        }

        canSelect = false;
        totalCardTouches++;

        if (gameLogger != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, card.transform.position);
            bool isFirstCard = (firstSelected == null);
            gameLogger.LogCardTouch(totalCardTouches, screenPos, card.iconSprite.name, isFirstCard);
        }

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
                matchAttempts++;
                StartCoroutine(CheckMatching(firstSelected, secondSelected));
            }
        });
    }

    private IEnumerator CheckMatching(Card a, Card b)
    {
        float matchTime = Time.time - matchStartTime;
        
        yield return new WaitForSeconds(0.3f);

        if (a.iconSprite == b.iconSprite)
        {
            Debug.Log("✅ MATCH CORRETO!");
            
            if (matchCorrectAudio != null)
            {
                PlaySFX(matchCorrectAudio);
            }
            
            if (gameLogger != null)
            {
                gameLogger.LogCardMatch(true, a.iconSprite.name, b.iconSprite.name, matchTime, matchAttempts);
            }

            a.CorrectMatch();
            b.CorrectMatch();
            matchCounts++;

            AddScore(10);

            if (matchCounts >= spritePairs.Count / 2)
            {
                yield return StartCoroutine(HandleRoundComplete());
            }
            else
            {
                matchAttempts = 0;
                matchStartTime = Time.time;
            }
        }
        else
        {
            Debug.Log("❌ MATCH ERRADO!");
            
            if (gameLogger != null)
            {
                gameLogger.LogCardMatch(false, a.iconSprite.name, b.iconSprite.name, matchTime, matchAttempts);
            }

            yield return new WaitForSeconds(0.5f);
            
            a.Hide();
            b.Hide();
        }

        firstSelected = null;
        secondSelected = null;
        canSelect = true;
    }

    private IEnumerator HandleRoundComplete()
    {
        if (gameLogger != null)
        {
            int pairsThisRound = pairsPerRound[Mathf.Clamp(currentRound, 0, pairsPerRound.Length - 1)];
            gameLogger.LogRoundComplete(currentRound + 1, pairsThisRound, Time.time - matchStartTime);
        }

        if (roundCompleteAudio != null) 
        {
            PlaySFX(roundCompleteAudio);
        }

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
    
    // ==== FUNÇÕES DE CONTROLE DE ÁUDIO ====
    
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    public void PlaySyllable(AudioClip clip, System.Action onComplete = null)
    {
        if (syllableSource != null && clip != null)
        {
            syllableSource.clip = clip;
            syllableSource.Play();
            
            if (onComplete != null)
            {
                StartCoroutine(WaitForSyllableComplete(onComplete));
            }
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    
    private IEnumerator WaitForSyllableComplete(System.Action onComplete)
    {
        yield return new WaitWhile(() => syllableSource.isPlaying);
        onComplete?.Invoke();
    }
    
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }
    
    public void StopMusic() => musicSource?.Stop();
    public void PauseMusic() => musicSource?.Pause();
    public void UnpauseMusic() => musicSource?.UnPause();
    
    // ==== CONTROLES DE VOLUME ====
    
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = Mathf.Clamp01(volume);
    }
    
    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = Mathf.Clamp01(volume);
    }
    
    public void SetSyllableVolume(float volume)
    {
        if (syllableSource != null) syllableSource.volume = Mathf.Clamp01(volume);
    }
    
    public void SetMasterVolume(float volume) => AudioListener.volume = Mathf.Clamp01(volume);
    
    // ==== GETTERS DE VOLUME ====
    
    public float GetMusicVolume() => musicSource != null ? musicSource.volume : 0f;
    public float GetSFXVolume() => sfxSource != null ? sfxSource.volume : 0f;
    public float GetSyllableVolume() => syllableSource != null ? syllableSource.volume : 0f;
    public float GetMasterVolume() => AudioListener.volume;

    // ==== SCORE & UI ====
    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        UpdateAllScoreDisplays();
    }

    public void ShowEndPhasePanel()
    {
        if (gameLogger != null)
        {
            gameLogger.LogSessionEnd(score);
        }

        if (endPhasePanel != null) 
            endPhasePanel.SetActive(true);
        
        if (endGameAudio != null) 
            PlaySFX(endGameAudio);
        
        if (endOfLevelConfetti != null) 
            endOfLevelConfetti.Play();
        
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
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
        
        if (scorePause != null) 
            scorePause.text = "Score: " + score.ToString();
            
        PauseMusic();
        Time.timeScale = 0;
    }

    public void ClosePauseMenu()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
            
        Time.timeScale = 1f;
        UnpauseMusic();
    }
}