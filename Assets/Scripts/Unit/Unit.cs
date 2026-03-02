using System.Collections;
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

    AudioManager audioManager;

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
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        _healthbar.value = (float)CurrentHealth / MaxHealth;

        OnDamageTaken(damage);
        DisplayHealthScoreText(-damage, Color.red); // Red for damage

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
        return true;
    }

    protected virtual void OnDamageTaken(int damage) { }
    protected virtual void OnDeath()
    {
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
}