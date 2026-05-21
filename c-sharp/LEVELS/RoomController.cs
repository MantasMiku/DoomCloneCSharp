using Godot;

public partial class RoomController : Node3D
{
    [Export] public PackedScene ImpScene;
    [Export] public PackedScene SoldierScene;
    [Export] public PackedScene EtcScene;

    [Export] public int ImpCount = 6;
    [Export] public int SoldierCount = 2;
    [Export] public int EtcCount = 0;
    [Export] public int Waves = 2;
    [Export] public float TimeBetweenWaves = 2f;

    [Signal] public delegate void RoomClearedEventHandler();

    private bool _triggered = false;
    private int _currentWave = 0;
    private int _aliveCount = 0;

    public override void _Ready()
    {
        GetNode<Area3D>("Area3D").BodyEntered += OnBodyEntered;
        PreWarm(ImpScene);
		PreWarm(SoldierScene);
		PreWarm(EtcScene);
	}

	private void PreWarm(PackedScene scene)
	{
		if (scene == null) return;
		var dummy = scene.Instantiate<Node>();
		AddChild(dummy);
		dummy.QueueFree();
	}

    private void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup("PLAYER") || _triggered) return;
        _triggered = true;
		foreach (var child in GetChildren())
		{
			if (child is Door door)
				door.CloseDoor();
		}
        SpawnWave();
    }

    private void SpawnWave()
    {
        _currentWave++;
        _aliveCount = 0;

        SpawnGroup(ImpScene,"SpawnPointsImp",ImpCount);
        SpawnGroup(SoldierScene,"SpawnPointsSoldier",SoldierCount);
        SpawnGroup(EtcScene,"SpawnPointsEtc",EtcCount);
    }

    private void SpawnGroup(PackedScene scene, string pointsNodeName, int count)
    {
        if (scene == null || count <= 0) return;

        var markers = GetNode<Node3D>(pointsNodeName).GetChildren();
        for (int i = 0; i < count; i++)
        {
            var marker = markers[i % markers.Count] as Marker3D;
            if (marker == null) continue;

            var enemy = scene.Instantiate<Node3D>();
            GetTree().CurrentScene.AddChild(enemy);

            int overlapIndex = i / markers.Count;
            Vector3 offset = overlapIndex == 0
                ? Vector3.Zero
                : new Vector3(
                    (float)GD.RandRange(-1.5f, 1.5f), 0,
                    (float)GD.RandRange(-1.5f, 1.5f));

            enemy.GlobalPosition = marker.GlobalPosition + offset;
            _aliveCount++;
            WatchEnemy(enemy);
        }
    }

    private async void WatchEnemy(Node3D enemy)
    {
        while (IsInstanceValid(enemy))
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        _aliveCount--;

        if (_aliveCount > 0) return; // wave still in progress

        if (_currentWave >= Waves)
		{
			EmitSignal(SignalName.RoomCleared);
			GD.Print("ROOM CLEARED");
			return;
		}

        // wait then spawn next wave
        await ToSignal(GetTree().CreateTimer(TimeBetweenWaves), SceneTreeTimer.SignalName.Timeout);
        SpawnWave();
    }
}