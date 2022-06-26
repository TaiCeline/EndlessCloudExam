using System;
using UnityEngine;

public class Enemy : MonoBehaviour , ICamp
{
    public Camp Camp { get; set; }

    public Action<Enemy> OnDeath;

    [SerializeField]
    private float m_HurtingMaxCD = 0.5f;
    
    [SerializeField]
    private int m_HealthMAX = 5;

    [SerializeField]
    private Transform m_BulletDepot;

    [SerializeField]
    private Bullet m_BulletPrefab;

    [SerializeField]
    private int m_AttackInterval = 3;

    [SerializeField]
    private MeshRenderer m_bodyMeshRenderer;

    private Player m_player;
    private BoxCollider m_selfCollider;

    private float m_hurtingCD;
    private Color m_bodyColor = new Color(0.13f , 0.1f , 0.27f);
    private Color m_hurtingColor = new Color(0.69f , 0.1f , 0.27f);
    
    private BulletPool m_bulletPool;
    private int m_currentHealth;
    private float m_time;

    private const string ATTACK_TRIGGER = "AttackTrigger";
    private const int POOL_PRELOAD_COUNT = 6;


    void Awake()
    {
        m_selfCollider = GetComponent<BoxCollider>();
        m_bulletPool = new BulletPool(POOL_PRELOAD_COUNT , m_BulletDepot , m_BulletPrefab);
        var playerObj = GameObject.FindWithTag(Camp.Player.ToString());
        if (playerObj != null)
        {
            m_player = playerObj.GetComponent<Player>();
        }

        m_BulletPrefab.SetCamp(Camp.Enemy);
    }
    
    public void Initialize()
    {
        Camp = Camp.Enemy;
        m_currentHealth = m_HealthMAX;
        m_time = 0;
        m_selfCollider.enabled = true;
        m_bodyMeshRenderer.material.color = m_bodyColor;
        m_hurtingCD = 0;
    }

    void Update()
    {
        m_time += Time.deltaTime;
        if (m_time >= m_AttackInterval)
        {
            Attack();
            m_time = 0;
        }

        m_hurtingCD -= Time.deltaTime;
        if (m_hurtingCD <= 0)
        {
            m_bodyMeshRenderer.material.color = m_bodyColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ATTACK_TRIGGER))
        {
            var camp = other.GetComponent<ICamp>();
            if (camp != null && camp.Camp == Camp.Player)
            {
                Hurt();
            }
        }
    }

    void Attack()
    {
        var bullet = m_bulletPool.Get();
        bullet.OnDisappear += RecycleBullet;
        bullet.ShootAtTarget(m_player.Body, Camp.Player);
    }

    void Hurt()
    {
        if (m_hurtingCD <= 0)
        {
            m_hurtingCD = m_HurtingMaxCD;
            if (m_currentHealth > 0)
            {
                m_bodyMeshRenderer.material.color = m_hurtingColor;
                m_currentHealth--;
                Debug.Log($"Enemy : [Hurt] hp: {m_currentHealth}");
            }
            else
            {
                Decease();
            }
        }
    }

    void Decease()
    {
        m_selfCollider.enabled = false;
        Debug.Log($"Enemy : [Die] hp: {m_currentHealth}");
        OnDeath?.Invoke(this);
    }

    void RecycleBullet(Bullet bullet)
    {
        bullet.OnDisappear -= RecycleBullet;
        m_bulletPool.Recycle(bullet);
    }
}