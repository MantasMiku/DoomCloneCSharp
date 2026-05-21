using Godot;
using System.Collections.Generic;

public partial class PickUpSpawner : Node3D
{
    [Export] public PackedScene HealthPickup;
    [Export] public PackedScene AmmoPickup;
    [Export] public PackedScene ArmorPickup;

    [Export] public float RespawnTime = 10f;

    private List<Marker3D> spawnPoints = new();

    private Dictionary<Marker3D, Node3D> activePickups = new();

    public override void _Ready()
    {
        foreach (Node child in GetNode("SpawnPoints").GetChildren())
        {
            if (child is Marker3D marker)
            {
                SpawnPickupAt(marker);
                spawnPoints.Add(marker);
            }
        }

        GD.Print("Pickup system initialized (no stacking)");
    }

    // SPAWN SPECIFIC MARKER
    private void SpawnPickupAt(Marker3D marker)
    {
        if (activePickups.ContainsKey(marker))
            return; 

        PackedScene scene = GetRandomPickup();

        Node3D pickup = scene.Instantiate<Node3D>();

        Vector3 pos = GetGroundPosition(marker.GlobalPosition);
        pos += Vector3.Up * 0.5f; 

        AddChild(pickup);

        pickup.GlobalPosition = pos;

        

        activePickups[marker] = pickup;

        pickup.TreeExited += () => OnPickupRemoved(marker);
    }

    // RESPAWN LOGIC
    private async void OnPickupRemoved(Marker3D marker)
    {
        activePickups.Remove(marker);

        await ToSignal(GetTree().CreateTimer(RespawnTime), "timeout");

        SpawnPickupAt(marker);
    }

    // RANDOM PICKUP
    private PackedScene GetRandomPickup()
    {
        int r = GD.RandRange(0, 2);

        switch (r)
        {
            case 0: return HealthPickup;
            case 1: return AmmoPickup;
            case 2: return ArmorPickup;
        }

        return HealthPickup;
    }

    private Vector3 GetGroundPosition(Vector3 start)
    {
        var space = GetWorld3D().DirectSpaceState;

        var query = PhysicsRayQueryParameters3D.Create(
            start + Vector3.Up * 5f,
            start + Vector3.Down * 20f
        );

        var result = space.IntersectRay(query);

        if (result.Count > 0)
            return (Vector3)result["position"];

        return start;
    }
}