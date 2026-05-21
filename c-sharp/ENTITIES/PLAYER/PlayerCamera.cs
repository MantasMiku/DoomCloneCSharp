using Godot;

public partial class PlayerCamera : Camera3D
{
    [Export] public float MouseSensitivity = 0.1f;
    [Export] public float MaxPitch = 90f;
    [Export] public float WalkFov = 70f;
    [Export] public float RunFov = 90f;
    [Export] public float FovLerpSpeed = 6f;
    
    
    private float _roll = 0f;
    private float _pitch = 0f;
    private float _value = 1.5f;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if(PlayerStats.playerHealth > 0)
        {
            // Only process if this camera is active
            if (!Current)
                return;
                
            if (@event is InputEventMouseMotion motion)
            {
                Vector2 mouseDelta = motion.Relative * MouseSensitivity;
                
                if (GetParent() is Node3D parent)
                    parent.RotateY(-Mathf.DegToRad(mouseDelta.X));
                
                _pitch -= mouseDelta.Y;
                _pitch = Mathf.Clamp(_pitch, -MaxPitch, MaxPitch);
                RotationDegrees = new Vector3(_pitch, 0, 0);
            }
        }
    }
    
    public override void _Process(double delta)
    {
        // Only process if this camera is active
        if(PlayerStats.playerHealth <= 0)
        {
            _value = Mathf.Lerp(_value,0.2f, (float)delta * 4f);
            Position = new Vector3(Position.X,_value, Position.Z);
            _roll = Mathf.Lerp(_roll, -6, (float)delta * 2f);
            RotationDegrees = new Vector3(RotationDegrees.X, RotationDegrees.Y, _roll);
        }
        else
        {
            if (!Current)
                return;
            
            bool crouchSlide = GetParent<TestPlayer>().crouchSlide;
            float targetFov = WalkFov;
            
            if (Input.IsActionPressed("Forward") && GetParent() is CharacterBody3D player)
            {
                float horizontalSpeed = new Vector2(player.Velocity.X, player.Velocity.Z).Length();
                if (horizontalSpeed > 0.1f)
                    targetFov = RunFov;
            }
            
            Fov = Mathf.Lerp(Fov, targetFov, (float)delta * FovLerpSpeed);
            
            float targetRoll = 0f;
            if (Input.IsActionPressed("Left") && !crouchSlide)
                targetRoll = 6f;
            else if (Input.IsActionPressed("Right") && !crouchSlide)
                targetRoll = -6f;
            else if (crouchSlide || GetParent<TestPlayer>().lockOnWall)
                targetRoll = -8f;
            
            _roll = Mathf.Lerp(_roll, targetRoll, (float)delta * 5f);
            RotationDegrees = new Vector3(RotationDegrees.X, RotationDegrees.Y, _roll);
        }
    }
}