using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Unit : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int _maxHealth = 30;
    [SerializeField] private int _attackDamage = 10;
    [SerializeField] private int _moveRange = 3;     // Max tiles to move per turn

    [SerializeField] private string _healthbarName;
    [SerializeField] private string _healthScoreName;
    private Slider _healthbar;
    [HideInInspector] public TextMeshProUGUI healthScore;

    public AudioManager audioManager;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth { get; private set; }
    public int AttackDamage => _attackDamage;
    public int MoveRange => _moveRange;
    public bool IsAlive => CurrentHealth > 0;
    public Tile OccupiedTile { get; private set; }
    public Vector2 GridPos => OccupiedTile != null ? OccupiedTile.GridPosition : Vector2.zero;

    protected virtual void Awake()
    {
        CurrentHealth = _maxHealth;

        FindHealthBar();

        healthScore.gameObject.SetActive(false);

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    private void FindHealthBar()
    {
        GameObject healthbarObj = GameObject.Find(_healthbarName);

        if (healthbarObj != null)
        {
            _healthbar = healthbarObj.GetComponent<Slider>();
            if (_healthbar != null)
            {
                _healthbar.maxValue = 1f;
                _healthbar.value = 1f;
                Debug.Log($"Unit {name} found health bar: {healthbarObj.name}");
            }
        }
        else
        {
            Debug.LogError($"Unit {name} could not find any health bar in scene!");
        }

        GameObject healthScoreObj = GameObject.Find(_healthScoreName);

        if (healthScoreObj != null)
        {
            healthScore = healthScoreObj.GetComponent<TextMeshProUGUI>();
        }

    }

    public void InitOnTile(Tile tile)
    {
        if (tile == null) return;

        OccupiedTile = tile;
        tile.OccupiedUnit = this;
        transform.position = new Vector3(tile.GridPosition.x, tile.GridPosition.y, -1f);
    }


    public void MoveToTile(Tile newTile)
    {
        if (newTile == null || !newTile.IsWalkable) return;
        if (newTile.OccupiedUnit != null) return;  // Tile already taken

        // Vacate old tile
        if (OccupiedTile != null)
            OccupiedTile.OccupiedUnit = null;

        // Occupy new tile
        OccupiedTile = newTile;
        newTile.OccupiedUnit = this;
        transform.position = new Vector3(newTile.GridPosition.x, newTile.GridPosition.y, -1f);

        if (!(this is PlayerUnit))
        {
            audioManager.PlaySFX(audioManager.enemyMoveSound);
        }

        GameManager.Instance.feedText.text = $"{name} moved.";
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        _healthbar.value = (float)CurrentHealth / MaxHealth;

        OnDamageTaken(damage);
        DisplayHealthScoreText(-damage, Color.red); // Red for damage

        audioManager.PlaySFX(audioManager.damageSound);

        if (!IsAlive)
            OnDeath();
    }

    public bool TryAttack(Unit target)
    {
        if (target == null || !target.IsAlive) return false;

        float dist = Vector2.Distance(GridPos, target.GridPos);
        if (dist > 1.5f) return false;  // Must be adjacent (horizontal/vertical)

        target.TakeDamage(_attackDamage);
        audioManager.PlaySFX(audioManager.attackSound);
        GameManager.Instance.feedText.text = $"{name} attacked!";
        return true;
    }

    protected virtual void OnDamageTaken(int damage) { }
    protected virtual void OnDeath()
    {
        healthScore.gameObject.SetActive(false);

        // Vacate tile and disable
        if (OccupiedTile != null)
            OccupiedTile.OccupiedUnit = null;

        gameObject.SetActive(false);
    }

    public abstract void TakeTurn();

    public void Heal(int amount)
    {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        _healthbar.value = (float)CurrentHealth / MaxHealth;

        DisplayHealthScoreText(amount, Color.green);

        Debug.Log($"{name} was healed for {amount}. HP: {CurrentHealth}/{MaxHealth}");

        audioManager.PlaySFX(audioManager.healSound);
    }

    public void DisplayHealthScoreText(int value, Color textColor)
    {
        // Format text based on positive/negative value
        if (value > 0)
        {
            healthScore.text = "+ " + value; // Healing
        }
        else
        {
            healthScore.text = "- " + Mathf.Abs(value); // Damage
        }

        healthScore.gameObject.SetActive(true);

        StartCoroutine(TextFadeOut(textColor));
    }

    private IEnumerator TextFadeOut(Color textColor)
    {
        float duration = 1f;
        float elapsed = 0f;

        // Set the initial color (with full alpha)
        healthScore.color = new Color(
            textColor.r,
            textColor.g,
            textColor.b,
            1f
        );

        yield return new WaitForSeconds(0.3f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            healthScore.color = new Color(
                textColor.r,
                textColor.g,
                textColor.b,
                alpha
            );

            yield return null;
        }

        healthScore.gameObject.SetActive(false);
    }

    public bool RetreatFromPlayer(PlayerUnit player)
    {
        Vector2 fleeDirection = (GridPos - player.GridPos).normalized;

        Tile bestRetreatTile = FindFarthestRetreatTile(fleeDirection, MoveRange);

        if (bestRetreatTile != null && bestRetreatTile != OccupiedTile)
        {
            List<Tile> path = Pathfinder.FindPath(OccupiedTile, bestRetreatTile);

            if (path.Count > 0)
            {
                int stepsToTake = Mathf.Min(MoveRange, path.Count);

                for (int i = 0; i < stepsToTake; i++)
                {
                    Tile nextTile = path[i];

                    if (nextTile.OccupiedUnit != null)
                    {
                        Debug.Log($"{name}: Retreat path blocked at step {i + 1}, stopping.");
                        break;
                    }

                    MoveToTile(nextTile);
                }

                float newDist = Vector2.Distance(GridPos, player.GridPos);
                Debug.Log($"{name}: Retreated to {OccupiedTile.GridPosition} (now {newDist:F1} tiles away)");
                GameManager.Instance.feedText.text = $"{name} retreated!";
                audioManager.PlaySFX(audioManager.enemyMoveSound);
                return true;
            }
        }

        Debug.Log($"{name}: No valid retreat path found!");
        audioManager.PlaySFX(audioManager.enemyNotMoveSound);
        GameManager.Instance.feedText.text = $"{name} stood their ground.";
        return false;
    }

    private Tile FindFarthestRetreatTile(Vector2 direction, int maxSteps)
    {
        Tile bestRetreatTile = null;
        float bestDistFromPlayer = 0f;

        // Try straight back first (best option)
        for (int dist = maxSteps; dist >= 1; dist--)
        {
            Vector2 targetPos = GridPos + direction * dist;
            Tile tile = GridManager.Instance.GetTileAtPosition(targetPos);

            if (tile != null && tile.IsWalkable && tile.OccupiedUnit == null)
            {
                // Check if we can actually path to it
                List<Tile> testPath = Pathfinder.FindPath(OccupiedTile, tile);
                if (testPath.Count > 0)
                {
                    return tile; // Found a clear path straight back
                }
            }
        }
        // Straight back is blocked, try diagonal/perpendicular retreat
        Vector2[] alternateDirections = new Vector2[]
        {
            (direction + new Vector2(-direction.y, direction.x)).normalized,  // diagonal-right back
            (direction + new Vector2(direction.y, -direction.x)).normalized,  // diagonal-left back
            new Vector2(-direction.y, direction.x),   // perpendicular right
            new Vector2(direction.y, -direction.x)    // perpendicular left
        };

        foreach (Vector2 altDir in alternateDirections)
        {
            for (int dist = maxSteps; dist >= 1; dist--)
            {
                Vector2 targetPos = GridPos + altDir * dist;
                Tile tile = GridManager.Instance.GetTileAtPosition(targetPos);

                if (tile != null && tile.IsWalkable && tile.OccupiedUnit == null)
                {
                    // Check if we can actually path to it
                    List<Tile> testPath = Pathfinder.FindPath(OccupiedTile, tile);

                    if (testPath.Count > 0)
                    {
                        float distFromPlayer = Vector2.Distance(tile.GridPosition, GameManager.Instance.Player.GridPos);

                        // Pick the tile that puts us farthest from the player
                        if (distFromPlayer > bestDistFromPlayer)
                        {
                            bestDistFromPlayer = distFromPlayer;
                            bestRetreatTile = tile;
                        }
                    }
                }
            }
        }

        return bestRetreatTile;
    }
}