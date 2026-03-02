using System.Collections.Generic;
using UnityEngine;

public class HealerNPC : Unit
{
    [Header("Healer Settings")]
    [SerializeField] private int _healAmount = 15;
    [SerializeField] private int _healRange = 2;
    [SerializeField] private int _retreatDistance = 3; // How close player needs to be to trigger retreat

    private bool _hasHealedThisTurn = false;

    public override void TakeTurn()
    {
        PlayerUnit player = GameManager.Instance.Player;

        if (player == null || !player.IsAlive)
        {
            TurnManager.Instance.NextTurn();
            return;
        }

        _hasHealedThisTurn = false;

        // CASE 1: Last enemy alive, attack player
        if (IsLastEnemyAlive())
        {
            FightPlayer(player);
            TurnManager.Instance.NextTurn();
            return;
        }

        // CASE 2: Try to move first (to get in range of wounded allies)
        TryMoveToHelpAllies();

        // CASE 3: After moving, heal anyone in range
        HealAlliesInRange();

        // CASE 4: If no one needed healing, check if player is too close
        if (!_hasHealedThisTurn)
        {
            CheckPlayerDistance(player);
        }

        TurnManager.Instance.NextTurn();
    }

    private bool IsLastEnemyAlive()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int aliveEnemies = 0;

        foreach (Unit unit in allUnits)
        {
            if (unit.IsAlive && unit != this && !(unit is PlayerUnit))
                aliveEnemies++;
        }

        return aliveEnemies == 0;
    }

    private void TryMoveToHelpAllies()
    {
        Unit closestWoundedAlly = FindClosestWoundedAlly();

        if (closestWoundedAlly != null)
        {
            float distToAlly = Vector2.Distance(GridPos, closestWoundedAlly.GridPos);

            // If wounded ally is out of range, move toward them
            if (distToAlly > _healRange)
            {
                List<Tile> path = Pathfinder.FindPath(OccupiedTile, closestWoundedAlly.OccupiedTile);

                if (path.Count > 0)
                {
                    int steps = Mathf.Min(MoveRange, path.Count);
                    for (int i = 0; i < steps; i++)
                    {
                        if (path[i].OccupiedUnit == null)
                        {
                            MoveToTile(path[i]);
                        }
                        else break;
                    }
                    Debug.Log($"{name}: Moved toward wounded {closestWoundedAlly.name}");
                }
            }
        }
    }

    private Unit FindClosestWoundedAlly()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit closest = null;
        float closestDist = float.MaxValue;

        foreach (Unit unit in allUnits)
        {
            if (unit == this || !unit.IsAlive || unit is PlayerUnit)
                continue;

            // Only consider allies below max health
            if (unit.CurrentHealth < unit.MaxHealth)
            {
                float dist = Vector2.Distance(GridPos, unit.GridPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = unit;
                }
            }
        }

        return closest;
    }

    private void HealAlliesInRange()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        bool healedAnyone = false;

        foreach (Unit unit in allUnits)
        {
            if (unit == this || !unit.IsAlive || unit is PlayerUnit)
                continue;

            // Check if ally is in range and needs healing
            float dist = Vector2.Distance(GridPos, unit.GridPos);
            if (dist <= _healRange && unit.CurrentHealth < unit.MaxHealth)
            {
                int healValue = Mathf.Min(_healAmount, unit.MaxHealth - unit.CurrentHealth);
                unit.Heal(healValue);
                healedAnyone = true;
                _hasHealedThisTurn = true;
                Debug.Log($"{name}: Healed {unit.name} for {healValue} HP!");
            }
        }

        if (healedAnyone)
            GameManager.Instance.feedText.text = "Healer healed allies!";
    }

    private void CheckPlayerDistance(PlayerUnit player)
    {
        float distToPlayer = Vector2.Distance(GridPos, player.GridPos);

        // If player is too close, retreat exactly 3 tiles behind
        if (distToPlayer <= _retreatDistance)
        {
            RetreatFromPlayer(player);
        }
        else
        {
            // Player is at safe distance, do nothing
            Debug.Log($"{name}: Everyone is healthy and player is at safe distance ({distToPlayer:F1} tiles). Standing by.");
        }
    }

    private void FightPlayer(PlayerUnit player)
    {
        // Try to attack first
        if (TryAttack(player))
        {
            Debug.Log($"{name}: Attacked player for {AttackDamage} damage!");
            GameManager.Instance.feedText.text = "Healer attacked!";
            return;
        }

        // Move toward player if can't attack
        List<Tile> path = Pathfinder.FindPath(OccupiedTile, player.OccupiedTile);
        if (path.Count > 0)
        {
            int steps = Mathf.Min(MoveRange, path.Count);
            for (int i = 0; i < steps; i++)
            {
                if (path[i].OccupiedUnit == null)
                {
                    MoveToTile(path[i]);
                }
                else break;
            }
            Debug.Log($"{name}: Chasing player!");

            // Try attack again after moving
            TryAttack(player);
        }
    }

    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"{name} (Healer) took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        Debug.Log($"{name} (Healer) has been defeated!");
        GameManager.Instance.feedText.text = "Healer was defeated!";
    }
}