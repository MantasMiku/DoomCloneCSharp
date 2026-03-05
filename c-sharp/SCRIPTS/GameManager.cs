using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    [Export] public PackedScene PlayerScene;
    
    private Dictionary<long, int> playerSpawnIndex = new();
    private int nextSpawnIndex = 0;
    
    public override void _Ready()
    {
        GD.Print("GameManager ready!");
        
        // Load player scene if not set in editor
        if (PlayerScene == null)
        {
            PlayerScene = GD.Load<PackedScene>("res://ENTITIES/PLAYER/test_player.tscn");
            if (PlayerScene == null)
            {
                GD.PrintErr("GameManager: Failed to load player scene!");
            }
        }
    }
    
    public void SpawnPlayer(long playerId)
    {
        GD.Print($"[GameManager] Spawning player {playerId}");
        
        // Get or assign spawn index
        if (!playerSpawnIndex.ContainsKey(playerId))
        {
            playerSpawnIndex[playerId] = GetNextAvailableSpawnIndex();
        }
        
        int spawnIndex = playerSpawnIndex[playerId];
        CallDeferred(nameof(DeferredSpawnPlayer), playerId, spawnIndex);
    }
    
    public void SpawnPlayerAtIndex(long playerId, int spawnIndex)
    {
        GD.Print($"[GameManager] Spawning player {playerId} at spawn index {spawnIndex}");
        
        playerSpawnIndex[playerId] = spawnIndex;
        CallDeferred(nameof(DeferredSpawnPlayer), playerId, spawnIndex);
    }
    
    private void DeferredSpawnPlayer(long playerId, int spawnIndex)
	{
		GD.Print($"[{Multiplayer.GetUniqueId()}] DeferredSpawnPlayer for {playerId} at spawn {spawnIndex}");
		
		// Check if player already exists (as child of GameManager)
		var existingPlayer = GetNodeOrNull(playerId.ToString());
		if (existingPlayer != null)
		{
			GD.Print($"[{Multiplayer.GetUniqueId()}] Player {playerId} already exists, skipping");
			return;
		}
		
		// Instantiate player
		var player = PlayerScene.Instantiate<TestPlayer>();
		if (player == null)
		{
			GD.PrintErr($"Failed to instantiate player {playerId}!");
			return;
		}
		
		player.Name = playerId.ToString();
		
		// Add to GameManager (this node) FIRST
		AddChild(player, true);
		
		// THEN set authority
		player.SetMultiplayerAuthority((int)playerId);
		
		GD.Print($"[{Multiplayer.GetUniqueId()}] ✓ Player {playerId} spawned with authority {player.GetMultiplayerAuthority()}");
		
		// Set spawn position
		SetPlayerSpawnPosition(player, playerId, spawnIndex);
	}
    
    private void SetPlayerSpawnPosition(TestPlayer player, long playerId, int spawnIndex)
    {
        var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
        int totalSpawns = spawnPoints.Count;
        
        if (totalSpawns > 0)
        {
            // Clamp spawn index to valid range
            int indexToUse = Math.Clamp(spawnIndex, 0, totalSpawns - 1);
            
            var spawn = spawnPoints[indexToUse] as Node3D;
            if (spawn != null)
            {
                player.GlobalPosition = spawn.GlobalPosition;
                player.GlobalRotation = spawn.GlobalRotation;
                GD.Print($"[{Multiplayer.GetUniqueId()}] Player {playerId} at spawn {indexToUse}: {spawn.GlobalPosition}");
            }
            else
            {
                // Fallback to offset
                float offset = (playerId % 4) * 3f;
                player.GlobalPosition = new Vector3(offset, 2, 0);
                GD.Print($"[{Multiplayer.GetUniqueId()}] Player {playerId} at fallback position");
            }
        }
        else
        {
            // No spawn points - spread players in a line
            int playerIndex = GetChildren().Count - 1; // -1 for the player we just added
            float offset = playerIndex * 3f;
            player.GlobalPosition = new Vector3(offset, 2, 0);
            GD.Print($"[{Multiplayer.GetUniqueId()}] No spawn points, player {playerId} at offset {offset}");
        }
    }
    
    public void RemovePlayer(long playerId)
	{
		var player = GetNodeOrNull(playerId.ToString());
		if (player != null)
		{
			player.QueueFree();
			GD.Print($"[GameManager] Removed player {playerId}");
		}
		
		// Free up spawn index
		if (playerSpawnIndex.ContainsKey(playerId))
		{
			playerSpawnIndex.Remove(playerId);
		}
	}
    
    private int GetNextAvailableSpawnIndex()
    {
        var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
        int totalSpawns = spawnPoints.Count;
        
        if (totalSpawns == 0)
        {
            return 0;
        }
        
        // Round-robin spawn assignment
        int index = nextSpawnIndex % totalSpawns;
        nextSpawnIndex = (nextSpawnIndex + 1) % totalSpawns;
        
        return index;
    }
    
    public int GetSpawnIndexForPlayer(long playerId)
    {
        if (playerSpawnIndex.ContainsKey(playerId))
        {
            return playerSpawnIndex[playerId];
        }
        
        int index = GetNextAvailableSpawnIndex();
        playerSpawnIndex[playerId] = index;
        return index;
    }
    
    public void ResetSpawnSystem()
    {
        playerSpawnIndex.Clear();
        nextSpawnIndex = 0;
        GD.Print("[GameManager] Spawn system reset");
    }
}