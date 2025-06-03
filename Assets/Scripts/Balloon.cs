using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float upSpeed; // Velocidade de subida do balão
    private AudioManager audioManager; // Referência para o gerenciador de áudio
    // private GameManager gameManager; // Referência para o gerenciador do jogo - REMOVIDA SE NÃO USADA DIRETAMENTE AQUI

    [SerializeField] private GameObject spriteDropPrefab; // Prefab do sprite que será dropado
    [SerializeField] private Sprite[] dropSprites; // Array de sprites para drop
    [SerializeField] private AudioClip[] dropAudioClips; // Array de clipes de áudio para drop
    public GameObject floatingPoints;


    [Header("===== Configurações Específicas do Balão =====")]
    public bool isGolden = false; // Marque no Inspector para o GoldenBalloonPrefab
    public int scoreValue = 10;   // Valor padrão de pontos (será 10 para balões normais)


    void Awake()
    {
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
        // Mostra o efeito de pontos voando lá
        if (floatingPoints != null)
        {
            GameObject points = Instantiate(floatingPoints, transform.position, Quaternion.identity);
            // MODIFICADO: Usa a variável scoreValue para o texto do popup
            points.transform.GetChild(0).GetComponent<TextMesh>().text = "+" + scoreValue.ToString();
        }
        else
        {
            Debug.LogError("ERRO: A variável 'floatingPoints' não está atribuída no Inspector do Balão: " + gameObject.name);
        }

        // Adiciona pontos e toca som de estouro
        // MODIFICADO: Usa a variável scoreValue para adicionar os pontos
        GameManager.Instance.AddScore(scoreValue); 
        if (audioManager != null && audioManager.ballonPop != null) // Adicionada checagem para audioManager
        {
            audioManager.PlaySFX(audioManager.ballonPop); 
        }
        
        // Dropa o próximo sprite e destrói o balão
        DropNextSprite(); 
        Destroy(gameObject); 
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