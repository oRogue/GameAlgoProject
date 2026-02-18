using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Unit Prefabs")]
    [SerializeField] private PlayerUnit _playerPrefab;


    public PlayerUnit Player { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        SpawnPlayer();

        TurnManager.Instance.StartGame();
    }

    private void SpawnPlayer()
    {
        // will try to spawn the player in the bottom left of the grid in a 3x3 area
        Vector2[] bottomLeftTiles = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 2),
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 2),
            new Vector2(2, 0), new Vector2(2, 1), new Vector2(2, 2)
        };

        Tile spawnTile = null;
        foreach (Vector2 pos in bottomLeftTiles)
        {
            Tile candidate = GridManager.Instance.GetTileAtPosition(pos);
            if (candidate != null && candidate.IsWalkable && candidate.OccupiedUnit == null)
            {
                spawnTile = candidate;
                break;
            }
        }

        if (spawnTile == null)
        {
            Debug.LogError("GameManager: No walkable tile found in bottom-left region to spawn player!");
            return;
        }

        Player = Instantiate(_playerPrefab);
        Player.name = "Player";
        Player.InitOnTile(spawnTile);

        TurnManager.Instance.RegisterUnit(Player);

        Debug.Log($"Player spawned at {spawnTile.GridPosition}");
    }
}