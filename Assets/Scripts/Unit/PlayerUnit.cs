using UnityEngine;
using TMPro;
using System.Collections;

/*
Player-controlled unit.
WASD to move up to 3 tiles per turn
Walking into an enemy tile attacks that enemy (bump-to-attack)
Space to end turn early
*/
public class PlayerUnit : Unit
{
    private bool _isTurn = false;
    private int _movesRemaining = 0;

    protected override void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (!_isTurn || !IsAlive) return;

        HandleMovementInput();
        HandleEndTurnInput();
    }

    public override void TakeTurn()
    {
        _isTurn = true;
        _movesRemaining = MoveRange; 
        Debug.Log($"Player's turn — {_movesRemaining} moves remaining. WASD to move/attack, Space to end turn.");
        GameManager.Instance.ShowTurnIndicator();
    }

    private void HandleMovementInput()
    {
        if (_movesRemaining <= 0)
        {
            Debug.Log("No moves remaining — press Space to end turn.");
            EndTurn();
            return;
        }

        Vector2 direction = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.W)) direction = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S)) direction = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.A)) direction = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D)) direction = Vector2.right;
        else return; // No key pressed

        Vector2 targetPos = GridPos + direction;
        Tile targetTile = GridManager.Instance.GetTileAtPosition(targetPos);

        if (targetTile == null)
        {
            Debug.Log("Can't move there — out of bounds.");
            return;
        }

        if (!targetTile.IsWalkable)
        {
            Debug.Log("Can't move there — tile is blocked.");
            return;
        }

    
        if (targetTile.OccupiedUnit != null)
        {
            Unit target = targetTile.OccupiedUnit;

            if (TryAttack(target))
            {
                Debug.Log($"Player attacked {target.name} for {AttackDamage} damage! {target.name} HP: {target.CurrentHealth}/{target.MaxHealth}");
                _movesRemaining--;  // Attacking costs a move
            }
            return;
        }

 
        MoveToTile(targetTile);
        _movesRemaining--;
        Debug.Log($"Player moved to {targetPos}. Moves remaining: {_movesRemaining}");

        audioManager.PlaySFX(audioManager.moveSound);

        if (_movesRemaining <= 0)
        {
            Debug.Log("No moves remaining — ending turn automatically.");
            EndTurn();
        }
    }

    private void HandleEndTurnInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Player ended their turn early.");
            GameManager.Instance.feedText.text = "Player ended their turn.";
            EndTurn();
        }
    }

    private void EndTurn()
    {
        _isTurn = false;
        _movesRemaining = 0;
        TurnManager.Instance.NextTurn();
    }

    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"Player took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        Debug.Log("Player has been defeated — Game Over!");
        // TODO: Hook into UI manager to show game over screen
    }
}