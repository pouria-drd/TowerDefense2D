using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<int> OnLivesChanged;
    public static event Action<int> OnCoinsChanged;

    [SerializeField] private TMP_FontAsset globalFont;

    private int _lives = 5;
    public int Lives => _lives;
    private int _coins = 175;
    public int Coins => _coins;

    private float _gameSpeed = 1f;
    public float GameSpeed => _gameSpeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ApplyGlobalFont();
        }
    }

    private void OnEnable()
    {
        Enemy.OnEnemyReachedEnd += HandleEnemyReachedEnd;
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd;
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        OnLivesChanged?.Invoke(_lives);
        OnCoinsChanged?.Invoke(_coins);
    }

    private void HandleEnemyReachedEnd(EnemyData data, int damage)
    {
        _lives = Mathf.Max(0, _lives - damage);
        OnLivesChanged?.Invoke(_lives);
    }

    private void HandleEnemyDestroyed(Enemy enemy, float reward)
    {
        AddCoins(Mathf.RoundToInt(reward));
    }

    private void AddCoins(int amount)
    {
        _coins += amount;
        OnCoinsChanged?.Invoke(_coins);
    }

    // for pausing/unpausing, UI needs
    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }

    // for game speed buttons
    public void SetGameSpeed(float newSpeed)
    {
        _gameSpeed = newSpeed;
        SetTimeScale(_gameSpeed);
    }

    public void SpendCoins(int amount)
    {
        if (_coins >= amount)
        {
            _coins -= amount;
            OnCoinsChanged?.Invoke(_coins);
        }
    }

    public void ResetGameState()
    {
        _lives = LevelManager.Instance.CurrentLevel.startingLives;
        OnLivesChanged?.Invoke(_lives);
        _coins = LevelManager.Instance.CurrentLevel.startingCoins;
        OnCoinsChanged?.Invoke(_coins);

        SetGameSpeed(1f);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            AudioManager.Instance.PlayMusic(AudioManager.Instance.mainMenuMusic);
        }
        else if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
        {
            ResetGameState();
            AudioManager.Instance.PlayMusic(AudioManager.Instance.gameplayMusic);
        }
    }

    private void ApplyGlobalFont()
    {
        foreach (var tmp in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            tmp.font = globalFont;
        }
    }
}
