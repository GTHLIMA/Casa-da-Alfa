using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [System.Serializable]
    public class Syllable
    {
        public string name; 
        public List<PuzzleSlot> slotPrefabs;
        public List<PuzzlePiece> piecePrefabs;
        public AudioClip completeClip;
        public Vector2[] slotPositions = new Vector2[6]; 
    }
    
    [SerializeField] private List<Syllable> _syllables = new List<Syllable>();
    [SerializeField] private Transform _slotParent, _pieceParent;
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _pickUpClip, _dropClip;
    
    [Header("Spawn das Peças")]
    [SerializeField] private Vector2[] _topSpawnPositions = new Vector2[3];
    [SerializeField] private Vector2[] _bottomSpawnPositions = new Vector2[3];
    
    private int _currentSyllableIndex = 0;
    private int _totalPieces;
    private int _placedPieces;
    private int _currentPieceIndex;
    private List<PuzzleSlot> _spawnedSlots = new List<PuzzleSlot>();
    private List<PuzzlePiece> _spawnedPieces = new List<PuzzlePiece>();

    // 🔥 NOVO: Variáveis para logging
    private PuzzleSyllableGameLogger gameLogger;
    private float syllableStartTime;
    private float piecePickupTime;
    
    [Header("Pause Menu && Painel de Fim de Fase")]
    public GameObject PauseMenu;
    public GameObject endPhasePanel;
    public ParticleSystem confettiEffect;
    public AudioClip end2;
    private AudioSource audioSource;
    
    void Start()
    {
        // 🔥 NOVO: Inicializar logger
        gameLogger = FindObjectOfType<PuzzleSyllableGameLogger>();
        
        StartSyllable();
    }
    
    void StartSyllable()
    {
        if (_currentSyllableIndex >= _syllables.Count)
        {
            Debug.Log("Todas as sílabas completadas!");
            
            // 🔥 NOVO: Log de jogo completo
            if (gameLogger != null)
            {
                float totalPlayTime = Time.time - syllableStartTime;
                gameLogger.LogGameCompleted(_syllables.Count, _placedPieces, totalPlayTime);
            }
            
            ShowEndPhasePanel();
            return;
        }
        
        // 🔥 NOVO: Iniciar tempo da sílaba
        syllableStartTime = Time.time;
        
        foreach (var slot in _spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        _spawnedSlots.Clear();
        
        foreach (var piece in _spawnedPieces)
        {
            if (piece != null)
                Destroy(piece.gameObject);
        }
        _spawnedPieces.Clear();
        
        var currentSyllable = _syllables[_currentSyllableIndex];
        _totalPieces = currentSyllable.slotPrefabs.Count;
        _placedPieces = 0;
        _currentPieceIndex = 0;
        
        for (int i = 0; i < currentSyllable.slotPrefabs.Count; i++)
        {
            var spawnedSlot = Instantiate(currentSyllable.slotPrefabs[i], currentSyllable.slotPositions[i], Quaternion.identity);
            spawnedSlot.transform.SetParent(_slotParent);
            _spawnedSlots.Add(spawnedSlot);
        }
        
        SpawnNextPiece();
    }
    
    void SpawnNextPiece()
    {
        var currentSyllable = _syllables[_currentSyllableIndex];
        
        if (_currentPieceIndex >= currentSyllable.slotPrefabs.Count)
            return;
        
        var currentSlot = _spawnedSlots[_currentPieceIndex];
        var matchingPiece = currentSyllable.piecePrefabs.FirstOrDefault(p => p._renderer.sprite == currentSlot.Renderer.sprite);
        
        if (matchingPiece != null)
        {
            Vector2 spawnPosition;
            
            if (_currentPieceIndex < 3)
            {
                spawnPosition = _topSpawnPositions[_currentPieceIndex];
            }
            else
            {
                spawnPosition = _bottomSpawnPositions[_currentPieceIndex - 3];
            }
            
            var spawnedPiece = Instantiate(matchingPiece, spawnPosition, Quaternion.identity);
            spawnedPiece.transform.SetParent(_pieceParent);
            spawnedPiece.Init(currentSlot, this, _currentPieceIndex); // 🔥 MODIFICADO: Passa pieceIndex
            _spawnedPieces.Add(spawnedPiece);
        }
    }
    
    // 🔥 NOVO: Método para peça pega
    public void OnPiecePickedUp(int pieceIndex, Vector2 piecePosition)
    {
        _source.PlayOneShot(_pickUpClip);
        
        // 🔥 NOVO: Log do pickup
        if (gameLogger != null)
        {
            string currentSyllable = _syllables[_currentSyllableIndex].name;
            gameLogger.LogPiecePickup(currentSyllable, _currentSyllableIndex, pieceIndex, piecePosition);
        }
        
        piecePickupTime = Time.time; // 🔥 NOVO: Registrar tempo do pickup
    }
    
    public void OnPiecePlaced(bool wasCorrect, int pieceIndex, Vector2 piecePosition)
    {
        _source.PlayOneShot(_dropClip);
        
        // 🔥 NOVO: Log do placement
        if (gameLogger != null)
        {
            string currentSyllable = _syllables[_currentSyllableIndex].name;
            float placementTime = Time.time - piecePickupTime;
            gameLogger.LogPiecePlacement(currentSyllable, _currentSyllableIndex, pieceIndex, piecePosition, wasCorrect, placementTime);
        }
        
        if (wasCorrect)
        {
            _placedPieces++;
            _currentPieceIndex++;
            
            if (_placedPieces >= _totalPieces)
            {
                var currentSyllable = _syllables[_currentSyllableIndex];
                if (currentSyllable.completeClip != null)
                {
                    _source.PlayOneShot(currentSyllable.completeClip);
                }
                
                // 🔥 NOVO: Log de puzzle completo
                if (gameLogger != null)
                {
                    float completionTime = Time.time - syllableStartTime;
                    gameLogger.LogPuzzleCompleted(currentSyllable.name, _currentSyllableIndex, _totalPieces, completionTime);
                }
                
                _currentSyllableIndex++;
                
                Invoke(nameof(StartSyllable), 1.5f);
            }
            else
            {
                SpawnNextPiece();
            }
        }
    }
    
    // 🔥 MANTIDO: Método antigo para compatibilidade
    public void OnPiecePickedUp()
    {
        // Chamada vazia para manter compatibilidade
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

    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    private IEnumerator ShowEndPhasePanelCoroutine()
    {
        yield return new WaitForSeconds(2f);

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