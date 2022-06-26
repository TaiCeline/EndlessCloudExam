using UnityEngine;

public class Camera : MonoBehaviour
{
    private Transform m_player;

    private void Start()
    {
        m_player = GameObject.FindWithTag(Camp.Player.ToString()).transform;
    }

    private void LateUpdate()
    {
        transform.LookAt(m_player);
    }
}
