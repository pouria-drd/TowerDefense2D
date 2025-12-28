using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    [Tooltip("Match a scene name")] public string levelName; // match a scene name
    [Range(1, 100)] public int wavesToWin;
    [Range(100, 1000)] public int startingCoins;
    [Range(1, 100)] public int startingLives;

    public Vector2 initialSpawnPosition;

    public WaveData[] waves;

    //public AudioClip backgroundMusic;
}
