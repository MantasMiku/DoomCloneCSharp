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
	float speed = 5;
	#endregion

	#region Node References
	AnimatedSprite3D anim;
	AnimatedSprite3D animEffect;
	NavigationAgent3D navAgent;
	Area3D area;
	RayCast3D ray;
	PackedScene projectile;
	PackedScene med;
	PackedScene stim;
	PackedScene[] DropPrefabs =
	{
		ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/clip.tscn"),
		ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/shells.tscn"),
        ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/stim.tscn"),
	};
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
	public int health ;
	float smoothRot;
	float walkTime = 0f;
	float backTimer = 0f;
	float animSpeed = 5;
	int fromRange;
	int toRange;
	int damage = 10;

	Node3D player = null;
	Node3D playerAuthority = null;

	public int MinDrops = 1;
	public int MaxDrops = 1;
	public float DropRadius = 1.0f;
	int scoreValue = 100;
	private Timer deathTimer;

	AudioStreamPlayer3D impPlayer;
	AudioStreamPlayer3D impWalkPlayer;
	AudioStream walkSound;
	#endregion

	#region Godot Lifecycle
	public override void _Ready()
	{
		deathTimer = GetNode<Timer>("DeathTimer");
		deathTimer.Timeout += QueueFree;
		impPlayer = new AudioStreamPlayer3D();
        AddChild(impPlayer);
		impWalkPlayer = new AudioStreamPlayer3D();
        AddChild(impWalkPlayer);

		walkSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsbgact.wav");
		
		impWalkPlayer.Stream = walkSound;
		impPlayer.UnitSize = 3;
		impWalkPlayer.UnitSize = 5;
		// impWalkPlayer.Play();

		//animSpeed = animSpeed * speed;
		//AddToGroup("PLAYER");
		InitializeNodes();
		InitializeStats();
		projectile = ResourceLoader.Load<PackedScene>("res://ENTITIES/ENEMIES/IMP/imp_projectile.tscn");
		med = ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/med.tscn");
		stim = ResourceLoader.Load<PackedScene>("res://ENTITIES/PLAYER/OBJECTS/stim.tscn");
		area = GetNode<Area3D>("Area3D");
		//area.AreaEntered += OnAreaEntered;
		area.Monitoring = false;
		projectilePos = GetNode<Node3D>("ProjectilePos");
		anim.FrameChanged += OnFireFrameChanged;
		area.BodyEntered += OnBodyEntered;
		anim.Visible=false;
		PlayEffect();
		
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
		//GD.Print(Rotation);
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
		bool canPlayWalkSound = Velocity.Length() > 0.1f && !isHit && !attack;

		if (canPlayWalkSound)
		{
			if (!impWalkPlayer.Playing)
			{
				impWalkPlayer.Play();
			}
		}
		else
		{
			if (impWalkPlayer.Playing)
			{
				impWalkPlayer.Stop();
			}
		}
		
			
		//GD.Print(speed);
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
		animEffect = GetNode<AnimatedSprite3D>("AnimatedSprite3D2");
		navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		ray = GetNode<RayCast3D>("RayCast3D");

		// // Enable avoidance for smooth multi-enemy behavior
		// navAgent.AvoidanceEnabled = true;
		// navAgent.Radius = 2f; // Adjust based on enemy size
		// navAgent.MaxSpeed = speed;
	}

	void InitializeStats()
	{
		health = 200;
		// fromRange = (int)GD.RandRange(10, 12);
		// toRange = (int)GD.RandRange(6, 8);
		fromRange = 11;
		toRange = 8;
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
		HandleBehaviorLogic(distanceToPlayer, delta, target);
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
				WalkForward(target, nextNavPoint, delta, speed);
			}
		}
	}

	void HandleBehaviorLogic(float distanceToPlayer, double delta, Vector3 target)
	{
		if (ray.IsColliding())
		{
			HandleRayCollision(distanceToPlayer, target);
		}
		else
		{
			HandleNoRayCollision();
		}

		HandleRandomRangeUpdate();
		HandleBackwardTimer(distanceToPlayer, delta);
	}

	void HandleRayCollision(float distanceToPlayer, Vector3 target)
	{
		if (!panic)
		{
			if (distanceToPlayer > fromRange)
			{
				walkBack = false;
			}
			else if (distanceToPlayer <= toRange && !attack && !walkBack)
			{
				LookAt(target, Vector3.Up);
				Rotation = new Vector3(Rotation.X, Rotation.Y, Rotation.Z);
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
			// fromRange = (int)GD.RandRange(10, 12);
			// toRange = (int)GD.RandRange(6, 8);
			fromRange = 11;
			toRange = 8;
			//GD.Print($"New ranges - From: {fromRange}, To: {toRange}");
			lastWalkBackState = walkBack;
		}
	}

	void HandleBackwardTimer(float distanceToPlayer, double delta)
	{
		if (walkBack && distanceToPlayer < fromRange / 1.5f && !panic)
		{
			backTimer += (float)delta;
			if (backTimer > 1.5f)
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
			smoothRot = Mathf.LerpAngle(smoothRot, Rotation.Y, (float)delta * animSpeed * 6);
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
		//GD.Print(p_fwd);
		var fwd = GlobalTransform.Basis.Z;
		var left = GlobalTransform.Basis.X;
		l_dot = left.Dot(p_fwd);
		f_dot = fwd.Dot(p_fwd);
		//GD.Print(l_dot + " ", f_dot);
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
			DropLoot();
			if (health <= 0 && damageAmount >= 150)
			{
				anim.Play("EXPLODE");
				impWalkPlayer.Stop();
				impPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsslop.wav");
				impPlayer.Play();
				PlayerStats.ChangeScore(scoreValue + 50);
			}
			else
			{
				anim.Play("DEATH");
				impWalkPlayer.Stop();
				impPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsbgdth2.wav");
				impPlayer.Play();
				PlayerStats.ChangeScore(scoreValue);
			}
		}
		
		GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
		deathTimer.Start();
		SetProcess(false);
		await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
	}
	#endregion

	#region Public Methods
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

			GetParent().AddChild(pickup); // add first
			pickup.GlobalPosition = GlobalPosition + offset; // then set position
		}
	}

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
			impWalkPlayer.Stop();
			impPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dspopain.wav");
			impPlayer.Play();
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

	private async void OnBodyEntered(Node3D body)
	{
		if (body.IsInGroup("PLAYER") && panic)
		{
			//GD.Print("Hit player mele");
			PlayerStats.ChangeHealth(-damage);
			impWalkPlayer.Stop();
			impPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsclaw.wav");
			impPlayer.Play();
			//GD.Print("Player Health " + PlayerStats.playerHealth);
			//QueueFree();
		}
	}
	private void OnFireFrameChanged()
	{
		if(attack && anim.Frame == 1 && !panic && attackRange)
		{
			var projectileInstance = projectile.Instantiate<Node3D>();
			projectileInstance.GlobalTransform = projectilePos.GlobalTransform;   
			GetTree().CurrentScene.AddChild(projectileInstance);
			attackRange = false;
			impWalkPlayer.Stop();
			impPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsfirsht.wav");
			impPlayer.Play();
		}
	}
}
