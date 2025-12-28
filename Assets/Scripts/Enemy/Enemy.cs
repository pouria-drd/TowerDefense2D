using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;

    public static event Action<EnemyData, int> OnEnemyReachedEnd;
    public static event Action<Enemy, float> OnEnemyDestroyed;

    private Path _currentPath;
    private Vector3 _targetPosition;
    public Vector3 CurrentTargetPosition => _targetPosition;
    private int _currentWaypoint;
    private Vector3 _offset;

    private int _damge;
    private float _speed;
    private float _lives;
    private float _reward;
    private float _maxLives;

    [SerializeField] private Transform healthBar;
    private Vector3 _healthBarOriginalScale;

    private bool _hasBeenCounted = false;

    private void Awake()
    {
        _healthBarOriginalScale = healthBar.localScale;
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        if (_hasBeenCounted) return;

        // move towards target position
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);

        // when target reached, set new target position
        float relativeDistance = (transform.position - _targetPosition).magnitude;
        if (relativeDistance < 0.1f)
        {
            if (_currentWaypoint < _currentPath.Waypoints.Length - 1)
            {
                _currentWaypoint++;
                _targetPosition = _currentPath.GetPosition(_currentWaypoint) + _offset;
            }
            else // reached last waypoint
            {
                _hasBeenCounted = true;
                OnEnemyReachedEnd?.Invoke(data, _damge);
                gameObject.SetActive(false);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (_hasBeenCounted) return;

        _lives -= damage;
        _lives = Math.Max(_lives, 0);
        UpdateHealthBar();

        if (_lives <= 0)
        {
            AudioManager.Instance.PlayEnemyDestroyed();
            _hasBeenCounted = true;
            OnEnemyDestroyed?.Invoke(this, _reward);
            gameObject.SetActive(false);
        }
    }

    private void UpdateHealthBar()
    {
        float healthPercent = _lives / _maxLives;
        Vector3 scale = _healthBarOriginalScale;
        scale.x = _healthBarOriginalScale.x * healthPercent;
        healthBar.localScale = scale;
    }

    public void Initialize(Path path, float waveMultiplier)
    {
        _currentPath = path;
        _currentWaypoint = 0;

        _targetPosition = _currentPath.GetPosition(_currentWaypoint) + _offset;
        _hasBeenCounted = false;

        _maxLives = data.lives * waveMultiplier;
        _lives = _maxLives;
        UpdateHealthBar();

        _damge = Mathf.RoundToInt(data.damage * waveMultiplier);

        _reward = data.coinReward * waveMultiplier;

        _speed = UnityEngine.Random.Range(data.minSpeed, data.maxSpeed);

        float offsetX = UnityEngine.Random.Range(-data.offsetX, data.offsetX);
        float offsetY = UnityEngine.Random.Range(-data.offsetY, data.offsetY);
        _offset = new Vector2(offsetX, offsetY);
    }
}
