using Godot;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

public partial class InGameUI : Control
{
    public static InGameUI instance;
    MarginContainer pause;
    MarginContainer playerUI;
    Button resume;
    Button options;
    Button exit;
    Label health;
    Label ammo;
    Sprite2D ammoSprite;
    Sprite2D weaponSprite;
    ColorRect texture;
    private ShaderMaterial shaderMat;
    private Dictionary<AudioStreamPlayer, Tween> activeSoundTweens = new Dictionary<AudioStreamPlayer, Tween>();
    private Tween _tween;

    private AudioStreamPlayer uiSoundPlayer; 
    private AudioStreamPlayer transitionSoundPlayer;

    private AudioStream clickSound;
    private AudioStream transitionSound;
    
    [Export] public float FadeDuration = 0.3f;
    [Export] public float MinIntensity = 0.06f;
    [Export] public float MaxIntensity = 1.0f;

    private WeaponsManager weaponsManager;
     
    public override void _Ready()
    {
        pause = GetNode<MarginContainer>("Pause");
        playerUI = GetNode<MarginContainer>("PlayerUI");

        resume = GetNode<Button>("Pause/VBoxContainer/Button");
        options = GetNode<Button>("Pause/VBoxContainer/Button2");
        exit = GetNode<Button>("Pause/VBoxContainer/Button3");

        resume.Pressed += ResumePressed;
        options.Pressed += OptionsPressed;
        exit.Pressed += ExitPressed;

        uiSoundPlayer = new AudioStreamPlayer();
        AddChild(uiSoundPlayer);

        transitionSoundPlayer = new AudioStreamPlayer();
        AddChild(transitionSoundPlayer);

        clickSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/Tape Sounds/ButtonPressed.wav");
        transitionSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/Tape Sounds/Static01.wav");        

        pause.Visible = false;
        playerUI.Visible = true;
        
        texture = GetNode<ColorRect>("ColorRect");
        shaderMat = new ShaderMaterial();
        shaderMat.Shader = GD.Load<Shader>("res://LEVELS/UI/CRT.gdshader");
        texture.Material = shaderMat;

        health = GetNode<Label>("PlayerUI/HBoxContainer/VBoxContainer/Health");
        ammo = GetNode<Label>("PlayerUI/HBoxContainer/VBoxContainer2/Ammo");

        weaponSprite = GetNode<Sprite2D>("PlayerUI/HBoxContainer/VBoxContainer2/WeaponIcon");
        ammoSprite = GetNode<Sprite2D>("PlayerUI/HBoxContainer/VBoxContainer2/AmmoIcon");

        health.Text = "{PlayerStats.playerHealth}";
        ammoSprite.Texture = null;
        weaponSprite.Texture = null;
        texture.Visible = true;

        // Shader parameters
        shaderMat.SetShaderParameter("static_noise_intensity", MinIntensity);
        shaderMat.SetShaderParameter("roll_speed", 3);
        shaderMat.SetShaderParameter("scanlines_opacity", 0);
        shaderMat.SetShaderParameter("scanlines_width", 0.5);
        shaderMat.SetShaderParameter("aberration", 0.01);
        shaderMat.SetShaderParameter("brightness", 1.8);
    }

    public override void _PhysicsProcess(double delta)
    {
        health.Text = $"{PlayerStats.playerHealth}";
        if (weaponsManager == null || !IsInstanceValid(weaponsManager))
        {
            weaponsManager = FindWeaponsManager();
        }
        
        if (weaponsManager != null)
        {
            if(weaponsManager.GetCurrentWeaponName() == "HAND_PISTOL" || weaponsManager.GetCurrentWeaponName() == "MELE")
            {
                ammo.Text = "";
            }
            else
            {
                ammo.Text = $"{weaponsManager.GetCurrentWeaponAmmo()}";
            }
                
            if(weaponsManager.GetCurrentWeaponName() == "SHOTGUN")
            {
                ammoSprite.Texture = GD.Load<Texture2D>("res://GRAPHICS/SPRITES/PICKUPS/sboxa0.png");
            }
            else if(weaponsManager.GetCurrentWeaponName() == "SUPERSHOTGUN")
            {
                ammoSprite.Texture = GD.Load<Texture2D>("res://GRAPHICS/SPRITES/PICKUPS/sboxa0.png");
            }
            else if(weaponsManager.GetCurrentWeaponName() == "PISTOL")
            {
                ammoSprite.Texture = GD.Load<Texture2D>("res://GRAPHICS/SPRITES/PICKUPS/ammoa0.png");

            }
            else if(weaponsManager.GetCurrentWeaponName() == "MINIGUN")
            {
                ammoSprite.Texture = GD.Load<Texture2D>("res://GRAPHICS/SPRITES/PICKUPS/ammoa0.png");
            }
            else
            {
                ammoSprite.Texture = null;
                weaponSprite.Texture = null;
            }
        }
        else
        {
            ammo.Text = "--";
        }
    }

    public void HurtEffect()
    {
        shaderMat.SetShaderParameter("roll", true);
        shaderMat.SetShaderParameter("roll_speed", 100);
        shaderMat.SetShaderParameter("roll_variation", 5);
        shaderMat.SetShaderParameter("distort_intensity", 0.05);
        shaderMat.SetShaderParameter("noise_opacity", 0.15);
    }
    private WeaponsManager FindWeaponsManager()
    {
        // Try to find by group first (most reliable across scenes)
        var managers = GetTree().GetNodesInGroup("WeaponsManager");
        if (managers.Count > 0)
            return managers[0] as WeaponsManager;
        
        // Fallback: Try common paths
        var player = GetTree().GetFirstNodeInGroup("PLAYER");
        if (player != null)
        {
            return player.GetNodeOrNull<WeaponsManager>("Camera/Weapons");
        }
        
        return null;
    }

    private async void ExitPressed()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        await FadeIn();
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://LEVELS/UI/main_menu.tscn");
    }


    private async void ResumePressed()
    {
        await FadeIn();
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        pause.Visible = false;
        playerUI.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().Paused = false;
        await FadeOut();

    }


    private void OptionsPressed()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
    }


    public override async void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("Escape")) // recommended action
        {
            if (!pause.Visible)
            {
                GetTree().Paused = true;
                await FadeIn();
                PlaySoundOnPlayer(uiSoundPlayer, clickSound);
                pause.Visible = true;
                playerUI.Visible = false;
                Input.MouseMode = Input.MouseModeEnum.Visible;
                await FadeOut();
                shaderMat.SetShaderParameter("static_noise_intensity", MinIntensity);
                shaderMat.SetShaderParameter("roll_speed", 3);
                shaderMat.SetShaderParameter("roll_size", 15);
                shaderMat.SetShaderParameter("roll", true);
                shaderMat.SetShaderParameter("scanlines_opacity", 0);
                shaderMat.SetShaderParameter("scanlines_width", 0.5);
                shaderMat.SetShaderParameter("aberration", 0.01);
                shaderMat.SetShaderParameter("brightness", 1.8);

            }
            else
            {
                await FadeIn();
                PlaySoundOnPlayer(uiSoundPlayer, clickSound);
                pause.Visible = false;
                playerUI.Visible = true;
                Input.MouseMode = Input.MouseModeEnum.Captured;
                GetTree().Paused = false;
                await FadeOut();
                // Shader parameters
                
                
                shaderMat.SetShaderParameter("static_noise_intensity", MinIntensity);
                shaderMat.SetShaderParameter("roll_speed", 3);
                shaderMat.SetShaderParameter("roll_size", 0);
                shaderMat.SetShaderParameter("roll", false);
                shaderMat.SetShaderParameter("scanlines_opacity", 0);
                shaderMat.SetShaderParameter("scanlines_width", 0.5);
                shaderMat.SetShaderParameter("aberration", 0.005);
                shaderMat.SetShaderParameter("brightness", 1.8);
                

            }

            GetViewport().SetInputAsHandled();
        }
    }

    // ==================== SOUND SYSTEM ====================
    private void PlaySoundOnPlayer(AudioStreamPlayer player, AudioStream sound, float volumeDb = 0f)
    {
        if (sound == null || player == null) return;
        
        player.Stream = sound;
        player.VolumeDb = volumeDb;
        player.Play();
    }
    
    private void PlaySoundWithFadeOut(AudioStreamPlayer player, AudioStream sound, float fadeOutDuration, float volumeDb = 0f)
    {
        if (sound == null || player == null) return;
        
        player.Stream = sound;
        player.VolumeDb = volumeDb;
        player.Play();
        
        float soundLength = (float)sound.GetLength();
        float waitTime = soundLength - fadeOutDuration;
        
        if (waitTime > 0)
        {
            DelayedFadeOut(player, waitTime, fadeOutDuration, volumeDb);
        }
    }
    
    private async void DelayedFadeOut(AudioStreamPlayer player, float delay, float fadeOutDuration, float originalVolumeDb)
    {
        await Task.Delay((int)(delay * 1000));
        
        if (player.Playing)
        {
            FadeOutSoundPlayer(player, fadeOutDuration, originalVolumeDb);
        }
    }
    
    private void FadeOutSoundPlayer(AudioStreamPlayer player, float fadeOutDuration, float originalVolumeDb)
    {
        // Stop any existing tween for this player
        if (activeSoundTweens.ContainsKey(player))
        {
            activeSoundTweens[player]?.Kill();
        }
        
        var tween = CreateTween();
        activeSoundTweens[player] = tween;
        
        tween.TweenProperty(player, "volume_db", -80f, fadeOutDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        
        tween.TweenCallback(Callable.From(() => {
            player.Stop();
            player.VolumeDb = originalVolumeDb;
            activeSoundTweens.Remove(player);
        }));
    }
    
    private void StopAllSounds()
    {
        uiSoundPlayer?.Stop();
        transitionSoundPlayer?.Stop();
        
        foreach (var tween in activeSoundTweens.Values)
        {
            tween?.Kill();
        }
        activeSoundTweens.Clear();
    }

    private async Task FadeIn()
    {
        PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.2f);
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenMethod(Callable.From<float>(value => 
            shaderMat.SetShaderParameter("static_noise_intensity", value)
        ), MinIntensity, MaxIntensity, FadeDuration);
        
        await ToSignal(_tween, Tween.SignalName.Finished);
        StopAllSounds();

    }
    
    // Fade from full static back to minimal (1.0 -> 0.06)
    private async Task FadeOut()
    {
        PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.2f);
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenMethod(Callable.From<float>(value => 
            shaderMat.SetShaderParameter("static_noise_intensity", value)
        ), MaxIntensity, MinIntensity, FadeDuration);
        
        await ToSignal(_tween, Tween.SignalName.Finished);
        StopAllSounds();
    }

}
