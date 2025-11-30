using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [SerializeField] public SpriteRenderer _renderer;
    
    private bool _dragging, _placed;
    private Vector2 _offset, _originalPosition;
    private PuzzleSlot _slot;
    private PuzzleManager _manager;
    private int _pieceIndex; // 🔥 NOVO: Índice da peça
    
    // 🔥 MODIFICADO: Agora recebe pieceIndex
    public void Init(PuzzleSlot slot, PuzzleManager manager, int pieceIndex)
    {
        _slot = slot;
        _manager = manager;
        _pieceIndex = pieceIndex;
    }
    
    void Awake()
    {
        _originalPosition = transform.position;
    }
    
    void Update()
    {
        if(_placed) return;      
        if(!_dragging) return;
        
        var mousePosition = GetMousePos();
        transform.position = mousePosition - _offset;
    }
    
    void OnMouseDown()
    {
        if(_placed) return;
        
        _dragging = true;
        
        // 🔥 MODIFICADO: Chama novo método com posição
        _manager.OnPiecePickedUp(_pieceIndex, transform.position);
        
        _offset = GetMousePos() - (Vector2)transform.position;
    }
    
    void OnMouseUp()
    {
        _dragging = false;
        
        Vector2 currentPosition = transform.position;
        bool wasCorrect = false;
        
        if(Vector2.Distance(currentPosition, _slot.transform.position) < 1f)
        {
            transform.position = _slot.transform.position;
            _placed = true;
            wasCorrect = true;
        }
        else
        {
            transform.position = _originalPosition;
            wasCorrect = false;
        }
        
        // 🔥 MODIFICADO: Chama novo método com posição e resultado
        _manager.OnPiecePlaced(wasCorrect, _pieceIndex, currentPosition);
    }
    
    Vector2 GetMousePos()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}