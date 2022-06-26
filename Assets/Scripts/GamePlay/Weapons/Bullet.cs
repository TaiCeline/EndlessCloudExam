using System;
using UnityEngine;

public class Bullet : Weapon
{
    public Action<Bullet> OnDeath;
    public Action<Transform> OnHitTarget;

    [SerializeField]
    private int m_Speed = 5;

    [SerializeField]
    private float m_AliveTime = 3;
    
    private Transform m_bulletArea;
    RaycastHit[] m_hitResults = new RaycastHit[RAYCAST_MAX_HIT_COUNT];
    private int m_targetLayerMask;
    private Vector3 m_ownPrevPosition;
    private Vector3 m_aimingPosition;
    private Vector3 m_distanceVector3;
    private Camp m_targetCamp;
    private bool m_isShooting;
    private float m_time;
    
    private const int SPEED_COEFFICIENT = 10;
    private const string BULLET_AREA = "BulletArea";
    private const int RAYCAST_MAX_HIT_COUNT = 10;

    void Awake()
    {
        m_bulletArea = GameObject.FindWithTag(BULLET_AREA).transform;
    }

    public void ShootAtTarget(Transform target, Transform aimingPoint)
    {
        m_hitResults = new RaycastHit[RAYCAST_MAX_HIT_COUNT];
        m_targetLayerMask = target.gameObject.layer;
        m_targetCamp = target.GetComponent<ICamp>()?.Camp ?? Camp.None;
        if(m_targetCamp == Camp.None) 
            Debug.LogError($"[Bullet] TargetNotSet 'ICamp' OR Camp is None.");
        m_aimingPosition = aimingPoint.position;
        
        transform.SetParent(m_bulletArea);
        m_ownPrevPosition = transform.position;
        m_distanceVector3 = (m_aimingPosition - m_ownPrevPosition).normalized;
        m_isShooting = true;
    }
    
    void Update()
    {
        if (m_isShooting)
        {
            m_time += Time.deltaTime;
            if (m_time >= m_AliveTime)
            {
                Death();
            }
        }
    }

    void FixedUpdate()
    {
        if (m_isShooting)
        {
            m_ownPrevPosition = transform.position;
            transform.Translate(m_distanceVector3 * m_Speed * SPEED_COEFFICIENT * Time.deltaTime);
            
            GenerateTriggerCast(out int hitCount);
            if (hitCount > 0)
            {
                int hitTargets = 0; 
                for (var index = 0 ; index < m_hitResults.Length ; index++)
                {
                    var hitResult = m_hitResults[index];
                    if (hitResult.transform != null)
                    {
                        if (hitResult.transform.CompareTag(m_targetCamp.ToString()))
                        {
                            OnHitTarget?.Invoke(hitResult.transform);
                            hitTargets++;
                        }
                    }
                }
                
                if (hitTargets > 0)
                    Death();
            }
        }
    }

    void GenerateTriggerCast(out int hitCount)
    {
        var ray = new Ray(m_ownPrevPosition , (transform.position - m_ownPrevPosition).normalized);
        var distance = (transform.position - m_ownPrevPosition).magnitude;
        hitCount = Physics.RaycastNonAlloc(ray , m_hitResults , distance , 1 << m_targetLayerMask);
    }

    void Death()
    {
        m_isShooting = false;
        OnDeath?.Invoke(this);
        m_hitResults = null;
        m_time = 0;
    }
}