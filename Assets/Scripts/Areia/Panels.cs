using UnityEngine;
using System.Collections;
public class Panels : MonoBehaviour
{

    public GameObject PauseMenu;
    public GameObject endPhasePanel;
    public GameObject Strokes;
    public ParticleSystem confettiEffect;

    public static Panels Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }






public void OpenPauseMenuLvl1()
{
    PauseMenu.SetActive(true);


    Time.timeScale = 0f;
    Debug.Log("Jogo pausado: traço oculto e tempo parado.");
}

public void ClosePauseMenuLvl1()
{
    Time.timeScale = 1f;



    if (PauseMenu != null)
        PauseMenu.SetActive(false);

    Debug.Log("Jogo retomado: traço visível.");
}
    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    public IEnumerator ShowEndPhasePanelCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        Strokes.SetActive(false);


        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        if (confettiEffect != null)
        {
            confettiEffect.Play();
            Debug.Log("Efeito de confete ativado!");
        }
    }
}