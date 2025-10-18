// MainGameManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class SyllableDado
{
    public string syllableText;     // texto da sílaba (ex: "BA")
    public Sprite syllableSprite;   // sprite da sílaba (imagem usada nos balões e arco)
    public AudioClip syllableClip;  // som da sílaba
    public AudioClip correctClip;   // som de acerto ("Muito bem!")
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

    balloonManager.StopSpawning();
    balloonManager.ClearAllBalloons();
    if (musicSource != null) musicSource.Pause();

    // Mostra sílaba grande no centro novamente
    ShowCurrentSyllableAtCenter();

    yield return new WaitForSeconds(1.0f);

    // Reproduz o som da sílaba antes de escutar
    var data = syllables[currentSyllableIndex];
    if (syllableSource != null && data.syllableClip != null)
        syllableSource.PlayOneShot(data.syllableClip);

    yield return new WaitForSeconds(0.5f);

    // Ativa reconhecimento de voz (comparando com o texto da sílaba)
    voiceManager.StartListening(data.syllableText, OnVoiceResult);
}

   void OnVoiceResult(bool correct)
{
    var data = syllables[currentSyllableIndex];

    if (correct)
    {
        if (sfxSource != null && data.correctClip != null)
            sfxSource.PlayOneShot(data.correctClip);

        StartCoroutine(AdvanceToNextSyllable(1.2f));
    }
    else
    {
        // repete o som da sílaba e tenta novamente
        if (syllableSource != null && data.syllableClip != null)
            syllableSource.PlayOneShot(data.syllableClip);

        StartCoroutine(RestartSameSyllable());
    }
}

IEnumerator RestartSameSyllable()
{
    yield return new WaitForSeconds(1f);
    inVoicePhase = false;
    arcController.ResetArc();
    if (musicSource != null) musicSource.UnPause();
    balloonManager.StartSpawning(syllables[currentSyllableIndex].syllableSprite);
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
