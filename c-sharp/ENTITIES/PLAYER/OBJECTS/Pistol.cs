using Godot;
using System;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

public partial class Pistol : Node3D
{
	AnimatedSprite2D gun;
    RayCast3D ray;
    Sprite2D fire;
	bool shoot = false;
    bool canShoot = false;
    bool shootHold = false;
    int damage = 50;
	public override void _Ready()
    {
        gun = GetNode<AnimatedSprite2D>("CenterContainer/GUN");
        fire = GetNode<Sprite2D>("CenterContainer/FIRE");
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
		if (!shoot)
        {
			gun.Play("IDLE");
        }

        if(Input.IsActionPressed("Shoot") && !shoot && canShoot)
        {
            Shoot();
        }

        if (PlayerStats.pistolAmmo > 0)
        {
            canShoot = true;
        }
        else
        {
            canShoot = false;
        }

        if (gun.Animation == "SHOOT" && gun.Frame == 0)
        {
            fire.Visible = true;
        }
        else
        {
            fire.Visible = false;
        }
    }

	public async void Shoot()
    {
        if (!IsMultiplayerAuthority()) return;
        
        shoot = true;
        
        // Local effects
        PlayerStats.ChangePistolAmmo(-1);
        GD.Print(PlayerStats.pistolAmmo);
        gun.Play("SHOOT");
        
        // Check if we're the server
        if (Multiplayer.IsServer())
        {
            // We ARE the server, process directly
            ServerShoot(GlobalTransform);
        }
        else
        {
            // We're a client, send to server
            RpcId(1, nameof(ServerShoot), GlobalTransform);
        }
        
        // Local raycast for feedback
        ray.Enabled = true;
        ray.ForceRaycastUpdate();
        
        if (ray.IsColliding())
        {
            var node = ray.GetCollider() as Node3D;
            if (node != null && node.IsInGroup("ENEMY"))
            {
                // Just show effect locally
                //ShowHitEffect(ray.GetCollisionPoint());
            }
        }
        
        ray.Enabled = false;
        
        await ToSignal(gun, AnimationPlayer.SignalName.AnimationFinished);
        shoot = false;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ServerShoot(Transform3D shootTransform)
    {
        if (!Multiplayer.IsServer()) return;
        
        // Server raycast
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            shootTransform.Origin,
            shootTransform.Origin - shootTransform.Basis.Z * 100f
        );
        
        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            var node = result["collider"].AsGodotObject() as Node3D;
            if (node != null && node.IsInGroup("ENEMY"))
            {
                node.Call("TakeDamage", damage);
            }
        }
    }
    void Hit()
    {
        
    }
}
