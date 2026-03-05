using Godot;
using System;

public partial class HandPistol : Node3D
{
	AnimatedSprite2D gun;
    RayCast3D ray;
	bool shoot = false;
    bool canShoot = false;
    bool shootHold = false;
    int damage = 66666;
	public override void _Ready()
    {
        gun = GetNode<AnimatedSprite2D>("CenterContainer/GUN");
        ray = GetNode<RayCast3D>("RayCast3D");
        ray.SetProcess(false);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
		if (!shoot)
        {
			gun.Play("IDLE");
        }

        if(Input.IsActionPressed("Shoot") && !shoot)
        {
            Shoot();
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
                node.Call("TakeDamage", damage);
            }
        }
		gun.Play("SHOOT");
		await ToSignal(gun, AnimationPlayer.SignalName.AnimationFinished);
        shoot = false;
    }
}
