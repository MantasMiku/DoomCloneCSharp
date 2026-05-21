using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class MainUI : Control
{
    Button start;
    Button options;
    Button exit;
    Button popUpStay;
    Button popUpExit;
    Button spStart;
    Button eStart;
    Button tStart;
    Button spLevel01;
    Button spLevel02;
    Button spLevel03;
    ColorRect texture;
    private ShaderMaterial shaderMat;
    MarginContainer mainMenu;
    MarginContainer optionsMenu;
    MarginContainer levelsMenu;
    MarginContainer levelsMenuSp;
    MarginContainer popUp;
    
    private bool spMouseEntered = false;
    private bool eMouseEntered = false;
    private bool tMouseEntered = false;
    
    private bool spPlayReverse = false;
    private bool tPlayReverse = false;
    
    AnimatedSprite2D spSprite;
    AnimatedSprite2D eSprite;
    AnimatedSprite2D tSprite;

    AnimatedSprite2D spSprite01;
    AnimatedSprite2D spSprite02;
    AnimatedSprite2D spSprite03;
    
    private AudioStreamPlayer uiSoundPlayer;      
    private AudioStreamPlayer hoverSoundPlayer;  
    private AudioStreamPlayer shootSoundPlayer;  
    private AudioStreamPlayer transitionSoundPlayer; 
    
    private Dictionary<AudioStreamPlayer, Tween> activeSoundTweens = new Dictionary<AudioStreamPlayer, Tween>();
    
    private Tween _tween;

    private AudioStream hoverSound;
    private AudioStream clickSound;
    private AudioStream shootSound;
    private AudioStream transitionSound;
    private AudioStream monsterScream;

    [Export] public float FadeDuration = 0.3f;
    [Export] public float MinIntensity = 0.06f;
    [Export] public float MaxIntensity = 1.0f;
    
    public override async void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        //Margins
        mainMenu = GetNode<MarginContainer>("Main");
        optionsMenu = GetNode<MarginContainer>("Options");
        levelsMenu = GetNode<MarginContainer>("Levels");
        levelsMenuSp = GetNode<MarginContainer>("LevelsSP");
        popUp = GetNode<MarginContainer>("PopUp");

        //Shader
        texture = GetNode<ColorRect>("ColorRect");

        //Buttons
        start = GetNode<Button>("Main/VBoxContainer/HBoxContainer/VBoxContainer/Start");
        options = GetNode<Button>("Main/VBoxContainer/HBoxContainer/VBoxContainer/Options");
        exit = GetNode<Button>("Main/VBoxContainer/HBoxContainer/VBoxContainer/Exit");
        popUpStay = GetNode<Button>("PopUp/HBoxContainer/VBoxContainer2/Stay");
        popUpExit = GetNode<Button>("PopUp/HBoxContainer/VBoxContainer/Exit");

        spStart = GetNode<Button>("Levels/VBoxContainer/Panel/HBoxContainer/Button");
        eStart = GetNode<Button>("Levels/VBoxContainer/Panel/HBoxContainer/Button2");
        tStart = GetNode<Button>("Levels/VBoxContainer/Panel/HBoxContainer/Button3");

        spLevel01 = GetNode<Button>("LevelsSP/VBoxContainer/Panel/HBoxContainer/Button");
        spLevel02 = GetNode<Button>("LevelsSP/VBoxContainer/Panel/HBoxContainer/Button2");
        spLevel03 = GetNode<Button>("LevelsSP/VBoxContainer/Panel/HBoxContainer/Button3");

        //AnimSprites
        spSprite = GetNode<AnimatedSprite2D>("Levels/VBoxContainer/Panel/HBoxContainer/Button/MarginContainer/AnimatedSprite2D");
        eSprite = GetNode<AnimatedSprite2D>("Levels/VBoxContainer/Panel/HBoxContainer/Button2/MarginContainer/AnimatedSprite2D");
        tSprite = GetNode<AnimatedSprite2D>("Levels/VBoxContainer/Panel/HBoxContainer/Button3/MarginContainer/AnimatedSprite2D");

        spSprite01 = GetNode<AnimatedSprite2D>("LevelsSP/VBoxContainer/Panel/HBoxContainer/Button/MarginContainer/AnimatedSprite2D");
        spSprite02 = GetNode<AnimatedSprite2D>("LevelsSP/VBoxContainer/Panel/HBoxContainer/Button2/MarginContainer/AnimatedSprite2D");
        spSprite03 = GetNode<AnimatedSprite2D>("LevelsSP/VBoxContainer/Panel/HBoxContainer/Button3/MarginContainer/AnimatedSprite2D");
        
        // Initialize dedicated sound players
        uiSoundPlayer = new AudioStreamPlayer();
        AddChild(uiSoundPlayer);
        
        hoverSoundPlayer = new AudioStreamPlayer();
        AddChild(hoverSoundPlayer);
        
        shootSoundPlayer = new AudioStreamPlayer();
        AddChild(shootSoundPlayer);
        
        transitionSoundPlayer = new AudioStreamPlayer();
        AddChild(transitionSoundPlayer);

        // Load sound effects
        //hoverSound = GD.Load<AudioStream>("res://sounds/hover.wav");
        clickSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/Tape Sounds/ButtonPressed.wav");
        shootSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dspistol.wav");
        transitionSound = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/Tape Sounds/Static01.wav");
        monsterScream = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/dsbgsit2.wav");


        //Button signals
        start.Pressed += StartPressed;
        exit.Pressed += ExitPressed;
        options.Pressed += OptionsPressed;
        popUpStay.Pressed += PopUpPressedStay;
        popUpExit.Pressed += PopUpPressedExit;

        spStart.MouseEntered += MouseEnteredSp;
        spStart.MouseExited += MouseExitedSp;
        spStart.Pressed += MousePressedSp;

        eStart.MouseEntered += MouseEnteredE;
        eStart.MouseExited += MouseExitedE;
        eStart.Pressed += MousePressedE;

        tStart.MouseEntered += MouseEnteredT;
        tStart.MouseExited += MouseExitedT;
        tStart.Pressed += MousePressedT;

        spLevel01.Pressed += MousePressedL01;
        spLevel02.Pressed += MousePressedL02;
        spLevel03.Pressed += MousePressedL03;

        //AnimSprite Signals
        spSprite.AnimationFinished += OnAnimationFinishedSp;
        eSprite.AnimationFinished += OnAnimationFinishedE;
        tSprite.AnimationFinished += OnAnimationFinishedT;
        eSprite.FrameChanged += OnESpriteFrameChanged;
        
        spSprite.Play("Idle");
        eSprite.Play("Idle");
        tSprite.Play("Idle");

        spSprite01.Play("Idle");
        spSprite02.Play("Idle");
        spSprite03.Play("Idle");

        mainMenu.Visible = true;
        optionsMenu.Visible = false;
        levelsMenu.Visible = false;
        popUp.Visible = false;
        
        shaderMat = new ShaderMaterial();
        shaderMat.Shader = GD.Load<Shader>("res://LEVELS/UI/CRT.gdshader");
        texture.Material = shaderMat;
        
        // Shader parameters
        shaderMat.SetShaderParameter("static_noise_intensity", MinIntensity);
        shaderMat.SetShaderParameter("roll_speed", 3);
        shaderMat.SetShaderParameter("scanlines_opacity", 0);
        shaderMat.SetShaderParameter("scanlines_width", 0.5);
        shaderMat.SetShaderParameter("aberration", 0.01);
        shaderMat.SetShaderParameter("brightness", 1.8);

        await FadeOut();
    }

    private async void MousePressedL03()
    {
        PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);
        await FadeIn();
        GetTree().ChangeSceneToFile("res://LEVELS/SINGLEPLAYER/LEVEL03.tscn");
    }


    private async void MousePressedL02()
    {
        PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);
        await FadeIn();
        GetTree().ChangeSceneToFile("res://LEVELS/SINGLEPLAYER/LEVEL02.tscn");
    }


    private async void MousePressedL01()
    {
        PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);
        
        // eSprite.Play("Shoot");
        // // Sound will play automatically on frames 1 and 3 via FrameChanged
        
        // await ToSignal(eSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        // eSprite.Stop();
        
        await FadeIn();
        GetTree().ChangeSceneToFile("res://LEVELS/trench_broom_prototype.tscn");
    }


    private void OnESpriteFrameChanged()
    {
        if (eSprite.Animation == "Shoot")
        {
            int currentFrame = eSprite.Frame;
            
            // Play sound on frame 1 and frame 3
            if (currentFrame == 1 || currentFrame == 3)
            {
                PlaySoundOnPlayer(shootSoundPlayer, shootSound);
            }
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
        hoverSoundPlayer?.Stop();
        shootSoundPlayer?.Stop();
        transitionSoundPlayer?.Stop();
        
        foreach (var tween in activeSoundTweens.Values)
        {
            tween?.Kill();
        }
        activeSoundTweens.Clear();
    }

    // ==================== SP SPRITE HANDLERS ====================
    private void MouseEnteredSp()
    {
        spMouseEntered = true;
        spSprite.Play("Spin");
        PlaySoundOnPlayer(hoverSoundPlayer, hoverSound);
    }

    private void MouseExitedSp()
    {
        spMouseEntered = false;
        spPlayReverse = false;
        spSprite.FlipH = false;
        spSprite.Play("Idle");
    }

    private async void MousePressedSp()
    {
        // Play both sounds simultaneously
        // PlaySoundOnPlayer(shootSoundPlayer, monsterScream);
        // PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);
        
        // spSprite.Play("Shoot");
        // await ToSignal(spSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        // spSprite.Stop();
        
        // await FadeIn();
        //GetTree().ChangeSceneToFile("res://LEVELS/SINGLEPLAYER/LEVEL01.tscn");
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        await FadeIn();
        levelsMenu.Visible = false;
        levelsMenuSp.Visible = true;
        //PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);

        await FadeOut();
    }

    private void OnAnimationFinishedSp()
    {
        if (spSprite.Animation == "Shoot")
        {
            if (spMouseEntered)
                spSprite.Play("Spin");
            else
                spSprite.Play("Idle");
            return;
        }

        if (spMouseEntered && spSprite.Animation == "Spin")
        {
            if (!spPlayReverse)
            {
                spSprite.PlayBackwards("Spin");
                spSprite.FlipH = true;
                spPlayReverse = true;
            }
            else
            {
                spSprite.FlipH = false;
                spSprite.Play("Spin");
                spPlayReverse = false;
            }
        }
        else
        {
            spSprite.Play("Idle");
            spSprite.FlipH = false;
            spPlayReverse = false;
        }
    }

    // ==================== E SPRITE HANDLERS ====================
    private void MouseEnteredE()
    {
        eMouseEntered = true;
        eSprite.Play("Spin");
        PlaySoundOnPlayer(hoverSoundPlayer, hoverSound);
    }

    private void MouseExitedE()
    {
        eMouseEntered = false;
        eSprite.Play("Idle");
    }

    private async void MousePressedE()
    {
        PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);
        
        eSprite.Play("Shoot");
        // Sound will play automatically on frames 1 and 3 via FrameChanged
        
        await ToSignal(eSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        eSprite.Stop();
        
        await FadeIn();
        GetTree().ChangeSceneToFile("res://LEVELS/ENDLESS/ENDLESS.tscn");
    }

    private void OnAnimationFinishedE()
    {
        if (eSprite.Animation == "Shoot")
        {
            if (eMouseEntered)
                eSprite.Play("Spin");
            else
                eSprite.Play("Idle");
            return;
        }

        if (eMouseEntered && eSprite.Animation == "Spin")
        {
            eSprite.Play("Spin");
        }
        else
        {
            eSprite.Play("Idle");
        }
    }

    // ==================== T SPRITE HANDLERS ====================
    private void MouseEnteredT()
    {
        tMouseEntered = true;
        tSprite.Play("Spin");
        PlaySoundOnPlayer(hoverSoundPlayer, hoverSound);
    }

    private void MouseExitedT()
    {
        tMouseEntered = false;
        tPlayReverse = false;
        tSprite.FlipH = true;
        tSprite.Play("Idle");
    }

    private async void MousePressedT()
    {
        // Play both sounds simultaneously
        PlaySoundOnPlayer(shootSoundPlayer, shootSound);
        
        tSprite.Play("Shoot");
        await ToSignal(tSprite, AnimatedSprite2D.SignalName.AnimationFinished); 
        tSprite.Play("Bleed");

        PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);
        await FadeIn();
        GetTree().ChangeSceneToFile("res://LEVELS/TRAINING/TRAINING.tscn");
    }

    private void OnAnimationFinishedT()
    {
        if (tSprite.Animation == "Shoot")
        {
            if (tMouseEntered)
                tSprite.Play("Spin");
            else
                tSprite.Play("Idle");
            return;
        }

        if (tMouseEntered && tSprite.Animation == "Spin")
        {
            if (!tPlayReverse)
            {
                tSprite.PlayBackwards("Spin");
                tSprite.FlipH = false;
                tPlayReverse = true;
            }
            else
            {
                tSprite.FlipH = true;
                tSprite.Play("Spin");
                tPlayReverse = false;
            }
        }
        else
        {
            tSprite.Play("Idle");
            tSprite.FlipH = true;
            tPlayReverse = false;
        }
    }

    // ==================== MENU NAVIGATION ====================
    private async void PopUpPressedExit()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        await FadeIn();
        GetTree().Quit();    
    }

    private async void PopUpPressedStay()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        await FadeIn();
        popUp.Visible = false;
        await FadeOut();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("Escape"))
        {
            HandleBack();
            GetViewport().SetInputAsHandled();
        }
    }

    private async void HandleBack()
    {
        if (_tween != null && _tween.IsRunning())
            return;

        PlaySoundOnPlayer(uiSoundPlayer, clickSound);

        if (popUp.Visible)
        {
            await FadeIn();
            popUp.Visible = false;
            await FadeOut();
            return;
        }

        if (levelsMenu.Visible)
        {
            await FadeIn();
            levelsMenu.Visible = false;
            mainMenu.Visible = true;
            await FadeOut();
            return;
        }
        if (levelsMenuSp.Visible)
        {
            await FadeIn();
            levelsMenuSp.Visible = false;   
            levelsMenu.Visible = true;
            await FadeOut();
            return;
        }

        if (optionsMenu.Visible)
        {
            await FadeIn();
            optionsMenu.Visible = false;
            mainMenu.Visible = true;
            await FadeOut();
            return;
        }

        if (mainMenu.Visible)
        {
            await FadeIn();
            popUp.Visible = true;
            await FadeOut();
            return;
        }
    }
    
    private async void OptionsPressed()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        //PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);

        await FadeIn();
        mainMenu.Visible = false;
        optionsMenu.Visible = true;

        await FadeOut();
    }
    
    private async void ExitPressed()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        //PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);
        await FadeIn();
        popUp.Visible = true;
         await FadeOut();
        //GetTree().Quit();    
    }
    
    private async void StartPressed()
    {
        PlaySoundOnPlayer(uiSoundPlayer, clickSound);
        await FadeIn();
        mainMenu.Visible = false;
        levelsMenu.Visible = true;
        //PlaySoundWithFadeOut(transitionSoundPlayer, transitionSound, 0.5f);

        await FadeOut();
    }
    
    // Fade to full static (0.06 -> 1.0)
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