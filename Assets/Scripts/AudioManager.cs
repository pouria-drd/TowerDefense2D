using UnityEngine;

/// <summary>
/// Simple audio manager with persistent SFX/Music ON/OFF toggles via PlayerPrefs.
/// No sliders: either full volume (configured in inspector) or muted.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const string SFX_PREF_KEY = "SFX_ENABLED";
    private const string MUSIC_PREF_KEY = "MUSIC_ENABLED";

    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Default Volumes (when enabled)")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.2f;

    [Header("Music Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;

    [Header("UI / Game SFX")]
    public AudioClip buttonClickClip;
    public AudioClip buttonHoverClip;
    public AudioClip pauseClip;
    public AudioClip unpauseClip;
    public AudioClip speedSlowClip;
    public AudioClip speedNormalClip;
    public AudioClip speedFastClip;
    public AudioClip panelToggleClip;
    public AudioClip warningClip;
    public AudioClip towerPlacedClip;
    public AudioClip enemyDestroyedClip;
    public AudioClip missionCompleteClip;
    public AudioClip gameOverClip;

    public bool IsSfxEnabled { get; private set; }
    public bool IsMusicEnabled { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Safety: prevent null reference crashes
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (musicSource == null) musicSource = GetComponent<AudioSource>();

        LoadAudioSettings();
        ApplyVolumes();
    }

    #region Public API (Toggle)

    /// <summary>
    /// Enables/disables SFX and saves to PlayerPrefs.
    /// </summary>
    public void SetSfxEnabled(bool enabled)
    {
        IsSfxEnabled = enabled;
        PlayerPrefs.SetInt(SFX_PREF_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    /// <summary>
    /// Enables/disables Music and saves to PlayerPrefs.
    /// If disabling, stops current music. If enabling, resumes current clip if set.
    /// </summary>
    public void SetMusicEnabled(bool enabled)
    {
        IsMusicEnabled = enabled;
        PlayerPrefs.SetInt(MUSIC_PREF_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        ApplyVolumes();

        if (!IsMusicEnabled)
        {
            if (musicSource != null) musicSource.Stop();
        }
        else
        {
            // If something was already assigned, resume it
            if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.Play();
        }
    }

    /// <summary>
    /// Convenience method for UI toggle buttons.
    /// </summary>
    public void ToggleSfx() => SetSfxEnabled(!IsSfxEnabled);

    /// <summary>
    /// Convenience method for UI toggle buttons.
    /// </summary>
    public void ToggleMusic() => SetMusicEnabled(!IsMusicEnabled);

    #endregion

    #region SFX

    /// <summary>
    /// Plays a one-shot SFX clip if SFX are enabled.
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (!IsSfxEnabled) return;
        if (clip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayTowerPlaced() => PlaySound(towerPlacedClip);
    public void PlayEnemyDestroyed() => PlaySound(enemyDestroyedClip);
    public void PlayButtonClick() => PlaySound(buttonClickClip);
    public void PlayButtonHover() => PlaySound(buttonHoverClip);
    public void PlayMissionComplete() => PlaySound(missionCompleteClip);
    public void PlayGameOver() => PlaySound(gameOverClip);
    public void PlayUnpause() => PlaySound(unpauseClip);
    public void PlayPause() => PlaySound(pauseClip);
    public void PlaySpeedSlow() => PlaySound(speedSlowClip);
    public void PlaySpeedNormal() => PlaySound(speedNormalClip);
    public void PlaySpeedFast() => PlaySound(speedFastClip);
    public void PlayWarning() => PlaySound(warningClip);
    public void PlayPanelToggle() => PlaySound(panelToggleClip);

    #endregion

    #region Music

    /// <summary>
    /// Starts looping a music clip (if music enabled). If the same clip is already playing, does nothing.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) return;

        musicSource.clip = clip;

        if (!IsMusicEnabled)
        {
            musicSource.Stop();
            return;
        }

        if (clip == null) return;

        if (musicSource.isPlaying && musicSource.clip == clip)
            return;

        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayMainMenuMusic() => PlayMusic(mainMenuMusic);
    public void PlayGameplayMusic() => PlayMusic(gameplayMusic);

    #endregion

    #region Persistence / Setup

    private void LoadAudioSettings()
    {
        // Default to ON if keys don't exist
        IsSfxEnabled = PlayerPrefs.GetInt(SFX_PREF_KEY, 1) == 1;
        IsMusicEnabled = PlayerPrefs.GetInt(MUSIC_PREF_KEY, 1) == 1;
    }

    private void ApplyVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = IsSfxEnabled ? sfxVolume : 0f;

        if (musicSource != null)
            musicSource.volume = IsMusicEnabled ? musicVolume : 0f;
    }

    #endregion
}
