using Godot;
using System;

[Tool]
public partial class TriangleGen : StaticBody3D
{
	[Export] MeshInstance3D meshInstance;
	[Export] CollisionShape3D collisionShape;

	public override void _Ready()
	{
		Vector3[] vertex =
		{
			new Vector3(0,0,0),
			new Vector3(1,0,0),
			new Vector3(.5f,0,2f),

			new Vector3(0,1,0),
			new Vector3(1,1,0),
			new Vector3(.5f,1,2f)
		};

		int[] index =
		{
			//bottom
			0, 2, 1,

			//back
			0, 1, 4,
			0, 4, 3,

			//sides
			0, 5, 2,
			0, 3, 5,

			1, 2, 5,
			1, 5, 4,

			//top
			3, 4, 5
		};

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertex;
		arrays[(int)Mesh.ArrayType.Index] = index;


		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		meshInstance.Mesh = mesh;
		collisionShape.Shape = mesh.CreateConvexShape();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
