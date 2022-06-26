using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    [SerializeField]
    private int m_NumberOfEnemies = 1;

    [SerializeField]
    private float m_SpawnerInterval = 0.5f;

    [SerializeField]
    private float m_Radius = 3f;

    [SerializeField]
    private float m_SpawnRangeMin = 0.8f;

    [SerializeField]
    private float m_SpawnRangeMax = 1f;

    [SerializeField]
    private Enemy m_EnemyPrefab;

    private List<Enemy> m_enemyList;

    private Transform m_player;
    private EnemyPool m_enemyPool;
    private float m_time;

    private const int POOL_PRELOAD_COUNT = 5;

    void Start()
    {
        m_player = GameObject.FindWithTag(Camp.Player.ToString()).transform;
        m_enemyList = new List<Enemy>();
        m_enemyPool = new EnemyPool(POOL_PRELOAD_COUNT , transform , m_EnemyPrefab);

        for (int index = 0 ; index < m_NumberOfEnemies ; index++)
            SpawnEnemy();
    }

    void SpawnEnemy()
    {
        var enemy = m_enemyPool.Get();
        enemy.transform.SetParent(transform.parent);
        enemy.transform.position = GetSpawnLocation();
        enemy.OnDeath += RecycleEnemy;
        enemy.Initialize();
        m_enemyList.Add(enemy);
    }
    
    Vector3 GetSpawnLocation() 
    {
        var direction = new Vector3(0 , 0 , 1);
        direction = Quaternion.Euler(0 , Random.Range(0 , 360f) , 0) * direction;
        var pos = direction * m_Radius * Random.Range(m_SpawnRangeMin , m_SpawnRangeMax);
        var newPos = m_player.position + pos; // around the player.
        return newPos;
    }

    void RecycleEnemy(Enemy enemy)
    {
        enemy.OnDeath -= RecycleEnemy;
        m_enemyPool.Recycle(enemy);
        m_enemyList.Remove(enemy);
        StartCoroutine(WaitToSpawn());
    }

    IEnumerator WaitToSpawn()
    {
        yield return new WaitForSeconds(m_SpawnerInterval);
        SpawnEnemy();
        yield return null;
    }
}