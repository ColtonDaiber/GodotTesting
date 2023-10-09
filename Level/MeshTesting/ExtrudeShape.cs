using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

[Tool]
public partial class ExtrudeShape : Node3D
{
	[Export] public Node3D physicsBody;

	public Vector2[] pointsInShape = 
	{
		new Vector2(0,0),
		new Vector2(1,0),
		new Vector2(1,1),
		new Vector2(-.5f,1),
		new Vector2(1,1.5f),
		new Vector2(0,1.5f)
	};
	public int[] indiciesInShape =
	{
		0,3,2,
		0,2,1,
		3,4,2,
		4,3,5 
	};

	MeshInstance3D meshInstance;
	CollisionShape3D collisionShape;

	public void Create3DShape(Vector3[] vertexs, int[] indicies)
	{
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertexs;
		arrays[(int)Mesh.ArrayType.Index] = indicies;


		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		if(meshInstance == null)
		{
			meshInstance = new MeshInstance3D();
			physicsBody.AddChild(meshInstance);
		}
		if(collisionShape == null)
		{
			collisionShape = new CollisionShape3D();
			physicsBody.AddChild(collisionShape);
		}
		
		meshInstance.Mesh = mesh;
		collisionShape.Shape = mesh.CreateConvexShape();
	}

	public void Extrude2DShape(Vector2[] points, int[] indicies, float extrudeDist)
	{
		if(indicies.Length % 3 != 0) //this stores list of triangles, so if its not a multiple of 3, theres an error
		{
			GD.PushError("invalid indicies array");
			return;
		}

		//get all faces
		List<Vector2I> newFaces = new List<Vector2I>();
		for(int i = 0; i < indicies.Length; i += 3)
		{
			newFaces.Add(new Vector2I(indicies[i], indicies[i+1]));
			newFaces.Add(new Vector2I(indicies[i+1], indicies[i+2]));
			newFaces.Add(new Vector2I(indicies[i+2], indicies[i]));
		}

		//remove internal faces
		bool[] removeFaces = new bool[newFaces.Count];
		for(int i = 0; i < newFaces.Count; i++)
		{
			int multiple = -1;
			for(int w = 0; w < newFaces.Count; w++)
			{
				if( i != w && ( (newFaces[w].X == newFaces[i].X && newFaces[w].Y == newFaces[i].Y) || (newFaces[w].Y == newFaces[i].X && newFaces[w].X == newFaces[i].Y)) )
				{
					if(multiple == -1) 
					{
						multiple = w;
						break;
					}
				}
			}
			if(multiple != -1)
			{
				removeFaces[multiple] = true;
				removeFaces[i] = true;
			}
		}
		for(int i = newFaces.Count -1; i >= 0; i--) if(removeFaces[i]) newFaces.RemoveAt(i);

		// GD.Print("new faces");
		// for(int i = 0; i < newFaces.Count; i++)
		// {
		// 	GD.Print(newFaces[i].X + ", " + newFaces[i].Y);
		// }

		//organize indicies and verticies
		Vector3[] verticies = new Vector3[points.Length * 2];
		for(int i = 0; i < points.Length; i++) verticies[i] = new Vector3(points[i].X, points[i].Y, 0);
		for(int i = 0; i < points.Length; i++) verticies[i+points.Length] = new Vector3(points[i].X, points[i].Y, -extrudeDist);

		List<int> addIndicies = new List<int>();

		for(int i = 0; i < newFaces.Count; i++)
		{
			//tri 1
			addIndicies.Add(newFaces[i].Y);
			addIndicies.Add(newFaces[i].X);
			addIndicies.Add(newFaces[i].X + points.Length);
			//tri 2
			addIndicies.Add(newFaces[i].Y);
			addIndicies.Add(newFaces[i].X + points.Length);
			addIndicies.Add(newFaces[i].Y + points.Length);
			// break;
		}

		//add bottom face
		for(int i = 0; i < indicies.Length; i += 3)
		{
			addIndicies.Add(indicies[i]+points.Length);
			addIndicies.Add(indicies[i+2]+points.Length);
			addIndicies.Add(indicies[i+1]+points.Length);
		}

		int[] indicies3D = new int[indicies.Length + addIndicies.Count];
		for(int i = 0; i < indicies.Length; i++) indicies3D[i] = indicies[i];
		for(int i = 0; i < addIndicies.Count; i++) indicies3D[i+indicies.Length] = addIndicies[i];
		Create3DShape(verticies, indicies3D);
	}



	Vector3[] PointsToVertex(Vector2[] points, float Zpos = 0)
	{
		Vector3[] vertex = new Vector3[points.Length];

		for(int i = 0; i < points.Length; i++)
		{
			vertex[i] = new Vector3(points[i].X, points[i].Y, 0);
		}

		return vertex;
	}

	public override void _Ready()
	{
		RemoveChildren();
		// Create3DShape(PointsToVertex(pointsInShape), indiciesInShape);
		Extrude2DShape(pointsInShape, indiciesInShape, 1);
	}

	void RemoveChildren()
	{
		var children = this.GetChildren();
		for(int i = children.Count - 1; i >= 0; i--)
		{
			if(children[i] != physicsBody) children[i].QueueFree();
		}
	}

	public override void _Process(double delta)
	{
	}
}
