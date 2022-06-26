using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour , ICamp , IHurt
{
    public Camp Camp { get; set; }

    [SerializeField]
    private float m_HurtingMaxCD = 0.5f;

    [SerializeField]
    private int m_Speed = 10;

    [SerializeField]
    private int m_DodgeSpeed = 12;

    [SerializeField]
    private Transform m_DodgeDistance;

    [SerializeField]
    private Weapon m_Weapon;

    [SerializeField]
    private Transform m_Body; // for projectile weapon aiming.

    public Transform Body => m_Body;

    [SerializeField]
    private Animator m_Animator;

    [SerializeField]
    private Text m_Info;

    private bool m_isDodging;
    private bool m_isHurting;

    private float m_hurtingCD;

    private float m_prevHorizontal;
    private float m_prevVertical;

    private int m_currentSpeed;
    private int m_attackPhase;
    
    private bool m_useDodgeBackswing = true;
    private bool m_useAttackBackswing = true;

    private AnimationSMBReceiver m_smbReceiver;
    private Dictionary<int , bool> m_behavioralPriorityPassDic;
    private Dictionary<int , Action> m_behavioralPriorityCancelActionDic;
    private Dictionary<PlayerAnimationType , Action> m_animationEnterActions;
    private Dictionary<PlayerAnimationType , Action<float>> m_animationUpdateActions;
    private Dictionary<PlayerAnimationType , Action> m_animationExitActions;

    private readonly int hash_DODGE = Animator.StringToHash("Dodge");
    private readonly int hash_HURT = Animator.StringToHash("Hurt");
    private readonly int hash_ATTACK = Animator.StringToHash("Attack");
    private readonly int hash_ATTACK_PHASE = Animator.StringToHash("AttackPhase");
    private readonly int hash_MOVE = Animator.StringToHash("Move");

    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    void Start()
    {
        m_behavioralPriorityPassDic = new Dictionary<int , bool>();
        for (int priority = 0 ; priority < (int)PlayerBehavioralPriority.MAX ; priority++)
            m_behavioralPriorityPassDic.Add(priority , false);

        m_behavioralPriorityCancelActionDic = new Dictionary<int , Action>()
        {
            { (int)PlayerBehavioralPriority.Dodge , CancelDodge } ,
            { (int)PlayerBehavioralPriority.Hurt , CancelHurt } ,
            { (int)PlayerBehavioralPriority.Attack , CancelAttack } ,
            { (int)PlayerBehavioralPriority.Move , CancelMove } ,
        };

        #region Animation

        m_animationEnterActions = new Dictionary<PlayerAnimationType , Action>()
            { };

        m_animationUpdateActions = new Dictionary<PlayerAnimationType , Action<float>>()
        {
            { PlayerAnimationType.Dodge , OnDodgeUpdate } ,
            { PlayerAnimationType.Attack1 , OnAttack1Update } ,
            { PlayerAnimationType.Attack2 , OnAttack2Update } ,
        };

        m_animationExitActions = new Dictionary<PlayerAnimationType , Action>()
        {
            { PlayerAnimationType.Dodge , OnDodgeExit } ,
            { PlayerAnimationType.Hurt , OnHurtExit } ,
            { PlayerAnimationType.Attack1 , OnAttack1Exit } ,
            { PlayerAnimationType.Attack2 , OnAttack2Exit } ,
        };

        m_smbReceiver = m_Animator.GetComponent<AnimationSMBReceiver>();
        if (m_smbReceiver != null)
        {
            m_smbReceiver.OnAnimationEnter += OnAnimationEnter;
            m_smbReceiver.OnAnimationUpdate += OnAnimationUpdate;
            m_smbReceiver.OnAnimationExit += OnAnimationExit;
        }
    #endregion
        
        Camp = Camp.Player;
        m_currentSpeed = m_Speed;
        m_hurtingCD = 0;
        m_attackPhase = 1;
        m_Animator.SetInteger(hash_ATTACK_PHASE , m_attackPhase);

        m_Info.text = $"F1 (Use/Unuse) DodgeBackswing : {m_useDodgeBackswing} \n" +
                      $"F2 (Use/Unuse) AttackBackswing : {m_useAttackBackswing} ";
    }

    void OnDestroy()
    {
        if (m_smbReceiver != null)
        {
            m_smbReceiver.OnAnimationEnter -= OnAnimationEnter;
            m_smbReceiver.OnAnimationUpdate -= OnAnimationUpdate;
            m_smbReceiver.OnAnimationExit -= OnAnimationExit;
        }
    }

    void Update()
    {
        RealtimeCheckPriorityGotCanceled();

        if (Input.GetAxis(HORIZONTAL) != 0 || Input.GetAxis(VERTICAL) != 0)
            Move();

        if (Input.GetKeyDown(KeyCode.P))
            Attack();

        if (Input.GetKeyDown(KeyCode.Space))
            Dodge();

        if (m_isDodging)
            DodgeMoving();

        if (m_isHurting)
            Hurting();

        RealtimeMovement(Input.GetAxis(HORIZONTAL) , Input.GetAxis(VERTICAL));

        /*  sugar mode. */
        if (Input.GetKeyDown(KeyCode.F1))
            SetDodgeBackswing();

        if (Input.GetKeyDown(KeyCode.F2))
            SetAttackBackswing();
    }

    void RealtimeCheckPriorityGotCanceled()
    {
        for (var index = 0 ; index < m_behavioralPriorityCancelActionDic.Count ; index++)
        {
            var priority = m_behavioralPriorityCancelActionDic.ElementAt(index).Key;
            if (m_behavioralPriorityPassDic[priority] == false)
                m_behavioralPriorityCancelActionDic[priority].Invoke();
        }
    }

    void RealtimeMovement(float horizontal , float vertical)
    {
        // Move
        float moveX = horizontal * m_currentSpeed * Time.deltaTime;
        float moveY = vertical * m_currentSpeed * Time.deltaTime;
        transform.Translate(moveX , 0 , moveY);

        // Rotate
        var isMoving = (moveX != 0 || moveY != 0);
        if (isMoving)
        {
            m_prevHorizontal = horizontal;
            m_prevVertical = vertical;
        }

        Vector3 direction = new Vector3(m_prevHorizontal , 0f , m_prevVertical);
        if (direction != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(direction , Vector3.up);
            var bodyToBeRotated = transform.GetChild(0);
            bodyToBeRotated.localRotation = rotation;
        }

        m_Animator.SetBool(hash_MOVE , isMoving);
    }

    void Move()
    {
        ExecutionPriority(PlayerBehavioralPriority.Move);
        if (m_behavioralPriorityPassDic[(int)PlayerBehavioralPriority.Move])
        {
            m_currentSpeed = m_Speed;
        }
    }

    void CancelMove()
    {
        m_currentSpeed = 0;
        m_Animator.SetBool(hash_MOVE , false);
    }

    void Attack()
    {
        ExecutionPriority(PlayerBehavioralPriority.Attack);
        if (m_behavioralPriorityPassDic[(int)PlayerBehavioralPriority.Attack])
        {
            m_attackPhase = 1;
            m_Animator.SetInteger(hash_ATTACK_PHASE , m_attackPhase);
            m_Animator.SetBool(hash_ATTACK , true);
        }
    }

    void CancelAttack()
    {
        m_Weapon.SetCollider(false);
        m_attackPhase = 1;
        m_Animator.SetInteger(hash_ATTACK_PHASE , m_attackPhase);
        m_Animator.SetBool(hash_ATTACK , false);
    }

    void Dodge()
    {
        ExecutionPriority(PlayerBehavioralPriority.Dodge);
        if (m_behavioralPriorityPassDic[(int)PlayerBehavioralPriority.Dodge])
        {
            m_isDodging = true;
            m_Animator.SetBool(hash_DODGE , m_isDodging);
        }
    }

    void CancelDodge()
    {
        m_isDodging = false;
        m_Animator.SetBool(hash_DODGE , m_isDodging);
    }

    void DodgeMoving()
    {
        transform.position = Vector3.MoveTowards(transform.position , m_DodgeDistance.position , m_DodgeSpeed * Time.deltaTime);
    }

    public void Hurt()
    {
        ExecutionPriority(PlayerBehavioralPriority.Hurt);
        if (m_behavioralPriorityPassDic[(int)PlayerBehavioralPriority.Hurt])
        {
            if (m_hurtingCD <= 0)
            {
                m_hurtingCD = m_HurtingMaxCD;
                m_isHurting = true;
                m_Animator.SetBool(hash_HURT , m_isHurting);
            }
        }
    }

    void CancelHurt()
    {
        m_hurtingCD = 0;
        m_isHurting = false;
        m_Animator.SetBool(hash_HURT , m_isHurting);
    }

    void Hurting()
    {
        m_hurtingCD -= Time.deltaTime;
    }

    void ExecutionPriority(PlayerBehavioralPriority priority)
    {
        if (m_behavioralPriorityPassDic[(int)priority])
            return;

        for (var index = 0 ; index < m_behavioralPriorityPassDic.Count ; index++)
        {
            var otherBehavior = m_behavioralPriorityPassDic.ElementAt(index);
            var otherPriority = otherBehavior.Key;
            var otherPriorityIsOn = otherBehavior.Value;
            if ((int)priority == otherPriority)
                continue;

            if (otherPriorityIsOn)
            {
                if ((int)priority < otherPriority)
                {
                    m_behavioralPriorityPassDic[(int)priority] = true;
                    m_behavioralPriorityPassDic[otherPriority] = false;
                }
                else
                {
                    m_behavioralPriorityPassDic[(int)priority] = false;
                    break;
                }
            }
            else
            {
                m_behavioralPriorityPassDic[(int)priority] = true;
            }
        }
    }

    void CancelPriority(PlayerBehavioralPriority priority)
    {
        m_behavioralPriorityPassDic[(int)priority] = false;
    }

#region Animation

#region Enter

    void OnAnimationEnter(PlayerAnimationType type)
    {
        if (m_animationEnterActions.ContainsKey(type))
        {
            m_animationEnterActions[type].Invoke();
        }
    }

#endregion

#region Update

    void OnDodgeUpdate(float normalizedTime)
    {
        if (normalizedTime >= 0.6f) // moment of dodge animation over.
        {
            m_isDodging = false;
            
            if(m_useDodgeBackswing == false)
                CancelPriority(PlayerBehavioralPriority.Dodge);
        }
    }

    void OnAttack1Update(float normalizedTime)
    {
        if (normalizedTime >= 0.2f) // moment of weapon hit.
        {
            m_Weapon.SetCollider(true);
        }

        if (normalizedTime >= 0.3f) // moment of combo.
        {
            if (Input.GetKeyDown(KeyCode.P) && m_attackPhase == 1)
            {
                m_attackPhase = 2;
                m_Animator.SetInteger(hash_ATTACK_PHASE , m_attackPhase);
            }
        }

        if (normalizedTime >= 0.5f) // sugar mode.
        {
            if (m_useAttackBackswing == false)
            {
                m_Weapon.SetCollider(false);
                CancelPriority(PlayerBehavioralPriority.Attack);
            }
        }
        
        if (normalizedTime >= 0.7f) // moment of weapon put away.
        {
            m_Weapon.SetCollider(false);
        }
    }

    void OnAttack2Update(float normalizedTime)
    {
        if (normalizedTime >= 0.2f) // moment of weapon hit.
        {
            m_Weapon.SetCollider(true);
        }

        if (normalizedTime >= 0.5f) // sugar mode.
        {
            if (m_useAttackBackswing == false)
            {
                m_Weapon.SetCollider(false);
                CancelPriority(PlayerBehavioralPriority.Attack);
            }
        }
        
        if (normalizedTime >= 0.7f) // moment of weapon put away.
        {
            m_Weapon.SetCollider(false);
        }
    }

    void OnAnimationUpdate(PlayerAnimationType type , float normalizedTime)
    {
        if (m_animationUpdateActions.ContainsKey(type))
        {
            m_animationUpdateActions[type].Invoke(normalizedTime);
        }
    }

#endregion

#region Exit

    void OnDodgeExit()
    {
        CancelPriority(PlayerBehavioralPriority.Dodge);
    }

    void OnHurtExit()
    {
        CancelPriority(PlayerBehavioralPriority.Hurt);
    }

    void OnAttack1Exit()
    {
        if (m_attackPhase == 1)
            CancelPriority(PlayerBehavioralPriority.Attack);
    }

    void OnAttack2Exit()
    {
        CancelPriority(PlayerBehavioralPriority.Attack);
    }

    void OnAnimationExit(PlayerAnimationType type)
    {
        if (m_animationExitActions.ContainsKey(type))
        {
            m_animationExitActions[type].Invoke();
        }
    }

#endregion

#endregion

    void SetDodgeBackswing()
    {
        m_useDodgeBackswing = !m_useDodgeBackswing;
        m_Info.text = $"F1 (Use/Unuse) DodgeBackswing : {m_useDodgeBackswing} \n" +
                      $"F2 (Use/Unuse) AttackBackswing : {m_useAttackBackswing} ";
    }

    void SetAttackBackswing()
    {
        m_useAttackBackswing = !m_useAttackBackswing;
        m_Info.text = $"F1 (Use/Unuse) DodgeBackswing : {m_useDodgeBackswing} \n" +
                      $"F2 (Use/Unuse) AttackBackswing : {m_useAttackBackswing} ";
    }
}