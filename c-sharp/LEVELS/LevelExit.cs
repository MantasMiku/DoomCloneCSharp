using Godot;
using System;

public partial class LevelExit : Area3D
{
	public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }
	private async void OnBodyEntered(Node3D body)
    {
        if(body.IsInGroup("PLAYER"))
		{
			GD.Print("1");
			GetTree().ChangeSceneToFile("res://LEVELS/UI/main_menu.tscn");
		}
	}
}
