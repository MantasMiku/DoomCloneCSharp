using Godot;

public partial class Door : Node3D
{
    [Export] public float OpenHeight = 4f;
    [Export] public float OpenSpeed = 2f;
    [Export] public float CloseDelay = 3f;
    [Export] public RoomController Room; // drag your Room node here in Inspector

    private StaticBody3D door;
    private Area3D trigger;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private Tween tween;
    private bool isOpen = false;
    private bool _roomCleared = false;
    AudioStreamPlayer3D doorPlayer;
    AudioStream doorOpen;
    AudioStream doorClose;

    public override void _Ready()
    {
		doorPlayer = new AudioStreamPlayer3D();
        AddChild(doorPlayer);
        doorPlayer.VolumeDb = -2;
        doorOpen = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/door.wav");
        doorClose = GD.Load<AudioStream>("res://GRAPHICS/SOUNDS/door_close.wav");

        door = GetNode<StaticBody3D>("StaticBody3D");
        trigger = GetNode<Area3D>("Area3D");

        closedPosition = door.Position;
        openPosition = closedPosition + Vector3.Up * OpenHeight;

        trigger.BodyEntered += OnBodyEntered;
        trigger.BodyExited += OnBodyExited;
		OpenDoor();
		
        if (Room != null)
            Room.RoomCleared += OnRoomCleared;
        else
            GD.PushWarning("Door: No RoomController assigned in Inspector.");
    }

    private void OnRoomCleared()
    {
		OpenDoor();
        _roomCleared = true;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup("PLAYER")) return;
        if (isOpen) return;
        if (!_roomCleared) return; // locked until room is cleared

        isOpen = true;
        OpenDoor();
    }

    private void OnBodyExited(Node3D body)
    {
        if (!body.IsInGroup("PLAYER")) return;
        if (!isOpen) return;

        CloseDoor();
        isOpen = false;
    }

    private void OpenDoor()
    {
        doorPlayer.Stream = doorOpen;
        doorPlayer.Play();
        tween?.Kill();
        tween = CreateTween();
        tween.TweenProperty(door, "position", openPosition, OpenSpeed);
    }

    public void CloseDoor()
    {
        doorPlayer.Stream = doorClose;
        doorPlayer.Play();
        tween?.Kill();
        tween = CreateTween();
        tween.TweenProperty(door, "position", closedPosition, OpenSpeed);
    }
}