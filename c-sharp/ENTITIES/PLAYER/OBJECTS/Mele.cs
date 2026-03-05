using Godot;
using System;
using System.Collections.Generic;

public partial class Mele : Node3D
{
    
    List<Node3D> enemiesInHitbox = new List<Node3D>();
	AnimatedSprite2D gun;
    AnimatedSprite2D gun2;
    AnimatedSprite2D gun3;
    AnimatedSprite2D gun4;
    Sprite2D hand;
    Area3D hitBox;
	bool shoot = false;
    bool canShoot = false;
    bool shootHold = false;
    double timer = 0.0;
    float speedScale = 1;
    bool enemyEntered = false;
    int damage = 30;
	public override void _Ready()
    {
        gun = GetNode<AnimatedSprite2D>("CenterContainer/GUN");
        gun2 = GetNode<AnimatedSprite2D>("CenterContainer/GUN2");
        gun3 = GetNode<AnimatedSprite2D>("CenterContainer/GUN3");
        gun4 = GetNode<AnimatedSprite2D>("CenterContainer/GUN4");
        hand = GetNode<Sprite2D>("CenterContainer/HAND");
        gun2.Visible = false;
        gun3.Visible = false;
        gun4.Visible = false;
        //hitBox.SetProcess(false);
        hitBox = GetNode<Area3D>("Area3D");
        hitBox.BodyEntered += BodyEntered;
        hitBox.BodyExited += BodyExited;
    }

    private void BodyExited(Node3D body)
    {
        if (body.IsInGroup("ENEMY"))
        {
            enemiesInHitbox.Add(body);
        }
    }


    private void BodyEntered(Node3D body)
    {
        if (body.IsInGroup("ENEMY"))
        {
            enemiesInHitbox.Remove(body);
        }
    }

    public override void _Process(double delta)
    {
		if (!shoot)
        {
			hand.Visible = true;
			gun.Visible = false;
			gun.Play("IDLE");
            gun2.Play("IDLE");
            gun3.Play("IDLE");
            gun4.Play("IDLE");
        }

        if(Input.IsActionPressed("Shoot") && !shoot)
        {
            //hitBox.SetProcess(true);
            Shoot();
            timer += delta * 100;
            GD.Print(timer);
            if (timer >= 4)
            {
                SecondPunch(delta);
                speedScale = 1.2f;
                //damage = 60;
            }
                
            if (timer >= 8)
            {
                ThirdPunch();
                speedScale = 1.5f;
                //damage = 120;
            }
                
            if (timer >= 12)
            {
                FourthPunch();
                speedScale = 1.8f;
                //damage = 130;
            }
                
        }
        else
        {
            //hitBox.SetProcess(false);     
        }
        
        if(Input.IsActionJustReleased("Shoot"))
        {
            timer = 0.0;
            speedScale = 1;
            gun2.Visible = false;
            gun3.Visible = false;
            gun4.Visible = false;
        }
    }

	public async void Shoot()
    {
        shoot = true;
		hand.Visible = false;
		gun.Visible = true;
		gun.Play("SHOOT");
        gun.SpeedScale = speedScale;
        // Deal damage to enemies in hitbox
        foreach (var enemyNode in enemiesInHitbox)
        {
            if (enemyNode.IsInGroup("ENEMY"))// cast to your Enemy script
            {
                enemyNode.Call("TakeDamage", damage);            
            }
        }
		await ToSignal(gun, AnimationPlayer.SignalName.AnimationFinished);
        shoot = false;
    }

    public async void SecondPunch(double delta)
    {
		hand.Visible = false;
		gun2.Visible = true;
		gun2.Play("SHOOT");
        gun2.SpeedScale = speedScale;
        foreach (var enemyNode in enemiesInHitbox)
        {
            if (enemyNode.IsInGroup("ENEMY"))// cast to your Enemy script
            {
                enemyNode.Call("TakeDamage", damage);            
            }
        }
		await ToSignal(gun2, AnimationPlayer.SignalName.AnimationFinished);
    }
    public async void ThirdPunch()
    {
		hand.Visible = false;
		gun3.Visible = true;
		gun3.Play("SHOOT");
        gun3.SpeedScale = speedScale;
        foreach (var enemyNode in enemiesInHitbox)
        {
            if (enemyNode.IsInGroup("ENEMY"))// cast to your Enemy script
            {
                enemyNode.Call("TakeDamage", damage);            
            }
        }
		await ToSignal(gun3, AnimationPlayer.SignalName.AnimationFinished);
    }
    public async void FourthPunch()
    {
		hand.Visible = false;
		gun4.Visible = true;
		gun4.Play("SHOOT");
        gun4.SpeedScale = speedScale;
        foreach (var enemyNode in enemiesInHitbox)
        {
            if (enemyNode.IsInGroup("ENEMY"))// cast to your Enemy script
            {
                enemyNode.Call("TakeDamage", damage);            
            }
        }
		await ToSignal(gun4, AnimationPlayer.SignalName.AnimationFinished);
    }
}
