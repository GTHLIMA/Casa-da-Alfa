// SUBSTITUA O MainGameManager.cs COMPLETO POR ESTE:

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SyllableDado
{
    public string syllableText;
    public Sprite syllableSprite;
    public AudioClip syllableClip;
    public AudioClip correctClip;
}

public class MainGameManager : MonoBehaviour
{
    public static MainGameManager Instance;

    [Header("References")]
    public BalloonManager balloonManager;
    public ArcProgressController arcController;
    public VoiceRecognitionManager voiceManager;

    [Header("AudioSources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource syllableSource;

    [Header("Syllable data")]
    public List<SyllableDado> syllables = new List<SyllableDado>();
    public int currentSyllableIndex = 0;

    [Header("Syllable Intro UI")]
    public CanvasGroup introPanelGroup;
    public Image syllableIntroImage;

    [Header("UI Positions")]
    public Transform syllableStartPosition;
    public Transform syllableArcPosition;
    public Canvas mainCanvas;

    [Header("Gameplay")]
    public int popsToComplete = 5;

    private bool inVoicePhase = false;
    private bool spawningActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

#if UNITY_ANDROID
        if (!SpeechToText.IsInitialized())
            SpeechToText.Initialize("pt-BR");
#endif
    }

    private void Start()
    {
        if (musicSource != null) musicSource.Play();
        
        // Validar referências críticas
        if (!ValidateReferences())
        {
            Debug.LogError("[MainGameManager] Referências críticas não atribuídas! Verifique o Inspector.");
            enabled = false;
            return;
        }

        ShowCurrentSyllableAtCenter();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (balloonManager == null)
        {
            Debug.LogError("[MainGameManager] BalloonManager não atribuído!");
            valid = false;
        }

        if (arcController == null)
        {
            Debug.LogError("[MainGameManager] ArcProgressController não atribuído!");
            valid = false;
        }

        if (syllables == null || syllables.Count == 0)
        {
            Debug.LogError("[MainGameManager] Lista de sílabas vazia!");
            valid = false;
        }

        if (introPanelGroup == null)
        {
            Debug.LogError("[MainGameManager] introPanelGroup não atribuído!");
            valid = false;
        }

        if (syllableIntroImage == null)
        {
            Debug.LogError("[MainGameManager] syllableIntroImage não atribuído!");
            valid = false;
        }

        // Avisos (não bloqueiam)
        if (mainCanvas == null)
            Debug.LogWarning("[MainGameManager] mainCanvas não atribuído - animação de movimento desabilitada");
        
        if (syllableStartPosition == null)
            Debug.LogWarning("[MainGameManager] syllableStartPosition não atribuído");
        
        if (syllableArcPosition == null)
            Debug.LogWarning("[MainGameManager] syllableArcPosition não atribuído");

        return valid;
    }

    void ShowCurrentSyllableAtCenter()
    {
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            return;
        }

        var data = syllables[currentSyllableIndex];

        if (syllableIntroImage != null)
            syllableIntroImage.sprite = data.syllableSprite;

        StartCoroutine(ShowIntroSequence(data));
    }

    IEnumerator ShowIntroSequence(SyllableDado data)
    {
        introPanelGroup.alpha = 1f;
        introPanelGroup.gameObject.SetActive(true);

        if (syllableSource && data.syllableClip)
            syllableSource.PlayOneShot(data.syllableClip);

        yield return new WaitForSeconds(1.2f);

        // Fade out
        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);

        // Animar para o arco (se referências existirem)
        if (mainCanvas != null && syllableStartPosition != null && syllableArcPosition != null)
        {
            yield return StartCoroutine(MoveSyllableToArc(data.syllableSprite));
        }
        else
        {
            Debug.LogWarning("[MainGameManager] Pulando animação de movimento - referências faltando");
        }

        // Setup do arco
        arcController.SetSyllable(data.syllableSprite);
        arcController.ResetArc();

        // Limpar listener antigo e adicionar novo
        balloonManager.onBalloonPopped -= OnBalloonPopped;
        balloonManager.onBalloonPopped += OnBalloonPopped;

        // Iniciar spawn
        balloonManager.StartSpawning(data.syllableSprite);
        spawningActive = true;
    }

    IEnumerator MoveSyllableToArc(Sprite sprite)
    {
        // Validação já feita antes de chamar
        GameObject temp = new GameObject("MovingSyllable");
        Image img = temp.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        RectTransform rt = temp.GetComponent<RectTransform>();
        rt.SetParent(mainCanvas.transform, false);
        rt.position = syllableStartPosition.position;

        float duration = 0.8f;
        Vector3 start = syllableStartPosition.position;
        Vector3 end = syllableArcPosition.position;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (temp == null) yield break; // Safety check
            rt.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }

        if (temp != null)
            Destroy(temp);
    }

    void OnBalloonPopped()
    {
        arcController.IncrementProgress();

        if (arcController.IsComplete() && !inVoicePhase)
        {
            spawningActive = false;
            StartCoroutine(BeginVoicePhase());
        }
    }

    IEnumerator BeginVoicePhase()
    {
        inVoicePhase = true;

        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();

        if (musicSource) musicSource.Pause();

        yield return new WaitForSeconds(0.5f);

        // Mostrar sílaba e DEIXAR NA TELA até acertar
        var data = syllables[currentSyllableIndex];
        
        introPanelGroup.alpha = 1f;
        introPanelGroup.gameObject.SetActive(true);
        if (syllableIntroImage != null)
            syllableIntroImage.sprite = data.syllableSprite;

        if (syllableSource && data.syllableClip)
            syllableSource.PlayOneShot(data.syllableClip);

        yield return new WaitForSeconds(1.5f);

        // NÃO fazer fade out - sílaba fica visível!
        // Removi o código de fade aqui

        // Iniciar reconhecimento (sílaba continua visível)
        if (voiceManager != null)
        {
            Debug.Log($"[MainGameManager] Iniciando reconhecimento para: {data.syllableText}");
            voiceManager.StartListening(data.syllableText, OnVoiceResult);
        }
        else
        {
            Debug.LogError("[MainGameManager] VoiceManager não atribuído!");
            OnVoiceResult(false);
        }
    }

    void OnVoiceResult(bool correct)
    {
        if (!inVoicePhase) return;

        if (correct)
        {
            Debug.Log("[MainGameManager] ✓ Resposta correta!");
            
            // Fade out da sílaba AGORA que acertou
            StartCoroutine(FadeOutSyllableAndAdvance());
        }
        else
        {
            Debug.Log("[MainGameManager] ✗ Resposta incorreta - sílaba continua na tela");
            // Sílaba continua visível, apenas toca o áudio novamente como hint
            var data = syllables[currentSyllableIndex];
            if (syllableSource && data.syllableClip)
                syllableSource.PlayOneShot(data.syllableClip);
        }
    }

    IEnumerator FadeOutSyllableAndAdvance()
    {
        inVoicePhase = false;

        // Tocar som de acerto
        if (sfxSource && syllables[currentSyllableIndex].correctClip)
            sfxSource.PlayOneShot(syllables[currentSyllableIndex].correctClip);

        yield return new WaitForSeconds(0.5f);

        // Fade out da sílaba
        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.6f);

        // Avançar para próxima sílaba
        balloonManager.onBalloonPopped -= OnBalloonPopped;
        
        currentSyllableIndex++;
        
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            yield break;
        }

        if (musicSource != null) musicSource.UnPause();
        
        ShowCurrentSyllableAtCenter();
    }

    IEnumerator RestartSameSyllable()
    {
        yield return new WaitForSeconds(1f);
        
        arcController.ResetArc();
        
        if (musicSource != null) musicSource.UnPause();
        
        // Fade out da sílaba
        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);
        
        ShowCurrentSyllableAtCenter();
    }

    IEnumerator AdvanceToNextSyllable(float delay)
    {
        balloonManager.onBalloonPopped -= OnBalloonPopped;
        
        yield return new WaitForSeconds(delay);

        currentSyllableIndex++;
        
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            yield break;
        }

        if (musicSource != null) musicSource.UnPause();
        
        ShowCurrentSyllableAtCenter();
    }

    void EndGame()
    {
        Debug.Log("🎉 Jogo concluído!");
        
        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();
        
        if (sfxSource != null)
        {
            // Tocar som de vitória aqui
        }
        
        // Você pode adicionar UI de vitória aqui
    }

    // ========== DEBUG HOTKEYS (Editor apenas) ==========
    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[DEBUG] Simulando voz CORRETA");
            if (inVoicePhase && voiceManager != null)
            {
                voiceManager.StopListening();
                OnVoiceResult(true);
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("[DEBUG] Simulando voz INCORRETA");
            if (inVoicePhase && voiceManager != null)
            {
                voiceManager.StopListening();
                OnVoiceResult(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("[DEBUG] Toggle Spawn");
            if (spawningActive)
            {
                balloonManager.StopSpawning();
                spawningActive = false;
            }
            else
            {
                if (syllables.Count > 0)
                {
                    balloonManager.StartSpawning(syllables[currentSyllableIndex].syllableSprite);
                    spawningActive = true;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("[DEBUG] Forçar próxima sílaba");
            if (!inVoicePhase)
            {
                balloonManager.onBalloonPopped -= OnBalloonPopped;
                balloonManager.StopSpawning();
                currentSyllableIndex++;
                if (currentSyllableIndex < syllables.Count)
                {
                    ShowCurrentSyllableAtCenter();
                }
                else
                {
                    EndGame();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[DEBUG] Resetar jogo");
            balloonManager.StopSpawning();
            balloonManager.ClearAllBalloons();
            StopAllCoroutines();
            currentSyllableIndex = 0;
            inVoicePhase = false;
            spawningActive = false;
            if (introPanelGroup != null)
                introPanelGroup.gameObject.SetActive(false);
            ShowCurrentSyllableAtCenter();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[DEBUG] Completar arco instantaneamente");
            for (int i = 0; i < 5; i++)
            {
                arcController.IncrementProgress();
            }
        }
#endif
    }
}