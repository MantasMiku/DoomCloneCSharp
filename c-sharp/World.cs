using Godot;

public partial class World : Node
{
    private Control MainMenu => GetNode<Control>("CanvasLayer/MainMenu");
    private LineEdit AddressEntry => GetNode<LineEdit>("CanvasLayer/MainMenu/MarginContainer/VBoxContainer/AddressEntry");

    private const int PORT = 9999;
    private const int MAX_CLIENTS = 4;

    private PackedScene PlayerScene = GD.Load<PackedScene>("res://ENTITIES/PLAYER/test_player.tscn");
    private PackedScene ImpScene = GD.Load<PackedScene>("res://ENTITIES/ENEMIES/IMP/imp.tscn");

    private ENetMultiplayerPeer _enetPeer = new ENetMultiplayerPeer();

    [Export] private Node3D _spawnP1;
    [Export] private Node3D _spawnP2;
    [Export] private Node3D _spawnP3;
    [Export] private Node3D _spawnP4;

    // Track player spawn assignments
    private int _nextSpawnIndex = 0;

    [Export] private Node3D[] _enemySpawnPoints = new Node3D[4];

    public override void _Ready()
    {
        GetNode<Button>("CanvasLayer/MainMenu/MarginContainer/VBoxContainer/Host").Pressed += OnHostButtonDown;
        GetNode<Button>("CanvasLayer/MainMenu/MarginContainer/VBoxContainer/Join").Pressed += OnJoinButtonDown;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("quit"))
        {
            GetTree().Quit();
        }
    }

    private void OnHostButtonDown()
    {
        MainMenu.Hide();

        _enetPeer.CreateServer(PORT, MAX_CLIENTS);
        Multiplayer.MultiplayerPeer = _enetPeer;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        // Add the host player
        AddPlayer(Multiplayer.GetUniqueId());
        if(Multiplayer.IsServer() && IsMultiplayerAuthority())
        {
            AddEnmies();
        }

        //UpnpSetup();
    }

    private void OnJoinButtonDown()
    {
        MainMenu.Hide();

        _enetPeer.CreateClient(AddressEntry.Text, PORT);
        Multiplayer.MultiplayerPeer = _enetPeer;

        Multiplayer.ConnectedToServer += OnConnectedToServer;
    }

    private void OnConnectedToServer()
    {
        GD.Print("Connected to server!");
    }

    private void OnPeerConnected(long peerId)
    {
        GD.Print($"Peer connected: {peerId}");
        
        // Server spawns the new player for everyone
        if (Multiplayer.IsServer())
        {
            // Get the next spawn index
            int spawnIndex = _nextSpawnIndex;
            _nextSpawnIndex = (_nextSpawnIndex + 1) % 4; // Cycle through 0-3

            // Tell all clients (including the new one) to spawn this player
            RpcId(0, nameof(SpawnPlayer), peerId, spawnIndex); // 0 = broadcast to all
            //Rpc(nameof(AddEnmies));
        }
    }

    private void OnPeerDisconnected(long peerId)
    {
        GD.Print($"Peer disconnected: {peerId}");
        
        // Remove the player
        var player = GetNodeOrNull(peerId.ToString());
        if (player != null)
        {
            player.QueueFree();
        }
    }

    private void AddPlayer(long peerId)
    {
        int spawnIndex = _nextSpawnIndex;
        _nextSpawnIndex = (_nextSpawnIndex + 1) % 4;
        
        SpawnPlayer(peerId, spawnIndex);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void AddEnmies()
    {
        //GD.Print("EnemyID: "+peerId);
        for (int i = 0; i < _enemySpawnPoints.Length; i++)
        {
            if (_enemySpawnPoints[i] != null)
            {
                //GD.Print(Multiplayer.GetUniqueId);
                string enemyName = $"Enemy_{Time.GetTicksMsec()}_{GD.Randi()}";
                Node enemy = ImpScene.Instantiate();
                enemy.Name = enemyName;
                AddChild(enemy);
                
                if (enemy is Node3D enemy3d)
                {
                    enemy3d.GlobalTransform = _enemySpawnPoints[i].GlobalTransform;
                    GD.Print($"Spawned enemy {i} at {_enemySpawnPoints[i].GlobalPosition}");
                }
                
                // Server controls enemy
                if (enemy is Node enemyNode)
                {
                    enemyNode.SetMultiplayerAuthority(Multiplayer.GetUniqueId());
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SpawnPlayer(long peerId, int spawnIndex)
    {
        GD.Print($"[{Multiplayer.GetUniqueId()}] Spawning player {peerId} at spawn index {spawnIndex}");

        // Check if player already exists
        if (GetNodeOrNull(peerId.ToString()) != null)
        {
            GD.Print($"Player {peerId} already exists, skipping");
            return;
        }

        Node player = PlayerScene.Instantiate();
        player.Name = peerId.ToString();
        AddChild(player);

        if (player is Node3D player3d)
        {
            // Assign spawn position based on spawn index
            switch (spawnIndex)
            {
                case 0: // First player
                    if (_spawnP1 != null)
                    {
                        GD.Print($"Player {peerId} spawned at P1");
                        player3d.GlobalTransform = _spawnP1.GlobalTransform;
                    }
                    break;

                case 1: // Second player
                    if (_spawnP2 != null)
                    {
                        GD.Print($"Player {peerId} spawned at P2");
                        player3d.GlobalTransform = _spawnP2.GlobalTransform;
                    }
                    break;

                case 2: // Third player  
                    if (_spawnP3 != null)
                    {
                        GD.Print($"Player {peerId} spawned at P3");
                        player3d.GlobalTransform = _spawnP3.GlobalTransform;
                    }
                    break;

                case 3: // Fourth player
                    if (_spawnP4 != null)
                    {
                        GD.Print($"Player {peerId} spawned at P4");
                        player3d.GlobalTransform = _spawnP4.GlobalTransform;
                    }
                    break;
            }
            
            GD.Print($"Player {peerId} final position: {player3d.GlobalPosition}");
        }

        // Set multiplayer authority
        if (player is Node playerNode)
        {
            playerNode.SetMultiplayerAuthority((int)peerId);
        }
    }

    private void UpnpSetup()
    {
        Upnp upnp = new Upnp();

        // Discover
        Upnp.UpnpResult discoverResult = (Upnp.UpnpResult)upnp.Discover();
        GD.Print($"discoverResult: {discoverResult}");

        if (discoverResult != Upnp.UpnpResult.Success)
            return;

        // Get gateway
        UpnpDevice gateway = upnp.GetGateway();
        if (gateway == null || !gateway.IsValidGateway())
        {
            GD.PushError("No valid UPNP gateway found");
            return;
        }

        // Add port mapping
        Upnp.UpnpResult mapResult = (Upnp.UpnpResult)upnp.AddPortMapping(PORT, PORT, "Godot Multiplayer", "UDP");
        GD.Print($"mapResult: {mapResult}");

        if (mapResult != Upnp.UpnpResult.Success)
            return;

        // External address
        string address = upnp.QueryExternalAddress();
        GD.Print($"Address: {address}");
    }
}