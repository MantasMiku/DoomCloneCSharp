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
    MarginContainer death;
    MarginContainer playerUI;
    Button resume;
    Button options;
    Button exit;
    Button exitDeath;
    Button respawn;
    Label health;
    Label ammo;
    Label score;
    Sprite2D ammoSprite;
    Sprite2D weaponSprite;
    ColorRect texture;
    ColorRect textureHurt;

    private ShaderMaterial shaderMat;
    private Dictionary<AudioStreamPlayer, Tween> activeSoundTweens = new Dictionary<AudioStreamPlayer, Tween>();
    private Tween _tween;

    private AudioStreamPlayer uiSoundPlayer; 
    private AudioStreamPlayer transitionSoundPlayer;
     private AudioStreamPlayer hurtPlayer;

    private AudioStream clickSound;
    private AudioStream transitionSound;
    private AudioStream hurtSound;
    
    [Export] public float FadeDuration = 0.3f;
    [Export] public float MinIntensity = 0.06f;
    [Export] public float MaxIntensity = 1.0f;

    private Label[] labels;
    private Timer[] timers;

    private WeaponsManager weaponsManager;
    bool dead = false;
    bool deadSound = false;
     
    public override void _Ready()
    {
        
        PlayerStats.Reset();
        instance = this;
        PlayerStats.RegisterUI(this);
        pause = GetNode<MarginContainer>("Pause");
        death = GetNode<MarginContainer>("Death");
        playerUI = GetNode<MarginContainer>("PlayerUI");

        resume = GetNode<Button>("Pause/VBoxContainer/Button");
        options = GetNode<Button>("Pause/VBoxContainer/Button2");
        exit = GetNode<Button>("Pause/VBoxContainer/Button3");
        exitDeath = GetNode<Button>("Death/VBoxContainer/Button3");
        respawn = GetNode<Button>("Death/VBoxContainer/Button");

        resume.Pressed += ResumePressed;
        options.Pressed += OptionsPressed;
        exit.Pressed += ExitPressed;
        exitDeath.Pressed += ExitPressedOnDeath;
        respawn.Pressed += RespawnPressed;

        uiSoundPlayer = new AudioStreamPlayer();
        AddChild(uiSoundPlayer);

        transitionSoundPlayer = new AudioStreamPlayer();
        AddChild(transitionSoundPlayer);

        hurtPlayer = new AudioStreamPlayer();
        AddChild(hurtPlayer);

        clickSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/Tape Sounds/ButtonPressed.wav");
        transitionSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/Tape Sounds/Static01.wav");       
        hurtSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsnoway.wav");     

        pause.Visible = false;
        death.Visible = false;
        playerUI.Visible = true;
        
        textureHurt = GetNode<ColorRect>("ColorRect2");

        texture = GetNode<ColorRect>("ColorRect");
        shaderMat = new ShaderMaterial();
        shaderMat.Shader = GD.Load<Shader>("res://LEVELS/UI/CRT.gdshader");
        texture.Material = shaderMat;

        health = GetNode<Label>("PlayerUI/HBoxContainer/VBoxContainer/Health");
        ammo = GetNode<Label>("PlayerUI/HBoxContainer/VBoxContainer2/Ammo");
        score = GetNode<Label>("Score");
        score.Text = "0";

        weaponSprite = GetNode<Sprite2D>("PlayerUI/HBoxContainer/VBoxContainer2/WeaponIcon");
        ammoSprite = GetNode<Sprite2D>("PlayerUI/HBoxContainer/VBoxContainer2/AmmoIcon");

        health.Text = "{PlayerStats.playerHealth}";
        ammoSprite.Texture = null;
        weaponSprite.Texture = null;
        //texture.Visible = true;

        //labels timers
        labels = new Label[]
        {
            GetNode<Label>("MarginContainer/HBoxContainer/VBoxContainer/Label"),
            GetNode<Label>("MarginContainer/HBoxContainer/VBoxContainer/Label2"),
            GetNode<Label>("MarginContainer/HBoxContainer/VBoxContainer/Label3"),
            GetNode<Label>("MarginContainer/HBoxContainer/VBoxContainer/Label4")
        };
        timers = new Timer[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].Visible = false;

            timers[i] = new Timer();
            timers[i].OneShot = true;
            timers[i].WaitTime = 2.0f;

            int index = i; 
            timers[i].Timeout += () => HideLabel(index);

            AddChild(timers[i]);
        }

        // Shader parameters
        shaderMat.SetShaderParameter("static_noise_intensity", MinIntensity);
        shaderMat.SetShaderParameter("roll_speed", 0);
        shaderMat.SetShaderParameter("roll_size", 0);
        shaderMat.SetShaderParameter("roll", false);
        shaderMat.SetShaderParameter("scanlines_opacity", 0.0);
        shaderMat.SetShaderParameter("scanlines_width", 0.1);
        shaderMat.SetShaderParameter("grille_opacity", 0.0);
        shaderMat.SetShaderParameter("aberration", 0.01);
        shaderMat.SetShaderParameter("brightness", 1.3);
    }

    public override void _ExitTree()
    {
        PlayerStats.UnregisterUI();
    }


    public void ShowPickup(string text)
    {
        for (int i = labels.Length - 1; i > 0; i--)
        {
            labels[i].Text = labels[i - 1].Text;
            labels[i].Visible = labels[i - 1].Visible;
        }

        labels[0].Text = text;
        labels[0].Visible = true;

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].Visible)
            {
                timers[i].Stop();
                timers[i].Start();
            }
        }
    }

    private async void HideLabel(int index)
    {
        var tween = CreateTween();
        tween.TweenProperty(labels[index], "modulate:a", 0, 0.4f);

        await ToSignal(tween, "finished");

        labels[index].Visible = false;
        labels[index].Modulate = new Color(1,1,1,1);
    }

    public override void _PhysicsProcess(double delta)
    {
        
        health.Text = $"{PlayerStats.playerHealth}";
        score.Text = $"{PlayerStats.playerScore}";
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

        OnDeathUI();
        PlayerHurtOverlay();
        UpdateHealthLabel();
    }
    public async void OnDeathUI()
    {
        if(PlayerStats.playerHealth <= 0)
        {
            dead = true;
            //GetTree().Paused = true;
            pause.Visible = false;
            playerUI.Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            death.Visible = true;
            shaderMat.SetShaderParameter("static_noise_intensity", MinIntensity);
            shaderMat.SetShaderParameter("roll_speed", 3);
            shaderMat.SetShaderParameter("roll_size", 15);
            shaderMat.SetShaderParameter("roll", true);
            shaderMat.SetShaderParameter("scanlines_opacity", 0);
            shaderMat.SetShaderParameter("scanlines_width", 0.5);
            shaderMat.SetShaderParameter("aberration", 0.01);
            shaderMat.SetShaderParameter("brightness", 1.8);
        }

    } 
    public void PlayerHurtOverlay()
    {
        if(PlayerStats.playerHealth <= 70 && PlayerStats.playerHealth > 50)
        {
            textureHurt.Color = new Color(1.0f, 0f, 0f, 0.1f);
        }
        else if(PlayerStats.playerHealth <= 50 && PlayerStats.playerHealth > 20)
        {
            textureHurt.Color = new Color(1.0f, 0f, 0f, 0.2f);
        }
        else if(PlayerStats.playerHealth <= 30)
        {
            textureHurt.Color = new Color(1.0f, 0f, 0f, 0.3f);
        }
        else
        {
            textureHurt.Color = new Color(1.0f, 0f, 0f, 0.0f);
        }

        // if(PlayerStats.playerHealth == PlayerStats.playerMaxHealth)
        // {
        //     health.AddThemeColorOverride("font_outline_color", Colors.Red);
        // }
        // else
        // {
        //     health.AddThemeColorOverride("font_outline_color", Colors.White);
        // }
    }
    private void UpdateHealthLabel()
    {
        float healthPercent =
            Mathf.Clamp(
                (float)PlayerStats.playerHealth / PlayerStats.playerMaxHealth,
                0f,
                1f
            );

        // alpha becomes stronger when health is low
        float alpha =
            Mathf.Lerp(0.2f, 1f, healthPercent);
        GD.Print(alpha);
        Color color = new Color(
            1f, // red
            0f,
            0f,
            alpha
        );

        health.AddThemeColorOverride(
            "font_color",
            color
        );

        health.AddThemeColorOverride(
            "font_outline_color",
            Colors.Black
        );

        health.AddThemeConstantOverride(
            "outline_size",
            6
        );
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
    private async void ExitPressedOnDeath()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        await FadeIn();
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://LEVELS/UI/main_menu.tscn");
    }
    // private async void RespawnPressed()
    // {
        
    // }
    private async void RespawnPressed()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        await FadeIn();
        GetTree().Paused = false;
        PlayerStats.Reset();
        GetTree().ReloadCurrentScene();
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
        if (@event.IsActionPressed("Escape") && !dead) 
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
                shaderMat.SetShaderParameter("roll_speed", 0);
                shaderMat.SetShaderParameter("roll_size", 0);
                shaderMat.SetShaderParameter("roll", false);
                shaderMat.SetShaderParameter("scanlines_opacity", 0.0);
                shaderMat.SetShaderParameter("scanlines_width", 0.0);
                shaderMat.SetShaderParameter("grille_opacity", 0.0);
                shaderMat.SetShaderParameter("aberration", 0.01);
                shaderMat.SetShaderParameter("brightness", 1.3);
                
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
        
        if (!IsInstanceValid(player)) return; // check before touching the object
        if (!player.Playing) return;
        
        FadeOutSoundPlayer(player, fadeOutDuration, originalVolumeDb);
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
    public async void PlayerHurt()
    {
        if(!dead)
        {
             if (!hurtPlayer.Playing)
            {
                hurtPlayer.Stream = hurtSound;
                hurtPlayer.VolumeDb = 0;
                hurtPlayer.Play();
            }
            PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.2f);
            _tween?.Kill();
            _tween = CreateTween();
            _tween.TweenMethod(Callable.From<float>(value => 
                shaderMat.SetShaderParameter("static_noise_intensity", value)
            ), 0.5, MinIntensity, 0.2f);
            _tween.TweenMethod(Callable.From<float>(value => 
                shaderMat.SetShaderParameter("aberration", value)
            ), 0.04, 0.01, 0.2f);
            await ToSignal(_tween, Tween.SignalName.Finished);
            StopAllSounds();
        }
        else 
        {
            if(!deadSound)
            {
                deadSound = true;
                hurtPlayer.Stream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dspldeth.wav"); 
                hurtPlayer.Play();
            }
            
        }
    }

}
