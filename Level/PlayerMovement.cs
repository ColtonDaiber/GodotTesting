using System;
using Godot;
// using System;

public partial class PlayerMovement : RigidBody3D
{
	[Export] private Node3D playerCamera;
	const float cameraHeight = 2;
	const float cameraDist = 3.5f;
	float cameraAngle = 90;
	const int cameraSpeed = 200;

	bool forward = false;
	bool backward = false;
	bool left = false;
	bool right = false;
	bool jump = false;

	const int forceMag = 20;
	// const int cancelInitaMult = 3;
	
	public override void _Ready()
	{
	}

    public override void _PhysicsProcess(double delta)
    {
		Vector3 force = new Vector3(Mathf.Cos(DegToRad(cameraAngle))*((forward ? -1 : 0)+(backward ? 1 : 0)), 0, Mathf.Sin(DegToRad(cameraAngle))*((forward ? -1 : 0)+(backward ? 1 : 0)) );
        force = force.Normalized() * forceMag;
		
		// if((this.LinearVelocity.X > 0 && force.X < 0) || (this.LinearVelocity.X < 0 && force.X > 0)) force.X *= cancelInitaMult;
		// if((this.LinearVelocity.Z > 0 && force.Z < 0) || (this.LinearVelocity.Z < 0 && force.Z > 0)) force.Z *= cancelInitaMult;

		if(jump) this.ApplyImpulse(new Vector3(0, 5, 0));
		jump = false;

		this.ApplyCentralForce(force);
    }

    public override void _Process(double delta)
	{
		if(left) cameraAngle -= (float)delta * cameraSpeed;
		if(right) cameraAngle += (float)delta * cameraSpeed;
		PlaceCamera();
	}

	public override void _Input(InputEvent inputEvent)
    {
        if(inputEvent.IsActionPressed("Forward")) forward = true;
		if(inputEvent.IsActionPressed("Backward")) backward = true;
		if(inputEvent.IsActionPressed("Left")) left = true;
		if(inputEvent.IsActionPressed("Right")) right = true;
		if(inputEvent.IsActionPressed("Jump")) jump = true;

		if(inputEvent.IsActionReleased("Forward")) forward = false;
		if(inputEvent.IsActionReleased("Backward")) backward = false;
		if(inputEvent.IsActionReleased("Left")) left = false;
		if(inputEvent.IsActionReleased("Right")) right = false;
		if(inputEvent.IsActionReleased("Jump")) jump = false;
    }

	void PlaceCamera()
	{
		playerCamera.RotationDegrees = new Vector3(playerCamera.RotationDegrees.X, 90-cameraAngle, playerCamera.RotationDegrees.Z);
		Vector3 camPos = playerCamera.Position;
		camPos.Y = this.Position.Y + cameraHeight;
		camPos.X = this.Position.X + cameraDist*Mathf.Cos(DegToRad(cameraAngle));
		camPos.Z = this.Position.Z + cameraDist*Mathf.Sin(DegToRad(cameraAngle));
		playerCamera.Position = camPos;
	}

	float DegToRad(float degree)
	{
		return degree * (Mathf.Pi/180);
	}
}
