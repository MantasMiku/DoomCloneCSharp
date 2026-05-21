using System;
using Godot;

public partial class Soldier : CharacterBody3D
{
    private enum AnimState { Walk, Attack, Hit, Death }

    // Exported properties
    [Export] public float Speed = 3f;

    // Node references
    private AnimatedSprite3D anim;
    private NavigationAgent3D navAgent;
    private RayCast3D ray;
    private Timer walkTimer;
    private Timer attackTimer;
    private Timer deathTimer;
    private Timer shootTimer;
    private Timer runAwayTimer;
    private Timer zigZagTimer;
    private Area3D playerDetection;
    //private Area3D avoidanceDetection;

    // Player tracking
    private Node3D player;
    private Node3D playerAuthority;

    // State
    private AnimState currentAnimState = AnimState.Walk;
    private bool isHit = false;
    private bool dead = false;
    private bool attack = false;
    private bool isClose = false;
    private bool isShooting = false;
    private bool isFacing = false;
    private bool zigZagRight = true;
    private bool smoothTurnAfterRunAway = false;

    // Movement state
    private bool isStateActive = false;
    private int currentMovementState;
    private int randomDirection;
    // Stats
    public int Health = 250;
    public int damage = 5;

    // Movement
    private float smoothRotation;
    private float animSpeed = 8f;
    private Vector3 moveDirection = Vector3.Zero;
    public int MinDrops = 1;
	public int MaxDrops = 1;
	public float DropRadius = 1.0f;
    private bool playerDetected = false;
    AudioStreamPlayer3D soldierPlayer;
    AudioStreamPlayer3D soldierShootPlayer;
	AudioStreamPlayer3D  soldierWalkPlayer;
	AudioStream walkSound;
    private bool _attackAnimPlaying = false;
    AnimatedSprite3D animEffect;
    int scoreValue = 200;
    PackedScene[] DropPrefabs =
	{
		ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/clip.tscn"),
		ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/shells.tscn"),
        ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/stim.tscn"),
	};

    // Godot lifecycle
    public override void _Ready()
    {
        animEffect = GetNode<AnimatedSprite3D>("AnimatedSprite3D2");
        soldierPlayer = new AudioStreamPlayer3D();
        AddChild(soldierPlayer);
		soldierWalkPlayer = new AudioStreamPlayer3D();
        AddChild(soldierWalkPlayer);
        soldierShootPlayer = new AudioStreamPlayer3D();
        AddChild(soldierShootPlayer);

		walkSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsbgact.wav");

        soldierPlayer.UnitSize = 3;
        soldierShootPlayer.UnitSize = 5;
        soldierWalkPlayer.UnitSize = 5;
        InitializeNodes();
        ConnectSignals();
        attack = false;
        isStateActive = true;
        zigZagRight = true;
        isClose = false;
        playerDetected = true;
        walkTimer.Start();
        zigZagTimer.Start();
        anim.Visible=false;
        PlayEffect();
        // walkTimer.Start();
        // zigZagTimer.Start();
        
    }
    async void PlayEffect()
	{
		animEffect.Play("SPAWN");
		await ToSignal(animEffect, AnimationPlayer.SignalName.AnimationFinished);
		animEffect.Visible = false;
		anim.Visible=true;
	}

    public override void _Process(double delta)
    {
        if(animEffect.Visible)	
			return;
        //GD.Print(zigZagRight);
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
        deathTimer = GetNode<Timer>("DeathTimer");
        shootTimer = GetNode<Timer>("ShootTimer");
        runAwayTimer = GetNode<Timer>("RunAwayTimer");
        zigZagTimer = GetNode<Timer>("ZigZagTimer");
        playerDetection = GetNode<Area3D>("DetectPlayer");
        //avoidanceDetection = GetNode<Area3D>("AvoidanceDetection");
    }

    private void ConnectSignals()
    {
        anim.AnimationFinished += OnAnimationFinished;
        //walkTimer.Timeout += () => isStateActive = false;
        walkTimer.Timeout += OnWalkTimeout;
        //walkTimer.TimeLeft 
        attackTimer.Timeout += OnAttackTimeout;
        deathTimer.Timeout += QueueFree;
        shootTimer.Timeout += OnShootCooldown;
        runAwayTimer.Timeout += OnRunAwayTimeout;
        zigZagTimer.Timeout += OnZigZagTimeout;
        playerDetection.BodyEntered += OnPlayerDetected;
        //avoidanceDetection.BodyEntered += OnDetectionEntered;
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
    // private void OnDetectionEntered(Node body)
    // {
    //     if (body.IsInGroup("ENEMY"))
    //     {
    //         // Avoidance logic can be added here
    //     }
    // }

    // State machine
    private void UpdateStateMachine()
    {
        if (Health <= 0)
            currentAnimState = AnimState.Death;
        else if (attack && !isHit)
            currentAnimState = AnimState.Attack;
        else if (isHit)
            currentAnimState = AnimState.Hit;
        else if(!isHit)
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
        if(playerDetected)
        {
            Velocity = Vector3.Zero;
            navAgent.TargetPosition = player.GlobalPosition;
            var nextNavPoint = navAgent.GetNextPathPosition();
            Vector3 target = new Vector3(nextNavPoint.X, GlobalPosition.Y, nextNavPoint.Z);
            float distanceToPlayer = Position.DistanceTo(player.Position);

            if (anim.Animation == "WALK_0")
                ray.LookAt(player.Position);

            if (attack)
            {
                RotateTowardsPlayer(delta);
            } 

            if (!isHit && !dead)
            {          
                HandleCombatMovement(target, delta, distanceToPlayer);       
            }
                

            CheckAttackRange(distanceToPlayer);
        }
        else
        {
            RoamAround(delta);
        }
    }
    private void RoamAround(double delta)
    {
        RotateBy(Mathf.DegToRad(10f), delta);
        moveDirection = -Transform.Basis.Z;
        Velocity = moveDirection * Speed;
    }

    private void HandleCombatMovement(Vector3 target, double delta, float distanceToPlayer)
    {
        
        LookAt(target, Vector3.Up);
        StatesMovementShootingLogic(delta);

        moveDirection = -Transform.Basis.Z;
        if(!attack)
            Velocity = moveDirection * Speed;
    }
    // private bool SmoothLookAtOnce(Vector3 target, double delta, float speed = 5f)
    // {
    //     Vector3 dir = target - GlobalPosition;
    //     dir.Y = 0;

    //     if (dir.LengthSquared() < 0.0001f)
    //         return true;

    //     dir = dir.Normalized();

    //     float targetY = Mathf.Atan2(-dir.X, -dir.Z);
    //     float newY = Mathf.LerpAngle(Rotation.Y, targetY, (float)delta * speed);
    //     Rotation = new Vector3(Rotation.X, newY, Rotation.Z);

    //     float angleDiff = Mathf.Abs(Mathf.AngleDifference(newY, targetY));
    //     return angleDiff < 0.08f;
    // }
    void ChoosePath()
    {
        currentMovementState = (int)GD.RandRange(0, 2);
    }
    void StatesMovementShootingLogic(double delta)
    {
        if(!isClose)
        {
            //RotateBy(Mathf.DegToRad(180), delta); 
            Speed = 4f;
            if(isStateActive)
            {
                // if(currentMovementState == 0)
                // {
                    
                // }  
                if(zigZagRight)
                {
                    RotateBy(Mathf.DegToRad(60), delta);
                }
                else
                {
                    RotateBy(Mathf.DegToRad(-60), delta);
                }

            }
        }
        else
        {
            if(!attack)
            {
                RotateBy(Mathf.DegToRad(180), delta);    
                Speed = 6;
                if(zigZagRight)
                {
                    RotateBy(Mathf.DegToRad(60), delta);
                }
                else
                {
                    RotateBy(Mathf.DegToRad(-60), delta);
                }
            }  
        }       
    }
    private void RotateBy(float angleChange, double delta)
    {
        float targetRotation = Rotation.Y + angleChange;
        smoothRotation = Mathf.LerpAngle(smoothRotation, targetRotation, (float)delta * animSpeed);
        Rotation = new Vector3(Rotation.X, smoothRotation, Rotation.Z);
    }

    private void RotateTowardsPlayer(double delta)
    {
        Vector3 toTarget = player.GlobalPosition - GlobalPosition;
        toTarget.Y = 0;
        toTarget = toTarget.Normalized();

        float targetY = Mathf.Atan2(-toTarget.X, -toTarget.Z);
        smoothRotation = Mathf.LerpAngle(smoothRotation, targetY, (float)delta * 2f);
        Rotation = new Vector3(Rotation.X, smoothRotation, Rotation.Z);
    }
    private void CheckAttackRange(float distanceToPlayer)
    {
        if (distanceToPlayer <= 4 && !isClose)
        {
            isClose = true;
            walkTimer.Stop();
            //attackTimer.Stop();
            isStateActive = false;
            if(attack)
            {
                runAwayTimer.Start();
                attack = false;
            }
            else if(!attack)
            {
                attack = true;
                attackTimer.Start();
            }       
        }
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
        if (isHit)
        {
            shootTimer.Stop();
            isShooting = false;
            attack = false;
            ray.SetProcess(false);
        }

        if (!isShooting && attack)
        {
            shootTimer.Start();
            isShooting = true;

        }

        if (forwardDot < -0.85)
        {
            anim.Play("ATTACK_0");
            isFacing = true;
            if (isShooting)
                ray.LookAt(new Vector3(player.Position.X, player.Position.Y+1.5f,player.Position.Z));
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

        if (!isShooting && attack)
        {
            shootTimer.Start();
            isShooting = true;
        }

        if (_attackAnimPlaying) return; // prevent stacking
        _attackAnimPlaying = true;
        
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
        _attackAnimPlaying = false;

        // if (!isClose)
        // {
        //     shootTimer.Stop();
        //     isShooting = false;
        //     attack = false;
        //     ray.SetProcess(false);
        // }
    }

    private async void PlayHitAnimation(float forwardDot, float leftDot)
    {
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

    private async void PlayDeathAnimation(int damageAmount)
    {
        if (!dead)
		{
            DropLoot();
			dead = true;
			if (Health <= 0 && damageAmount >= 150)
			{
                soldierPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsslop.wav");
                soldierPlayer.Play();
				anim.Play("EXPLODE");
                PlayerStats.ChangeScore(scoreValue + 100);
			}
			else
			{
                soldierPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dspodth3.wav");
                soldierPlayer.Play();
				anim.Play("DEATH");
                PlayerStats.ChangeScore(scoreValue);
			}
		}

        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        deathTimer.Start();
        SetProcess(false);
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
    }
    private void DropLoot()
	{
		if (DropPrefabs == null || DropPrefabs.Length == 0)
			return;

		int dropCount = (int)GD.RandRange(MinDrops, MaxDrops);

		for (int i = 0; i < dropCount; i++)
		{
			PackedScene scene = DropPrefabs[GD.RandRange(0, DropPrefabs.Length - 1)];
			if (scene == null)
				continue;

			Node3D pickup = scene.Instantiate<Node3D>();

			float angle = (float)GD.RandRange(0, Mathf.Tau);
			float radius = (float)GD.RandRange(0.2f, DropRadius);
			Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.2f, Mathf.Sin(angle) * radius);

            GetParent().AddChild(pickup);
			pickup.GlobalPosition = GlobalPosition + offset;
			
		}
	}


    private void OnAnimationFinished()
    {
        if (anim.Animation == "WALK_0" || anim.Animation == "WALK_4")
            anim.FlipH = !anim.FlipH;
    }

    // Combat
    private void OnShootCooldown()
    {
        if (dead || isHit)
            return;

        ray.ForceRaycastUpdate();

        var rayHit = ray.GetCollider();
        soldierShootPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dspistol.wav");
        soldierShootPlayer.Play();
        if (rayHit is TestPlayer testPlayer)
        {
            if (!testPlayer.IsInvulnerable)
            {
                PlayerStats.ChangeHealth(-damage);
            }
        }

        isShooting = false;
    }
    private void OnAttackTimeout()
    {
        soldierShootPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsposact.wav");
        soldierShootPlayer.VolumeDb = 5;
        soldierShootPlayer.Play();
        isShooting = false;
        shootTimer.Stop();

        if (isClose)
        {
            runAwayTimer.Start();
            attack = false;
            zigZagTimer.Start();
            zigZagRight = true;
        }
        else
        {
            attack = false;
            ray.SetProcess(true);
            isStateActive = true;
            walkTimer.Start();
            zigZagTimer.Start();
            zigZagRight = true;
        }
    }
    private void OnWalkTimeout()
    {
        
        if(!isClose)
        {
            isStateActive = false;
            attackTimer.Start();
            attack = true; 
            zigZagRight = true;
            ChoosePath();
        }
    }
    private void OnRunAwayTimeout()
    {
        soldierShootPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsposact.wav");
        soldierShootPlayer.VolumeDb = 5;
        soldierShootPlayer.Play();
        runAwayTimer.Stop();
        isClose = false;
        walkTimer.Start();
        smoothTurnAfterRunAway = true;
        zigZagRight = true;
        
    }
    private void OnZigZagTimeout()
    {
        
        if(isStateActive || isClose)
        {
           if(zigZagRight)
            {
                zigZagRight = false;
            }
            else
            {
                zigZagRight = true;
            }
            zigZagTimer.Start(); 
        }
    }
    private void OnPlayerDetected(Node3D body)
    {
        if (!body.IsInGroup("PLAYER")) return;
        
        if (body is TestPlayer testPlayer && !testPlayer.crouchPressed && !playerDetected)
        {
            playerDetected = true;
            walkTimer.Start();
            zigZagTimer.Start();
        }
            
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void TakeDamage(int damageAmount)
    {
        soldierPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dspopain.wav");
        soldierPlayer.Play();
        Health -= damageAmount;
        
        if (Health <= 0)
            PlayDeathAnimation(damageAmount);
        else
            isHit = true;
    }
}