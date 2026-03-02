using System.Collections.Generic;
using UnityEngine;

public class PatrolNPC : Unit
{
    [Header("Patrol Settings")]
    [SerializeField] private int _detectionRange = 4; 

    private List<Tile> _patrolRoute = new List<Tile>();
    private int _patrolIndex = 0;  

    /// State machine
    private enum State { Patrol, Chase }
    private State _state = State.Patrol;

    /// The tile this NPC spawned on — used as the centre of the patrol circle
    private Vector2 _spawnCenter;

    protected override void Awake()
    {
        base.Awake();
    }

    public void InitPatrol()
    {
        _spawnCenter = GridPos;
        BuildPatrolRoute();

        if (_patrolRoute.Count == 0)
            Debug.LogWarning($"{name}: No valid patrol tiles found around {_spawnCenter}.");
        else
            Debug.Log($"{name}: Patrol route built with {_patrolRoute.Count} waypoints.");
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

        
        float distToPlayer = Vector2.Distance(GridPos, player.GridPos);

        if (distToPlayer <= _detectionRange)
            _state = State.Chase;
        else
            _state = State.Patrol;

        Debug.Log($"{name}: State = {_state}, Player distance = {distToPlayer}");

        switch (_state)
        {
            case State.Patrol: DoPatrol(); break;
            case State.Chase: DoChase(player); break;
        }
    }


    private void DoPatrol()
    {
        if (_patrolRoute.Count == 0)
        {
            Debug.Log($"{name}: No patrol route, skipping.");
            TurnManager.Instance.NextTurn();
            return;
        }

        
        Tile waypoint = GetNextWaypoint();

        if (waypoint == null)
        {
            Debug.Log($"{name}: Waypoint occupied or unreachable, waiting.");
            TurnManager.Instance.NextTurn();
            return;
        }

    
        List<Tile> path = Pathfinder.FindPath(OccupiedTile, waypoint);

        if (path.Count == 0)
        {
            Debug.Log($"{name}: No path to waypoint, advancing to next.");
            AdvancePatrolIndex();
            TurnManager.Instance.NextTurn();
            return;
        }

        MoveAlongPath(path);

        
        if (OccupiedTile == waypoint)
            AdvancePatrolIndex();

        TurnManager.Instance.NextTurn();
    }

    private void DoChase(PlayerUnit player)
    {
        
        if (TryAttack(player))
        {
            Debug.Log($"{name}: Attacked player for {AttackDamage} damage! Player HP: {player.CurrentHealth}/{player.MaxHealth}");
            TurnManager.Instance.NextTurn();
            return;
        }

        
        List<Tile> path = Pathfinder.FindPath(OccupiedTile, player.OccupiedTile);

        if (path.Count == 0)
        {
            Debug.Log($"{name}: No path to player.");
            TurnManager.Instance.NextTurn();
            return;
        }

        MoveAlongPath(path);

       
        if (TryAttack(player))
            Debug.Log($"{name}: Attacked player for {AttackDamage} damage! Player HP: {player.CurrentHealth}/{player.MaxHealth}");

        TurnManager.Instance.NextTurn();
    }

 
    private void BuildPatrolRoute()
    {
        _patrolRoute.Clear();

 
        Vector2[] clockwiseOffsets = new Vector2[]
        {
            new Vector2( 0,  2),  // Top
            new Vector2( 1,  2),
            new Vector2( 2,  1),  // Top-right
            new Vector2( 2,  0),  // Right
            new Vector2( 2, -1),
            new Vector2( 1, -2),  // Bottom-right
            new Vector2( 0, -2),  // Bottom
            new Vector2(-1, -2),
            new Vector2(-2, -1),  // Bottom-left
            new Vector2(-2,  0),  // Left
            new Vector2(-2,  1),
            new Vector2(-1,  2),  // Top-left
        };

        foreach (Vector2 offset in clockwiseOffsets)
        {
            Vector2 pos = _spawnCenter + offset;
            Tile tile = GridManager.Instance.GetTileAtPosition(pos);

            if (tile != null && tile.IsWalkable)
                _patrolRoute.Add(tile);
        }
    }

 
    private Tile GetNextWaypoint()
    {
        // Try up to a full loop to find a free waypoint
        for (int i = 0; i < _patrolRoute.Count; i++)
        {
            int index = (_patrolIndex + i) % _patrolRoute.Count;
            Tile waypoint = _patrolRoute[index];

            if (waypoint.OccupiedUnit == null || waypoint == OccupiedTile)
            {
                _patrolIndex = index;
                return waypoint;
            }
        }

        return null; 
    }

    private void AdvancePatrolIndex()
    {
        _patrolIndex = (_patrolIndex + 1) % _patrolRoute.Count;
    }

    private void MoveAlongPath(List<Tile> path)
    {
        int steps = Mathf.Min(MoveRange, path.Count);

        for (int i = 0; i < steps; i++)
        {
            Tile next = path[i];

            if (next.OccupiedUnit != null)
            {
                Debug.Log($"{name}: Tile {next.GridPosition} occupied, stopping.");
                break;
            }

            MoveToTile(next);
            Debug.Log($"{name}: Moved to {next.GridPosition}");
        }
    }


    protected override void OnDamageTaken(int damage)
    {
        Debug.Log($"{name} took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");
    }

    protected override void OnDeath()
    {
        Debug.Log($"{name} has been defeated!");
        GameManager.Instance.feedText.text = "Patrol was defeated!";
        base.OnDeath();
    }
}