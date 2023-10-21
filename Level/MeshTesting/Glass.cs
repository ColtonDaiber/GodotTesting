using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[Tool]
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
	Shape initalShape;
	ExtrudeShape extrudeShape = new ExtrudeShape();

	public override void _Ready()
	{
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
		

		initalShape = new Shape(initalPoints, initalIndicies);
		// Shape shape = extrudeShape.CreateShapeFromPoints(new List<Vector2>(initalPoints));
		Shape3D shape3D = extrudeShape.Extrude2DShape(initalShape, 0.5f);
		// extrudeShape.Create3DShape(body3D, shape3D.verticies, shape3D.indicies);
	}

	void Break()
	{
		GD.Print("break");
		Vector3 bodyPos = body3D.Position;
		RemoveChildren();
		Node3D node = new Node3D();
		this.AddChild(node);
		node.Position = bodyPos;

		List<Shape> shapes = new List<Shape>();
		Shape[] cutShapes = extrudeShape.CutShape(initalShape, new Vector2(0,0), new Vector2(1, 5));
		shapes.Add(cutShapes[0]);
		shapes.Add(cutShapes[1]);

		List<Shape> tempShapes = new List<Shape>();
		for(int i = 0; i < shapes.Count; i++)
		{
			Shape[] tempCutShapes = extrudeShape.CutShape(shapes[i], new Vector2(1,4), new Vector2(5,1));
			tempShapes.Add(tempCutShapes[0]);
			tempShapes.Add(tempCutShapes[1]);
		}

		for(int i = 3; i < tempShapes.Count; i++)
		{
			RigidBody3D body = new RigidBody3D();
			node.AddChild(body);
			extrudeShape.Create3DShape(body, extrudeShape.Extrude2DShape(tempShapes[i], 0.5f));
			for(int w = 0; w < tempShapes[i].points.Count; w++) GD.Print(tempShapes[i].points[w]);
			break;
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
	double timeDelay = 0f;
	public override void _Process(double delta)
	{
		if(timerRunning && timeDelay > 0) timeDelay -= delta;
		if(timerRunning && timeDelay <= 0)
		{
			timerRunning = false;
			Break();
		}
	}
}
