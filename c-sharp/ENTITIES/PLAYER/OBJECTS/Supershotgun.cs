using Godot;
using System;
using System.Linq;

public partial class Supershotgun : Node3D
{
	AnimatedSprite2D gun;
    RayCast3D[] gunRays;
	bool shoot = false;
	bool canShoot = false;
	bool reload = false;
    int damage = 100;
	public override void _Ready()
    {
        gun = GetNode<AnimatedSprite2D>("CenterContainer/GUN");
        gunRays = GetNode<Node3D>("GunRays").GetChildren().OfType<RayCast3D>().ToArray();
		if (PlayerStats.shotgunAmmo >= 2)
        {
            canShoot = true;
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
		if(!shoot && !reload)
        {
            gun.Play("IDLE");
        }
        if(Input.IsActionJustPressed("Shoot") && !shoot && canShoot && !reload)
        {
           Shoot();
        }

		if (PlayerStats.shotgunAmmo >= 2 && !canShoot && !shoot)
        {
			canShoot = true;
            Reload();
        }
    }
    void CheckHit()
    {
        foreach (RayCast3D ray in gunRays)
        {
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
        }
    }
	public async void Shoot()
	{
        shoot = true;
		CheckHit();
		PlayerStats.ChangeShotGunAmmo(-2);
		GD.Print(PlayerStats.shotgunAmmo);
		shoot = true;
		gun.Play("SHOOT");
		await ToSignal(gun, AnimatedSprite2D.SignalName.AnimationFinished);
		if(PlayerStats.shotgunAmmo >= 2)
        {
            Reload();
        }
        else
        {
            canShoot = false;
			shoot = false;
        }
		
	}

	public async void Reload()
    {
		reload = true;
        gun.Play("RELOAD");
		await ToSignal(gun, AnimatedSprite2D.SignalName.AnimationFinished);
		shoot = false;
		canShoot = true;
		reload = false;
    }

}
