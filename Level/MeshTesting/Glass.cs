using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

// [Tool]
public partial class Glass : Node3D
{
	[Export] public RigidBody3D body3D;
	// public Vector2[] initalPoints = 
	// {
	// 	new Vector2(-0f,4.75f),
	// 	new Vector2(0,5f),
	// 	new Vector2(1,5),
	// 	new Vector2(0.82608694f,4.130435f)
	// };
	public Vector2[] initalPoints = 
	{
		new Vector2(0,0),
		new Vector2(5,0),
		new Vector2(5,5),
		new Vector2(0,5)
	};
	public int[] initalIndicies =
	{
		0,3,2,
		0,2,1
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
	// // {
	// // 	new Vector2(0, 0),
	// // 	new Vector2(1, 0),
	// // 	new Vector2(2, 2),
	// // };
	// {
	// 	new Vector2(0, 0),
	// 	new Vector2(2.5f, 2.5f),
	// 	new Vector2(4.4f, 5),
	// };
	const bool canBreak = true;
	Shape initalShape;
	ExtrudeShape extrudeShape = new ExtrudeShape();

	List<Shape> shapeList;
	private RandomNumberGenerator random = new RandomNumberGenerator();
	public override void _Ready()
	{
		// random.Randomize(); //sets seed
		random.Seed = 1;
		initalShape = new Shape(initalPoints, initalIndicies);
		// initalShape = extrudeShape.CreateShapeFromPoints(new List<Vector2>(initalPoints));
		shapeList = new List<Shape>{initalShape};
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
		
		extrudeShape.Create3DShape(body3D, extrudeShape.Extrude2DShape(initalShape, 0.5f));

		// Shape3D newShape = extrudeShape.Extrude2DShape(initalShape, .5f);
		// extrudeShape.Create3DShape(body3D, newShape);

		// Shape[] newShape = extrudeShape.CutShape(initalShape, new Vector2(2.52f,3.6f), new Vector2(2.5f,2.5f));
		// extrudeShape.Create3DShape(body3D, extrudeShape.Extrude2DShape(newShape[1], 0.5f));
	}

	// List<Shape> shapes = new List<Shape>();
	// void Break()
	// {
	// 	if(shapes.Count == 0) shapes.Add(initalShape);
	// 	// float x = random.RandfRange(0,5);
	// 	// float y = random.RandfRange(0,5);
	// 	// shapes = extrudeShape.CutShapes(shapes, new Vector2(x, y), new Vector2(2.5f, 2.5f));
	// 	shapes = extrudeShape.CutShapes(shapes, new Vector2(2.8240297f, 0.16908048f), new Vector2(2.5f, 2.5f));
	// 	// shapes = extrudeShape.CutShapes(shapes, new Vector2(2.1575003f, 1.3231566f), new Vector2(2.5f, 2.5f));
	// 	// shapes = extrudeShape.CutShapes(shapes, new Vector2(1.7900112f, 2.7643127f), new Vector2(2.5f, 2.5f));
	// 	RemoveChildren();

	// 	for(int i = 0; i < shapes.Count; i++)
	// 	{
	// 		RigidBody3D rb3d = new RigidBody3D();
	// 		this.AddChild(rb3d);
	// 		extrudeShape.Create3DShape(rb3d, extrudeShape.Extrude2DShape(shapes[i], 0.5f));
	// 		// for(int w = 0; w < shapes[i].points.Count; w++) GD.Print(shapes[i].points[w]);
	// 		// break;
	// 	}
	// 	// GD.Print(x + " " + y);
	// }


	void Break()
	{
		
		RemoveChildren();
		float x = random.RandfRange(0,5);
		float y = random.RandfRange(0,5);
		shapeList = extrudeShape.CutShapes(shapeList, new Vector2(x, y), new Vector2(2.5f, 2.5f));

		for(int i = 0; i < shapeList.Count; i++)
		{
			RigidBody3D rb = new RigidBody3D();
			this.AddChild(rb);
			extrudeShape.Create3DShape(rb, extrudeShape.Extrude2DShape(shapeList[i], 0.5f));
		}
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
	double timeDelay = 1f;
	// double timeDelay = 0f;
	public override void _Process(double delta)
	{
		if(timerRunning && timeDelay > 0) timeDelay -= delta;
		if(timerRunning && timeDelay <= 0)
		{
			timeDelay = 1;
			// timerRunning = false;
			if(canBreak) Break();
		}
	}
}
