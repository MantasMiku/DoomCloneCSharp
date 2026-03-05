using Godot;
using System;
using System.Collections.Generic;

public partial class NetworkManager : Node
{
    private const int PORT = 7777;
    private PackedScene gameScene;
    
    private List<long> pendingPlayers = new();
    private bool gameStarted = false;
    
    public override void _Ready()
    {
        GD.Print("NetworkManager ready!");
        
        gameScene = GD.Load<PackedScene>("res://test_scene_multiplayer.tscn");
        if (gameScene == null)
        {
            GD.PrintErr("Failed to load game scene!");
        }
        else
        {
            GD.Print("Game scene loaded successfully");
        }
        
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
        
        GD.Print("NetworkManager initialized");
    }
    
    public void HostGame()
    {
        GD.Print("=== HOST GAME ===");
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(PORT, 4);
        
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to create server: " + error);
            return;
        }
        
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Server hosting on port " + PORT);
    }
    
    public void JoinGame(string ip = "127.0.0.1")
    {
        GD.Print("=== JOIN GAME ===");
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(ip, PORT);
        
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to connect to server: " + error);
            return;
        }
        
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Connecting to " + ip + "...");
    }
    
    private void OnConnectedToServer()
    {
        GD.Print("Successfully connected to server!");
    }
    
    private void OnPeerConnected(long id)
    {
        GD.Print($"Peer connected: {id} (I am {Multiplayer.GetUniqueId()})");
        
        if (Multiplayer.IsServer())
        {
            pendingPlayers.Add(id);
            GD.Print($"Server: Added player {id} to pending players");
            
            if (gameStarted)
            {
                GD.Print($"Game already started, syncing for player {id}");
                // Load scene for new player
                RpcId(id, nameof(LoadGameScene));
                
                GetTree().CreateTimer(0.5).Timeout += () => {
                    if (pendingPlayers.Contains(id))
                    {
                        SyncNewPlayer(id);
                    }
                };
            }
        }
    }
    
    private void SyncNewPlayer(long newPlayerId)
    {
        var gameManager = GetGameManager();
        if (gameManager == null)
        {
            GD.PrintErr("GameManager not found!");
            return;
        }
        
        // Get spawn indices for all players
        var playerIds = pendingPlayers.ToArray();
        var spawnIndices = new int[playerIds.Length];
        
        for (int i = 0; i < playerIds.Length; i++)
        {
            spawnIndices[i] = gameManager.GetSpawnIndexForPlayer(playerIds[i]);
        }
        
        // Tell new client to spawn all players
        RpcId(newPlayerId, nameof(SpawnAllPlayers), playerIds, spawnIndices);
        
        // Spawn new player on server
        int newPlayerSpawnIndex = gameManager.GetSpawnIndexForPlayer(newPlayerId);
        gameManager.SpawnPlayerAtIndex(newPlayerId, newPlayerSpawnIndex);
        
        // Tell existing clients about new player
        foreach (var existingId in pendingPlayers)
        {
            if (existingId != newPlayerId && existingId != Multiplayer.GetUniqueId())
            {
                RpcId(existingId, nameof(SpawnSinglePlayer), newPlayerId, newPlayerSpawnIndex);
            }
        }
    }
    
    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Player disconnected: {id}");
        pendingPlayers.Remove(id);
        
        if (gameStarted)
        {
            Rpc(nameof(RemovePlayer), id);
            RemovePlayer(id);
        }
    }
    
    private void OnConnectionFailed()
    {
        GD.Print("Failed to connect to server");
    }
    
    private void OnServerDisconnected()
    {
        GD.Print("Disconnected from server");
        ResetGame();
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void SpawnSinglePlayer(long playerId, int spawnIndex)
    {
        GD.Print($"[{Multiplayer.GetUniqueId()}] SpawnSinglePlayer: {playerId} at spawn {spawnIndex}");
        var gameManager = GetGameManager();
        if (gameManager != null)
        {
            gameManager.SpawnPlayerAtIndex(playerId, spawnIndex);
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void SpawnAllPlayers(long[] playerIds, int[] spawnIndices)
    {
        GD.Print($"[{Multiplayer.GetUniqueId()}] SpawnAllPlayers: {playerIds.Length} players");
        var gameManager = GetGameManager();
        if (gameManager == null) return;
        
        for (int i = 0; i < playerIds.Length; i++)
        {
            gameManager.SpawnPlayerAtIndex(playerIds[i], spawnIndices[i]);
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RemovePlayer(long playerId)
    {
        var gameManager = GetGameManager();
        if (gameManager != null)
        {
            gameManager.RemovePlayer(playerId);
        }
    }
    
    public void StartGameForAll()
    {
        GD.Print("=== START GAME FOR ALL ===");
        
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("Only the host can start the game!");
            return;
        }
        
        // Add host to pending players
        if (!pendingPlayers.Contains(Multiplayer.GetUniqueId()))
        {
            pendingPlayers.Add(Multiplayer.GetUniqueId());
        }
        
        gameStarted = true;
        
        // Load game scene
        Rpc(nameof(LoadGameScene));
        LoadGameScene();
        
        // Wait for scenes to load, then spawn all players
        GetTree().CreateTimer(0.5).Timeout += () => {
            SpawnInitialPlayers();
        };
    }
    
    private void SpawnInitialPlayers()
    {
        var gameManager = GetGameManager();
        if (gameManager == null)
        {
            GD.PrintErr("GameManager not found!");
            return;
        }
        
        gameManager.ResetSpawnSystem();
        
        var playerIds = pendingPlayers.ToArray();
        var spawnIndices = new int[playerIds.Length];
        
        // Assign spawn indices
        for (int i = 0; i < playerIds.Length; i++)
        {
            spawnIndices[i] = gameManager.GetSpawnIndexForPlayer(playerIds[i]);
        }
        
        // Tell all CLIENTS to spawn all players (not the server)
        Rpc(nameof(SpawnAllPlayers), playerIds, spawnIndices);
        
        // Spawn on server only (since RPC has CallLocal = false)
        for (int i = 0; i < playerIds.Length; i++)
        {
            gameManager.SpawnPlayerAtIndex(playerIds[i], spawnIndices[i]);
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void LoadGameScene()
    {
        GD.Print($"LoadGameScene called for player {Multiplayer.GetUniqueId()}");
        
        var existingScene = GetTree().Root.GetNodeOrNull("test_scene_multiplayer");
        if (existingScene != null)
        {
            existingScene.QueueFree();
        }
        
        var gameInstance = gameScene.Instantiate();
        if (gameInstance == null)
        {
            GD.PrintErr("Failed to instantiate game scene!");
            return;
        }
        
        gameInstance.Name = "test_scene_multiplayer";
        GetTree().Root.AddChild(gameInstance);
        GD.Print("Game scene loaded");
    }
    
    private GameManager GetGameManager()
    {
        var gameLevel = GetTree().Root.GetNodeOrNull("test_scene_multiplayer");
        if (gameLevel == null)
        {
            GD.PrintErr("Game scene not found!");
            return null;
        }
        
        var gameManager = gameLevel.GetNodeOrNull<GameManager>("GameManager");
        if (gameManager == null)
        {
            GD.PrintErr("GameManager not found in game scene!");
        }
        
        return gameManager;
    }
    
    private void ResetGame()
    {
        gameStarted = false;
        pendingPlayers.Clear();
        
        var gameLevel = GetTree().Root.GetNodeOrNull("test_scene_multiplayer");
        if (gameLevel != null)
        {
            gameLevel.QueueFree();
        }
        
        GD.Print("Game reset");
    }
}
