// MainGameManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class SyllableDado
{
    public string expectedWord; // palavra esperada para reconhecimento de voz
    public Sprite syllableSprite; // sprite usada no balão e no arco
    public AudioClip syllableClip; // som curto da sílaba
    public Sprite drawingSprite; // imagem exibida na fase de fala
    public AudioClip hintBasicClip; // dica 1
    public AudioClip hintFinalClip; // dica 2 (final)
    public AudioClip correctClip; // som de acerto
}

public class MainGameManager : MonoBehaviour
{
    public static MainGameManager Instance;

    [Header("References")]
    public BalloonManager balloonManager;
    public ArcProgressController arcController;
    public VoiceRecognitionManager voiceManager;

    [Header("AudioSources (attach in inspector)")]
    public AudioSource musicSource; // música ambiente
    public AudioSource sfxSource; // estouro, confete, acerto/erro
    public AudioSource syllableSource; // sons de sílaba e dicas

    [Header("Syllable data")]
    public List<SyllableDado> syllables = new List<SyllableDado>();
    public int currentSyllableIndex = 0;

    [Header("UI")]
    public Transform syllableStartPosition; // posição central
    public Transform syllableArcPosition;   // posição no arco superior esquerdo

    [Header("Gameplay")]
    public int popsToComplete = 5;

    private bool inVoicePhase = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // inicia música ambiente em volume baixo
        if (musicSource != null) musicSource.Play();
        ShowCurrentSyllableAtCenter();
    }

    void ShowCurrentSyllableAtCenter()
    {
        var data = syllables[currentSyllableIndex];

        // toca o som da sílaba
        if (syllableSource != null && data.syllableClip != null)
            syllableSource.PlayOneShot(data.syllableClip);

        // após pequeno delay, move sílaba para o arco e começa o spawn
        StartCoroutine(MoveSyllableThenStartSpawn(1.0f));
    }

    IEnumerator MoveSyllableThenStartSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        // define sprite da sílaba no arco
        arcController.SetSyllable(syllables[currentSyllableIndex].syllableSprite);
        arcController.ResetArc();

        // começa a gerar os balões dessa sílaba
        balloonManager.StartSpawning(syllables[currentSyllableIndex].syllableSprite);
        balloonManager.onBalloonPopped += OnBalloonPopped;
    }

    void OnBalloonPopped()
    {
        arcController.IncrementProgress();
        if (arcController.IsComplete())
        {
            StartCoroutine(BeginVoicePhase());
        }
    }

    IEnumerator BeginVoicePhase()
    {
        inVoicePhase = true;

        // pausa e limpa os balões da tela
        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();

        // pausa música ambiente
        if (musicSource != null) musicSource.Pause();

        yield return new WaitForSeconds(0.25f);

        // inicia fase de reconhecimento de voz
        voiceManager.StartListening(syllables[currentSyllableIndex].expectedWord, OnVoiceResult);
    }

    void OnVoiceResult(bool correct)
    {
        if (correct)
        {
            // toca som de acerto (ou som padrão)
            if (sfxSource != null && syllables[currentSyllableIndex].correctClip != null)
                sfxSource.PlayOneShot(syllables[currentSyllableIndex].correctClip);

            StartCoroutine(AdvanceToNextSyllable(0.8f));
        }
        else
        {
            // se errar, deixa o voiceManager lidar com dicas e depois retoma o ciclo
            StartCoroutine(HandleFailedVoiceAttempts());
        }
    }

    IEnumerator HandleFailedVoiceAttempts()
    {
        yield return new WaitForSeconds(0.5f);

        // retoma música e spawn para tentar novamente a mesma sílaba
        if (musicSource != null) musicSource.UnPause();
        arcController.ResetArc();
        balloonManager.StartSpawning(syllables[currentSyllableIndex].syllableSprite);
        inVoicePhase = false;
    }

    IEnumerator AdvanceToNextSyllable(float delay)
    {
        yield return new WaitForSeconds(delay);

        currentSyllableIndex++;
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            yield break;
        }

        // retoma música e avança para próxima sílaba
        if (musicSource != null) musicSource.UnPause();
        ShowCurrentSyllableAtCenter();
        inVoicePhase = false;
    }

    void EndGame()
    {
        // final da fase: tocar confete, música de vitória, etc.
        if (sfxSource != null)
        {
            // toque som final se desejar
        }

        Debug.Log("🎉 Fase concluída!");
    }
}
