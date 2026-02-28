using System.Collections.Generic;
using UnityEngine;

public class ChaserNPC : Unit
{
    protected override void Awake()
    {
        base.Awake();
    }

    public override void TakeTurn()
    {
        PlayerUnit player = GameManager.Instance.Player;

        if (player == null || !player.IsAlive)
        {
            Debug.Log($"{name}: Player is gone, skipping turn.");
            TurnManager.Instance.NextTurn();
            return;
        }

        if (TryAttack(player))
        {
            Debug.Log($"{name}: Attacked player for {AttackDamage} damage! Player HP: {player.CurrentHealth}/{player.MaxHealth}");
            TurnManager.Instance.NextTurn();
            return;
        }

        List<Tile> path = Pathfinder.FindPath(OccupiedTile, player.OccupiedTile);

        if (path.Count == 0)
        {
            Debug.Log($"{name}: No path to player found.");
            TurnManager.Instance.NextTurn();
            return;
        }

        int steps = Mathf.Min(MoveRange, path.Count);

        for (int i = 0; i < steps; i++)
        {
            Tile nextTile = path[i];

            if (nextTile.OccupiedUnit != null)
            {
                Debug.Log($"{name}: Tile occupied, stopping movement.");
                break;
            }

            MoveToTile(nextTile);
            Debug.Log($"{name}: Moved to {nextTile.GridPosition}");
        }

        if (TryAttack(player))
        {
            Debug.Log($"{name}: Attacked player for {AttackDamage} damage! Player HP: {player.CurrentHealth}/{player.MaxHealth}");
        }

        TurnManager.Instance.NextTurn();
    }

    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"{name} took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        Debug.Log($"{name} has been defeated!");
        GameManager.Instance.feedText.text = "Chaser was defeated!";
    }
}