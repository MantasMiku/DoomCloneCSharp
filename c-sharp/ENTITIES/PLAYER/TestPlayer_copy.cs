using Godot;

public partial class TestPlayer_copy : CharacterBody3D
{
    private const float MAX_VELOCITY_AIR = 0.6f;
    private const float MAX_VELOCITY_GROUND = 6.0f;
    private const float MAX_ACCELERATION = 10f * MAX_VELOCITY_GROUND;
    private const float GRAVITY = 15.34f;
    private const float STOP_SPEED = 10f;
    private readonly float JUMP_IMPULSE = Mathf.Sqrt(2 * GRAVITY * 0.85f);

    private float friction = 4f;

    private Vector3 direction = Vector3.Zero;
    private bool wishJump = false;

    public override void _Ready()
    {
        PlayerStats.playerHealth = 100;
        //GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        ProcessInput();
        ProcessMovement(dt);
        TestMethod();  
    }
    private void ProcessInput()
    {
        direction = Vector3.Zero;

        if (Input.IsActionPressed("Forward"))
            direction -= Transform.Basis.Z;
        if (Input.IsActionPressed("Back"))
            direction += Transform.Basis.Z;
        if (Input.IsActionPressed("Left"))
            direction -= Transform.Basis.X;
        if (Input.IsActionPressed("Right"))
            direction += Transform.Basis.X;

        direction.Y = 0; 
        direction = direction.Normalized();

        wishJump = Input.IsActionJustPressed("Jump");
    }

    private void ProcessMovement(float dt)
    {
        if (IsOnFloor())
        {
            if (wishJump)
            {
                Velocity = new Vector3(Velocity.X, JUMP_IMPULSE, Velocity.Z);
                Velocity = UpdateVelocityAir(direction, dt);
                wishJump = false;
            }
            else
            {
                Velocity = UpdateVelocityGround(direction, dt);
            }
        }
        else
        {
            // Gravity only in the air
            Velocity -= new Vector3(0, GRAVITY * dt, 0);
            Velocity = UpdateVelocityAir(direction, dt);
        }

        MoveAndSlide();
    }
    private Vector3 UpdateVelocityGround(Vector3 wishDir, float dt)
    {
        ApplyFriction(dt);
        return Accelerate(wishDir, MAX_VELOCITY_GROUND, dt);
    }
    private Vector3 UpdateVelocityAir(Vector3 wishDir, float dt)
    {
        return Accelerate(wishDir, MAX_VELOCITY_AIR, dt);
    }

    private Vector3 Accelerate(Vector3 wishDir, float maxVelocity, float dt)
    {
        float currentSpeed = Velocity.Dot(wishDir);
        float addSpeed = maxVelocity - currentSpeed;

        if (addSpeed <= 0)
            return Velocity;

        float accelSpeed = Mathf.Clamp(addSpeed, 0, MAX_ACCELERATION * dt);
        return Velocity + wishDir * accelSpeed;
    }
    private void ApplyFriction(float dt)
    {
        float speed = Velocity.Length();

        if (speed <= 0)
            return;

        float control = Mathf.Max(STOP_SPEED, speed);
        float drop = control * friction * dt;

        float newSpeed = Mathf.Max(speed - drop, 0);
        newSpeed /= speed;

        Velocity = new Vector3(
            Velocity.X * newSpeed,
            Velocity.Y,
            Velocity.Z * newSpeed
        );
    }

    public void ReloadScene()
    {
        // Get the current scene
        var currentScene = GetTree().CurrentScene;

        // Get its path
        var scenePath = currentScene.SceneFilePath;

        // Reload the scene
        var newScene = ResourceLoader.Load<PackedScene>(scenePath);
        GetTree().ChangeSceneToPacked(newScene);
    }

    public void TestMethod()
    {
        if(Input.IsKeyPressed(Key.O))
        {
            PlayerStats.shotgunAmmo += 2;
            PlayerStats.pistolAmmo += 2;
        }

        if (PlayerStats.playerHealth <= 0 || Input.IsKeyPressed(Key.F10))
        {
            ReloadScene();
        }

        if(Input.IsKeyPressed(Key.P))
        {
            PlayerStats.PrintStats();
        }
    }
}
