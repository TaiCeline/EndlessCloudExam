using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class test : MonoBehaviour , ICamp
{
    public Camp Camp { get; set; }

    [SerializeField]
    private int m_Speed = 10;

    [SerializeField]
    private int m_DodgeSpeed = 13;

    [SerializeField]
    private Transform m_DodgeDistance;

    [SerializeField]
    private Transform m_Body;

    public Transform Body => m_Body;

    [SerializeField]
    private BoxCollider m_WeaponCollider;

    [SerializeField]
    private Animator m_Animator;

    /////////////////////////////////////////////////////////////////////////////////
    [ReadOnly]
    public bool m_Hurt;

    [ReadOnly]
    public bool m_Dodge;

    [ReadOnly]
    public bool m_Attack;

    [ReadOnly]
    public bool m_Move;

    [ReadOnly]
    public bool m_Idle;
    /////////////////////////////////////////////////////////////////////////////////


    private bool m_isDodging;
    private bool m_isHurting;

    private float m_prevHorizontal;
    private float m_prevVertical;

    private int m_currentSpeed;
    private int m_attackPhase;

    private PlayerAnimationEvent m_animationEvent;
    private AnimationSMBReceiver m_smbReceiver;
    // private Dictionary<int , bool> m_behavioralPriorityDic;
    private Dictionary<PlayerAnimationType , Action> m_animationEnterActions;
    private Dictionary<PlayerAnimationType , Action<float>> m_animationUpdateActions;
    private Dictionary<PlayerAnimationType , Action> m_animationExitActions;

    private readonly int hash_MOVE = Animator.StringToHash("Move");
    private readonly int hash_DODGE = Animator.StringToHash("Dodge");
    private readonly int hash_ATTACK = Animator.StringToHash("Attack");
    private readonly int hash_ATTACK_PHASE = Animator.StringToHash("AttackPhase");

    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    void Start()
    {
        // m_behavioralPriorityDic = new Dictionary<int , bool>();
        // for (int priority = 0 ; priority < (int)PlayerBehavioralPriority.MAX ; priority++)
        //     m_behavioralPriorityDic.Add(priority , false);

        m_animationEnterActions = new Dictionary<PlayerAnimationType , Action>()
        {
            { PlayerAnimationType.Idle , OnIdleEnter } ,
        };
        m_animationUpdateActions = new Dictionary<PlayerAnimationType , Action<float>>()
        {
            { PlayerAnimationType.Attack1 , OnAttack1Update } ,
            { PlayerAnimationType.Dodge , OnDodgeUpdate } ,
        };
        m_animationExitActions = new Dictionary<PlayerAnimationType , Action>()
        {
            { PlayerAnimationType.Attack1 , OnAttack1Exit } ,
            { PlayerAnimationType.Attack2 , OnAttack2Exit } ,
            { PlayerAnimationType.Dodge , OnDodgeExit } ,
        };

        m_animationEvent = m_Animator.GetComponent<PlayerAnimationEvent>();
        if (m_animationEvent != null)
        {
            m_animationEvent.OnHit += SetWeaponCollider;
        }

        m_smbReceiver = m_Animator.GetComponent<AnimationSMBReceiver>();
        if (m_smbReceiver != null)
        {
            m_smbReceiver.OnAnimationEnter += OnAnimationEnter;
            m_smbReceiver.OnAnimationUpdate += OnAnimationUpdate;
            m_smbReceiver.OnAnimationExit += OnAnimationExit;
        }

        Camp = Camp.Player;
        m_currentSpeed = m_Speed;
        // ExecutionPriority(PlayerBehavioralPriority.Move);
        m_attackPhase = 1;
    }

    void OnDestroy()
    {
        if (m_animationEvent != null)
        {
            m_animationEvent.OnHit -= SetWeaponCollider;
        }

        if (m_smbReceiver != null)
        {
            m_smbReceiver.OnAnimationEnter -= OnAnimationEnter;
            m_smbReceiver.OnAnimationUpdate -= OnAnimationUpdate;
            m_smbReceiver.OnAnimationExit -= OnAnimationExit;
        }
    }

    void Update()
    {
        // m_Hurt = m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Hurt];
        // m_Dodge = m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Dodge];
        // m_Attack = m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Attack];
        // m_Move = m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Move];
        // m_Idle = m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Idle];


        Rotate(Input.GetAxis(HORIZONTAL) , Input.GetAxis(VERTICAL));
        Move(Input.GetAxis(HORIZONTAL) , Input.GetAxis(VERTICAL));

        if (Input.GetKeyDown(KeyCode.P))
            Attack();

        if (Input.GetKeyDown(KeyCode.Space))
            Dodge();

        if (m_isDodging)
            DodgeMoving();
    }

    void Move(float horizontal , float vertical)
    {
        // if (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Move])
        // {
            float x = horizontal * m_currentSpeed * Time.deltaTime;
            float y = vertical * m_currentSpeed * Time.deltaTime;
            transform.Translate(x , 0 , y);
            var isMoving = x != 0 || y != 0;
            m_Animator.SetBool(hash_MOVE , isMoving);
        // }
    }

    void Rotate(float horizontal , float vertical)
    {
        float x = horizontal * m_currentSpeed * Time.deltaTime;
        float y = vertical * m_currentSpeed * Time.deltaTime;
        bool isMoving = x != 0 || y != 0;
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
    }

    void Attack()
    {
        // ExecutionPriority(PlayerBehavioralPriority.Attack);
        // if (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Attack])
        // {
        //     CancelPriority(PlayerBehavioralPriority.Move);
            m_currentSpeed = 0;
            m_attackPhase = 1;
            m_Animator.SetInteger(hash_ATTACK_PHASE , m_attackPhase);
            m_Animator.SetBool(hash_ATTACK , true);
        // }
    }

    void Dodge()
    {
        // ExecutionPriority(PlayerBehavioralPriority.Dodge);
        // if (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Dodge])
        // {
            // CancelPriority(PlayerBehavioralPriority.Move);
            m_currentSpeed = 0;
            m_isDodging = true;
            m_Animator.SetTrigger(hash_DODGE);
        // }
    }

    void DodgeMoving()
    {
        transform.position = Vector3.MoveTowards(transform.position , m_DodgeDistance.position , m_DodgeSpeed * Time.deltaTime);
    }

    void OnAnimationEnter(PlayerAnimationType type)
    {
        if (m_animationEnterActions.ContainsKey(type))
        {
            m_animationEnterActions[type].Invoke();
        }
    }

    void OnAnimationUpdate(PlayerAnimationType type , float normalizedTime)
    {
        if (m_animationUpdateActions.ContainsKey(type))
        {
            m_animationUpdateActions[type].Invoke(normalizedTime);
        }
    }

    void OnAnimationExit(PlayerAnimationType type)
    {
        if (m_animationExitActions.ContainsKey(type))
        {
            m_animationExitActions[type].Invoke();
        }
    }

    void SetWeaponCollider(bool enabled)
    {
        m_WeaponCollider.enabled = enabled;
    }

    void OnIdleEnter()
    {
        // ExecutionPriority(PlayerBehavioralPriority.Idle);

        m_Animator.SetBool(hash_ATTACK , false);
        // CancelPriority(PlayerBehavioralPriority.Attack);
    }

    void OnAttack1Update(float normalizedTime)
    {
        // CancelPriority(PlayerBehavioralPriority.Move);
        m_currentSpeed = 0;
        if (normalizedTime >= 0.3f) // 0.3f can combo moment.
        {
            if (Input.GetKeyDown(KeyCode.P) && m_attackPhase == 1)
            {
                m_attackPhase = 2;
                m_Animator.SetInteger(hash_ATTACK_PHASE , m_attackPhase);
            }
        }
    }

    void OnDodgeUpdate(float normalizedTime)
    {
        if (normalizedTime >= 0.6f)
        {
            m_isDodging = false;
            // CancelPriority(PlayerBehavioralPriority.Dodge);
        }
    }

    void OnAttack1Exit()
    {
        if (m_attackPhase == 1)
        {
            m_currentSpeed = m_Speed;
            // ExecutionPriority(PlayerBehavioralPriority.Move);
        }

        m_Animator.SetBool(hash_ATTACK , false);
        // CancelPriority(PlayerBehavioralPriority.Attack);
    }

    void OnAttack2Exit()
    {
        m_currentSpeed = m_Speed;
        // ExecutionPriority(PlayerBehavioralPriority.Move);

        m_Animator.SetBool(hash_ATTACK , false);
        // CancelPriority(PlayerBehavioralPriority.Attack);
    }

    void OnDodgeExit()
    {
        m_isDodging = false;
        // CancelPriority(PlayerBehavioralPriority.Dodge);
        m_currentSpeed = m_Speed;
        // ExecutionPriority(PlayerBehavioralPriority.Move);
    }

    // void ExecutionPriority(PlayerBehavioralPriority priority)
    // {
    //     if (m_behavioralPriorityDic[(int)priority])
    //         return;
    //
    //     for (var index = 0 ; index < m_behavioralPriorityDic.Count ; index++)
    //     {
    //         var otherBehavior = m_behavioralPriorityDic.ElementAt(index);
    //         var otherPriority = otherBehavior.Key;
    //         if ((int)priority == otherPriority)
    //             continue;
    //         
    //         if ((int)priority < otherPriority)
    //         {
    //             m_behavioralPriorityDic[(int)priority] = true;
    //             m_behavioralPriorityDic[otherPriority] = false;
    //         }
    //         else
    //         {
    //             m_behavioralPriorityDic[(int)priority] = false;
    //         }
    //     }
    // }
    //
    // void CancelPriority(PlayerBehavioralPriority priority)
    // {
    //     m_behavioralPriorityDic[(int)priority] = false;
    // }
}