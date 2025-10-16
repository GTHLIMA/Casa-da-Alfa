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
    
    [Header("Game Logger")]
    [SerializeField] private CardGameLogger gameLogger;
    
    [Header("--- ÁUDIO (SFX Controller) ---")]
    [Tooltip("AudioSource dedicado para tocar efeitos sonoros (SFX).")]
    [SerializeField] private AudioSource SFXSource;
    [Tooltip("Define o volume inicial para os efeitos sonoros (SFX) nesta cena.")]
    [SerializeField] private float initialSFXVolume = 1.0f; 

    private AudioSource audioSource;
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
        
        // Cria e anexa o AudioSource principal
        audioSource = gameObject.AddComponent<AudioSource>();
        
        // Aplica a configuração inicial de volume SFX
        SetSFXVolume(initialSFXVolume);
        
        // Verificar se o logger está configurado
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

        // LOG DO INÍCIO DO ROUND
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
        
        // Mostra todas as cartas
        foreach (var card in allCards)
        {
            card.Show();
        }

        yield return new WaitForSeconds(3f);

        // Esconde todas as cartas
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
            card.Initialize(spritePairs[i], (audioPairs != null && i < audioPairs.Count) ? audioPairs[i] : null, this);
            allCards.Add(card);
        }
    }

    public void SetSelected(Card card)
    {
        if (!canSelect || !card.isSelected) 
        {
            Debug.Log($"⚠️ Carta não pode ser selecionada: canSelect={canSelect}, isSelected={card.isSelected}");
            return;
        }
        
        if (firstSelected == card) 
        {
            Debug.Log("⚠️ Carta já é a primeira selecionada");
            return;
        }

        Debug.Log($"🎯 Carta selecionada: {card.iconSprite.name}");

        canSelect = false;
        totalCardTouches++;

        // LOG DO TOQUE NA CARTA
        if (gameLogger != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, card.transform.position);
            bool isFirstCard = (firstSelected == null);
            gameLogger.LogCardTouch(totalCardTouches, screenPos, card.iconSprite.name, isFirstCard);
        }

        // Mostra a carta
        card.Show();

        // Toca o áudio e depois processa a seleção
        card.PlayAudio(() =>
        {
            if (firstSelected == null)
            {
                Debug.Log("📌 Primeira carta selecionada");
                firstSelected = card;
                canSelect = true;
            }
            else if (secondSelected == null)
            {
                Debug.Log("📌 Segunda carta selecionada - verificando match...");
                secondSelected = card;
                matchAttempts++;
                StartCoroutine(CheckMatching(firstSelected, secondSelected));
            }
        });
    }

    private IEnumerator CheckMatching(Card a, Card b)
    {
        float matchTime = Time.time - matchStartTime;
        
        Debug.Log($"🔍 Verificando match: {a.iconSprite.name} vs {b.iconSprite.name}");

        yield return new WaitForSeconds(0.3f);

        if (a.iconSprite == b.iconSprite)
        {
            Debug.Log("✅ MATCH CORRETO!");
            
            // LOG DO MATCH CORRETO
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
                Debug.Log($"🎯 Todos os {matchCounts} pares encontrados! Round completo.");
                yield return StartCoroutine(HandleRoundComplete());
            }
            else
            {
                matchAttempts = 0; // Reset attempts for next match
                matchStartTime = Time.time; // Reset match timer
                Debug.Log($"📊 Progresso: {matchCounts}/{spritePairs.Count / 2} pares");
            }
        }
        else
        {
            Debug.Log("❌ MATCH ERRADO!");
            
            // LOG DO MATCH ERRADO
            if (gameLogger != null)
            {
                gameLogger.LogCardMatch(false, a.iconSprite.name, b.iconSprite.name, matchTime, matchAttempts);
            }

            yield return new WaitForSeconds(0.5f); // Pequena pausa antes de esconder
            
            a.Hide();
            b.Hide();
        }

        firstSelected = null;
        secondSelected = null;
        canSelect = true;
        
        Debug.Log("🔄 Pronto para próxima seleção");
    }

    private IEnumerator HandleRoundComplete()
    {
        Debug.Log($"🎯 ROUND {currentRound + 1} COMPLETO!");

        // LOG DA CONCLUSÃO DO ROUND
        if (gameLogger != null)
        {
            int pairsThisRound = pairsPerRound[Mathf.Clamp(currentRound, 0, pairsPerRound.Length - 1)];
            gameLogger.LogRoundComplete(currentRound + 1, pairsThisRound, Time.time - matchStartTime);
        }

        if (roundCompleteAudio != null) 
        {
            audioSource.PlayOneShot(roundCompleteAudio);
            Debug.Log("🔊 Tocando áudio de round completo");
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
            Debug.Log($"➡️ Avançando para round {currentRound + 1}");
            StartRound();
        }
        else
        {
            Debug.Log("🏁 Todos os rounds completos! Fim de jogo.");
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
        
        Debug.Log($"🔀 {spritesList.Count} cartas embaralhadas");
    }
    
    // ==== FUNÇÕES DE ÁUDIO ====
    
    public void PlaySFX(AudioClip clip)
    {
        if (SFXSource != null && clip != null)
        {
            SFXSource.PlayOneShot(clip);
            Debug.Log($"🔊 Tocando SFX: {clip.name}");
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        if (SFXSource != null)
        {
            SFXSource.volume = Mathf.Clamp01(volume);
            Debug.Log($"🔊 Volume SFX ajustado para: {volume}");
        }
    }

    public void SetBackgroundVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
            Debug.Log($"Volume background ajustado para: {volume}");
        }
    }

    // ==== SCORE & UI ====
    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        UpdateAllScoreDisplays();
        Debug.Log($"Score atualizado: +{amount} = {score}");
    }

    public void ShowEndPhasePanel()
    {
        Debug.Log("ShowEndPhasePanel] - FIM DE JOGO! ===");
        
        // LOG DO FIM DA SESSÃO
        if (gameLogger != null)
        {
            gameLogger.LogSessionEnd(score);
        }

        if (endPhasePanel != null) 
        {
            endPhasePanel.SetActive(true);
            Debug.Log("Painel de fim de fase ativado");
        }
        
        if (endGameAudio != null) 
        {
            audioSource.PlayOneShot(endGameAudio);
            Debug.Log("Tocando áudio de fim de jogo");
        }
        
        if (endOfLevelConfetti != null) 
        {
            endOfLevelConfetti.Play();
            Debug.Log("Confetti ativado!");
        }
        
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
        {
            pauseMenu.SetActive(true);
            Debug.Log("Menu de pausa aberto");
        }
        
        if (scorePause != null) 
            scorePause.text = "Score: " + score.ToString();
            
        if (audioSource != null)
            audioSource.Pause();
            
        Time.timeScale = 0;
    }

    public void ClosePauseMenu()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            Debug.Log(" Menu de pausa fechado");
        }
            
        Time.timeScale = 1f;
        
        if (audioSource != null)
            audioSource.UnPause();
    }
}