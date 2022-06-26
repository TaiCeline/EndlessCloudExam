using System;
using System.Linq;
using UnityEngine;

public class Bullet : AttackTrigger
{
    public Action<Bullet> OnDisappear;

    [SerializeField]
    private int m_Speed = 5;

    [SerializeField]
    private float m_AliveTime = 3;
    
    private Transform m_bulletArea;
    RaycastHit[] m_hitResults = new RaycastHit[RAYCAST_MAX_HIT_COUNT];
    private Vector3 m_prevPos;
    private Vector3 m_targetPosition;
    private Vector3 m_distanceVec3;
    private Camp m_targetCamp;
    private bool m_isShooting;
    private float m_time;
    
    private const string BULLET_AREA = "BulletArea";
    private const int RAYCAST_MAX_HIT_COUNT = 10;

    void Awake()
    {
        m_bulletArea = GameObject.FindWithTag(BULLET_AREA).transform;
    }

    public void ShootAtTarget(Transform target, Camp camp)
    {
        m_hitResults = new RaycastHit[RAYCAST_MAX_HIT_COUNT];
        m_targetPosition = target.position;
        m_targetCamp = camp;
        transform.SetParent(m_bulletArea);
        m_prevPos = transform.position;
        m_distanceVec3 = m_targetPosition - m_prevPos;
        m_isShooting = true;
    }

    void Update()
    {
        if (m_isShooting)
        {
            var hitCount = GetHitCount();
            if (hitCount > 0)
            {
                var hitTarget = m_hitResults.FirstOrDefault(hit =>
                {
                    if(hit.transform != null)
                        return hit.transform.CompareTag(m_targetCamp.ToString());
                  
                    return false;
                });

                if (hitTarget.Equals(null) == false)
                {
                    Disappear();
                } 
            }

            m_time += Time.deltaTime;
            if (m_time >= m_AliveTime)
            {
                Disappear();
            }
        }
    }

    int GetHitCount()
    {
        m_prevPos = transform.position;
        transform.Translate(m_distanceVec3 * m_Speed * Time.deltaTime);
        var ray = new Ray(m_prevPos , (transform.position - m_prevPos).normalized);
        var distance = (transform.position - m_prevPos).magnitude;
        var hits = Physics.RaycastNonAlloc(ray , m_hitResults , distance /*, 要設定玩家Layer*/);
        return hits;
    }

    void Disappear()
    {
        m_isShooting = false;
        OnDisappear?.Invoke(this);
        m_hitResults = null;
        m_time = 0;
    }
}