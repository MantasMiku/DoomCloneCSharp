using Godot;
using System.Collections.Generic;

public partial class EnemyManager : Node3D
{
    [Export] public PackedScene ImpScene;
    [Export] public PackedScene SoldierScene;
    [Export] public PackedScene SpiderMinionScene;
    [Export] public PackedScene MotherSpiderScene;

    [Export] public int BaseMaxEnemies = 10;
    [Export] public float SpawnDelay = 2;

    private List<Marker3D> spawnPoints = new();

    private int wave = 1;
    private int enemiesToSpawn = 0;
    private int enemiesAlive = 0;

    private Timer spawnTimer;

    public override void _Ready()
    {
        foreach (Node child in GetNode("SpawnPoints").GetChildren())
        {
            if (child is Marker3D marker)
                spawnPoints.Add(marker);
        }

        spawnTimer = new Timer();
        spawnTimer.WaitTime = SpawnDelay;
        spawnTimer.Timeout += SpawnNextEnemy;
        AddChild(spawnTimer);

        StartWaveWithBoss();
    }

    // START WAVE
    private void StartWave()
    {
        GD.Print($"STARTING WAVE {wave}");

        enemiesToSpawn = CalculateEnemiesForWave();
        enemiesAlive = 0;

        spawnTimer.Start();
    }

    // WAVE SIZE 
    private int CalculateEnemiesForWave()
    {
        if (wave == 1)
            return BaseMaxEnemies;

        if (wave == 2)
            return (int)(BaseMaxEnemies * 1.5f);

        if (wave == 3)
            return (int)(BaseMaxEnemies * 1.75f);

        // 4+ waves scale linearly
        return (int)(BaseMaxEnemies * (wave / 3));
    }

    // SPAWN LOOP
    private void SpawnNextEnemy()
    {
        if(enemiesAlive >= 30)
            return;

        if (enemiesToSpawn <= 0)
        {
            spawnTimer.Stop();
            return;
        }

        PackedScene enemyScene = GetEnemyForWave();

        Node3D enemy = enemyScene.Instantiate<Node3D>();
        AddChild(enemy);
        enemy.GlobalPosition = GetSpawnPosition();

        enemiesToSpawn--;
        enemiesAlive++;

        enemy.TreeExited += OnEnemyDied;
    }

    private void OnEnemyDied()
    {
        enemiesAlive--;

        // Wave finished
        if (enemiesAlive <= 0 && enemiesToSpawn <= 0)
        {
            wave++;
            StartWaveWithBoss();
        }
    }

    // ENEMY TYPES PER WAVE

    private PackedScene GetEnemyForWave()
    {
        int r = GD.RandRange(0, 99);

        // Wave 1 → only imps
        if (wave == 1)
            return ImpScene;

        // Wave 2 → imps + soldiers
        if (wave == 2)
            return (r < 60) ? ImpScene : SoldierScene;

        // Wave 3 → add spider minions
        if (wave == 3)
        {
            if (r < 40) return ImpScene;
            if (r < 80) return SoldierScene;
            return SpiderMinionScene;
        }

        // Wave 4+ → all enemies
        if (r < 50) return ImpScene;
        if (r < 90) return SoldierScene;
        return SpiderMinionScene;
    }

    // BOSS 
      private void SpawnBossIfNeeded()
    {
        if (wave % 4 == 0)
        {
            GD.Print("BOSS WAVE");

            int bossCount = wave / 4;
            if(bossCount > 3)
            {
                bossCount = 3;
            }
            for (int i = 0; i < bossCount; i++)
            {
                Node3D boss = MotherSpiderScene.Instantiate<Node3D>();
                AddChild(boss);
                boss.GlobalPosition = GetSpawnPosition();

                

                enemiesAlive++;
                boss.TreeExited += OnEnemyDied;
            }
        }
    }

    // Call boss spawn at start of wave
    private void StartWaveWithBoss()
    {
        SpawnBossIfNeeded();
        StartWave();
    }

    // SPAWN POSITION
    private Vector3 GetSpawnPosition()
    {
        for (int i = 0; i < 10; i++)
        {
            var point = spawnPoints[GD.RandRange(0, spawnPoints.Count - 1)];

            Vector3 pos = GetGroundPosition(point.GlobalPosition);

            pos += new Vector3(
                (float)GD.RandRange(-1.5f, 1.5f),
                0,
                (float)GD.RandRange(-1.5f, 1.5f)
            );

            if (IsFarFromPlayer(pos))
                return pos;
        }

        return spawnPoints[0].GlobalPosition;
    }

    private Vector3 GetGroundPosition(Vector3 start)
    {
        var space = GetWorld3D().DirectSpaceState;

        var query = PhysicsRayQueryParameters3D.Create(
            start + Vector3.Up * 3f,
            start + Vector3.Down * 15f
        );

        var result = space.IntersectRay(query);

        if (result.Count > 0)
            return (Vector3)result["position"];

        return start;
    }

    private bool IsFarFromPlayer(Vector3 pos)
    {
        var players = GetTree().GetNodesInGroup("PLAYER");

        foreach (Node p in players)
        {
            if (p is Node3D p3d)
            {
                if (pos.DistanceTo(p3d.GlobalPosition) < 6f)
                    return false;
            }
        }

        return true;
    }
}