using Godot;
using System;

public partial class MusicManager : Node
{
    private AudioStreamPlayer musicPlayer;

    // Music tracks
    private AudioStream mainMenu;
    private AudioStream level01;
    private AudioStream level02;
    private AudioStream level03;
    private AudioStream level04;

    // Prevent replaying same song every frame
    private string currentScene = "";

    public override void _Ready()
    {
        // Create player
        musicPlayer = new AudioStreamPlayer();
        AddChild(musicPlayer);

        // Load music
        mainMenu = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/DoomSoundtrack/05. E1M3 - Dark Halls.mp3");

        level01 = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/DoomSoundtrack/03. E1M1 - At Doom's Gate.mp3");
        level02 = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/DoomSoundtrack/04. E1M2 - The Imp's Song.mp3");
        level03 = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/DoomSoundtrack/22. E3M8 - Facing The Spider.mp3");
        //level04 = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/DoomSoundtrack/04.mp3");

        musicPlayer.VolumeDb = -2;
    }

    public override void _Process(double delta)
    {
        if (GetTree().CurrentScene == null)
            return;

        string sceneName = GetTree().CurrentScene.Name;

        // only change when scene changes
        if (sceneName == currentScene)
            return;

        currentScene = sceneName;

        switch (sceneName)
        {
            case "MainMenu":
                PlayMusic(mainMenu);
                break;

            case "TrenchBroomPrototype":
                PlayMusic(level01);
                break;

            case "Level02":
                PlayMusic(level02);
                break;

            case "Level03":
                PlayMusic(level03);
                break;

            case "Level04":
                PlayMusic(level04);
                break;
        }
    }

    private void PlayMusic(AudioStream stream)
    {
        if (musicPlayer.Stream == stream && musicPlayer.Playing)
            return;

        musicPlayer.Stop();

        musicPlayer.Stream = stream;
        musicPlayer.Play();
    }
}