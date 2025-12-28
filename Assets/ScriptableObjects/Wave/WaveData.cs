using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    public EnemyType enemyType;
    [Range(0f, 10f)] public float spawnInterval;
    [Range(1, 100)] public int enemiesPerWave;
    public AudioClip waveSpawnClip;
}
