using UnityEngine;

public class MountainTile : Tile
{
    public override void Init(int x, int y)
    {
        base.Init(x, y);
        IsWalkable = false;
    }
}