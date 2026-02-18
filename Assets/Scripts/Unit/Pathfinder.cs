using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
 
    public static List<Tile> FindPath(Tile startTile, Tile targetTile)
    {
        if (startTile == null || targetTile == null) return new List<Tile>();
        if (!targetTile.IsWalkable) return new List<Tile>();
        if (startTile == targetTile) return new List<Tile>();

        List<PathNode> openList = new List<PathNode>();   
        HashSet<Tile> closedSet = new HashSet<Tile>();   

        PathNode startNode = new PathNode(startTile, null, 0, GetHeuristic(startTile, targetTile));
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            
            PathNode current = GetLowestFCostNode(openList);

            
            if (current.Tile == targetTile)
                return RetracePath(current);

            openList.Remove(current);
            closedSet.Add(current.Tile);

           
            foreach (Tile neighbour in GetNeighbours(current.Tile))
            {
                if (!neighbour.IsWalkable) continue;
                if (closedSet.Contains(neighbour)) continue;

               

                float gCost = current.GCost + 1f; 
                float hCost = GetHeuristic(neighbour, targetTile);

                PathNode existingNode = openList.Find(n => n.Tile == neighbour);

                if (existingNode == null)
                {
            
                    openList.Add(new PathNode(neighbour, current, gCost, hCost));
                }
                else if (gCost < existingNode.GCost)
                {

                    existingNode.GCost = gCost;
                    existingNode.Parent = current;
                }
            }
        }


        return new List<Tile>();
    }


    private static float GetHeuristic(Tile a, Tile b)
    {
        return Mathf.Abs(a.GridPosition.x - b.GridPosition.x)
             + Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
    }


    private static PathNode GetLowestFCostNode(List<PathNode> openList)
    {
        PathNode lowest = openList[0];
        foreach (PathNode node in openList)
        {
            if (node.FCost < lowest.FCost ||
               (node.FCost == lowest.FCost && node.HCost < lowest.HCost))
            {
                lowest = node;
            }
        }
        return lowest;
    }

    private static List<Tile> GetNeighbours(Tile tile)
    {
        List<Tile> neighbours = new List<Tile>();
        Vector2 pos = tile.GridPosition;

        Vector2[] directions = new Vector2[]
        {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right
        };

        foreach (Vector2 dir in directions)
        {
            Tile neighbour = GridManager.Instance.GetTileAtPosition(pos + dir);
            if (neighbour != null)
                neighbours.Add(neighbour);
        }

        return neighbours;
    }


    private static List<Tile> RetracePath(PathNode targetNode)
    {
        List<Tile> path = new List<Tile>();
        PathNode current = targetNode;

        while (current.Parent != null)
        {
            path.Add(current.Tile);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }
}

public class PathNode
{
    public Tile Tile { get; }
    public PathNode Parent { get; set; }
    public float GCost { get; set; } 
    public float HCost { get; }       
    public float FCost => GCost + HCost;

    public PathNode(Tile tile, PathNode parent, float gCost, float hCost)
    {
        Tile = tile;
        Parent = parent;
        GCost = gCost;
        HCost = hCost;
    }
}