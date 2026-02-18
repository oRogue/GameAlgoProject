using UnityEngine;


// Use WASD to move
// Press space to end turn
public class PlayerUnit : Unit
{
    private bool _isTurn = false;

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
        Debug.Log("Player's turn, use WASD to move, Space to end turn.");
    }

    public void EndTurn()
    {
        _isTurn = false;
        TurnManager.Instance.NextTurn();
    }

    private void HandleMovementInput()
    {
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
            Debug.Log("Can't move there — tile is occupied.");
            return;
        }

        MoveToTile(targetTile);
        Debug.Log($"Player moved to {targetPos}");
    }

    private void HandleEndTurnInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Player ended their turn.");
            EndTurn();
        }
    }

    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"Player took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        Debug.Log("Player has died. Game Over!");
        // TODO: Trigger game over screen via GameManager
    }
}