using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float upSpeed; // Velocidade de subida do balão
    private AudioManager audioManager; // Referência para o gerenciador de áudio
    

    [SerializeField] private GameObject spriteDropPrefab; // Prefab do sprite que será dropado
    [SerializeField] private Sprite[] dropSprites; // Array de sprites para drop
    [SerializeField] private AudioClip[] dropAudioClips; // Array de clipes de áudio para drop
    public GameObject floatingPoints; 


    [Header("===== Configurações Específicas do Balão =====")]
    public bool isGolden = false; // Marque no Inspector para o GoldenBalloonPrefab
    public int scoreValue = 10;   // Valor padrão de pontos (será 10 para balões normais)

    [Header("===== Pop Animation =====")]
    [SerializeField] private Sprite[] popAnimation; 
    [SerializeField] private float frameRate = 0.05f; 
    private SpriteRenderer spriteRenderer; // Referência ao SpriteRenderer do balão
    private bool isPopping = false; // Flag para verificar se o balão está estourando



    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // Pega o SpriteRenderer do balão
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Update()
    {

        // Verifica toques na tela para explodir o balão
        // MODIFICADO: Movi Input.mousePosition para dentro do if para melhor performance
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTouch(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0)) // Para testes no editor
        {
            HandleTouch(Input.mousePosition);
        }

        // Destroi o balão se chegar na posição 10 do eixo Y.
        if (transform.position.y > 10f)
        {
            Destroy(gameObject);
        }
    }

    // NOVO: Método para centralizar a lógica de toque/clique
    void HandleTouch(Vector2 screenPosition)
    {
        Vector2 touchPos = Camera.main.ScreenToWorldPoint(screenPosition);
        Collider2D collider = GetComponent<Collider2D>(); // Pega o collider uma vez

        if (collider != null && collider.OverlapPoint(touchPos))
        {
            PopBalloon();
        }
    }

    private void FixedUpdate()
    {
        // O balão sobe se o jogo começou
        if (GameManager.GameStarted) transform.Translate(0, upSpeed, 0);
    }

    private void PopBalloon()
    {
        if (isPopping) return; 

        PopBalloonAnimation(); 
    }
    
    private void PopBalloonAnimation()
    {
        if (isPopping) return;
        isPopping = true;

        // Mostra pontos flutuantes
        if (floatingPoints != null)
        {
            GameObject points = Instantiate(floatingPoints, transform.position, Quaternion.identity);
            points.transform.GetChild(0).GetComponent<TextMesh>().text = "+" + scoreValue.ToString();
        }

        GameManager.Instance.AddScore(scoreValue);
        if (audioManager != null && audioManager.ballonPop != null)
            audioManager.PlaySFX(audioManager.ballonPop);

        // Inicia a animação de estouro
        StartCoroutine(PlayPopAnimation());

        // Inicia o drop do sprite com delay (2 segundos)
        StartCoroutine(DelayedDrop(0.2f));
    }

    private IEnumerator DelayedDrop(float delay)
    {
        yield return new WaitForSeconds(delay);
        DropNextSprite();
        Destroy(gameObject); // Destrói o balão APÓS dropar o sprite
    }

    private IEnumerator PlayPopAnimation()
    {
        foreach (var frame in popAnimation)
        {
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(frameRate);
        }
        // Removido o Destroy(gameObject) daqui para evitar conflito
    }


    private void DropNextSprite()
    {

        if (dropSprites.Length == 0 || spriteDropPrefab == null) return; //

        // Reseta o índice se necessário
        if (GameManager.CurrentDropIndex >= dropSprites.Length) //
        {
            GameManager.CurrentDropIndex = 0; //
        }

        // Instancia o sprite dropado
        GameObject dropInstance = Instantiate(spriteDropPrefab, transform.position, Quaternion.identity); //

        // Configura o sprite
        SpriteRenderer sr = dropInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = dropSprites[GameManager.CurrentDropIndex]; //
        }

        // Toca o áudio correspondente
        if (GameManager.CurrentDropIndex < dropAudioClips.Length && dropAudioClips[GameManager.CurrentDropIndex] != null) //
        {
            if (audioManager != null) // Adicionada checagem para audioManager
            {
                audioManager.PlaySFX(dropAudioClips[GameManager.CurrentDropIndex]); //
            }
        }

        // Destrói o sprite após 4 segundos
        Destroy(dropInstance, 4f); //

        // Atualiza o índice e verifica se a fase terminou
        GameManager.CurrentDropIndex++;  //
        GameManager.Instance.CheckEndPhase(GameManager.CurrentDropIndex, dropSprites.Length); //
    }
}