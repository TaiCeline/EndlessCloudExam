using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationSMBReceiver : MonoBehaviour
{
    public Action<PlayerAnimationType> OnAnimationEnter;
    public Action<PlayerAnimationType, float> OnAnimationUpdate;
    public Action<PlayerAnimationType> OnAnimationExit;
}