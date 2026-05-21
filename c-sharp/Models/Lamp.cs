using Godot;
using System;

public partial class Lamp : Node3D
{
	AnimatedSprite3D anim;

	public override void _Ready()
	{
		anim = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
		anim.Play("Idle");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
