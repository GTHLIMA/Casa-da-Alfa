using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float upSpeed; // Velocidade de subida do balão
    private AudioManager audioManager; // Referência para o gerenciador de áudio
    private GameManager gameManager; // Referência para o gerenciador do jogo

    [SerializeField] private GameObject spriteDropPrefab; // Prefab do sprite que será dropado
    [SerializeField] private Sprite[] dropSprites; // Array de sprites  para drop
    [SerializeField] private AudioClip[] dropAudioClips; // Array de clipes de áudio para drop
    public GameObject floatingPoints; 

    void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Update()
    {
        // Verifica se o jogo começou | Se o player tocou na tela 
        if (!GameManager.GameStarted && (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0)))
        {
            GameManager.GameStarted = true;
        }

        // Verifica toques na tela para explodir o balão
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
        {
            // Converte a posição do toque para coordenadas do mundo
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Verifica se o toque colidiu com este balão
            if (GetComponent<Collider2D>().OverlapPoint(touchPos))
            {
                PopBalloon();
            }
        }

        // Destroi o balão se chegar na posição 10 do eixo Y. Melhorar << Talvez, se for necessário
        if (transform.position.y > 10f)
        {
            Destroy(gameObject);
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
            points.transform.GetChild(0).GetComponent<TextMesh>().text = "+10";
        }
        else
        {
            Debug.LogError("ERRO: A variável 'floatingPoints' não está atribuída no Inspector do Balão: " + gameObject.name);
        }

        // Adiciona pontos e toca som de estouro
        GameManager.Instance.AddScore(10);
        audioManager.PlaySFX(audioManager.ballonPop);
        
        // Dropa o próximo sprite e destrói o balão
        DropNextSprite();
        Destroy(gameObject);
    }

    private void DropNextSprite()
    {
        if (dropSprites.Length == 0 || spriteDropPrefab == null) return;

        // Reseta o índice se necessário
        if (GameManager.CurrentDropIndex >= dropSprites.Length)
        {
            GameManager.CurrentDropIndex = 0;
        }

        // Instancia o sprite dropado
        GameObject dropInstance = Instantiate(spriteDropPrefab, transform.position, Quaternion.identity);

        // Configura o sprite
        SpriteRenderer sr = dropInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = dropSprites[GameManager.CurrentDropIndex];
        }

        // Toca o áudio correspondente 
        if (GameManager.CurrentDropIndex < dropAudioClips.Length && dropAudioClips[GameManager.CurrentDropIndex] != null)
        {
            audioManager.PlaySFX(dropAudioClips[GameManager.CurrentDropIndex]);
        }
        
        // Destrói o sprite após 4 segundos
        Destroy(dropInstance, 4f);

        // Atualiza o índice e verifica se a fase terminou
        GameManager.CurrentDropIndex++; 
        GameManager.Instance.CheckEndPhase(GameManager.CurrentDropIndex, dropSprites.Length);
    }
}