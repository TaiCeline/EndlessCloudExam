using UnityEngine;

public class AttackTrigger : MonoBehaviour , ICamp
{
    public Camp Camp
    {
        get { return m_Camp; }
        set { m_Camp = value; }
    }

    [SerializeField]
    private Camp m_Camp;

    public void SetCamp(Camp camp)
    {
        m_Camp = camp;
    }
}