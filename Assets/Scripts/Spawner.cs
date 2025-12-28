using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip("+X% per wave (increases Diffculty")]
    private float waveMultiplier = 0.4f;

    [Header("Pools")]
    [Header("Base")]
    [SerializeField] private ObjectPooler orcPool;
    [SerializeField] private ObjectPooler dragonPool;
    [SerializeField] private ObjectPooler kaijuPool;
    [Header("Main")]
    [SerializeField] private ObjectPooler tanGoblinPool;
    [SerializeField] private ObjectPooler mummyOrcPool;
    [SerializeField] private ObjectPooler fairyOrcPool;
    [SerializeField] private ObjectPooler vampirePool;
    [SerializeField] private ObjectPooler pixieZombiePool;
    [SerializeField] private ObjectPooler wingedRabbitPool;


    public static Spawner Instance { get; private set; }

    public static event Action<int> OnWaveChanged;
    public static event Action<int> OnKillsChanged;


    public static event Action OnMissionComplete;

    private WaveData[] _waves => LevelManager.Instance.CurrentLevel.waves;

    private int _kills = 0;
    public int Kills => _kills;

    private int _waveCounter = 0;
    private int _currentWaveIndex = 0;
    private WaveData CurrentWave => _waves[_currentWaveIndex];

    private float _spawnTimer;
    private float _spawnCounter;
    private int _enemiesRemoved;


    private Dictionary<EnemyType, ObjectPooler> _poolDictionary;

    private float _timeBetweenWaves = 1f;
    private float _waveCooldown;
    private bool _isBetweenWaves = false;
    private bool _isEndlessMode = false;
    private bool _isGamePlayScene = false;

    private Path _currentPath;

    private void Awake()
    {
        _poolDictionary = new Dictionary<EnemyType, ObjectPooler>()
        {
            { EnemyType.Orc, orcPool},
            { EnemyType.Dragon, dragonPool},
            { EnemyType.Kaiju, kaijuPool},
            { EnemyType.TanGoblin, tanGoblinPool},
            { EnemyType.MummyOrc, mummyOrcPool},
            { EnemyType.FairyOrc, fairyOrcPool},
            { EnemyType.Vampire, vampirePool},
            { EnemyType.PixieZombie, pixieZombiePool},
            { EnemyType.WingedRabbit, wingedRabbitPool},
        };

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        OnWaveChanged?.Invoke(_waveCounter);
    }

    void Update()
    {
        if (!_isGamePlayScene) return;

        if (_isBetweenWaves)
        {
            _waveCooldown -= Time.deltaTime;
            if (_waveCooldown <= 0f)
            {
                _currentWaveIndex = (_currentWaveIndex + 1) % _waves.Length;
                _waveCounter++;
                OnWaveChanged?.Invoke(_waveCounter);
                AudioManager.Instance.PlaySound(CurrentWave.waveSpawnClip);
                _spawnCounter = 0;
                _enemiesRemoved = 0;
                _spawnTimer = 0f;
                _isBetweenWaves = false;
            }
        }
        else
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0 && _spawnCounter < CurrentWave.enemiesPerWave)
            {
                _spawnTimer = CurrentWave.spawnInterval;
                SpawnEnemy();
                _spawnCounter++;
            }
            else if (_spawnCounter >= CurrentWave.enemiesPerWave && _enemiesRemoved >= CurrentWave.enemiesPerWave)
            {
                if (_waveCounter + 1 >= LevelManager.Instance.CurrentLevel.wavesToWin && !_isEndlessMode && GameManager.Instance.Lives > 0)
                {
                    OnMissionComplete?.Invoke();
                }
                else
                {
                    _isBetweenWaves = true;
                    _waveCooldown = _timeBetweenWaves;
                }
            }
        }
    }

    private void SpawnEnemy()
    {
        if (_poolDictionary.TryGetValue(CurrentWave.enemyType, out var pool))
        {
            GameObject spawnedObject = pool.GetPooledObject();
            spawnedObject.transform.position = transform.position;

            float healthMultiplier = 1f + (_waveCounter * waveMultiplier);
            Enemy enemy = spawnedObject.GetComponent<Enemy>();
            enemy.Initialize(_currentPath, healthMultiplier);

            spawnedObject.SetActive(true);
        }
    }

    private void HandleEnemyReachedEnd(EnemyData data, int damage)
    {
        _enemiesRemoved++;
    }

    private void HandleEnemyDestroyed(Enemy enemy, float reward)
    {
        _enemiesRemoved++;
        _kills++;
        OnKillsChanged?.Invoke(_kills);
    }

    public void EnableEndlessMode()
    {
        _isEndlessMode = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isGamePlayScene = scene.name != "MainMenu";
        ResetWaveState();

        if (!_isGamePlayScene) return;

        _currentPath = GameObject.Find("Path1").GetComponent<Path>();
        AudioManager.Instance.PlaySound(CurrentWave.waveSpawnClip);

        if (LevelManager.Instance.CurrentLevel != null)
        {
            transform.position = LevelManager.Instance.CurrentLevel.initialSpawnPosition;
        }
    }

    private void ResetWaveState()
    {
        _currentWaveIndex = 0;
        _waveCounter = 0;
        OnWaveChanged?.Invoke(_waveCounter);
        _spawnCounter = 0;
        _enemiesRemoved = 0;
        _spawnTimer = 0f;
        _isBetweenWaves = false;
        _isEndlessMode = false;

        _kills = 0;
        OnKillsChanged?.Invoke(_kills);

        foreach (var pool in _poolDictionary.Values)
        {
            if (pool != null)
            {
                pool.ResetPool();
            }
        }
    }
}
