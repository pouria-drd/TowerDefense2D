using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const string QUALITY_PREF_KEY = "GraphicsQuality";

    [Header("Main Menu")]
    [SerializeField] private TMP_Text gameName;
    [SerializeField] private TMP_Text gameVesrion;
    [SerializeField] private GameObject mainMenu;

    [Header("Settings Menu")]
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private TMP_Dropdown graphicsDropdown;

    [Header("Audio")]
    [SerializeField] private Button sfxToggleButton;
    [SerializeField] private Button musicToggleButton;

    private TMP_Text _musicStatusText; // e.g. "Music: ON"
    private TMP_Text _sfxStatusText;   // e.g. "SFX: ON"

    private void Awake()
    {
        // Auto-fill dropdown with Quality settings names
        if (graphicsDropdown != null)
        {
            graphicsDropdown.ClearOptions();
            graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
        }

        _sfxStatusText = sfxToggleButton.GetComponentInChildren<TMP_Text>();
        _musicStatusText = musicToggleButton.GetComponentInChildren<TMP_Text>();
    }

    private void Start()
    {
        if (gameName != null)
            gameName.text = Application.productName;

        if (gameVesrion != null)
            gameVesrion.text = $"Version {Application.version}";

        if (mainMenu != null) mainMenu.SetActive(true);
        if (settingsMenu != null) settingsMenu.SetActive(false);

        LoadGraphicsQuality();

        // Hook button clicks (optional if you prefer Inspector OnClick)
        if (musicToggleButton != null)
            musicToggleButton.onClick.AddListener(ToggleMusicFromUI);

        if (sfxToggleButton != null)
            sfxToggleButton.onClick.AddListener(ToggleSfxFromUI);

        // Make sure UI shows saved values immediately
        RefreshAudioUI();
    }

    public void StartNewGame()
    {
        LevelManager.Instance.LoadLevel(LevelManager.Instance.allLevels[0]);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameplayMusic();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowSettingsMenu()
    {
        if (mainMenu != null) mainMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(true);

        RefreshAudioUI(); // show correct ON/OFF when opening settings
    }

    public void ShowMainMenu()
    {
        if (mainMenu != null) mainMenu.SetActive(true);
        if (settingsMenu != null) settingsMenu.SetActive(false);
    }

    /// <summary>
    /// Called by the Graphics dropdown OnValueChanged.
    /// Saves and applies the selected quality level.
    /// </summary>
    public void ChangeGraphicsQuality()
    {
        if (graphicsDropdown == null) return;

        int qualityIndex = Mathf.Clamp(graphicsDropdown.value, 0, QualitySettings.names.Length - 1);

        QualitySettings.SetQualityLevel(qualityIndex);

        PlayerPrefs.SetInt(QUALITY_PREF_KEY, qualityIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads saved quality (or uses current quality on first launch),
    /// applies it, and syncs the dropdown.
    /// </summary>
    private void LoadGraphicsQuality()
    {
        if (graphicsDropdown == null || QualitySettings.names.Length == 0)
            return;

        int qualityIndex;

        if (PlayerPrefs.HasKey(QUALITY_PREF_KEY))
        {
            qualityIndex = PlayerPrefs.GetInt(QUALITY_PREF_KEY);
        }
        else
        {
            // First launch: use whatever Unity is currently set to
            qualityIndex = QualitySettings.GetQualityLevel();
            PlayerPrefs.SetInt(QUALITY_PREF_KEY, qualityIndex);
            PlayerPrefs.Save();
        }

        qualityIndex = Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1);

        QualitySettings.SetQualityLevel(qualityIndex);

        graphicsDropdown.value = qualityIndex;
        graphicsDropdown.RefreshShownValue();
    }

    // -------------------- Audio UI --------------------

    /// <summary>
    /// Called by the Music toggle button (OnClick).
    /// </summary>
    public void ToggleMusicFromUI()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.ToggleMusic();
        RefreshAudioUI();

        // Optional click sound (will respect SFX enabled)
        AudioManager.Instance.PlayButtonClick();
    }

    /// <summary>
    /// Called by the SFX toggle button (OnClick).
    /// </summary>
    public void ToggleSfxFromUI()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.ToggleSfx();
        RefreshAudioUI();

        // Optional click sound (will respect SFX enabled)
        AudioManager.Instance.PlayButtonClick();
    }

    /// <summary>
    /// Updates settings UI text to match saved states from AudioManager.
    /// </summary>
    private void RefreshAudioUI()
    {
        if (AudioManager.Instance == null) return;

        if (_musicStatusText != null)
            _musicStatusText.text = $"Music: {(AudioManager.Instance.IsMusicEnabled ? "ON" : "OFF")}";

        if (_sfxStatusText != null)
            _sfxStatusText.text = $"SFX: {(AudioManager.Instance.IsSfxEnabled ? "ON" : "OFF")}";
    }
}
