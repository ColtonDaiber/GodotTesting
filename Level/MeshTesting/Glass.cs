using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

// [Tool]
public partial class Glass : Node3D
{
	[Export] ShaderMaterial GlassMat;
	public RigidBody3D body3D;
	// public Vector2[] initalPoints = 
	// {
	// 	new Vector2(-0f,4.75f),
	// 	new Vector2(0,5f),
	// 	new Vector2(1,5),
	// 	new Vector2(0.82608694f,4.130435f)
	// };
	public Vector2[] initalPoints = 
	{
		// new Vector2(0,0),
		// new Vector2(0,2.5f),
		// new Vector2(2.5f,2.5f),

		new Vector2(0,0),
		new Vector2(5,0),
		new Vector2(5,5),
		new Vector2(0,5)
	};
	public int[] initalIndicies =
	{
		// 0, 1, 2

		0,3,2,
		0,2,1,
	};
	// public Vector2[] initalPoints = 
	// {
	// 	new Vector2(0, 0),
	// 	new Vector2(2.7142856f, 2.7142856f),
	// 	new Vector2(5, 1),
	// 	new Vector2(5, 0)
	// };

	// tris
	// public Vector2[] initalPoints = 
	// {
	// 	new Vector2(0, .75f),
	// 	new Vector2(2.5f, 2.5f),
	// 	new Vector2(0, 2.1f),
	// };
	const bool canBreak = true;
	Shape initalShape;
	MeshGen meshGen = new MeshGen();

	List<ShapeObject> shapeList = new List<ShapeObject>();
	private RandomNumberGenerator random = new RandomNumberGenerator();
	public override void _Ready()
	{
		random.Randomize(); //sets seed
		GD.Print(random.Seed);
		// random.Seed = 1;
		// random.Seed = 7950268038645855679;
		initalShape = new Shape(initalPoints, initalIndicies);
		// initalShape = meshGen.CreateShapeFromPoints(new List<Vector2>(initalPoints));
		for(int i = 0; i < this.GetChildCount(); i++)
		{
			body3D = this.GetChildOrNull<RigidBody3D>(i);
			if(body3D != null) break;
		}
		if(body3D == null)
		{
			body3D = new RigidBody3D();
			this.AddChild(body3D);
		}
		shapeList.Add(new ShapeObject(new List<Shape>{initalShape}, body3D));
		meshGen.Create3DShape(body3D, meshGen.Extrude2DShape(initalShape, 0.5f), GlassMat);

		// RemoveChildren();
		// for(int i = 0; i < 10; i++)
		// {
		// 	Break();
		// }
		// for(int i = 0; i < shapeList.Count; i++)
		// {
		// 	for(int ii = 0; ii < shapeList[i].shapes.Count; ii++)
		// 	{
		// 		RigidBody3D rb = new RigidBody3D();
		// 		rb.Position = shapeList[i].rb.Position;
		// 		rb.Rotation = shapeList[i].rb.Rotation;
		// 		this.AddChild(rb);
		// 		meshGen.Create3DShape(rb, meshGen.Extrude2DShape(shapeList[i].shapes[ii], 0.5f), GlassMat);
		// 	}
		// }
	}

	public int breakCnt = 0;
	void Break()
	{
		int breakType = random.RandiRange(0,2);
		float x = random.RandfRange(0,5);
		float y = random.RandfRange(0,5);
		float x2 = 2.5f;
		float y2 = 2.5f;
		if(breakType == 2)
		{
			x2 = random.RandfRange(0,5);
			y2 = random.RandfRange(0,5);
		}
		
		shapeList = meshGen.CutShapes(shapeList, new Vector2(x, y), new Vector2(x2, y2));
	}

	void RemoveChildren()
	{
		var children = this.GetChildren();
		for(int i = children.Count - 1; i >= 0; i--)
		{
			children[i].QueueFree();
		}
	}

	bool timerRunning = true;
	const double startTimeDelay = 1f;
	double timeDelay = startTimeDelay;
	public override void _Process(double delta)
	{
		if(timerRunning && timeDelay > 0) timeDelay -= delta;
		if(timerRunning && timeDelay <= 0)
		{
			timeDelay = startTimeDelay;
			timerRunning = false;
			// if(breakCnt > 10) timerRunning = false;
			if(canBreak)
			{
				RemoveChildren();
				for(int i = 0; i < 10; i++)
				{
					Break();
				}
				for(int i = 0; i < shapeList.Count; i++)
				{
					for(int ii = 0; ii < shapeList[i].shapes.Count; ii++)
					{
						RigidBody3D rb = new RigidBody3D();
						rb.Position = shapeList[i].rb.Position;
						rb.Rotation = shapeList[i].rb.Rotation;
						this.AddChild(rb);
						meshGen.Create3DShape(rb, meshGen.Extrude2DShape(shapeList[i].shapes[ii], 0.5f), GlassMat);
					}
				}
			}
		}
	}
}
