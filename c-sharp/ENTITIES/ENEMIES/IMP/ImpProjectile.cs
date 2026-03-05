using Godot;
using System;
using System.Data;

public partial class ImpProjectile : Node3D
{
	// Called when the node enters the scene tree for the first time.
	[Export] public float Speed = 40f;
    private Vector3 direction;
	AnimatedSprite3D anim;
    float timer;
    bool explode = false;
    int damage = 10;

    public override void _Ready()
    {
        direction = -GlobalTransform.Basis.Z;
		anim = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        var hitbox = GetNode<Area3D>("Area3D");
        hitbox.BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
		
        timer += (float)delta;
        if(timer >= 4)
        {
            SetProcess(false);
            Explode();
        }
        else if(!explode)
        {
            anim.Play("IDLE");
            GlobalPosition += direction * Speed * (float)delta;
        }
    }

    public async void Explode()
    {
        anim.Play("EXPLODE");
        await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
        anim.Visible = false;
        QueueFree();
    }
    private void OnBodyEntered(Node3D body)
    {
        if (body.IsInGroup("PLAYER"))
        {
            GD.Print("Hit player proj");
            PlayerStats.ChangeHealth(-damage);
            explode = true;
            Explode();
        }
    }

}
