using UnityEngine;

public class GameManager5 : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource syllableSource;   // Para os sons das sílabas
    public AudioSource optionSource;     // Para os sons das figuras/opções
    public AudioSource sfxSource;        // Para efeitos sonoros (acerto/erro)

    [Header("Audio Clips de Feedback")]
    public AudioClip correctSfx;         
    public AudioClip wrongSfx;           

    /// Toca o áudio da sílaba ("CA de...")
    public void PlaySyllable(AudioClip clip)
    {
        if (syllableSource.isPlaying) syllableSource.Stop();
        syllableSource.clip = clip;
        syllableSource.Play();
    }

    /// Toca o áudio da opção clicada ("Casa", "Bola")
    public void PlayOption(AudioClip clip)
    {
        if (optionSource.isPlaying) optionSource.Stop();
        optionSource.clip = clip;
        optionSource.Play();
    }

    /// Toca o som de acerto
    public void PlayCorrect()
    {
        if (correctSfx != null)
            sfxSource.PlayOneShot(correctSfx);
    }

    /// Toca o som de erro
    public void PlayWrong()
    {
        if (wrongSfx != null)
            sfxSource.PlayOneShot(wrongSfx);
    }

    /// Verifica se o áudio de sílaba terminou
    public bool IsSyllablePlaying()
    {
        return syllableSource.isPlaying;
    }

    /// Verifica se o áudio de opção terminou
    public bool IsOptionPlaying()
    {
        return optionSource.isPlaying;
    }
}
