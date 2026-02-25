using System.Collections.Generic;
using UnityEngine;

public class HealerNPC : Unit
{
    [Header("Healer Settings")]
    [SerializeField] private int _healAmount = 15;
    [SerializeField] private int _healRange = 2;           
    [SerializeField] private float _healThreshold = 0.8f;  
    [SerializeField] private int _safeDistanceFromPlayer = 5;
    [SerializeField] private int _optimalAllyDistance = 3;

    public override void TakeTurn()
    {
        PlayerUnit player = GameManager.Instance.Player;

        if (player == null || !player.IsAlive)
        {
            Debug.Log($"{name}: Player is gone, skipping turn.");
            TurnManager.Instance.NextTurn();
            return;
        }

        if (IsLastEnemyAlive())
        {
            FightPlayer(player);
            TurnManager.Instance.NextTurn();
            return;
        }

        Unit mostWoundedAlly = FindMostWoundedAlly();

        if (mostWoundedAlly != null)
        {
            float distToAlly = Vector2.Distance(GridPos, mostWoundedAlly.GridPos);

            if (distToAlly <= _healRange)
            {
                Debug.Log($"{name}: Healing {mostWoundedAlly.name}!");
                HealAlly(mostWoundedAlly);
            }
            else
            {
                Debug.Log($"{name}: Moving toward {mostWoundedAlly.name} to heal (distance: {distToAlly:F1}).");
                MoveTowardAlly(mostWoundedAlly);
            }
        }
        else
        {
            Debug.Log($"{name}: No wounded allies, sticking to team.");
            StickToAlliesWhileStayingSafe(player);
        }

        TurnManager.Instance.NextTurn();
    }

    private bool IsLastEnemyAlive()
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int enemyCount = 0;

        foreach (Unit unit in allUnits)
        {
            if (unit.IsAlive && !(unit is PlayerUnit))
            {
                enemyCount++;
            }
        }
        return enemyCount == 1;
    }

    private void FightPlayer(PlayerUnit player)
    {
        if (TryAttack(player))
        {
            Debug.Log($"{name}: Attacked player for {AttackDamage} damage! Player HP: {player.CurrentHealth}/{player.MaxHealth}");
            return;
        }

        List<Tile> path = Pathfinder.FindPath(OccupiedTile, player.OccupiedTile);

        if (path.Count == 0)
        {
            Debug.Log($"{name}: No path to player.");
            return;
        }

        int stepsToTake = Mathf.Min(MoveRange, path.Count);

        for (int i = 0; i < stepsToTake; i++)
        {
            Tile nextTile = path[i];

            if (nextTile.OccupiedUnit != null)
            {
                Debug.Log($"{name}: Path blocked.");
                break;
            }

            MoveToTile(nextTile);
        }

        Debug.Log($"{name}: Chasing player!");
        TryAttack(player);
    }

    private Unit FindMostWoundedAlly()
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit mostWounded = null;
        float highestPriority = -1f;

        foreach (Unit unit in allUnits)
        {
            if (unit == this || !unit.IsAlive || unit is PlayerUnit)
                continue;

            float hpPercent = (float)unit.CurrentHealth / unit.MaxHealth;

            if (hpPercent < _healThreshold)
            {
                float priority = 1f - hpPercent;

                float distToPlayer = Vector2.Distance(unit.GridPos, GameManager.Instance.Player.GridPos);
                if (distToPlayer <= 3)
                {
                    priority += 0.3f;
                }

                if (priority > highestPriority)
                {
                    highestPriority = priority;
                    mostWounded = unit;
                }
            }
        }

        return mostWounded;
    }

    private void HealAlly(Unit ally)
    {
        int healedAmount = Mathf.Min(_healAmount, ally.MaxHealth - ally.CurrentHealth);

        ally.Heal(healedAmount);

        Debug.Log($"{name}: Healed {ally.name} for {healedAmount} HP! {ally.name} HP: {ally.CurrentHealth}/{ally.MaxHealth}");
    }

    private void MoveTowardAlly(Unit ally)
    {
        List<Tile> path = Pathfinder.FindPath(OccupiedTile, ally.OccupiedTile);

        if (path.Count == 0)
        {
            Debug.Log($"{name}: No path to {ally.name}.");
            return;
        }

        int stepsToTake = Mathf.Min(MoveRange, path.Count);

        for (int i = 0; i < stepsToTake; i++)
        {
            Tile nextTile = path[i];

            if (nextTile.OccupiedUnit != null)
            {
                Debug.Log($"{name}: Path to ally blocked, stopping.");
                break;
            }

            float distAfterMove = Vector2.Distance(nextTile.GridPosition, ally.GridPos);
            if (distAfterMove <= _healRange)
            {
                MoveToTile(nextTile);
                Debug.Log($"{name}: Moved to heal range of {ally.name}. Distance: {distAfterMove:F1} tiles");
                break;
            }

            MoveToTile(nextTile);
        }

        float finalDist = Vector2.Distance(GridPos, ally.GridPos);
        Debug.Log($"{name}: Final distance to {ally.name}: {finalDist:F1} tiles");
    }

    private void StickToAlliesWhileStayingSafe(PlayerUnit player)
    {
        if (player == null || !player.IsAlive) return;

        Unit bestAllyToFollow = FindBestAllyToFollow(player);

        if (bestAllyToFollow == null)
        {
            Debug.Log($"{name}: No allies to follow!");
            return;
        }

        float distToPlayer = Vector2.Distance(GridPos, player.GridPos);
        float distToAlly = Vector2.Distance(GridPos, bestAllyToFollow.GridPos);

        if (distToPlayer < _safeDistanceFromPlayer)
        {
            Debug.Log($"{name}: Player too close ({distToPlayer:F1} tiles), retreating toward {bestAllyToFollow.name}!");
            RetreatTowardAlly(player, bestAllyToFollow);
        }
        else if (distToAlly > _optimalAllyDistance + 1)
        {
            Debug.Log($"{name}: Too far from {bestAllyToFollow.name} ({distToAlly:F1} tiles), moving closer!");
            MoveCloserToAlly(bestAllyToFollow, player);
        }
        else if (distToAlly < 2)
        {
            Debug.Log($"{name}: Too close to {bestAllyToFollow.name} ({distToAlly:F1} tiles), backing off!");
            BackAwayFromAlly(bestAllyToFollow, player);
        }
        else
        {
            Debug.Log($"{name}: In good position near {bestAllyToFollow.name} ({distToAlly:F1} tiles from ally, {distToPlayer:F1} from player).");
        }
    }

    private Unit FindBestAllyToFollow(PlayerUnit player)
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit bestAlly = null;
        float bestScore = -1f;

        foreach (Unit unit in allUnits)
        {
            if (unit == this || !unit.IsAlive || unit is PlayerUnit) continue;

            float score = 0f;

            float hpPercent = (float)unit.CurrentHealth / unit.MaxHealth;
            if (hpPercent < 0.9f)
            {
                score += (1f - hpPercent) * 8f;
            }

            float distToPlayer = Vector2.Distance(unit.GridPos, player.GridPos);
            if (distToPlayer <= 5f)
            {
                score += 4f;
            }

            float distToUs = Vector2.Distance(unit.GridPos, GridPos);
            if (distToUs < 10f)
            {
                score += (10f - distToUs) * 0.3f;
            }

            if (unit is ChaserNPC)
            {
                score += 3f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestAlly = unit;
            }
        }

        return bestAlly;
    }

    private void RetreatTowardAlly(PlayerUnit player, Unit ally)
    {
        Vector2 fleeDirection = (GridPos - player.GridPos).normalized;
        Vector2 allyDirection = (ally.GridPos - GridPos).normalized;

        Vector2 optimalDirection = (fleeDirection * 0.7f + allyDirection * 0.3f).normalized;

        Tile bestTile = FindBestTileInDirection(optimalDirection, MoveRange);

        if (bestTile != null && bestTile != OccupiedTile)
        {
            List<Tile> path = Pathfinder.FindPath(OccupiedTile, bestTile);
            if (path.Count > 0)
            {
                MoveSafelyAlongPath(path);
                Debug.Log($"{name}: Retreated safely toward {ally.name}.");
                return;
            }
        }

        Debug.Log($"{name}: Can't find safe retreat, staying put.");
    }

    private void MoveCloserToAlly(Unit ally, PlayerUnit player)
    {
        List<Tile> path = Pathfinder.FindPath(OccupiedTile, ally.OccupiedTile);

        if (path.Count == 0)
        {
            Debug.Log($"{name}: No path to {ally.name}.");
            return;
        }

        int stepsToTake = Mathf.Min(MoveRange, path.Count);

        for (int i = 0; i < stepsToTake; i++)
        {
            Tile nextTile = path[i];

            if (nextTile.OccupiedUnit != null)
            {
                Debug.Log($"{name}: Path blocked.");
                break;
            }

            float distToPlayerAfterMove = Vector2.Distance(nextTile.GridPosition, player.GridPos);
            if (distToPlayerAfterMove < _safeDistanceFromPlayer - 1)
            {
                Debug.Log($"{name}: Moving closer would be unsafe, stopping.");
                break;
            }

            float distToAllyAfterMove = Vector2.Distance(nextTile.GridPosition, ally.GridPos);
            if (distToAllyAfterMove <= _optimalAllyDistance)
            {
                MoveToTile(nextTile);
                Debug.Log($"{name}: Reached optimal distance from {ally.name}.");
                break;
            }

            MoveToTile(nextTile);
        }
    }

    private void BackAwayFromAlly(Unit ally, PlayerUnit player)
    {
        Vector2 awayFromAlly = (GridPos - ally.GridPos).normalized;

        Vector2 awayFromPlayer = (GridPos - player.GridPos).normalized;

        Vector2 optimalDirection = (awayFromAlly * 0.5f + awayFromPlayer * 0.5f).normalized;

        Tile bestTile = FindBestTileInDirection(optimalDirection, 2);

        if (bestTile != null && bestTile != OccupiedTile)
        {
            float distToPlayerAfterMove = Vector2.Distance(bestTile.GridPosition, player.GridPos);
            if (distToPlayerAfterMove >= _safeDistanceFromPlayer)
            {
                MoveToTile(bestTile);
                Debug.Log($"{name}: Backed away from {ally.name} to maintain distance.");
            }
            else
            {
                Debug.Log($"{name}: Can't back away safely (player too close).");
            }
        }
    }

    private Tile FindBestTileInDirection(Vector2 direction, int maxSteps)
    {
        Tile bestTile = null;
        float bestScore = -1f;

        for (int dist = maxSteps; dist >= 1; dist--)
        {
            Vector2 targetPos = GridPos + direction * dist;
            Tile tile = GridManager.Instance.GetTileAtPosition(targetPos);

            if (tile != null && tile.IsWalkable && tile.OccupiedUnit == null)
            {
                float score = Vector2.Dot(direction, (tile.GridPosition - GridPos).normalized);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }
        }

        if (bestTile == null)
        {
            Vector2[] perpendiculars = new Vector2[]
            {
                new Vector2(-direction.y, direction.x),
                new Vector2(direction.y, -direction.x)
            };

            foreach (Vector2 perpDir in perpendiculars)
            {
                for (int dist = maxSteps; dist >= 1; dist--)
                {
                    Vector2 targetPos = GridPos + perpDir * dist;
                    Tile tile = GridManager.Instance.GetTileAtPosition(targetPos);

                    if (tile != null && tile.IsWalkable && tile.OccupiedUnit == null)
                    {
                        return tile;
                    }
                }
            }
        }

        return bestTile;
    }

    private void MoveSafelyAlongPath(List<Tile> path)
    {
        int stepsToTake = Mathf.Min(MoveRange, path.Count);

        for (int i = 0; i < stepsToTake; i++)
        {
            Tile nextTile = path[i];

            if (nextTile.OccupiedUnit != null)
            {
                break;
            }

            MoveToTile(nextTile);
        }
    }

    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"{name} (Healer) took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        Debug.Log($"{name} (Healer) has been defeated! Allies lost their support!");
    }
}