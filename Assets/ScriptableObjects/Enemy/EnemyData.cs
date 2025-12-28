using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Range(1f, 20f)] public float lives;
    [Range(1, 20)] public int damage;
    [Range(0.1f, 10f)] public float minSpeed;
    [Range(1f, 20f)] public float maxSpeed;
    [Range(1f, 100f)] public float coinReward;
    [Range(0.1f, 1f)] public float offsetX = 0.5f;
    [Range(0.1f, 1f)] public float offsetY = 0.5f;
}
