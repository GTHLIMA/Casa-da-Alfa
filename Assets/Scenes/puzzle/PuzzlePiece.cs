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
    
    public void Init(PuzzleSlot slot, PuzzleManager manager)
    {
        _slot = slot;
        _manager = manager;
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
        _manager.OnPiecePickedUp();
        _offset = GetMousePos() - (Vector2)transform.position;
    }
    
    void OnMouseUp()
    {
        _dragging = false;
        
        if(Vector2.Distance(transform.position, _slot.transform.position) < 1f)
        {
            transform.position = _slot.transform.position;
            _placed = true;
            _manager.OnPiecePlaced(true);
        }
        else
        {
            transform.position = _originalPosition;
            _manager.OnPiecePlaced(false);
        }
    }
    
    Vector2 GetMousePos()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}