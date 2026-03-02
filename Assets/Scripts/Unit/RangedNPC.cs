using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ranged NPC
// - Able to move in 2 spaces
// - Will move towards the player till they are 3 tiles away
// - Shoots the player if they are exactly 3 tiles away
// - Will retreat if the player is too close

public class RangedNPC : Unit
{
    [Header("Ranged Settings")]
    [SerializeField] private int _attackRange = 3;

    // Main turn logic for the Ranged NPC
    public override void TakeTurn()
    {
        PlayerUnit player = GameManager.Instance.Player;

        if (player == null || !player.IsAlive)
        {
            Debug.Log($"{name}: Player is gone, skipping turn.");
            TurnManager.Instance.NextTurn();
            return;
        }

        // Checks if the player is within shooting distance
        float distToPlayer = Vector2.Distance(GridPos, player.GridPos);

        if (distToPlayer == _attackRange)
        {
            Debug.Log($"{name}: Player at perfect range ({distToPlayer:F1} tiles), shooting!");
            TryRangedAttack(player);
        }

        // Retreats if player is too close
        else if (distToPlayer < _attackRange)
        {
            Debug.Log($"{name}: Player too close ({distToPlayer:F1} tiles), retreating 3 tiles!");

            bool retreatSuccessful = RetreatFromPlayer(player);

            // If retreat fails, shoots player as a last resort
            if (!retreatSuccessful)
            {
                Debug.Log($"{name}: Can't retreat! Shooting anyway!");
                TryRangedAttack(player);
            }
        }

        // Advances towards player if they are too far
        else
        {
            Debug.Log($"{name}: Player too far ({distToPlayer:F1} tiles), advancing 3 tiles!");
            AdvanceTowardPlayer(player);
        }

        TurnManager.Instance.NextTurn();
    }

    // Logic for attacking player
    private bool TryRangedAttack(Unit target)
    {
        if (target == null || !target.IsAlive) return false;

        float dist = Vector2.Distance(GridPos, target.GridPos);

        if (dist > _attackRange) return false;

        target.TakeDamage(AttackDamage);
        Debug.Log($"{name}: Ranged attack hit {target.name} for {AttackDamage} damage! HP: {target.CurrentHealth}/{target.MaxHealth}");
        GameManager.Instance.feedText.text = $"{name} attacked!";
        audioManager.PlaySFX(audioManager.shootSound);
        return true;
    }

    // Logic for moving towards player using pathfinding
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

    
    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"{name} took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        Debug.Log($"{name} has been defeated!");
        GameManager.Instance.feedText.text = "Ranged was defeated!";

        base.OnDeath();
    }
}