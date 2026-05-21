using Godot;
using System;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

public partial class PickUps : Node3D
{
	private Area3D area;
	AudioStreamPlayer3D pickUpPlayere;
	AudioStream healthSound;
	AudioStream ammoSound;
	bool playerEntered = false;
	public override void _Ready()
	{
		pickUpPlayere = new AudioStreamPlayer3D();
		AddChild(pickUpPlayere);
		healthSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsitemup.wav");
		ammoSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dswpnup.wav");
		area = GetNode<Area3D>("Area3D");
		area.BodyEntered += OnBodyEntered;
		pickUpPlayere.VolumeDb = -2;
	}

    private async void OnBodyEntered(Node3D body)
    {
        if(body.IsInGroup("PLAYER") && !playerEntered)
		{
			playerEntered = true;
			Visible = false;
			string path = SceneFilePath;

			if (path.EndsWith("med.tscn") && PlayerStats.playerHealth != PlayerStats.playerMaxHealth)
			{
				pickUpPlayere.Stream = healthSound;
				pickUpPlayere.Play();
				PlayerStats.ChangeHealth(25);
				InGameUI.instance.ShowPickup("+25 health");
				await ToSignal(pickUpPlayere, AudioStreamPlayer3D.SignalName.Finished);
				QueueFree();
			}
			else if (path.EndsWith("stim.tscn") && PlayerStats.playerHealth != PlayerStats.playerMaxHealth)
			{
				pickUpPlayere.Stream = healthSound;
				pickUpPlayere.Play();
				PlayerStats.ChangeHealth(10);
				InGameUI.instance.ShowPickup("+10 health");
				await ToSignal(pickUpPlayere, AudioStreamPlayer3D.SignalName.Finished);
				QueueFree();
			}
			else if (path.EndsWith("ammo.tscn"))
			{
				pickUpPlayere.Stream = ammoSound;
				pickUpPlayere.Play();
				PlayerStats.ChangePistolAmmo(50);
				InGameUI.instance.ShowPickup("+50 ammo");
				await ToSignal(pickUpPlayere, AudioStreamPlayer3D.SignalName.Finished);
				QueueFree();
			}
			else if (path.EndsWith("clip.tscn"))
			{
				pickUpPlayere.Stream = ammoSound;
				pickUpPlayere.Play();
				PlayerStats.ChangePistolAmmo(25);
				InGameUI.instance.ShowPickup("+25 ammo");
				await ToSignal(pickUpPlayere, AudioStreamPlayer3D.SignalName.Finished);
				QueueFree();
			}
			else if (path.EndsWith("shells.tscn"))
			{
				pickUpPlayere.Stream = ammoSound;
				pickUpPlayere.Play();
				PlayerStats.ChangeShotGunAmmo(6);
				InGameUI.instance.ShowPickup("+6 shells");
				await ToSignal(pickUpPlayere, AudioStreamPlayer3D.SignalName.Finished);
				QueueFree();
			}
			else if (path.EndsWith("shellbox.tscn"))
			{
				pickUpPlayere.Stream = ammoSound;
				pickUpPlayere.Play();
				PlayerStats.ChangeShotGunAmmo(24);
				InGameUI.instance.ShowPickup("+24 shells");
				await ToSignal(pickUpPlayere, AudioStreamPlayer3D.SignalName.Finished);
				QueueFree();
			}
			else
			{
				GD.Print("Unknown pickup type: " + path);
			}

		}
    }
    public override void _Process(double delta)
	{
		
	}
}
