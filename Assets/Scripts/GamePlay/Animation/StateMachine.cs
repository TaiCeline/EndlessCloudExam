using UnityEngine;

public class StateMachine : StateMachineBehaviour
{
    [SerializeField]
    private PlayerAnimationType m_Animation;
 
    private AnimationSMBReceiver m_receiver;

    public override void OnStateEnter(Animator animator , AnimatorStateInfo stateInfo , int layerIndex)
    {
        base.OnStateEnter(animator , stateInfo , layerIndex);
        m_receiver = animator.GetComponent<AnimationSMBReceiver>();
        
        if (m_receiver != null)
        {
            m_receiver.OnAnimationEnter.Invoke(m_Animation);
        }
    }

    public override void OnStateUpdate(Animator animator , AnimatorStateInfo stateInfo , int layerIndex)
    {
        base.OnStateUpdate(animator , stateInfo , layerIndex);
        if (m_receiver != null)
        {
            m_receiver.OnAnimationUpdate.Invoke(m_Animation, stateInfo.normalizedTime);
        }
    }

    public override void OnStateExit(Animator animator , AnimatorStateInfo stateInfo , int layerIndex)
    {
        base.OnStateExit(animator , stateInfo , layerIndex);
        if (m_receiver != null)
        {
            m_receiver.OnAnimationExit.Invoke(m_Animation);
        }
    }
}
