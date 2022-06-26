using System;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour , ICamp , IHurt
{
    public Camp Camp { get; set; }

    public Action<Enemy> OnDeath;

    [SerializeField]
    private float m_HurtingMaxCD = 0.5f;

    [SerializeField]
    private int m_HealthMAX = 5;

    [SerializeField]
    private Scrollbar m_HealthScrollBar;

    [SerializeField]
    private Transform m_BulletDepot;

    [SerializeField]
    private Bullet m_BulletPrefab;

    [SerializeField]
    private int m_AttackInterval = 3;

    [SerializeField]
    private MeshRenderer m_bodyMeshRenderer;

    private Player m_player;
    private BoxCollider m_ownCollider;
    private RaycastHit[] m_hitResults = new RaycastHit[RAYCAST_MAX_HIT_COUNT];

    string[] m_layerMaskForReceive = { "MeleeWeapon" }; // projectile weapon doesn't need to detect injury, it will take the initiative to hurt.

    private float m_hurtingCD;
    private Color m_bodyColor = new Color(0.13f , 0.1f , 0.27f);
    private Color m_hurtingColor = new Color(0.69f , 0.1f , 0.27f);

    private BulletPool m_bulletPool;
    private int m_currentHealth;
    private float m_time;

    private const int POOL_PRELOAD_COUNT = 6;
    private const int RAYCAST_MAX_HIT_COUNT = 50;

    void Awake()
    {
        m_ownCollider = GetComponent<BoxCollider>();
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
        m_HealthScrollBar.size = (float)m_currentHealth / m_HealthMAX;
        m_time = 0;
        m_ownCollider.enabled = true;
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

    void FixedUpdate()
    {
        GenerateBodyTriggerCast(out int hitCount);
        DetectInjury(hitCount);
    }

    void GenerateBodyTriggerCast(out int hitCount)
    {
        Vector3 worldPos = m_ownCollider.transform.position;
        Quaternion rotation = m_ownCollider.transform.rotation;

        hitCount = Physics.BoxCastNonAlloc(worldPos + m_ownCollider.center , m_ownCollider.size / 2 ,
                                           Vector3.forward , m_hitResults , rotation , 0 ,
                                           LayerMask.GetMask(m_layerMaskForReceive));
    }

    void DetectInjury(int hitCount)
    {
        if (hitCount > 0)
        {
            for (int index = 0 ; index < hitCount ; index++)
            {
                var hitObj = m_hitResults[index].collider.gameObject;
                var camp = hitObj.GetComponent<ICamp>();
                if (camp != null)
                {
                    if (camp.Camp == Camp.Player)
                        Hurt();
                }
            }
        }
    }

    void Attack()
    {
        var bullet = m_bulletPool.Get();
        bullet.OnHitTarget += HitTarget;
        bullet.OnDeath += RecycleBullet;
        bullet.ShootAtTarget(m_player.transform , m_player.Body);
    }

    void HitTarget(Transform target)
    {
        target.GetComponent<IHurt>()?.Hurt();
    }

    public void Hurt()
    {
        if (m_hurtingCD <= 0)
        {
            m_hurtingCD = m_HurtingMaxCD;
            if (m_currentHealth > 0)
            {
                m_bodyMeshRenderer.material.color = m_hurtingColor;
                m_currentHealth--;
                m_HealthScrollBar.size = (float)m_currentHealth / m_HealthMAX;

                if (m_currentHealth <= 0)
                    Death();
            }
            else
            {
                Death();
            }
        }
    }

    void Death()
    {
        m_currentHealth = 0;
        m_ownCollider.enabled = false;
        OnDeath?.Invoke(this);
    }

    void RecycleBullet(Bullet bullet)
    {
        bullet.OnHitTarget -= HitTarget;
        bullet.OnDeath -= RecycleBullet;
        m_bulletPool.Recycle(bullet);
    }
}