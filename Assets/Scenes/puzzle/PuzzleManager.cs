using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [System.Serializable]
    public class Syllable
    {
        public string name; // BA, CA, DA, FA
        public List<PuzzleSlot> slotPrefabs;
        public List<PuzzlePiece> piecePrefabs;
        public AudioClip completeClip; // Som específico para esta sílaba
        public Vector2[] slotPositions = new Vector2[6]; // Posições dos 6 slots
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
    
    void Start()
    {
        StartSyllable();
    }
    
    void StartSyllable()
    {
        if (_currentSyllableIndex >= _syllables.Count)
        {
            Debug.Log("Todas as sílabas completadas!");
            return;
        }
        
        // Limpa os slots anteriores
        foreach (var slot in _spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        _spawnedSlots.Clear();
        
        var currentSyllable = _syllables[_currentSyllableIndex];
        _totalPieces = currentSyllable.slotPrefabs.Count;
        _placedPieces = 0;
        _currentPieceIndex = 0;
        
        // Spawna todos os slots da sílaba atual usando as posições específicas desta sílaba
        for (int i = 0; i < currentSyllable.slotPrefabs.Count; i++)
        {
            var spawnedSlot = Instantiate(currentSyllable.slotPrefabs[i], currentSyllable.slotPositions[i], Quaternion.identity);
            spawnedSlot.transform.SetParent(_slotParent);
            _spawnedSlots.Add(spawnedSlot);
        }
        
        // Spawna a primeira peça
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
            // Usa o índice atual para determinar a posição (não aleatório)
            Vector2 spawnPosition;
            
            if (_currentPieceIndex < 3)
            {
                // Primeiras 3 peças spawnam em cima (índices 0, 1, 2)
                spawnPosition = _topSpawnPositions[_currentPieceIndex];
            }
            else
            {
                // Últimas 3 peças spawnam embaixo (índices 3, 4, 5)
                spawnPosition = _bottomSpawnPositions[_currentPieceIndex - 3];
            }
            
            var spawnedPiece = Instantiate(matchingPiece, spawnPosition, Quaternion.identity);
            spawnedPiece.transform.SetParent(_pieceParent);
            spawnedPiece.Init(currentSlot, this);
        }
    }
    
    public void OnPiecePlaced(bool wasCorrect)
    {
        _source.PlayOneShot(_dropClip);
        
        if (wasCorrect)
        {
            _placedPieces++;
            _currentPieceIndex++;
            
            if (_placedPieces >= _totalPieces)
            {
                // Sílaba completa! Toca o som específico desta sílaba
                var currentSyllable = _syllables[_currentSyllableIndex];
                if (currentSyllable.completeClip != null)
                {
                    _source.PlayOneShot(currentSyllable.completeClip);
                }
                
                _currentSyllableIndex++;
                
                // Aguarda um pouco antes de iniciar a próxima sílaba
                Invoke(nameof(StartSyllable), 1.5f);
            }
            else
            {
                // Spawna a próxima peça da mesma sílaba
                SpawnNextPiece();
            }
        }
    }
    
    public void OnPiecePickedUp()
    {
        _source.PlayOneShot(_pickUpClip);
    }
}