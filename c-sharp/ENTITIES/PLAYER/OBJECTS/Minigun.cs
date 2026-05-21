using Godot;
using System;

public partial class Minigun : Node3D
{
	AnimatedSprite2D gun;
    AnimatedSprite2D fire;
    RayCast3D ray;
	bool shoot = false;
    bool canShoot = false;
    bool shootHold = false;
    int damage = 70;
    private AudioStreamPlayer ShootSoundPlayer;
    private AudioStream ShootSound;
	public override void _Ready()
    {
        ShootSoundPlayer = new AudioStreamPlayer();
        AddChild(ShootSoundPlayer);
        ShootSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dspistol.wav");

        gun = GetNode<AnimatedSprite2D>("CenterContainer/GUN");
        fire = GetNode<AnimatedSprite2D>("CenterContainer/FIRE");
        ray = GetNode<RayCast3D>("RayCast3D");
        ray.SetProcess(false);
        if(PlayerStats.pistolAmmo > 0)
        {
            canShoot = true;
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        if(PlayerStats.playerHealth <= 0)
            return;
            
		if (!shoot)
        {
			gun.Play("IDLE");
        }

        if(Input.IsActionPressed("Shoot") && !shoot && canShoot)
        {
            if(PlayerStats.pistolAmmo >= 2)
                Shoot();
        }

		if(Input.IsActionJustReleased("Shoot"))
        {
            fire.Visible = false;
        }

        if (PlayerStats.pistolAmmo > 0)
        {
            canShoot = true;
        }
        else
        {
            canShoot = false;
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
        PlayerStats.ChangePistolAmmo(-2);
        GD.Print(PlayerStats.pistolAmmo);
		fire.Visible = true;
		fire.Play("FIRE");
		gun.Play("SHOOT");
        ShootSoundPlayer.Stream = ShootSound;
        ShootSoundPlayer.Play();
		await ToSignal(fire, AnimationPlayer.SignalName.AnimationFinished);
        shoot = false;
    }
}
