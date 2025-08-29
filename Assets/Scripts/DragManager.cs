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

    [Header("Efeitos Visuais")]
    public GameObject smokePrefab;

    private AudioSource audioSource;
    private int currentIndex = 0;
    private GameObject currentBalloon;
    private List<GameObject> spawnedTargets = new List<GameObject>();
    public float targetYViewport = 0.15f;

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

    void SpawnTargetsAleatorios()
{
    var shuffled = targetVariants.OrderBy(x => Random.value).ToList();

    int count = Mathf.Min(9, shuffled.Count);
    float y = Mathf.Clamp01(targetYViewport); // 0..1; 0.5 = centro vertical

    for (int i = 0; i < count; i++)
    {
        // Distribui de 0.1 a 0.9 (deixando margens) e centraliza bem
        float t = (count == 1) ? 0.5f : (float)i / (count - 1);
        float xViewport = Mathf.Lerp(0.1f, 0.92f, t);

        Vector3 viewportPos = new Vector3(xViewport, y, Camera.main.nearClipPlane + 4f);
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(viewportPos);
        worldPos.z = 0f;

        GameObject instance = Instantiate(shuffled[i], worldPos, Quaternion.identity);
        spawnedTargets.Add(instance);

        var col = instance.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        instance.SetActive(true);
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

        // Procura o target com sprite igual ao prefab 
        fusionScript.currentTarget = null;
        foreach (var target in spawnedTargets)
        {
            var targetSpriteRenderer = target.GetComponent<SpriteRenderer>();
            if (targetSpriteRenderer != null && 
                (targetSpriteRenderer.sprite == prefabSprites[currentIndex] || 
                 targetSpriteRenderer.sprite == mergedSprites[currentIndex]))
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

        // Atualiza limite de queda com base no target
        fusionScript.UpdateFallLimit();
    }

    public void HandleFusion()
    {
        if (currentBalloon == null) return;

        var fusionScript = currentBalloon.GetComponent<ImageFusion>();
        if (fusionScript != null && fusionScript.currentTarget != null)
        {
            var targetRenderer = fusionScript.currentTarget.GetComponent<SpriteRenderer>();
            Vector3 fusionPoint = targetRenderer.bounds.center;

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
        if (target != null && currentIndex < mergedSprites.Length)
        {
            var targetSpriteRenderer = target.GetComponent<SpriteRenderer>();

            if (targetSpriteRenderer != null)
            {
                // Atualiza o sprite do target para o sprite fundido correspondente
                targetSpriteRenderer.sprite = mergedSprites[currentIndex];
            }

            // Garante que o collider está ativo para futuras fusões
            var collider = target.GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = true;

            // Mantém o target ativo (não desativa mais)
            target.SetActive(true);
        }

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

        if (currentIndex < prefabSprites.Length)
        {
            SpawnPlayerBalloon();
        }
        else
        {
            CheckEndPhase(currentIndex, prefabSprites.Length);
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
