using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerStats : Node
{
    private WeaponsManager weaponsManager;
	public static int playerHealth = 100;
    public static int playerMaxHealth = 200;
	public static int  playerArmour = 100;
	public static int  pistolAmmo = 100;
	public static int  shotgunAmmo = 80;
    public static int playerScore = 0;
    private static InGameUI playerUi;
    //private PackedScene mainMenuScene = ResourceLoader.Load<PackedScene>("res://LEVELS/UI/main_menu.tscn");

    public static void RegisterUI(InGameUI ui)
    {
        playerUi = ui;
    }
    public static void UnregisterUI()
    {
        playerUi = null;
    }
    public override void _Ready()
    {
        playerHealth = 100;
		playerArmour = 0;
		pistolAmmo = 50;
		shotgunAmmo = 24;
        playerScore = 0;
    }

    public override void _Process(double delta)
    {  
        TestMethod();
    }

    public static void ChangeScore(int ammount)
    {
        playerScore += ammount;
    }

	public static void ChangeHealth(int amount)
    {
        playerHealth += amount;
        if (playerHealth > playerMaxHealth)
            playerHealth = playerMaxHealth;

        if (amount < 0)
            playerUi.PlayerHurt(); // fire and forget explicitly, keeps async chain intact
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

        if (Input.IsKeyPressed(Key.F10))
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
    public static void Reset()
    {
        playerHealth = 100;
        pistolAmmo = 50;
        shotgunAmmo = 24;
        playerScore = 0;
    }
	
}
