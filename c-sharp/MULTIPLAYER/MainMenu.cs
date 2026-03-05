using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GD.Print("MainMenu loaded - Press H to host, J to join");
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // Access NetworkManager directly
            var networkManager = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
            if (networkManager == null)
            {
                GD.PrintErr("NetworkManager not found! Set it as autoload.");
                return;
            }
            
            if (keyEvent.Keycode == Key.H)
            {
                GD.Print("Hosting game...");
                networkManager.HostGame();
                Hide(); // Hide menu
            }
            else if (keyEvent.Keycode == Key.J)
            {
                GD.Print("Joining game at 127.0.0.1...");
                networkManager.JoinGame("127.0.0.1");
                Hide(); // Hide menu
            }
            else if (keyEvent.Keycode == Key.Escape)
            {
                GetTree().Quit();
            }
        }
    }
}