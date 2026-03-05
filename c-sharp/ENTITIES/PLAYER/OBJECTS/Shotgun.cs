using Godot;
using System;

public partial class Shotgun : Node3D
{
	AnimatedSprite2D gun;
	AnimatedSprite2D fire;
	RayCast3D ray;
	bool shoot = false;
	bool canShoot = false;
	int damage = 100;
	public override void _Ready()
    {
        gun = GetNode<AnimatedSprite2D>("CenterContainer/GUN");
		fire = GetNode<AnimatedSprite2D>("CenterContainer/FIRE");
		fire.Visible = false;
		fire.FrameChanged += OnFireFrameChanged;

		ray = GetNode<RayCast3D>("RayCast3D");
        ray.SetProcess(false);

		if(PlayerStats.shotgunAmmo > 0)
        {
            canShoot = true;
        }

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
		if(!shoot)
        {
            gun.Play("IDLE");
        }
        if(Input.IsActionJustPressed("Shoot") && !shoot && canShoot)
        {
           Shoot();
        }

		if(PlayerStats.shotgunAmmo > 0)
        {
            canShoot = true;
        }
        else
        {
            canShoot = false;
        }
    }

	private void OnFireFrameChanged()
	{
		// When the muzzle flash reaches frame 1
		if (fire.Frame == 1)
		{
			gun.Position = new Vector2(gun.Position.X, gun.Position.Y + 8);
		}
		else
		{
			// Reset when the animation leaves frame 1
			gun.Position = new Vector2(gun.Position.X, 0);
		}
	}


	public async void Shoot()
	{
		shoot = true;
		ray.SetProcess(true);
        var rayHit = ray.GetCollider();

        if (rayHit is Node3D node)
        {
            if (node.IsInGroup("ENEMY"))
            {
				float dist = ray.GlobalPosition.DistanceTo(node.GlobalPosition);
				if(dist <= 12 && dist >= 0)
                {
                    node.Call("TakeDamage", damage);
                }  
				else if (dist <= 20 && dist > 12)
                {
					node.Call("TakeDamage", 70);
                }    
				else if (dist > 20)
                {
					node.Call("TakeDamage", 50);
                }   
            }
        
        }

		PlayerStats.ChangeShotGunAmmo(-1);
		GD.Print(PlayerStats.shotgunAmmo);
		fire.Visible = true;
		fire.Play("FIRE");

		await ToSignal(fire, AnimatedSprite2D.SignalName.AnimationFinished);
		fire.Visible = false;

		gun.Play("RELOAD");
		await ToSignal(gun, AnimatedSprite2D.SignalName.AnimationFinished);

		shoot = false;
	}

}
