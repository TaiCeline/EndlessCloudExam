using UnityEngine;

public class Weapon : MonoBehaviour , ICamp
{
    public Camp Camp
    {
        get { return m_Camp; }
        set { m_Camp = value; }
    }

    [SerializeField]
    private Camp m_Camp;

    private Collider m_collider;

    void Awake()
    {
        m_collider = GetComponent<Collider>();
    }
    
    public void SetCamp(Camp camp)
    {
        m_Camp = camp;
    }

    public void SetCollider(bool enable)
    {
        m_collider.enabled = enable;
    }
}