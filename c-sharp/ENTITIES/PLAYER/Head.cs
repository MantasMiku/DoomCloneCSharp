using Godot;

public partial class Head : Camera3D
{
    [Export] public float MouseSensitivity = 0.2f;
    [Export] public float MaxPitch = 90f;

    private float _pitch = 0f; // vertical rotation

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        HandleMouseLook((float)delta);

        // Press Esc to release mouse
        if (Input.IsActionJustPressed("ui_cancel"))
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void HandleMouseLook(float delta)
    {
        // Get relative mouse motion
        Vector2 mouseDelta = Input.GetLastMouseVelocity() * MouseSensitivity * delta;

        // Horizontal rotation: rotate parent (player) yaw
        if (GetParent() is Node3D parent)
            parent.RotateY(-mouseDelta.X);

        // Vertical rotation: rotate camera pitch
        _pitch -= mouseDelta.Y;
        _pitch = Mathf.Clamp(_pitch, -MaxPitch, MaxPitch);
        RotationDegrees = new Vector3(_pitch, 0, 0);
    }
}
