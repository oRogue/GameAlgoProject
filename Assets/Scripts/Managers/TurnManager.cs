using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private List<Unit> _turnOrder = new List<Unit>();

    private int _currentTurnIndex = 0;

    private bool _isWaiting = false;

    [SerializeField] private float _turnDelay = 0.5f;

    public bool GameActive { get; private set; } = false;

    AudioManager audioManager;

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

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }


    public void RegisterUnit(Unit unit)
    {
        if (unit != null && !_turnOrder.Contains(unit))
            _turnOrder.Add(unit);
    }


    public void StartGame()
    {
        if (_turnOrder.Count == 0)
        {
            Debug.LogError("TurnManager: No units registered!");
            return;
        }

        GameActive = true;
        _currentTurnIndex = 0;
        Debug.Log("Game started!");
        StartCurrentTurn();
    }

    public void NextTurn()
    {
        if (!GameActive || _isWaiting) return;

        StartCoroutine(DelayedNextTurn());
    }

    public IEnumerator DelayedNextTurn()
    {
        _isWaiting = true;

        yield return new WaitForSeconds(_turnDelay);

        // Check win/lose before advancing
        if (CheckGameOver())
        {
            _isWaiting = false;
            yield break;
        }

        // Advance to next alive unit, skipping dead ones
        int attempts = 0;
        do
        {
            _currentTurnIndex = (_currentTurnIndex + 1) % _turnOrder.Count;
            attempts++;

            // Safety: if we've looped through everyone and no one is alive, stop
            if (attempts > _turnOrder.Count)
            {
                Debug.LogError("TurnManager: No alive units found!");
                _isWaiting = false;
                yield break;
            }

        } while (!_turnOrder[_currentTurnIndex].IsAlive);

        _isWaiting = false;
        StartCurrentTurn();
    }

    private void StartCurrentTurn()
    {
        Unit current = _turnOrder[_currentTurnIndex];

        if (!current.IsAlive)
        {
            NextTurn();
            return;
        }

        Debug.Log($"--- {current.name}'s turn ---");
        current.TakeTurn();
    }

    private bool CheckGameOver()
    {
        // Find player unit
        Unit player = _turnOrder.Find(u => u is PlayerUnit);

        if (player == null || !player.IsAlive)
        {
            EndGame(playerWon: false);
            return true;
        }

        // Check if all NPCs are dead
        bool anyNPCAlive = _turnOrder.Exists(u => !(u is PlayerUnit) && u.IsAlive);

        if (!anyNPCAlive)
        {
            EndGame(playerWon: true);
            return true;
        }

        return false;
    }

    private void EndGame(bool playerWon)
    {
        GameActive = false;

        if (playerWon)
        {
            Debug.Log("All NPCs defeated — Player wins!");
            audioManager.PlaySFX(audioManager.winSound);
            GameManager.Instance.endText.text = "You Win!";
        }
        else
        {
            Debug.Log("Player has been defeated — Game Over!");
            audioManager.PlaySFX(audioManager.loseSound);
            GameManager.Instance.endText.text = "You Lost!";
        }

        GameManager.Instance.ShowEndScreen();
    }
}