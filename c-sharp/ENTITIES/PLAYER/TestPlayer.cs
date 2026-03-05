using System;
using Godot;

public partial class TestPlayer : CharacterBody3D
{
    private const float MAX_VELOCITY_AIR = 0.6f;
    //private const float MAX_VELOCITY_GROUND = 6.0f;
    private const float MAX_VELOCITY_GROUND_SPRINT = 7.0f;
    private const float MAX_VELOCITY_CROUCH = 2.0f;
    private const float MAX_ACCELERATION = 10f * MAX_VELOCITY_GROUND_SPRINT;
    private const float GRAVITY = 15.34f;
    private const float STOP_SPEED = 10f;
    private readonly float JUMP_IMPULSE = Mathf.Sqrt(2.2f * GRAVITY * 0.85f);
    private float friction = 4f;

    private Vector3 direction = Vector3.Zero;
    private Vector3 lastDirection = Vector3.Zero;
    private bool wishJump = false;
    private bool sprintPressed = false;
    public bool crouchPressed = false;

    public bool crouchSlide = false;
    private bool wasCrouchSliding = false;
    private float invulnerableUntil = 0f;
    private const float ExtraInvulnTime = 1.0f;
    public bool IsInvulnerable => crouchSlide || invulnTimer > 0f;
    private float invulnTimer = 0f;


    private float crouchVal = 0;
    private float tempSpeed = 0;

    private CapsuleShape3D capsule;
    private float standHeight = 1.8f;
    private float crouchHeight = 1.0f;

    //Steps
    private RayCast3D stepRay;
    private RayCast3D stepRayDown;
    float stepScaleY = 1;
    private const float MAX_STEP_HEIGHT = 0.5f;
    Vector3 forwardDir;
    float lastVel;
    Vector3 lastStepPos;
    Vector3 velL;

    //Ledges
    RayCast3D ledgeRayUp;
    RayCast3D ledgeRayDown;
    RayCast3D ledgeRayHeight;
    bool canClimb = false;
    bool climbCanceled = false;
    bool lockOnWall = false;
    private bool isClimbingLedge = false;
    private Vector3 climbStart;
    private Vector3 climbEnd;
    private float climbTimer = 0f;
    private float climbDuration = 0.5f; // tuning speed
    private RayCast3D floorRay;
    bool slidingDownSlope;
    bool downSlope;
    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
    }
    public override void _Ready()
    {

        if (!IsMultiplayerAuthority())
            return;
        capsule = (CapsuleShape3D)GetNode<CollisionShape3D>("CollisionShape3D").Shape;
        stepRay = GetNode<RayCast3D>("StepRay");
        stepRayDown = GetNode<RayCast3D>("StepRayDown");
        Scale = new Vector3(1, 1, 1);
        PlayerStats.playerHealth = 150;
        GetNode<Camera3D>("Camera").Current = true;
        ledgeRayDown = GetNode<RayCast3D>("LedgeRayDown");
        ledgeRayUp = GetNode<RayCast3D>("LedgeRayUp");
        ledgeRayHeight = GetNode<RayCast3D>("LedgeRayHeight");
        floorRay = GetNode<RayCast3D>("FloorRay");

        //GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetMultiplayerAuthority(int.Parse(Name));

    }
    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority())
            return;

        if (invulnTimer > 0f)
            invulnTimer -= (float)delta;

        if(crouchSlide)
            invulnTimer = 0.2f;
        
        GD.Print(IsInvulnerable);

        float dt = (float)delta;
        ProcessInput();
        if (!lockOnWall)
        {
            ProcessCrouch(dt);
            //ProcessStepClimbing(dt);
            ProcessMovement(dt);
        }
        ProcessLedgeClimb();
        UpdateLedgeClimb((float)delta);
        //TestMethod();
        //GD.Print(Velocity.Length());

    }
    private void ProcessInput()
    {
        direction = Vector3.Zero;
        if (!crouchSlide)
        {
            if (Input.IsActionPressed("Forward"))
            {
                direction -= Transform.Basis.Z;
            }
            if (Input.IsActionPressed("Back"))
            {
                direction += Transform.Basis.Z;
            }
            if (Input.IsActionPressed("Left"))
            {
                direction -= Transform.Basis.X;
            }
            if (Input.IsActionPressed("Right"))
            {
                direction += Transform.Basis.X;
            }
        }

        //Steps
        direction.Y = 0;
        direction = direction.Normalized();
        float rayForward = 1.0f;
        float rayHeight = 1.0f;

        Vector3 localDir = GlobalTransform.Basis.Inverse() * direction;
        if (direction.Length() > 0)
        {
            lastStepPos = stepRay.Position;
            stepRay.Position = localDir * rayForward + Vector3.Up * rayHeight;

        }
        else
        {
            stepRay.Position = lastStepPos;
        }

        wishJump = Input.IsActionJustPressed("Jump");
        crouchPressed = Input.IsActionPressed("Crouch");
        sprintPressed = Input.IsActionPressed("Sprint");
        crouchSlide = crouchPressed && Velocity.Length() != 0 && tempSpeed > MAX_VELOCITY_CROUCH;

        if(ledgeRayDown.IsColliding() && !ledgeRayUp.IsColliding())
        {
            Node collider = ledgeRayDown.GetCollider() as Node;
            if(!collider.IsInGroup("ENEMY"))
            {
                canClimb = true;
            }
        }
        else
        {
            canClimb = false;
        }
        

        //Ledges
        
        //GD.Print(canClimb);
    }
    private void ProcessLedgeClimb()
    {

        if (canClimb && !IsOnFloor() && !lockOnWall && !climbCanceled && !crouchPressed)
        {
            lockOnWall = true;
            Velocity = Vector3.Zero;
        }

        if (lockOnWall && !isClimbingLedge)
        {
            //get up
            if (Input.IsActionPressed("Forward") && ledgeRayHeight.IsColliding())
            {
                var ledgePoint = ledgeRayHeight.GetCollisionPoint();
                var destHeight = ledgePoint.Y - GlobalPosition.Y;
                isClimbingLedge = true;
                climbTimer = 0f;

                climbStart = GlobalPosition;

                // where player will end up
                climbEnd = GlobalPosition + new Vector3(0, destHeight, 0) + direction * 1.5f;

                // freeze physics velocity during climb
                Velocity = Vector3.Zero;
            }
            if (wishJump && !ledgeRayHeight.IsColliding())
            {
                Velocity = Vector3.Up * 6 + direction * 8;
                isClimbingLedge = false;
                lockOnWall = false;
            }
            if (Input.IsActionJustPressed("Crouch"))
            {

                isClimbingLedge = false;
                lockOnWall = false;
                climbCanceled = true;
            }
        }
        if (IsOnFloor())
        {
            climbCanceled = false;
        }
    }
    private void UpdateLedgeClimb(float dt)
    {
        if (!isClimbingLedge)
            return;

        climbTimer += dt;
        float t = Mathf.Clamp(climbTimer / climbDuration, 0f, 1f);

        // smoothstep for nicer easing
        t = t * t * (3 - 2 * t);

        // optional arc
        Vector3 mid = (climbStart + climbEnd) / 2f + Vector3.Up * 0.5f;

        // quadratic bezier
        Vector3 a = climbStart.Lerp(mid, t);
        Vector3 b = mid.Lerp(climbEnd, t);

        GlobalPosition = a.Lerp(b, t);

        if (t >= 1f)
        {
            isClimbingLedge = false;
            lockOnWall = false;
        }
    }
    private void ProcessStepClimbing(double dt)
    {
        //GD.Print(IsOnWall());
        if (stepRay.IsColliding() && !IsOnWall())
        {

        }
        //forwardDir = lastStepPos;
        if (!IsOnWall() && !crouchPressed && !crouchSlide && ledgeRayDown.IsColliding())
        {
            lastVel = Velocity.Length();
            velL = Velocity;
            forwardDir = direction;
        }
        else if (IsOnWall() && stepRay.IsColliding())
        {
            float forwardPush;
            if (lastVel > 4)
            {
                forwardPush = lastVel / 1.5f;
            }
            else
            {
                forwardPush = 4;
            }
            var objectPosition = stepRay.GetCollisionPoint();
            var objectHeight = objectPosition.Y - GlobalPosition.Y;
            var objectHeightAditional = objectHeight + 0.05f;
            forwardDir.Y = 0;
            forwardDir = forwardDir.Normalized();

            var moveUp = Mathf.Lerp(0, objectHeightAditional, (float)dt * 10);
            if (objectHeight <= MAX_STEP_HEIGHT)
            {
                //Position = new Vector3(stepRay.GlobalPosition.X, Position.Y+objectHeightAditional, stepRay.GlobalPosition.Z);
                //Position = new Vector3 (Position.X, Position.Y + objectHeightAditional,Position.Z);
                GlobalPosition += new Vector3(0, objectHeight, 0);
                //Velocity = stepRay.Position * 2;
                Velocity = velL;
                //Velocity = forwardDir * forwardPush;

            }
        }
    }
    private void ProcessCrouch(double dt)
    {
        var cam = GetNode<Camera3D>("Camera");
        Vector3 camPos = cam.Position;
        if (crouchPressed && !climbCanceled)
        {
            capsule.Height = Mathf.Lerp(capsule.Height, 1f, (float)dt * 5);
            camPos.Y = Mathf.Lerp(camPos.Y, 1f, (float)dt * 5);
            stepScaleY = Mathf.Lerp(stepScaleY, 0.5f, (float)dt * 6);
        }
        else
        {
            capsule.Height = Mathf.Lerp(capsule.Height, 2f, (float)dt * 5);
            camPos.Y = Mathf.Lerp(camPos.Y, 1.5f, (float)dt * 5);
            stepScaleY = Mathf.Lerp(stepScaleY, 1, (float)dt * 4);
        }
        //stepRay.Scale = new Vector3(Scale.X,stepScaleY,Scale.Z);
        //GD.Print(stepRay.Scale);
        // float targetHeight = crouchPressed ? crouchHeight : standHeight;
        // capsule.Height = Mathf.Lerp(capsule.Height, targetHeight, (float)dt * 10);

        // Optional: move camera smoothly

        // camPos.Y = Mathf.Lerp(camPos.Y, crouchPressed ? 0.6f : 1.4f, (float)dt * 10);
        cam.Position = camPos;
    }
    private void ProcessMovement(float dt)
    {
        if (IsOnFloor())
        {
            if (wishJump && !lockOnWall)
            {
                Velocity = new Vector3(Velocity.X, JUMP_IMPULSE, Velocity.Z);
                Velocity = UpdateVelocityAir(direction, dt);
                wishJump = false;
                lastDirection = direction;
            }
            else if (crouchSlide )
            {
                Velocity = UpdateVelocityCrouchSlide(lastDirection, dt);  
                tempSpeed -= dt * 10;
            }
            else if (crouchPressed && !crouchSlide)
            {
                Velocity = UpdateVelocityCrouch(direction, dt);
                tempSpeed = MAX_VELOCITY_CROUCH;
            }
            else
            {
                //s += dt * 10;
                if (IsOnFloor() && GetFloorNormal() != Vector3.Up && !downSlope)
                {
                    tempSpeed = Velocity.Length() * 1.2f;
                }
                else
                {
                    tempSpeed = Velocity.Length() * 1.2f;
                }
                Velocity = UpdateVelocityGround(direction, dt);
                lastDirection = direction;
                //MAX_VELOCITY_GROUND += dt;
            }
        }

        if (!IsOnFloor() && !slidingDownSlope)
        {
            //GD.Print("Grav");
            Velocity -= new Vector3(0, GRAVITY * dt, 0);
            Velocity = UpdateVelocityAir(direction, dt);
        }

        //GD.Print(Mathf.RadToDeg(GetFloorAngle()));
        var floorNormal = GetFloorNormal();
        bool onSlope = IsOnFloor() && floorNormal != Vector3.Up;

        Vector3 downhillDir = -floorNormal.Slide(Vector3.Up).Normalized();
        //GD.Print(downhillDir);
        bool goingDownSlope = onSlope && Velocity != Vector3.Zero && Velocity.Normalized().Dot(downhillDir) > 0.0f;
        //GD.Print(goingDownSlope);
        var normal = floorRay.GetCollisionNormal();
        float slopeAngle = Mathf.RadToDeg(Mathf.Acos(normal.Dot(Vector3.Up)));
        float angleDeg = Mathf.RadToDeg(GetFloorAngle());
        //GD.Print(slopeAngle);

        if (IsOnFloor() && slopeAngle > 1.0f && slopeAngle < 75.0f)
        {

            if (GetRealVelocity().Y > 0)
            {
                //GD.Print("upSlope");
                downSlope = false;
            }
            else
            {
                //GD.Print("downSlope");
                downSlope = true;
                //Velocity -= new Vector3(0, GRAVITY * dt, 0);
            }
        }

        if (goingDownSlope && direction.Length() > 0)
        {
            //Velocity -= new Vector3(0, GRAVITY * dt, 0);
        }
        if (onSlope && crouchPressed)
        {
            //FloorMaxAngle = Mathf.DegToRad(0f);
            slidingDownSlope = true;
            //tempSpeed = tempSpeed * 1.2f;
            //Velocity -= new Vector3(0, 100 * dt, 0);              
        }
        else
        {
            //tempSpeed = Velocity.Length() * 1.2f;
            //FloorMaxAngle = Mathf.DegToRad(43f);

            slidingDownSlope = false;
        }

        MoveAndSlide();

        //ApplyFloorSnap();
    }
    private Vector3 UpdateVelocityGround(Vector3 wishDir, float dt)
    {
        ApplyFriction(dt);
        return Accelerate(wishDir, MAX_VELOCITY_GROUND_SPRINT, dt);
    }
    private Vector3 UpdateVelocityCrouch(Vector3 wishDir, float dt)
    {
        ApplyFriction(dt);
        return Accelerate(wishDir, MAX_VELOCITY_CROUCH, dt);
    }
    private Vector3 UpdateVelocityCrouchSlide(Vector3 wishDir, float dt)
    {
        ApplyFriction(dt);
        return Accelerate(wishDir, tempSpeed, dt);
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
    
}