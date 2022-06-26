using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour , ICamp
{
    public Camp Camp { get; set; }

    [SerializeField]
    private float m_HurtingMaxCD = 0.5f;
    
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
    
    private bool m_isDodging;
    private bool m_isHurting;
    
    private float m_hurtingCD;
    
    private float m_prevHorizontal;
    private float m_prevVertical;
    
    private int m_currentSpeed;
    private int m_attackPhase;

    private PlayerAnimationEvent m_animationEvent;
    private AnimationSMBReceiver m_smbReceiver;
    private Dictionary<int , bool> m_behavioralPriorityDic;
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
        m_behavioralPriorityDic = new Dictionary<int , bool>();
        for (int priority = 0 ; priority < (int)PlayerBehavioralPriority.MAX ; priority++)
            m_behavioralPriorityDic.Add(priority , false);

        m_animationEnterActions = new Dictionary<PlayerAnimationType , Action>()
        {
           
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
            { PlayerAnimationType.Hurt , OnHurtExit } ,
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
        m_attackPhase = 1;
        m_hurtingCD = 0;
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
        if(m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Dodge] == false)
        {
            m_isDodging = false;
            m_Animator.SetBool(hash_DODGE, m_isDodging);
        }

        if (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Hurt] == false)
        {
            m_hurtingCD = 0;
            m_isHurting = false;
            m_Animator.SetBool(hash_HURT , m_isHurting);
        }
        
        if(m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Attack] == false)
            m_Animator.SetBool(hash_ATTACK , false);
        
        if(m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Move] == false)
            m_currentSpeed = 0;

        
        
        if ((m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Dodge] == false) &&
            (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Hurt] == false) &&
            (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Attack] == false) &&
            (Input.GetAxis(HORIZONTAL) != 0 || Input.GetAxis(VERTICAL) != 0))
        {
            m_currentSpeed = m_Speed;
            ExecutionPriority(PlayerBehavioralPriority.Move);
        }

        Rotate(Input.GetAxis(HORIZONTAL) , Input.GetAxis(VERTICAL));
        Move(Input.GetAxis(HORIZONTAL) , Input.GetAxis(VERTICAL));

        if (Input.GetKey(KeyCode.Q))
            Hurt();
        
        if(m_isHurting)
            RunHurtingCD();
        
        if (Input.GetKeyDown(KeyCode.P))
            Attack();

        if (Input.GetKeyDown(KeyCode.Space))
            Dodge();

        if (m_isDodging)
            DodgeMoving();
        
    }

    void Move(float horizontal , float vertical)
    {
        float x = horizontal * m_currentSpeed * Time.deltaTime;
        float y = vertical * m_currentSpeed * Time.deltaTime;
        transform.Translate(x , 0 , y);
        var isMoving = x != 0 || y != 0;
        m_Animator.SetBool(hash_MOVE , isMoving);
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
        ExecutionPriority(PlayerBehavioralPriority.Attack);
        if (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Attack])
        {
            m_attackPhase = 1;
            m_Animator.SetInteger(hash_ATTACK_PHASE , m_attackPhase);
            m_Animator.SetBool(hash_ATTACK , true);
        }
    }

    void Dodge()
    {
        ExecutionPriority(PlayerBehavioralPriority.Dodge);
        if (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Dodge])
        {
            m_isDodging = true;
            m_Animator.SetBool(hash_DODGE, m_isDodging);
        }
    }

    void DodgeMoving()
    {
        transform.position = Vector3.MoveTowards(transform.position , m_DodgeDistance.position , m_DodgeSpeed * Time.deltaTime);
    }

    void Hurt()
    {
        ExecutionPriority(PlayerBehavioralPriority.Hurt);
        if (m_behavioralPriorityDic[(int)PlayerBehavioralPriority.Hurt])
        {
            if (m_hurtingCD <= 0)
            {
                Debug.Log($"Hurt !! {m_hurtingCD}");
                m_hurtingCD = m_HurtingMaxCD;
                m_isHurting = true;
                m_Animator.SetBool(hash_HURT , m_isHurting);
            }
        }
    }

    void RunHurtingCD()
    {
        m_hurtingCD -= Time.deltaTime;
    }
    
    #region Animation

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
    
#endregion
    
    void SetWeaponCollider(bool enabled)
    {
        m_WeaponCollider.enabled = enabled;
    }

    void OnAttack1Update(float normalizedTime)
    {
        if (normalizedTime >= 0.3f) // 0.3f combo moment.
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
        if (normalizedTime >= 0.6f) // dodge animation moment over.
        {
            m_isDodging = false;
        }
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

    void OnDodgeExit()
    {
        CancelPriority(PlayerBehavioralPriority.Dodge);
    }

    void OnHurtExit()
    {
        CancelPriority(PlayerBehavioralPriority.Hurt);
    }
    
    void ExecutionPriority(PlayerBehavioralPriority priority)
    {
        if (m_behavioralPriorityDic[(int)priority])
            return;
    
        for (var index = 0 ; index < m_behavioralPriorityDic.Count ; index++)
        {
            var otherBehavior = m_behavioralPriorityDic.ElementAt(index);
            var otherPriority = otherBehavior.Key;
            var otherPriorityIsOn = otherBehavior.Value;
            if ((int)priority == otherPriority)
                continue;
            
            if (otherPriorityIsOn)
            {
                if ((int)priority < otherPriority)
                {
                    m_behavioralPriorityDic[(int)priority] = true;
                    m_behavioralPriorityDic[otherPriority] = false;
                }
                else
                {
                    m_behavioralPriorityDic[(int)priority] = false;
                    break;
                }
            }
            else
            {
                m_behavioralPriorityDic[(int)priority] = true;
            }
        }
    }
    
    void CancelPriority(PlayerBehavioralPriority priority)
    {
        m_behavioralPriorityDic[(int)priority] = false;
    }
}