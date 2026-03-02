using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Unit Prefabs")]
    [SerializeField] private PlayerUnit _playerPrefab;
    [SerializeField] private ChaserNPC _chaserPrefab;
    [SerializeField] private PatrolNPC _patrolPrefab;
    [SerializeField] private RangedNPC _rangedPrefab;
    [SerializeField] private HealerNPC _healerPrefab;

    [Header("Spawn Counts")]
    [SerializeField][Range(0, 8)] private int _chaserCount = 1;
    [SerializeField][Range(0, 8)] private int _patrolCount = 1;
    [SerializeField][Range(0, 8)] private int _rangedCount = 1;
    [SerializeField][Range(0, 8)] private int _healerCount = 1;

    [Header("Turn Indicator")]
    [SerializeField] private GameObject _turnIndicatorObj;
    private CanvasGroup _turnIndicatorCanvasGroup;

    [Header("Feed")]
    [SerializeField] public TextMeshProUGUI feedText;

    public PlayerUnit Player { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;

        if (_turnIndicatorObj != null)
            _turnIndicatorCanvasGroup = _turnIndicatorObj.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        SpawnPlayer();
        SpawnNPCs(_chaserPrefab, _chaserCount, GetTopRightTiles(), "Chaser");
        SpawnPatrols();                                                           // Patrol needs InitPatrol() after InitOnTile()
        SpawnNPCs(_rangedPrefab, _rangedCount, GetTopLeftTiles(), "Ranged");
        SpawnNPCs(_healerPrefab, _healerCount, GetCenterTiles(), "Healer");

        _turnIndicatorObj.SetActive(false);
        feedText.text = string.Empty;

        TurnManager.Instance.StartGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }


    private void SpawnPlayer()
    {
        Tile spawnTile = GetFirstAvailableTile(GetBottomLeftTiles());

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

    private void SpawnNPCs(Unit prefab, int count, Vector2[] spawnArea, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"GameManager: No prefab assigned for {label}!");
            return;
        }

        int spawned = 0;
        foreach (Vector2 pos in spawnArea)
        {
            if (spawned >= count) break;

            Tile tile = GridManager.Instance.GetTileAtPosition(pos);
            if (tile == null || !tile.IsWalkable || tile.OccupiedUnit != null) continue;

            Unit npc = Instantiate(prefab);
            npc.name = $"{label}_{spawned + 1}";
            npc.InitOnTile(tile);
            TurnManager.Instance.RegisterUnit(npc);

            Debug.Log($"{npc.name} spawned at {tile.GridPosition}");
            spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"GameManager: Only spawned {spawned}/{count} {label}s — not enough walkable tiles in spawn area.");
    }

    private void SpawnPatrols()
    {
        if (_patrolPrefab == null)
        {
            Debug.LogWarning("GameManager: No prefab assigned for Patrol!");
            return;
        }

        Vector2[] spawnArea = GetBottomRightTiles();
        int spawned = 0;

        foreach (Vector2 pos in spawnArea)
        {
            if (spawned >= _patrolCount) break;

            Tile tile = GridManager.Instance.GetTileAtPosition(pos);
            if (tile == null || !tile.IsWalkable || tile.OccupiedUnit != null) continue;

            PatrolNPC patrol = Instantiate(_patrolPrefab);
            patrol.name = $"Patrol_{spawned + 1}";
            patrol.InitOnTile(tile);
            patrol.InitPatrol();                        
            TurnManager.Instance.RegisterUnit(patrol);

            Debug.Log($"{patrol.name} spawned at {tile.GridPosition}");
            spawned++;
        }

        if (spawned < _patrolCount)
            Debug.LogWarning($"GameManager: Only spawned {spawned}/{_patrolCount} Patrols — not enough walkable tiles in spawn area.");
    }


    private Vector2[] GetBottomLeftTiles()
    {
        return new Vector2[]
        {
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 2),
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 2),
            new Vector2(2, 0), new Vector2(2, 1), new Vector2(2, 2)
        };
    }

    private Vector2[] GetBottomRightTiles()
    {
        int w = GridManager.Instance.Width - 1;

        return new Vector2[]
        {
            new Vector2(w,     0), new Vector2(w,     1), new Vector2(w,     2),
            new Vector2(w - 1, 0), new Vector2(w - 1, 1), new Vector2(w - 1, 2),
            new Vector2(w - 2, 0), new Vector2(w - 2, 1), new Vector2(w - 2, 2)
        };
    }

    private Vector2[] GetTopRightTiles()
    {
        int w = GridManager.Instance.Width - 1;
        int h = GridManager.Instance.Height - 1;

        return new Vector2[]
        {
            new Vector2(w,     h),     new Vector2(w,     h - 1), new Vector2(w,     h - 2),
            new Vector2(w - 1, h),     new Vector2(w - 1, h - 1), new Vector2(w - 1, h - 2),
            new Vector2(w - 2, h),     new Vector2(w - 2, h - 1), new Vector2(w - 2, h - 2)
        };
    }

    private Vector2[] GetTopLeftTiles()
    {
        int h = GridManager.Instance.Height - 1;

        return new Vector2[]
        {
            new Vector2(0, h),     new Vector2(0, h - 1), new Vector2(0, h - 2),
            new Vector2(1, h),     new Vector2(1, h - 1), new Vector2(1, h - 2),
            new Vector2(2, h),     new Vector2(2, h - 1), new Vector2(2, h - 2)
        };
    }

    private Vector2[] GetCenterTiles()
    {
        int w = GridManager.Instance.Width - 1;
        int h = GridManager.Instance.Height - 1;
        int midW = w / 2;
        int midH = h / 2;

        return new Vector2[]
        {
            new Vector2(midW,     midH),     new Vector2(midW + 1, midH),     new Vector2(midW - 1, midH),
            new Vector2(midW,     midH + 1), new Vector2(midW + 1, midH + 1), new Vector2(midW - 1, midH + 1),
            new Vector2(midW,     midH - 1), new Vector2(midW + 1, midH - 1), new Vector2(midW - 1, midH - 1),
            new Vector2(midW + 2, midH),     new Vector2(midW - 2, midH),     new Vector2(midW,     midH + 2),
            new Vector2(midW,     midH - 2)
        };
    }

    private Tile GetFirstAvailableTile(Vector2[] positions)
    {
        foreach (Vector2 pos in positions)
        {
            Tile tile = GridManager.Instance.GetTileAtPosition(pos);
            if (tile != null && tile.IsWalkable && tile.OccupiedUnit == null)
                return tile;
        }
        return null;
    }

    public void ShowTurnIndicator()
    {
        if (_turnIndicatorObj == null)
        {
            Debug.LogWarning("GameManager: Turn indicator object not assigned!");
            return;
        }

        _turnIndicatorObj.SetActive(true);
        StartCoroutine(FadeInOutTurnIndicator());
    }

    private IEnumerator FadeInOutTurnIndicator()
    {
        float fadeInDuration = 0.2f;
        float displayDuration = 1f;
        float fadeOutDuration = 1f;

       
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (_turnIndicatorCanvasGroup != null)
                _turnIndicatorCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        if (_turnIndicatorCanvasGroup != null)
            _turnIndicatorCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (_turnIndicatorCanvasGroup != null)
                _turnIndicatorCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        _turnIndicatorObj.SetActive(false);
    }
}