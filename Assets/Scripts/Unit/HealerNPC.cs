using System.Collections.Generic;
using UnityEngine;

// Healer NPC
// - Heals allies in range with 10 HP
// - Will move towards wounded allies if they are out of range
// - Will retreat if player is too close but still prioritizes healing over retreating
// - Will remain idle if all alllies are full health
// - If healer is the last enemy alive, it will start attacking the player

public class HealerNPC : Unit
{
    [Header("Healer Settings")]
    [SerializeField] private int _healAmount = 15;
    [SerializeField] private int _healRange = 2;
    [SerializeField] private int _retreatDistance = 3; // How close player needs to be to trigger retreat

    private bool _hasHealedThisTurn = false;
    private bool _hasMovedThisTurn = false;

    // Main turn logic for the Healer NPC
    public override void TakeTurn()
    {
        PlayerUnit player = GameManager.Instance.Player;

        if (player == null || !player.IsAlive)
        {
            TurnManager.Instance.NextTurn();
            return;
        }

        _hasHealedThisTurn = false;
        _hasMovedThisTurn = false;

        // Attack if last enemy alive
        if (IsLastEnemyAlive())
        {
            FightPlayer(player);
            TurnManager.Instance.NextTurn();
            return;
        }

        // Get in range to heal allies
        TryMoveToHelpAllies();

        // Heal allies
        HealAlliesInRange();

        // Check player distance if it hasnt healed
        if (!_hasHealedThisTurn)
        {
            CheckPlayerDistance(player);
        }

        TurnManager.Instance.NextTurn();
    }

    // Helper to check if this is the last enemy alive
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

    // Logic to move towards wounded allies
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

                _hasMovedThisTurn = true;
            }
        }
    }

    // Helper to find the closest wounded ally
    private Unit FindClosestWoundedAlly()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit closest = null;
        float closestDist = float.MaxValue;

        foreach (Unit unit in allUnits)
        {
            if (unit == this || !unit.IsAlive || unit is PlayerUnit)
                continue;

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

    // Logic to heal all allies in range
    private void HealAlliesInRange()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);

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
                _hasHealedThisTurn = true;
                Debug.Log($"{name}: Healed {unit.name} for {healValue} HP!");
                GameManager.Instance.feedText.text = $"{name} healed allies.";
            }
        }
    }

    // Logic to retreat from player if they are too close
    private void CheckPlayerDistance(PlayerUnit player)
    {
        float distToPlayer = Vector2.Distance(GridPos, player.GridPos);

        // If player is too close, retreat
        if (distToPlayer <= _retreatDistance)
        {
            RetreatFromPlayer(player);
        }
        else
        {
            // Player is at safe distance, do nothing
            Debug.Log($"{name}: Everyone is healthy and player is at safe distance ({distToPlayer:F1} tiles). Standing by.");

            if (!_hasMovedThisTurn)
            {
                audioManager.PlaySFX(audioManager.enemyNotMoveSound);
                GameManager.Instance.feedText.text = $"{name} stood their ground.";
            }
        }
    }

    // Logic to attack player if last enemy alive
    private void FightPlayer(PlayerUnit player)
    {
        // Try to attack first
        if (TryAttack(player))
        {
            Debug.Log($"{name}: Attacked player for {AttackDamage} damage!");
            GameManager.Instance.feedText.text = "Healer attacked!";
            return;
        }

        // Move toward player if cant attack
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
        Debug.Log($"{name} (Healer) has been defeated!");
        GameManager.Instance.feedText.text = "Healer was defeated!";

        base.OnDeath();
    }
}