using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class DragManager : MonoBehaviour
{
    [Header("Balão do Jogador")]
    public GameObject fusionPrefab;
    public Transform spawnPoint;

    public Sprite[] prefabSprites;



    [Header("Target Variants")]
    public List<GameObject> targetVariants;



    [Header("Sprites da Fusão")]
    public Sprite[] mergedSprites;



    [Header("Áudios de Fusão")]
    public AudioClip[] fusionSounds;
    



    [Header("Pause Menu && Painel de Fim de Fase")]
    public GameObject PauseMenu;
    public GameObject endPhasePanel;
    public ParticleSystem confettiEffect;
    public AudioClip end2;
    // public Text scoreEndPhase;

    [Header("Efeitos Visuais")]
    public GameObject smokePrefab;


    private AudioSource audioSource;
    private int currentIndex = 0;
    private GameObject currentBalloon;
    private List<GameObject> spawnedTargets = new List<GameObject>();

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;

        SpawnTargetsAleatorios();
        SpawnPlayerBalloon();
    }

    [Header("Altura dos Targets")]
    [Range(0f, 1f)]
    public float targetYViewport = 0.15f;

    void SpawnTargetsAleatorios()
    {
        var shuffled = targetVariants.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < 4; i++)
        {
            float xViewport = 0.12f + i * 0.260f;
            Vector3 viewportPosition = new Vector3(xViewport, targetYViewport, Camera.main.nearClipPlane + 4f);
            Vector3 worldPos = Camera.main.ViewportToWorldPoint(viewportPosition);
            worldPos.z = 0f;

            GameObject instance = Instantiate(shuffled[i], worldPos, Quaternion.identity);
            spawnedTargets.Add(instance);
        }
    }


    public void SpawnPlayerBalloon()
    {
        if (currentIndex >= prefabSprites.Length)
        {
            Debug.Log("Deploy de todos os prefabs!");
            return;
        }

        currentBalloon = Instantiate(fusionPrefab, spawnPoint.position, Quaternion.identity);

        var fusionScript = currentBalloon.GetComponent<ImageFusion>();
        fusionScript.manager = this;

        var spriteRenderer = currentBalloon.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = prefabSprites[currentIndex];

        foreach (var target in spawnedTargets)
        {
            var targetSpriteRenderer = target.GetComponent<SpriteRenderer>();
            if (targetSpriteRenderer != null && targetSpriteRenderer.sprite == prefabSprites[currentIndex])
            {
                fusionScript.currentTarget = target;
                break;
            }
        }

        // Ativa a linha ao instanciar o prefab
        LineToBottom line = currentBalloon.GetComponent<LineToBottom>();
        if (line != null && line.lineRenderer != null)
        {
            line.lineRenderer.enabled = true;
        }
    }

    public void HandleFusion()
    {
        if (currentBalloon == null) return;

        var fusionScript = currentBalloon.GetComponent<ImageFusion>();
        if (fusionScript != null && fusionScript.currentTarget != null)
        {
            // Obter o centro visual do sprite alvo
            var targetRenderer = fusionScript.currentTarget.GetComponent<SpriteRenderer>();
            Vector3 fusionPoint = targetRenderer.bounds.center;

            // Instancia a fumaça no local da fusão
            if (smokePrefab != null)
            {
                GameObject smoke = Instantiate(smokePrefab, fusionPoint, Quaternion.identity);
                Destroy(smoke, 1f);
            }

            StartCoroutine(TransitionFusion(fusionScript.currentTarget));
        }

        Destroy(currentBalloon);
    

}

    private IEnumerator TransitionFusion(GameObject target)
    {
        if (target != null)
        {
            // Desativar o alvo temporariamente
            target.SetActive(false);
        }

        yield return new WaitForSeconds(0.1f);

        // Atualiza o sprite do target (mesmo ainda desativado)
        if (target != null && currentIndex < mergedSprites.Length)
        {
            var targetSpriteRenderer = target.GetComponent<SpriteRenderer>();
            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = mergedSprites[currentIndex];
            }
        }

        // Reativa o alvo
        if (target != null)
        {
            target.SetActive(true);
        }

        // Toca o som da fusão
        if (fusionSounds != null && currentIndex < fusionSounds.Length && audioSource != null)
        {
            audioSource.PlayOneShot(fusionSounds[currentIndex]);
        }

        yield return new WaitForSeconds(0.1f); 

        StartCoroutine(ShowMergedAndRespawn());
    }



    IEnumerator ShowMergedAndRespawn()
    {
        yield return new WaitForSeconds(3f);

        currentIndex++;

        if (currentIndex < prefabSprites
.Length)
        {
            SpawnPlayerBalloon();
        }
        else
        {
            CheckEndPhase(currentIndex, prefabSprites
    .Length);
        }
    }

    public void RespawnAfterFall(GameObject fallingObject)
    {
        StartCoroutine(HandleFall(fallingObject));
    }

    private IEnumerator HandleFall(GameObject fallingObject)
    {
        yield return new WaitForSeconds(1f);

        if (fallingObject != null)
            Destroy(fallingObject);

        SpawnPlayerBalloon();
    }

    public void PlayAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void SetBackgroundVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    public void OpenPauseMenuLvl2()
    {
        PauseMenu.SetActive(true);
        if (audioSource != null) audioSource.Pause();
        Time.timeScale = 0;
    }

    public void ClosePauseMenuLvl2()
    {
        PauseMenu.SetActive(false);
        if (audioSource != null) audioSource.UnPause();
        Time.timeScale = 1f;
    }

        public void CheckEndPhase(int index, int total)
    {
        if (index >= total)
        {
            ShowEndPhasePanel();
        }
    }

    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    private IEnumerator ShowEndPhasePanelCoroutine()
    {
        // yield return new WaitForSeconds(0.1f);
        


        if (endPhasePanel != null)
            endPhasePanel.SetActive(true);


        if (confettiEffect != null)
            confettiEffect.Play();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(end2);
            yield return new WaitForSeconds(end2.length);
            audioSource.Stop();
        }


    }
}
