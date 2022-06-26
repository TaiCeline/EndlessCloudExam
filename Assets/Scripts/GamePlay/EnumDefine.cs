public enum Camp
{
    None , 
    Player , 
    Enemy
}

public enum PlayerAnimationType
{
    Idle,
    Move,
    Attack1,
    Attack2,
    Hurt,
    Dodge
}

public enum PlayerBehavioralPriority : int
{
    Dodge = 0, 
    Hurt , 
    Attack ,
    Move ,

    MAX,
}

public enum PoolFullHandling
{
    None,
    Extend , 
    RepeatUse
}

