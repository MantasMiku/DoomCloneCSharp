using Godot;
using System;
using System.Threading.Tasks;

public partial class HeadImpaled : Node3D
{
	AnimatedSprite3D anim;
	int hitCount = 0;
	bool isHit = false;
	AudioStreamPlayer hurtPlayer; // not 3D

	public override void _Ready()
	{
		anim = GetNode<AnimatedSprite3D>("AnimatedSprite3D");

		hurtPlayer = new AudioStreamPlayer(); // non-spatial
		AddChild(hurtPlayer);

		AddToGroup("ENEMY");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//GD.Print(IsInGroup("ENEMY"));
		if(!isHit)
		{
			anim.Play("Idle");
		}
	}
	public async void TakeDamage(int damageAmount)
	{
		if (isHit) return; // ignore hits during recovery

		isHit = true;
		PlayerStats.ChangeHealth(-20);
		anim.Play("Hit");
		hurtPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/SPIDERMINION/HURT.wav");
		hurtPlayer.VolumeDb = 0;
		hurtPlayer.Play();
		await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
		isHit = false;
	}
}
