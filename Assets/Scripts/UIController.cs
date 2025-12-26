using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("HUD")]
    private TMP_Text _livesText;
    [SerializeField] private GameObject lives;

    private TMP_Text _coinsText;
    [SerializeField] private GameObject coins;

    private TMP_Text _waveText;
    [SerializeField] private GameObject waves;

    private TMP_Text _killsText;
    [SerializeField] private GameObject kills;

    private TMP_Text _alertText;
    [SerializeField] private GameObject alert;
    [SerializeField][Range(1f, 10f)] private float alertDuration = 3f;
    [Space]
    [SerializeField] private float alertHiddenY = 120f;   // off-screen / hidden Y
    [SerializeField] private float alertShownY = -120f;       // visible Y
    [SerializeField][Range(0.05f, 1f)] private float alertOpenTime = 0.2f;
    [SerializeField][Range(0.05f, 1f)] private float alertCloseTime = 0.2f;

    private RectTransform _alertRect;
    private Coroutine _alertRoutine;

    [Header("Buttons")]
    [SerializeField] private Button speed1Button;
    [SerializeField] private Button speed2Button;
    [SerializeField] private Button speed3Button;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button nextLevelButton;

    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = Color.blue;
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.white;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject missionCompletePanel;

    [Header("Tower Panel")]
    [SerializeField] private GameObject towerPanel;
    [SerializeField] private GameObject towerCardPrefab;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private TowerData[] towers;

    private Platform _currentPlatform;
    private readonly List<GameObject> activeCards = new();

    [Header("VFX")]
    [SerializeField] private ParticleSystem missionCompleteParticles;

    private UIState _state = UIState.Gameplay;
    private bool _missionCompleteSoundPlayed;

    #region Unity Lifecycle

    /// <summary>
    /// Initializes singleton instance and persists across scenes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Subscribes to gameplay events.
    /// </summary>
    private void OnEnable()
    {
        Spawner.OnWaveChanged += UpdateWaveText;
        Spawner.OnKillsChanged += UpdateKillsText;

        GameManager.OnLivesChanged += UpdateLivesText;
        GameManager.OnCoinsChanged += UpdateCoinsText;

        Platform.OnPlatformClicked += HandlePlatformClicked;
        TowerCard.OnTowerSelected += HandleTowerSelected;

        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawner.OnMissionComplete += ShowMissionComplete;
    }

    /// <summary>
    /// Unsubscribes from gameplay events.
    /// </summary>
    private void OnDisable()
    {
        Spawner.OnWaveChanged -= UpdateWaveText;
        Spawner.OnKillsChanged -= UpdateKillsText;

        GameManager.OnLivesChanged -= UpdateLivesText;
        GameManager.OnCoinsChanged -= UpdateCoinsText;

        Platform.OnPlatformClicked -= HandlePlatformClicked;
        TowerCard.OnTowerSelected -= HandleTowerSelected;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Spawner.OnMissionComplete -= ShowMissionComplete;
    }

    /// <summary>
    /// Binds UI button handlers.
    /// </summary>
    private void Start()
    {
        _alertText = alert.GetComponentInChildren<TMP_Text>();
        _alertRect = alert.GetComponent<RectTransform>();
        _alertRect.anchoredPosition = new Vector2(_alertRect.anchoredPosition.x, alertHiddenY);


        _livesText = lives.GetComponentInChildren<TMP_Text>();
        _coinsText = coins.GetComponentInChildren<TMP_Text>();

        _waveText = waves.GetComponentInChildren<TMP_Text>();
        _killsText = kills.GetComponentInChildren<TMP_Text>();

        BindSpeedButton(speed1Button, GameSpeedMode.Slow, AudioManager.Instance.PlaySpeedSlow);
        BindSpeedButton(speed2Button, GameSpeedMode.Normal, AudioManager.Instance.PlaySpeedNormal);
        BindSpeedButton(speed3Button, GameSpeedMode.Fast, AudioManager.Instance.PlaySpeedFast);

        HighlightSelectedSpeedButton(GameSpeed.FromTimeScale(GameManager.Instance.GameSpeed));
    }

    /// <summary>
    /// Handles pause hotkey (Escape) outside the main menu.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            SceneManager.GetActiveScene().name != "MainMenu")
        {
            TogglePause();
        }
    }

    #endregion

    #region Central UI State

    /// <summary>
    /// Applies a UI state: panel visibility + time scale + flags in one place.
    /// </summary>
    private void SetUIState(UIState newState)
    {
        if (_state == newState)
            return;

        _state = newState;

        // Panels
        if (towerPanel != null) towerPanel.SetActive(newState == UIState.SelectingTower);
        if (pausePanel != null) pausePanel.SetActive(newState == UIState.Paused);
        if (missionCompletePanel != null) missionCompletePanel.SetActive(newState == UIState.MissionComplete);
        if (gameOverPanel != null) gameOverPanel.SetActive(newState == UIState.GameOver);

        // External "panel open" flag (consider removing static later)
        Platform.towerPanelOpen = (newState == UIState.SelectingTower);

        // Time scale
        switch (newState)
        {
            case UIState.Gameplay:
                GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);
                break;

            case UIState.SelectingTower:
            case UIState.Paused:
            case UIState.MissionComplete:
            case UIState.GameOver:
                GameManager.Instance.SetTimeScale(0f);
                break;
        }
    }

    /// <summary>
    /// Returns true if gameplay should ignore "pause" input/actions.
    /// </summary>
    private bool IsBlockingPause() => _state == UIState.SelectingTower;

    #endregion

    #region HUD Updates

    private void UpdateWaveText(int currentWave)
    {
        if (_waveText == null) return;
        _waveText.text = currentWave.ToString();
    }

    private void UpdateKillsText(int currentKills)
    {
        if (_killsText == null) return;
        _killsText.text = currentKills.ToString();
    }

    private void UpdateLivesText(int currentLives)
    {
        if (_livesText != null)
        {
            _livesText.text = currentLives.ToString();
        }

        if (currentLives <= 0)
        {
            ShowGameOver();
        }
    }

    private void UpdateCoinsText(int currentCoins)
    {
        if (_coinsText == null) return;
        _coinsText.text = currentCoins.ToString();
    }

    #endregion

    #region Tower Placement Flow

    private void HandlePlatformClicked(Platform platform)
    {
        _currentPlatform = platform;
        ShowTowerPanel();
    }

    /// <summary>
    /// Opens tower selection and pauses gameplay.
    /// </summary>
    private void ShowTowerPanel()
    {
        PopulateTowerCards();
        SetUIState(UIState.SelectingTower);
        AudioManager.Instance.PlayPanelToggle();
    }

    /// <summary>
    /// Closes tower selection and returns to gameplay.
    /// </summary>
    public void HideTowerPanel()
    {
        if (_state == UIState.SelectingTower)
            SetUIState(UIState.Gameplay);
    }

    private void PopulateTowerCards()
    {
        ClearActiveCards();

        if (towers == null || towerCardPrefab == null || cardsContainer == null)
            return;

        foreach (var data in towers)
        {
            var cardGO = Instantiate(towerCardPrefab, cardsContainer);
            var card = cardGO.GetComponent<TowerCard>();
            card.Initialize(data);
            activeCards.Add(cardGO);
        }
    }

    private void HandleTowerSelected(TowerData towerData)
    {
        if (_currentPlatform == null)
        {
            HideTowerPanel();
            return;
        }

        if (_currentPlatform.transform.childCount > 0)
        {
            HideTowerPanel();
            StartCoroutine(ShowAlert("This platform already has a tower!"));
            return;
        }

        if (GameManager.Instance.Coins >= towerData.cost)
        {
            AudioManager.Instance.PlayTowerPlaced();
            GameManager.Instance.SpendCoins(towerData.cost);
            _currentPlatform.PlaceTower(towerData);
        }
        else
        {
            StartCoroutine(ShowAlert("Not enough Coins!"));
        }

        HideTowerPanel();
    }

    private IEnumerator ShowAlert(string message)
    {
        if (_alertText == null || _alertRect == null) yield break;

        // If alerts are spammed, restart cleanly.
        if (_alertRoutine != null)
            StopCoroutine(_alertRoutine);

        _alertRoutine = StartCoroutine(ShowAlertRoutine(message));
        yield break;
    }

    private IEnumerator ShowAlertRoutine(string message)
    {
        _alertText.text = message;
        AudioManager.Instance.PlayWarning();

        alert.SetActive(true);

        // Start from hidden position every time (prevents mid-animation states)
        SetAlertY(alertHiddenY);

        // Slide in
        yield return AnimateAlertY(alertHiddenY, alertShownY, alertOpenTime);

        // Stay visible (real-time, ignores timescale)
        yield return new WaitForSecondsRealtime(alertDuration);

        // Slide out
        yield return AnimateAlertY(alertShownY, alertHiddenY, alertCloseTime);

        alert.SetActive(false);
        _alertRoutine = null;
    }

    private IEnumerator AnimateAlertY(float fromY, float toY, float duration)
    {
        if (duration <= 0f)
        {
            SetAlertY(toY);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // works even when timeScale = 0
            float lerp = Mathf.Clamp01(t / duration);

            // Smooth step for nicer motion
            float eased = lerp * lerp * (3f - 2f * lerp);

            float y = Mathf.Lerp(fromY, toY, eased);
            SetAlertY(y);

            yield return null;
        }

        SetAlertY(toY);
    }

    private void SetAlertY(float y)
    {
        var pos = _alertRect.anchoredPosition;
        pos.y = y;
        _alertRect.anchoredPosition = pos;
    }

    private void ClearActiveCards()
    {
        for (int i = 0; i < activeCards.Count; i++)
            Destroy(activeCards[i]);

        activeCards.Clear();
    }

    #endregion

    #region Speed / Pause

    private void BindSpeedButton(Button button, GameSpeedMode mode, Action onSound)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            SetGameSpeed(mode);
            onSound?.Invoke();
            SetUIState(UIState.Gameplay);
        });
    }

    private void SetGameSpeed(GameSpeedMode mode)
    {
        HighlightSelectedSpeedButton(mode);

        float timeScale = GameSpeed.ToTimeScale(mode);
        GameManager.Instance.SetGameSpeed(timeScale);

        // If currently in gameplay, reflect speed immediately.
        if (_state == UIState.Gameplay)
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);
    }

    private void UpdateButtonVisual(Button button, bool isSelected)
    {
        if (button == null) return;

        button.image.color = isSelected ? selectedButtonColor : normalButtonColor;

        var text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.color = isSelected ? selectedTextColor : normalTextColor;
    }

    private void HighlightSelectedSpeedButton(GameSpeedMode mode)
    {
        UpdateButtonVisual(speed1Button, mode == GameSpeedMode.Slow);
        UpdateButtonVisual(speed2Button, mode == GameSpeedMode.Normal);
        UpdateButtonVisual(speed3Button, mode == GameSpeedMode.Fast);
    }

    /// <summary>
    /// Toggles between Paused and Gameplay. Does nothing while selecting a tower.
    /// </summary>
    public void TogglePause()
    {
        if (IsBlockingPause())
            return;

        if (_state == UIState.Paused)
        {
            SetUIState(UIState.Gameplay);
            AudioManager.Instance.PlayUnpause();
        }
        else if (_state == UIState.Gameplay)
        {
            SetUIState(UIState.Paused);
            AudioManager.Instance.PlayPause();
        }
    }

    #endregion

    #region Scene / Game State

    public void RestartLevel()
    {
        LevelManager.Instance.LoadLevel(LevelManager.Instance.CurrentLevel);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoToMainMenu()
    {
        // Going to menu should never be stuck paused.
        SetUIState(UIState.Gameplay);
        GameManager.Instance.SetTimeScale(1f);
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Shows game over UI and pauses via state.
    /// </summary>
    private void ShowGameOver()
    {
        SetUIState(UIState.GameOver);
        AudioManager.Instance.PlayGameOver();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var canvas = GetComponent<Canvas>();
        var camGO = GameObject.FindGameObjectWithTag("MainCamera");

        if (canvas != null && camGO != null && camGO.TryGetComponent(out Camera mainCamera))
            canvas.worldCamera = mainCamera;

        _missionCompleteSoundPlayed = false;
        SetUIState(UIState.Gameplay);

        if (scene.name == "MainMenu")
        {
            HideUI();
        }
        else
        {
            ShowUI();
            StartCoroutine(ShowAlert($"Defend {LevelManager.Instance.CurrentLevel.wavesToWin} waves!"));
        }
    }

    /// <summary>
    /// Shows mission complete UI once and pauses via state.
    /// </summary>
    private void ShowMissionComplete()
    {
        if (_missionCompleteSoundPlayed)
            return;

        UpdateNextLevelButton();

        SetUIState(UIState.MissionComplete);

        AudioManager.Instance.PlayMissionComplete();
        _missionCompleteSoundPlayed = true;

        if (missionCompleteParticles != null)
            missionCompleteParticles.Play();
    }

    public void EnterEndlessMode()
    {
        SetUIState(UIState.Gameplay);
        Spawner.Instance.EnableEndlessMode();
    }

    private void HideUI()
    {
        lives.SetActive(false);
        coins.SetActive(false);
        waves.SetActive(false);
        kills.SetActive(false);
        alert.SetActive(false);

        if (speed1Button != null) speed1Button.gameObject.SetActive(false);
        if (speed2Button != null) speed2Button.gameObject.SetActive(false);
        if (speed3Button != null) speed3Button.gameObject.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);

        // Also ensure gameplay panels are off in menu.
        if (towerPanel != null) towerPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void ShowUI()
    {
        lives.SetActive(true);
        coins.SetActive(true);
        waves.SetActive(true);
        kills.SetActive(true);


        if (speed1Button != null) speed1Button.gameObject.SetActive(true);
        if (speed2Button != null) speed2Button.gameObject.SetActive(true);
        if (speed3Button != null) speed3Button.gameObject.SetActive(true);

        HighlightSelectedSpeedButton(GameSpeed.FromTimeScale(GameManager.Instance.GameSpeed));

        if (pauseButton != null) pauseButton.gameObject.SetActive(true);
    }

    public void LoadNextLevel()
    {
        var levelManager = LevelManager.Instance;
        int currentIndex = Array.IndexOf(levelManager.allLevels, levelManager.CurrentLevel);
        int nextIndex = currentIndex + 1;

        if (nextIndex < levelManager.allLevels.Length)
            levelManager.LoadLevel(levelManager.allLevels[nextIndex]);
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null) return;

        var levelManager = LevelManager.Instance;
        int currentIndex = Array.IndexOf(levelManager.allLevels, levelManager.CurrentLevel);

        nextLevelButton.interactable = currentIndex + 1 < levelManager.allLevels.Length;
    }

    #endregion

    private static class GameSpeed
    {
        public const float Slow = 0.2f;
        public const float Normal = 1f;
        public const float Fast = 2f;

        public static float ToTimeScale(GameSpeedMode mode) => mode switch
        {
            GameSpeedMode.Slow => Slow,
            GameSpeedMode.Normal => Normal,
            GameSpeedMode.Fast => Fast,
            _ => Normal
        };

        public static GameSpeedMode FromTimeScale(float timeScale)
        {
            if (Mathf.Approximately(timeScale, Slow)) return GameSpeedMode.Slow;
            if (Mathf.Approximately(timeScale, Fast)) return GameSpeedMode.Fast;
            return GameSpeedMode.Normal;
        }
    }
}
