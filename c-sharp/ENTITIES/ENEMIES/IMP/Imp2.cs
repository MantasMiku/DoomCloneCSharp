using Godot;
using System;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
public partial class Imp2 : CharacterBody3D
{
    // Animation states
    enum AnimState
    {
        Walk,
        Attack,
        Hit,
        Death
    }
    
    [Export] CharacterBody3D player;
    AnimatedSprite3D anim;
    [Export] float speed = 3;
    NavigationAgent3D navAgent;
    float smoothRot;
    bool isFacing = false;
    bool walkBack = false;
	bool isHit = false;
	bool dead = false;
    bool attack = false;
	public int health;
    float walkTime = 0f;
    float backTimer = 0f;
    bool panic = false;
    float animSpeed = 5f;
    bool lastWalkBackState = false;
    int fromRange;
    int toRange;

    RayCast3D ray;
    AnimState currentAnimState = AnimState.Walk;
    
    public override void _Ready()
    {
        anim = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        ray = GetNode<RayCast3D>("RayCast3D");
        health = 100;
        fromRange = (int)GD.RandRange(10, 14);
        toRange = (int)GD.RandRange(4, 8);
    }
    
    public override void _Process(double delta)
    {
        MovementLogic(delta);
        StateMachine();
        Animations();
        MoveAndSlide();
    }

    void MovementLogic(double delta)
    {
        //walk -> detect edge -> walk no no -> turn arround -> if not walk forward (repeat)
        //                                                  -> if player close attack -> turn arround (repeat)
         if(IsOnFloor())
        {
            Velocity = Vector3.Zero;
            navAgent.TargetPosition = player.GlobalPosition;
            var next_nav_point = navAgent.GetNextPathPosition();
            Vector3 target = new Vector3(next_nav_point.X, GlobalPosition.Y, next_nav_point.Z);
            float dist = Position.DistanceTo(player.Position);
            //GD.Print(dist);
            
            if(!walkBack && !panic)
            {
                // Walk forward 
				if(!isHit && !dead)
                {
                    WalkForward(target, next_nav_point, delta, speed);
                }
            }
            else if(walkBack && !panic)
            {
                // Walk backwards
				if(!isHit && !dead && !attack)
                {
                    WalkBack(target, next_nav_point, delta);
                }
            }
            else if(panic)
            {
                if(!isHit && !dead && !attack)
                {   
                    WalkForward(target, next_nav_point, delta, 4);       
                }
            }

            if(ray.IsColliding())
            {
                
                if (!panic)
                {
                    if(dist > fromRange)
                    {
                        //GD.Print("From Player" + fromRange);
                        walkBack = false;
                    }
                    else if(dist <= toRange && !attack && !walkBack)
                    {
                        //GD.Print("To Player" + toRange);
                        backTimer = 0f;
                        attack = true;
                        walkBack = true;
                    }
                }
                else if (panic && dist <= 2)
                {
                    attack = true;
                    panic = false;
                    walkBack = true;
                }
            }
            else
            {
                walkBack = false;
                panic = true;
            }

            if (walkBack != lastWalkBackState)
            {
                fromRange = (int)GD.RandRange(9, 12);
                toRange = (int)GD.RandRange(5, 8);
                GD.Print($"New ranges - From: {fromRange}, To: {toRange}");
                lastWalkBackState = walkBack;
            }
        
            if(walkBack && dist < fromRange/2 && !panic)
            {
                backTimer += (float)delta;
                //GD.Print(backTimer);
                if (backTimer > 2.5f)
                {
                    panic = true;
                    walkBack = false;
                }
            }
          
            if(panic)
            {
                animSpeed = 8f;
                backTimer = 0f;
            }
            else
            {
                animSpeed = 5f;
            }
        }
        else
        {
            Velocity -= new Vector3(0, 10 * (float)delta, 0);
        }
    }

    void WalkForward(Vector3 target, Vector3 next_nav_point, double delta, float sp)
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
        
        if (isFacing)
            Velocity = (next_nav_point - GlobalPosition).Normalized() * sp;
    }
    
    void WalkBack(Vector3 target, Vector3 next_nav_point, double delta)
    {
        LookAt(target, Vector3.Up);
        float targetRotation = Rotation.Y + Mathf.Pi; // This Pi Adds 180 degrees carzy man
        smoothRot = Mathf.LerpAngle(smoothRot, targetRotation, (float)delta * animSpeed);
        Rotation = new Vector3(Rotation.X, smoothRot, Rotation.Z);
        Velocity = (next_nav_point - GlobalPosition).Normalized() * -speed;
    }
    void StateMachine()
    {
        //Walk() to player if distance is too far (done)
        //WalkBack() if player too close walk back (done)
        //Attack() if distance is good perform attack 
        //MoveSides() move left or right when performing an attack to change position
        if (health <= 0)
		{
			currentAnimState = AnimState.Death;
		}
		else if(!isHit && !attack)
		{
			currentAnimState = AnimState.Walk;
		}
		else if(isHit)
		{
			currentAnimState = AnimState.Hit;
		}
        else if(attack)
        {
            currentAnimState = AnimState.Attack;
        }
    }
    
    void Animations()
    {
        var p_fwd = player.GlobalTransform.Basis.Z;
        var fwd = GlobalTransform.Basis.Z;
        var left = GlobalTransform.Basis.X;
        var l_dot = left.Dot(p_fwd);
        var f_dot = fwd.Dot(p_fwd);
        
        anim.FlipH = false;
        
        // Switches anim states
        switch(currentAnimState)
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
    
    void PlayWalkAnimation(float f_dot, float l_dot)
    {
        if (f_dot < -0.85)
        {
            anim.Play("WALK_0");
            isFacing = true;
        }
        else if(f_dot > 0.85)
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
            else if(f_dot < 0)
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
        else if(f_dot > 0.85)
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
            else if(f_dot < 0)
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
        attack = false;
    }
    
    async void PlayHitAnimation(float f_dot, float l_dot)
    {
        if (f_dot < -0.85)
        {
            anim.Play("HIT_0");
            isFacing = true;
        }
        else if(f_dot > 0.85)
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
            else if(f_dot < 0)
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
			if(health <= 0 && damageAmount >= 70)
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

	public void TakeDamage(int damageAmount)
	{
		health -= damageAmount;
		//GD.Print(damageAmount);
		if (health <= 0)
        {
            PlayDeathAnimation(damageAmount);
        }
        else
        {
            isHit = true;
        }
	}
}