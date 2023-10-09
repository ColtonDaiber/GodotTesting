using Godot;
using Godot.NativeInterop;
using System;
using System.Runtime.InteropServices;

[Tool]
public partial class MeshGen : StaticBody3D
{
	[Export] MeshInstance3D meshInstance;
	[Export] CollisionShape3D collisionShape;

	public override void _Ready()
	{
		Vector3[] vertex =
		{
			new Vector3(0,0,0),
			new Vector3(1,0,0),
			new Vector3(1,0,1),
			new Vector3(0,0,1),

			new Vector3(0,1,0),
			new Vector3(1,1,0),
			new Vector3(1,1,1),
			new Vector3(0,1,1)
		};

		int[] index =
		{
			//bottom
			0, 2, 1,
			0, 3, 2,

			//sides
			0, 1, 5,
			0, 5, 4,

			0, 4, 7,
			0, 7, 3,

			2, 5, 1,
			2, 6, 5,

			2, 3, 7,
			2, 7, 6,

			//top
			4, 5, 6,
			4, 6, 7
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

	public override void _Process(double delta)
	{
	}
}
