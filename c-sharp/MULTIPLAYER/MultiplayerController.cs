using Godot;
using System;

public partial class MultiplayerController : Control
{
    private Button hostButton;
    private Button joinButton;
    private Button startButton;
    private LineEdit ipInput;
    
    private bool isHosting = false;
    
    public override void _Ready()
    {
        hostButton = GetNode<Button>("Host");
        joinButton = GetNode<Button>("Join");
        startButton = GetNode<Button>("StartGame");
        ipInput = GetNode<LineEdit>("LineEdit");
        
        ipInput.Text = "127.0.0.1";
        
        hostButton.Pressed += OnHostPressed;
        joinButton.Pressed += OnJoinPressed;
        startButton.Pressed += OnStartPressed;
        
        // Initially hide StartGame button
        startButton.Visible = false;
        
        GD.Print("MultiplayerController ready!");
        GD.Print("Click Host to create a game, then Start Game to begin");
    }
    
    private void OnHostPressed()
    {
        GD.Print("Host button pressed");
        
        var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
        networkManager.HostGame();
        
        // Show StartGame button for host
        startButton.Visible = true;
        startButton.Disabled = false;
        startButton.Text = "Start Game";
        
        isHosting = true;
        
        GD.Print("Host created. Now click 'Start Game' when ready!");
    }
    
    private void OnJoinPressed()
    {
        GD.Print("Join button pressed");
        
        string ip = ipInput.Text;
        if (string.IsNullOrEmpty(ip))
        {
            ip = "127.0.0.1";
        }
        
        var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
        networkManager.JoinGame(ip);
        
        // Hide menu for joining players
        Hide();
        
        GD.Print("Joined server. Waiting for host to start game...");
    }
    
    private void OnStartPressed()
    {
        if (!isHosting)
        {
            GD.PrintErr("Only the host can start the game!");
            return;
        }
        
        GD.Print("Start Game button pressed");
        
        var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
        networkManager.StartGameForAll();
        
        // Hide menu
        Hide();
        
        GD.Print("Game started!");
    }
    
    // Show menu again if disconnected
    public override void _Process(double delta)
    {
        var networkManager = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (networkManager != null && Multiplayer.MultiplayerPeer == null)
        {
            // We got disconnected, show menu again
            Show();
            startButton.Visible = false;
            isHosting = false;
        }
    }
}