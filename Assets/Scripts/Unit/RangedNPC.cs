using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RangedNPC : Unit
{
    [Header("Ranged Settings")]
    [SerializeField] private int _attackRange = 3;

    public override void TakeTurn()
    {
        PlayerUnit player = GameManager.Instance.Player;

        if (player == null || !player.IsAlive)
        {
            Debug.Log($"{name}: Player is gone, skipping turn.");
            TurnManager.Instance.NextTurn();
            return;
        }

        float distToPlayer = Vector2.Distance(GridPos, player.GridPos);

        if (distToPlayer == _attackRange)
        {
            Debug.Log($"{name}: Player at perfect range ({distToPlayer:F1} tiles), shooting!");
            TryRangedAttack(player);
        }

        else if (distToPlayer < _attackRange)
        {
            Debug.Log($"{name}: Player too close ({distToPlayer:F1} tiles), retreating 3 tiles!");

            bool retreatSuccessful = RetreatFromPlayer(player);

            if (!retreatSuccessful)
            {
                Debug.Log($"{name}: Can't retreat! Shooting anyway!");
                TryRangedAttack(player);
            }
        }

        else
        {
            Debug.Log($"{name}: Player too far ({distToPlayer:F1} tiles), advancing 3 tiles!");
            AdvanceTowardPlayer(player);
        }

        TurnManager.Instance.NextTurn();
    }

    private bool TryRangedAttack(Unit target)
    {
        if (target == null || !target.IsAlive) return false;

        float dist = Vector2.Distance(GridPos, target.GridPos);

        if (dist > _attackRange) return false;

        target.TakeDamage(AttackDamage);
        Debug.Log($"{name}: Ranged attack hit {target.name} for {AttackDamage} damage! HP: {target.CurrentHealth}/{target.MaxHealth}");
        return true;
    }

    private bool RetreatFromPlayer(PlayerUnit player)
    {
        Vector2 fleeDirection = (GridPos - player.GridPos).normalized;

        Tile bestRetreatTile = FindFarthestRetreatTile(fleeDirection, MoveRange);

        if (bestRetreatTile != null && bestRetreatTile != OccupiedTile)
        {
            List<Tile> path = Pathfinder.FindPath(OccupiedTile, bestRetreatTile);

            if (path.Count > 0)
            {
                int stepsToTake = Mathf.Min(MoveRange, path.Count);

                for (int i = 0; i < stepsToTake; i++)
                {
                    Tile nextTile = path[i];

                    if (nextTile.OccupiedUnit != null)
                    {
                        Debug.Log($"{name}: Retreat path blocked at step {i + 1}, stopping.");
                        break;
                    }

                    MoveToTile(nextTile);
                }

                float newDist = Vector2.Distance(GridPos, player.GridPos);
                Debug.Log($"{name}: Retreated to {OccupiedTile.GridPosition} (now {newDist:F1} tiles away)");
                return true;
            }
        }

        Debug.Log($"{name}: No valid retreat path found!");
        return false;
    }

    private void AdvanceTowardPlayer(PlayerUnit player)
    {
        List<Tile> path = Pathfinder.FindPath(OccupiedTile, player.OccupiedTile);

        if (path.Count == 0)
        {
            Debug.Log($"{name}: No path to player found.");
            return;
        }

        int stepsToTake = Mathf.Min(MoveRange, path.Count);

        for (int i = 0; i < stepsToTake; i++)
        {
            Tile nextTile = path[i];

            if (nextTile.OccupiedUnit != null)
            {
                Debug.Log($"{name}: Path blocked at step {i + 1}, stopping advance.");
                break;
            }

            MoveToTile(nextTile);
            Debug.Log($"{name}: Advanced to {nextTile.GridPosition} (step {i + 1}/{stepsToTake})");
        }

        float finalDist = Vector2.Distance(GridPos, player.GridPos);
        Debug.Log($"{name}: Finished advancing. Now {finalDist:F1} tiles from player.");
    }

    private Tile FindFarthestRetreatTile(Vector2 direction, int maxSteps)
    {
        Tile bestRetreatTile = null;
        float bestDistFromPlayer = 0f;

        // Try straight back first (best option)
        for (int dist = maxSteps; dist >= 1; dist--)
        {
            Vector2 targetPos = GridPos + direction * dist;
            Tile tile = GridManager.Instance.GetTileAtPosition(targetPos);

            if (tile != null && tile.IsWalkable && tile.OccupiedUnit == null)
            {
                // Check if we can actually path to it
                List<Tile> testPath = Pathfinder.FindPath(OccupiedTile, tile);
                if (testPath.Count > 0)
                {
                    return tile; // Found a clear path straight back
                }
            }
        }

        // Straight back is blocked, try diagonal/perpendicular retreat
        Vector2[] alternateDirections = new Vector2[]
        {
            (direction + new Vector2(-direction.y, direction.x)).normalized,  // diagonal-right back
            (direction + new Vector2(direction.y, -direction.x)).normalized,  // diagonal-left back
            new Vector2(-direction.y, direction.x),   // perpendicular right
            new Vector2(direction.y, -direction.x)    // perpendicular left
        };

        foreach (Vector2 altDir in alternateDirections)
        {
            for (int dist = maxSteps; dist >= 1; dist--)
            {
                Vector2 targetPos = GridPos + altDir * dist;
                Tile tile = GridManager.Instance.GetTileAtPosition(targetPos);

                if (tile != null && tile.IsWalkable && tile.OccupiedUnit == null)
                {
                    // Check if we can actually path to it
                    List<Tile> testPath = Pathfinder.FindPath(OccupiedTile, tile);

                    if (testPath.Count > 0)
                    {
                        float distFromPlayer = Vector2.Distance(tile.GridPosition, GameManager.Instance.Player.GridPos);

                        // Pick the tile that puts us farthest from the player
                        if (distFromPlayer > bestDistFromPlayer)
                        {
                            bestDistFromPlayer = distFromPlayer;
                            bestRetreatTile = tile;
                        }
                    }
                }
            }
        }

        return bestRetreatTile;
    }

    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"{name} took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        Debug.Log($"{name} has been defeated!");
    }
}