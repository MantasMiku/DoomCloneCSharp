using Godot;
using System;

public partial class PlayerStats : Node
{
    private WeaponsManager weaponsManager;
	public static int playerHealth = 100;
	public static int  playerArmour = 100;
	public static int  pistolAmmo = 100;
	public static int  shotgunAmmo = 80;
    public InGameUI ui;

    public override void _Ready()
    {
        playerHealth = 100;
		playerArmour = 0;
		pistolAmmo = 1000;
		shotgunAmmo = 80;
        var rootScene = GetTree().CurrentScene;
        ui = rootScene.GetNode<InGameUI>("InGameUI");
    }

    public override void _Process(double delta)
    {
        TestMethod();
    }


	public static void ChangeHealth(int ammount)
    {
        playerHealth += ammount;
        //ui.HurtEffect();
    }

	public static void ChangeArmour(int ammount)
    {
        playerArmour += ammount;
    }

	public static void ChangePistolAmmo(int ammount)
    {
        pistolAmmo += ammount;
    }

	public static void ChangeShotGunAmmo(int ammount)
    {
        shotgunAmmo += ammount;
    }

	public static void PrintStats()
    {
        GD.Print("Helth: " + playerHealth +"\n"+ "Armour: " + playerArmour +"\n"+ "PistolAmmo: " + pistolAmmo +"\n"+ "ShotgunAmmo: " + shotgunAmmo);
    }

    public void ReloadScene()
    {
        var currentScene = GetTree().CurrentScene;

        var scenePath = currentScene.SceneFilePath;

        var newScene = ResourceLoader.Load<PackedScene>(scenePath);
        GetTree().ChangeSceneToPacked(newScene);
    }

    public void TestMethod()
    {
        var mode = DisplayServer.WindowGetMode();
        if (Input.IsKeyPressed(Key.O))
        {
            PlayerStats.shotgunAmmo += 2;
            PlayerStats.pistolAmmo += 2;
        }

        if (PlayerStats.playerHealth <= 0 || Input.IsKeyPressed(Key.F10))
        {
            ReloadScene();
        }
        if(Input.IsKeyPressed(Key.F12))
        {
            if (mode != DisplayServer.WindowMode.Fullscreen)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            }
            else
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            }
        }

        if (Input.IsKeyPressed(Key.P))
        {
            PlayerStats.PrintStats();
        }
    }
	
}
