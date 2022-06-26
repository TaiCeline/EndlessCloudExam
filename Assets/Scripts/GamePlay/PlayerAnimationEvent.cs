using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationEvent : MonoBehaviour
{
    public Action<bool> OnHit;
    
    public void Hit(int enabled)
    {
        OnHit?.Invoke(enabled > 0);
    }
}