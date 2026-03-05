using Godot;
using System.Collections.Generic;

public partial class Spider : CharacterBody3D
{
    private enum AnimState { Walk, Attack, Hit, Death }

    // Exported properties
    [Export] public float Speed = 2f;
    [Export] public int MinionCount = 5;
    [Export] public float SpawnRadius = 5f;

    // Node references
    private AnimatedSprite3D anim;
    private NavigationAgent3D navAgent;
    private RayCast3D ray;
    private Timer walkTimer;
    private Timer attackTimer;
    private Timer spawnTimer;
    private Timer deathTimer;
    private Timer shootTimer;
    private Area3D playerDetection;

    // Player tracking
    private Node3D player;
    private Node3D playerAuthority;

    // State
    private AnimState currentAnimState = AnimState.Walk;
    private bool isHit = false;
    private bool dead = false;
    private bool attack = false;
    private bool isClose = false;
    private bool playerDetected = false;
    private bool isShooting = false;
    private bool isFacing = false;

    // Movement state
    private bool isStateActive = false;
    private int currentMovementState;
    private int randomDirection;
    private int shouldWalkAgain;
    private int tryAttack = 2;

    // Stats
    public int Health = 1000;
    private int damage = 10;

    // Minions
    private PackedScene minionScene;
    private List<SpiderMinion> minions = new();
    private bool canSpawn = false;
    private bool spawnTimerStarted = false;

    // Movement
    private float smoothRotation;
    private float animSpeed = 8f;
    private Vector3 moveDirection = Vector3.Zero;

    // Godot lifecycle
    public override void _Ready()
    {
        AddToGroup("Minions");
        InitializeNodes();
        ConnectSignals();
        minionScene = ResourceLoader.Load<PackedScene>("res://ENTITIES/ENEMIES/SPIDERMINION/spider_minion.tscn");
    }

    public override void _Process(double delta)
    {
        if (!FindNearestPlayer()) return;

        UpdateStateMachine();
        UpdateAnimations();
        UpdateMovement(delta);
        MoveAndSlide();
    }

    // Initialization
    private void InitializeNodes()
    {
        anim = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        ray = GetNode<RayCast3D>("RayCast3D");
        walkTimer = GetNode<Timer>("WalkTimer");
        attackTimer = GetNode<Timer>("AttackTimer");
        spawnTimer = GetNode<Timer>("SpawnTimer");
        deathTimer = GetNode<Timer>("DeathTimer");
        shootTimer = GetNode<Timer>("ShootTimer");
        playerDetection = GetNode<Area3D>("DetectPlayer");
    }

    private void ConnectSignals()
    {
        anim.AnimationFinished += OnAnimationFinished;
        playerDetection.BodyEntered += OnPlayerDetected;
        walkTimer.Timeout += () => isStateActive = false;
        attackTimer.Timeout += OnAttackTimeout;
        spawnTimer.Timeout += () => canSpawn = true;
        deathTimer.Timeout += QueueFree;
        shootTimer.Timeout += OnShootCooldown;

    }

    // Player detection and tracking
    private bool FindNearestPlayer()
    {
        var players = GetTree().GetNodesInGroup("PLAYER");
        if (players.Count == 0) return false;

        float nearestDistance = float.MaxValue;
        
        foreach (Node p in players)
        {
            if (p is Node3D player3d)
            {
                if (p.IsMultiplayerAuthority())
                    playerAuthority = player3d;

                float distance = GlobalPosition.DistanceTo(player3d.GlobalPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    player = player3d;
                }
            }
        }
        return true;
    }

    private void OnPlayerDetected(Node3D body)
    {
        if (!body.IsInGroup("PLAYER")) return;
        
        if (body is TestPlayer testPlayer && !testPlayer.crouchPressed && !playerDetected)
        {
            playerDetected = true;
            canSpawn = true;
        }
            
    }

    // State machine
    private void UpdateStateMachine()
    {
        if (Health <= 0)
            currentAnimState = AnimState.Death;
        else if (attack)
            currentAnimState = AnimState.Attack;
        else if (isHit)
            currentAnimState = AnimState.Hit;
        else
            currentAnimState = AnimState.Walk;
    }

    // Movement
    private void UpdateMovement(double delta)
    {
        if (IsOnFloor())
            HandleGroundMovement(delta);
        else
            Velocity -= new Vector3(0, 10 * (float)delta, 0);
    }

    private void HandleGroundMovement(double delta)
    {
        Velocity = Vector3.Zero;
        float distanceToPlayer = Position.DistanceTo(player.Position);

        if (attack)
            RotateTowardsPlayer(delta);

        if (!isHit && !dead && !attack)
        {
            if (playerDetected)
                HandleCombatMovement(delta, distanceToPlayer);
            else
                RoamAround(delta);
        }

        CheckAttackRange(distanceToPlayer);
    }

    private void HandleCombatMovement(double delta, float distanceToPlayer)
    {
        navAgent.TargetPosition = player.GlobalPosition;
        var nextNavPoint = navAgent.GetNextPathPosition();
        Vector3 target = new Vector3(nextNavPoint.X, GlobalPosition.Y, nextNavPoint.Z);

        // Minion spawning logic
        if (!spawnTimerStarted && minions.Count < MinionCount)
        {
            spawnTimer.Start();
            spawnTimerStarted = true;
        }

        if (canSpawn)
        {
            SpawnMinions();
            isStateActive = false;
        }
        else if (!isStateActive && !isClose && !attack)
        {
            InitiateNewMovementState();
        }

        ExecuteMovementState(target, delta, distanceToPlayer);
        
        moveDirection = -Transform.Basis.Z;
        Velocity = moveDirection * Speed;
    }

    private void InitiateNewMovementState()
    {
        walkTimer.Start();
        currentMovementState = (int)GD.RandRange(0, 10);
        randomDirection = (int)GD.RandRange(0, 1);
        shouldWalkAgain = (int)GD.RandRange(0, 1);
        isStateActive = true;
        tryAttack = 2;
    }

    private void ExecuteMovementState(Vector3 target, double delta, float distanceToPlayer)
    {
        LookAt(target, Vector3.Up);

        if (currentMovementState <= 4)
        {
            smoothRotation = Mathf.LerpAngle(smoothRotation, Rotation.Y, (float)delta * animSpeed);
            Rotation = new Vector3(Rotation.X, smoothRotation, Rotation.Z);

            if (distanceToPlayer <= 100 && !attack && tryAttack == 2)
            {
                tryAttack = (int)GD.RandRange(0, 1);
                if (tryAttack == 1)
                {
                    attackTimer.Start();
                    attack = true;
                }
            }
        }
        else
        {
            float direction = randomDirection == 1 ? -50f : 50f;
            RotateBy(Mathf.DegToRad(direction), delta);
        }
    }

    private void RoamAround(double delta)
    {
        RotateBy(Mathf.DegToRad(10f), delta);
        moveDirection = -Transform.Basis.Z;
        Velocity = moveDirection * Speed;
    }

    private void RotateBy(float angleChange, double delta)
    {
        float targetRotation = Rotation.Y + angleChange;
        smoothRotation = Mathf.LerpAngle(smoothRotation, targetRotation, (float)delta * animSpeed);
        Rotation = new Vector3(Rotation.X, smoothRotation, Rotation.Z);
    }

    private void RotateTowardsPlayer(double delta)
    {
        Vector3 toTarget = (player.GlobalPosition - GlobalPosition);
        toTarget.Y = 0;
        toTarget = toTarget.Normalized();

        float targetY = Mathf.Atan2(-toTarget.X, -toTarget.Z);
        smoothRotation = Mathf.LerpAngle(smoothRotation, targetY, (float)delta * 2f);
        Rotation = new Vector3(Rotation.X, smoothRotation, Rotation.Z);
    }

    private void CheckAttackRange(float distanceToPlayer)
    {
        if (distanceToPlayer <= 8)
        {
            attack = true;
            playerDetected = true;
            isClose = true;
        }
        else
        {
            isClose = false;
        }

        if (isHit && distanceToPlayer <= 100 && !playerDetected)
        {
            playerDetected = true;
            canSpawn = true;
        }
    }

    // Minion management
    private void SpawnMinions()
    {
        if (minionScene == null) return;

        int toSpawn = Mathf.Max(0, MinionCount - minions.Count);
        for (int i = 0; i < toSpawn; i++)
        {
            float angle = i * Mathf.Tau / MinionCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * SpawnRadius;

            SpiderMinion minion = minionScene.Instantiate<SpiderMinion>();
            minion.owner = this;
            minion.AddToGroup("MINION");

            GetParent().AddChild(minion); 

            minion.GlobalPosition = GlobalPosition + offset; 

            minions.Add(minion);

        }
        
        canSpawn = false;
        spawnTimerStarted = false;
    }

    public void DetectMinionDeath(SpiderMinion minion)
    {
        minions.Remove(minion);
    }

    // Animation system
    private void UpdateAnimations()
    {
        CalculateAnimationDirections(out float forwardDot, out float leftDot);

        switch (currentAnimState)
        {
            case AnimState.Attack:
                
                PlayAttackAnimation(forwardDot, leftDot);
                break;
            case AnimState.Hit:
                PlayHitAnimation(forwardDot, leftDot);
                break;
            case AnimState.Walk:
                PlayWalkAnimation(forwardDot, leftDot);
                break;
        }
    }

    private void CalculateAnimationDirections(out float forwardDot, out float leftDot)
    {
        var playerForward = playerAuthority.GlobalTransform.Basis.Z;
        var forward = GlobalTransform.Basis.Z;
        var left = GlobalTransform.Basis.X;
        leftDot = left.Dot(playerForward);
        forwardDot = forward.Dot(playerForward);
    }

    private void PlayWalkAnimation(float forwardDot, float leftDot)
    {
        if (forwardDot < -0.85)
        {
            anim.Play("WALK_0");
            isFacing = true;
        }
        else if (forwardDot > 0.85)
        {
            anim.Play("WALK_4");
            isFacing = false;
        }
        else
        {
            anim.FlipH = leftDot > 0;
            if (Mathf.Abs(forwardDot) < 0.3)
            {
                anim.Play("WALK_2");
                isFacing = false;
            }
            else if (forwardDot < 0)
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

    private async void PlayAttackAnimation(float forwardDot, float leftDot)
    {
        if(isHit)
        {
            PlayHitAnimation(forwardDot, forwardDot );
        }
        else
        {
            if (!isShooting)
            {
                shootTimer.Start();
                isShooting = true;
            }

            if (forwardDot < -0.85)
            {
                anim.Play("ATTACK_0");
                if (isShooting) 
                    ray.LookAt(new Vector3(player.Position.X, player.Position.Y+1,player.Position.Z));
                
                isFacing = true;
            }
            else if (forwardDot > 0.85)
            {
                anim.Play("ATTACK_4");
                isFacing = false;
            }
            else
            {
                anim.FlipH = leftDot > 0;
                if (Mathf.Abs(forwardDot) < 0.3)
                {
                    anim.Play("ATTACK_2");
                    isFacing = false;
                }
                else if (forwardDot < 0)
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
            if (!isClose && tryAttack != 1)
            {
                shootTimer.Stop();
                attack = false;
                isShooting = false;
                ray.SetProcess(false);
            }
        }
    }

    private async void PlayHitAnimation(float forwardDot, float leftDot)
    {
        if(canSpawn)
            isHit = false;

        if (forwardDot < -0.85)
        {
            anim.Play("HIT_0");
            isFacing = true;
        }
        else if (forwardDot > 0.85)
        {
            anim.Play("HIT_4");
            isFacing = false;
        }
        else
        {
            anim.FlipH = leftDot > 0;
            if (Mathf.Abs(forwardDot) < 0.3)
            {
                anim.Play("HIT_2");
                isFacing = false;
            }
            else if (forwardDot < 0)
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

    private async void PlayDeathAnimation()
    {
        if (!dead)
        {
            dead = true;
            anim.Play("DEATH");
        }
        
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        deathTimer.Start();
        SetProcess(false);
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
    }

    private void OnAnimationFinished()
    {
        if (anim.Animation == "WALK_0" || anim.Animation == "WALK_4")
            anim.FlipH = !anim.FlipH;
    }

    // Combat
    private void OnShootCooldown()
    {
        var rayHit = ray.GetCollider();

        if (rayHit is TestPlayer testPlayer)
        {
            GD.Print(testPlayer.crouchSlide);
            if(!testPlayer.IsInvulnerable)
            {
                ray.SetProcess(true);
                PlayerStats.ChangeHealth(-1);
            }
            else
            {
                ray.SetProcess(false);
                //PlayerStats.ChangeHealth(-1);
            }
                
        }

        isShooting = false;
    }

    private void OnAttackTimeout()
    {
        attack = false;
        tryAttack = 0;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void TakeDamage(int damageAmount)
    {
        Health -= damageAmount;
        
        if (Health <= 0)
            PlayDeathAnimation();
        else if (damageAmount > 20)
            isHit = true;
    }
}