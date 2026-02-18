using System.Collections.Generic;
using UnityEngine;

public abstract class Tile : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer _renderer;
    public bool IsWalkable { get; protected set; } = true;
    public Unit OccupiedUnit { get; set; }
    public Vector2 GridPosition { get; private set; }

    public virtual void Init(int x, int y)
    {
        GridPosition = new Vector2(x, y);
    }
}