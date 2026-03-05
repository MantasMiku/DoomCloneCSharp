using Godot;
using System;

public partial class Imp : CharacterBody3D
{
    #region Animation States
    enum AnimState
    {
        Walk,
        Attack,
        Hit,
        Death
    }
    #endregion

    #region Exported Variables
    //private CharacterBody3D player;
    [Export] float speed = 3;
    #endregion

    #region Node References
    AnimatedSprite3D anim;
    NavigationAgent3D navAgent;
    Area3D area;
    RayCast3D ray;
    PackedScene projectile;
    Node3D projectilePos;
    #endregion

    #region State Variables
    AnimState currentAnimState = AnimState.Walk;
    bool isFacing = false;
    bool walkBack = false;
    bool lastWalkBackState = false;
    bool isHit = false;
    bool dead = false;
    bool attack = false;
    bool panic = false;
    bool attackRange = false;
    #endregion

    #region Numeric Variables
    public int health;
    float smoothRot;
    float walkTime = 0f;
    float backTimer = 0f;
    float animSpeed = 10f;
    int fromRange;
    int toRange;
    int damage = 10;

    Node3D player = null;
    Node3D playerAuthority = null;
    #endregion

    #region Godot Lifecycle
    public override void _Ready()
    {
        //AddToGroup("PLAYER");
        InitializeNodes();
        InitializeStats();
        projectile = ResourceLoader.Load<PackedScene>("res://ENTITIES/ENEMIES/IMP/imp_projectile.tscn");
        area = GetNode<Area3D>("Area3D");
        //area.AreaEntered += OnAreaEntered;
        area.Monitoring = false;
        projectilePos = GetNode<Node3D>("ProjectilePos");
        anim.FrameChanged += OnFireFrameChanged;
        area.BodyEntered += OnBodyEntered;
        //navAgent.VelocityComputed += OnVelocityComputed;

        // var players = GetTree().GetNodesInGroup("PLAYER"); // group name is "Player"
        // if (players.Count > 0)
        // {
        //     player = players[0] as CharacterBody3D;
        // }

        // if (player == null)
        // {
        //     GD.PushWarning("Imp could not find a player in group 'Player'.");
        // }
    }
    public override void _Process(double delta)
    {
         if(Multiplayer.IsServer())
        {
            
        }
        
        var players = GetTree().GetNodesInGroup("PLAYER");
        if (players.Count == 0) return;
        
        // Find nearest player
        float nearestDistance = float.MaxValue;
        
        foreach (Node p in players)
        {
            if (p is Node3D player3d)
            {
                if(p.IsMultiplayerAuthority())
                {
                    playerAuthority = player3d;
                }
                float distance = GlobalPosition.DistanceTo(player3d.GlobalPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    player = player3d;
                }
            }
        }
        StateMachine();
        Animations();
        MovementLogic(delta);
        MoveAndSlide();
    }
    #endregion

    #region Initialization
    void InitializeNodes()
    {
        anim = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        ray = GetNode<RayCast3D>("RayCast3D");

        // // Enable avoidance for smooth multi-enemy behavior
        // navAgent.AvoidanceEnabled = true;
        // navAgent.Radius = 2f; // Adjust based on enemy size
        // navAgent.MaxSpeed = speed;
    }

    void InitializeStats()
    {
        health = 100;
        fromRange = (int)GD.RandRange(10, 14);
        toRange = (int)GD.RandRange(4, 8);
    }
    #endregion

    #region Movement Logic
    void MovementLogic(double delta)
    {
        if (IsOnFloor())
        {
            HandleGroundedMovement(delta);
        }
        else
        {
            ApplyGravity(delta);
        }
    }

    void HandleGroundedMovement(double delta)
    {
        Velocity = Vector3.Zero;
        navAgent.TargetPosition = player.GlobalPosition;
        var nextNavPoint = navAgent.GetNextPathPosition();
        Vector3 target = new Vector3(nextNavPoint.X, GlobalPosition.Y, nextNavPoint.Z);
        float distanceToPlayer = Position.DistanceTo(player.Position);
        Vector3 dirAwayFromPlayer = (GlobalPosition - player.GlobalPosition).Normalized();


        HandleMovementStates(target, nextNavPoint, delta, distanceToPlayer);
        HandleBehaviorLogic(distanceToPlayer, delta);
        UpdateAnimationSpeed();
    }

    void HandleMovementStates(Vector3 target, Vector3 nextNavPoint, double delta, float distanceToPlayer)
    {
        if (!isHit && !dead)
        {
            if (!walkBack && !panic)
            {
                WalkForward(target, nextNavPoint, delta, speed);
            }
            else if (walkBack && !panic && !attack)
            {
                WalkBack(target, nextNavPoint, delta);
            }
            else if (panic && !attack)
            {
                WalkForward(target, nextNavPoint, delta, 4);
            }
        }
    }

    void HandleBehaviorLogic(float distanceToPlayer, double delta)
    {
        if (ray.IsColliding())
        {
            HandleRayCollision(distanceToPlayer);
        }
        else
        {
            HandleNoRayCollision();
        }

        HandleRandomRangeUpdate();
        HandleBackwardTimer(distanceToPlayer, delta);
    }

    void HandleRayCollision(float distanceToPlayer)
    {
        if (!panic)
        {
            if (distanceToPlayer > fromRange)
            {
                walkBack = false;
            }
            else if (distanceToPlayer <= toRange && !attack && !walkBack)
            {
                backTimer = 0f;
                attack = true;
                attackRange = true;
                walkBack = true;
            }
        }
        else if (panic && distanceToPlayer <= 2)
        {
            attack = true;
            area.Monitoring = true;
            // panic = false;
            walkBack = true;
        }
    }

    void HandleNoRayCollision()
    {
        walkBack = false;
        panic = true;
    }

    void HandleRandomRangeUpdate()
    {
        if (walkBack != lastWalkBackState)
        {
            fromRange = (int)GD.RandRange(9, 12);
            toRange = (int)GD.RandRange(5, 8);
            //GD.Print($"New ranges - From: {fromRange}, To: {toRange}");
            lastWalkBackState = walkBack;
        }
    }

    void HandleBackwardTimer(float distanceToPlayer, double delta)
    {
        if (walkBack && distanceToPlayer < fromRange / 2 && !panic)
        {
            backTimer += (float)delta;
            if (backTimer > 2.5f)
            {
                panic = true;
                walkBack = false;
            }
        }
    }

    void UpdateAnimationSpeed()
    {
        if (panic)
        {
            //animSpeed = 8f;
            backTimer = 0f;
        }
        else
        {
            //animSpeed = 5f;
        }
    }

    void WalkForward(Vector3 target, Vector3 nextNavPoint, double delta, float sp)
    {
        LookAt(target, Vector3.Up);
        
        if (!panic)
        {
            smoothRot = Mathf.LerpAngle(smoothRot, Rotation.Y, (float)delta * animSpeed);
            Rotation = new Vector3(Rotation.X, smoothRot, Rotation.Z);
        }
        else
        {
            Rotation = new Vector3(Rotation.X, Rotation.Y, Rotation.Z);
        }

        // if (isFacing)
        // {
        //     Velocity = (nextNavPoint - GlobalPosition).Normalized() * sp;
        // }
        Velocity = (nextNavPoint - GlobalPosition).Normalized() * sp;
    }

    void WalkBack(Vector3 target, Vector3 nextNavPoint, double delta)
    {
        LookAt(target, Vector3.Up);
        float targetRotation = Rotation.Y + Mathf.Pi;
        smoothRot = Mathf.LerpAngle(smoothRot, targetRotation, (float)delta * animSpeed);
        Rotation = new Vector3(Rotation.X, smoothRot, Rotation.Z);
        Velocity = (nextNavPoint - GlobalPosition).Normalized() * -speed;
        area.Monitoring = false;
    }

    void ApplyGravity(double delta)
    {
        Velocity -= new Vector3(0, 10 * (float)delta, 0);
    }
    #endregion

    #region State Machine
    void StateMachine()
    {
        if (health <= 0)
        {
            currentAnimState = AnimState.Death;
        }
        else if (!isHit && !attack)
        {
            currentAnimState = AnimState.Walk;
        }
        else if (isHit)
        {
            currentAnimState = AnimState.Hit;
        }
        else if (attack)
        {
            currentAnimState = AnimState.Attack;
        }
    }
    #endregion

    #region Animation System
    void Animations()
    {
        CalculateAnimationDirections(out float f_dot, out float l_dot);
        anim.FlipH = false;

        switch (currentAnimState)
        {
            case AnimState.Attack:
                PlayAttackAnimation(f_dot, l_dot);
                break;

            case AnimState.Hit:
                PlayHitAnimation(f_dot, l_dot);
                break;

            case AnimState.Death:
                break;

            case AnimState.Walk:
            default:
                PlayWalkAnimation(f_dot, l_dot);
                break;
        }
    }

    void CalculateAnimationDirections(out float f_dot, out float l_dot)
    {
        var p_fwd = playerAuthority.GlobalTransform.Basis.Z;
        var fwd = GlobalTransform.Basis.Z;
        var left = GlobalTransform.Basis.X;
        l_dot = left.Dot(p_fwd);
        f_dot = fwd.Dot(p_fwd);
    }

    void PlayWalkAnimation(float f_dot, float l_dot)
    {
        if (f_dot < -0.85)
        {
            anim.Play("WALK_0");
            isFacing = true;
        }
        else if (f_dot > 0.85)
        {
            anim.Play("WALK_4");
            isFacing = false;
        }
        else
        {
            anim.FlipH = l_dot > 0;
            if (Mathf.Abs(f_dot) < 0.3)
            {
                anim.Play("WALK_2");
                isFacing = false;
            }
            else if (f_dot < 0)
            {
                anim.Play("WALK_1");
                isFacing = true;
            }
            else
            {
                anim.Play("WALK_3");
                isFacing = false;
            }
        }
    }

    async void PlayAttackAnimation(float f_dot, float l_dot)
    {
        if (f_dot < -0.85)
        {
            anim.Play("ATTACK_0");
            isFacing = true;
        }
        else if (f_dot > 0.85)
        {
            anim.Play("ATTACK_4");
            isFacing = false;
        }
        else
        {
            anim.FlipH = l_dot > 0;
            if (Mathf.Abs(f_dot) < 0.3)
            {
                anim.Play("ATTACK_2");
                isFacing = false;
            }
            else if (f_dot < 0)
            {
                anim.Play("ATTACK_1");
                isFacing = true;
            }
            else
            {
                anim.Play("ATTACK_3");
                isFacing = false;
            }
        }
        
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
        panic = false;
        attack = false;
    }

    async void PlayHitAnimation(float f_dot, float l_dot)
    {
        if (f_dot < -0.85)
        {
            anim.Play("HIT_0");
            isFacing = true;
        }
        else if (f_dot > 0.85)
        {
            anim.Play("HIT_4");
            isFacing = false;
        }
        else
        {
            anim.FlipH = l_dot > 0;
            if (Mathf.Abs(f_dot) < 0.3)
            {
                anim.Play("HIT_2");
                isFacing = false;
            }
            else if (f_dot < 0)
            {
                anim.Play("HIT_1");
                isFacing = true;
            }
            else
            {
                anim.Play("HIT_3");
                isFacing = false;
            }
        }
        
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
        isHit = false;
    }

    async void PlayDeathAnimation(int damageAmount)
    {
        if (!dead)
        {
            dead = true;
            if (health <= 0 && damageAmount >= 70)
            {
                anim.Play("EXPLODE");
            }
            else
            {
                anim.Play("DEATH");
            }
        }
        
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        SetProcess(false);
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
    }
    #endregion

    #region Public Methods
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        
        if (health <= 0)
        {
            PlayDeathAnimation(damageAmount);
        }
        else
        {
            isHit = true;
        }
    }
    #endregion

    // private void OnAreaEntered(Area3D area)
    // {
    //     PlayerStats.ChangeHealth(-damage);
    //     GD.Print(PlayerStats.playerHealth);
    //     area.Monitoring = false;
    // }

    private void OnBodyEntered(Node3D body)
    {
        if (body.IsInGroup("PLAYER"))
        {
            GD.Print("Hit player mele");
            PlayerStats.ChangeHealth(-damage);
            GD.Print("Player Health " + PlayerStats.playerHealth);
            //QueueFree();
        }
    }
    private void OnFireFrameChanged()
    {
        if(attack && anim.Frame == 2 && !panic && attackRange)
        {
            var projectileInstance = projectile.Instantiate<Node3D>();
            projectileInstance.GlobalTransform = projectilePos.GlobalTransform;   
            GetTree().CurrentScene.AddChild(projectileInstance);
            attackRange = false;
        }
    }
}