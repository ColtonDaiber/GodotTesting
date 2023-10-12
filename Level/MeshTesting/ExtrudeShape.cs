using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;

[Tool]
public partial class ExtrudeShape : Node3D
{
	[Export] public Node3D physicsBody;

	//inital shape
	// public Vector2[] pointsInShape = 
	// {
	// 	new Vector2(0,0),
	// 	new Vector2(2,0),
	// 	new Vector2(2,2),
	// 	new Vector2(0,2)
	// };
	// public int[] indiciesInShape =
	// {
	// 	0,3,2,
	// 	0,2,1
	// };
	public Vector2[] pointsInShape = 
	{
		new Vector2(0,0),
		new Vector2(2,2),
		new Vector2(0,2)
	};
	public int[] indiciesInShape =
	{
		0,2,1
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

		RigidBody3D body = (RigidBody3D)physicsBody;
		body.CenterOfMassMode = RigidBody3D.CenterOfMassModeEnum.Custom;
		Vector3 centerOfMass = new Vector3(0,0,0);
		for(int i = 0; i < vertexs.Length; i++) centerOfMass += vertexs[i];
		centerOfMass = centerOfMass/vertexs.Length;
		body.CenterOfMass = centerOfMass;
	}

	public void Extrude2DShape(Vector2[] points, int[] indicies, float extrudeDist)
	{
		if(indicies.Length % 3 != 0) //this stores list of triangles, so if its not a multiple of 3, theres an error
		{
			GD.PushError("invalid indicies array");
			return;
		}

		//get all new faces from lines
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
		for(int i = newFaces.Count-1; i >= 0; i--) if(removeFaces[i]) newFaces.RemoveAt(i);

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
		Extrude2DShape(pointsInShape, indiciesInShape, .25f);

		int[] ind = {indiciesInShape[0], indiciesInShape[1], indiciesInShape[2]};
		CutTriangle(pointsInShape, ind, new Vector2(0,1), new Vector2(1,1));
		// CutTriangle(pointsInShape, ind, 1, 1);
	}

	void RemoveChildren()
	{
		var children = this.GetChildren();
		for(int i = children.Count - 1; i >= 0; i--)
		{
			if(children[i] != physicsBody) children[i].QueueFree();
		}
	}

	void CutTriangle(Vector2[] verticies, int[] indicies, Vector2 pt1, Vector2 pt2)
	{
		// newVerticies = new List<Vector2>();
		// newIndicies = new List<int>();
		if(indicies.Length != 3)
		{
			GD.PushError("expected 3 indicies!");
			return;
		}

		/* cramer's rule		//shits fucked
			ax + by = c
			dx + ey = f
			Dx = c*e-b*f
			Dy = a*f-c*d
			D = a*e-d*b
		*/
		float lineCutSlope = (pt2.Y-pt1.Y)/(pt2.X-pt1.X);
		float lineCutYIntercept = (-pt1.X*lineCutSlope) + pt1.Y;

		List<Vector2> intersectPts = new List<Vector2>();
		List<int> intersectPtsLineIndex = new List<int>();
		bool sameLine = false; //TODO impliment same line check
		for(int i = 0; i < 3; i++)
		{
			int index2 = (i != 2 ? i+1 : 0);
			float slope = (verticies[indicies[index2]].Y-verticies[indicies[i]].Y)/(verticies[indicies[index2]].X-verticies[indicies[i]].X);
			float yIntercept = (-verticies[indicies[i]].X*slope) + verticies[indicies[i]].Y;
			float x = 0;
			float y = 0;

			bool intersect = true;
			if(float.IsInfinity(lineCutSlope) && !float.IsInfinity(slope))
			{
				GD.Print("lineCutSlope is undef");
				x = pt1.X;
				y = (slope*pt1.X) + yIntercept;
			}
			else if(float.IsInfinity(slope) && !float.IsInfinity(lineCutSlope))
			{
				GD.Print("slope is undef");
				x = verticies[indicies[i]].X;
				y = (lineCutSlope * verticies[indicies[i]].X) + lineCutYIntercept;
			}
			else if(!float.IsInfinity(slope) && !float.IsInfinity(lineCutSlope))
			{
				if(slope == lineCutSlope && yIntercept == lineCutYIntercept) sameLine = true;
				GD.Print("Cramers Rule");
				float a = -lineCutSlope;
				const float b = 1;
				float c = lineCutYIntercept;
				
				float d = -1*(slope);
				const float e = 1;
				float f = yIntercept;

				float D = a*e - d*b;

				x = (c*e - b*f) / D;
				y = (a*f - c*d) / D;
			}
			else
			{	//lines are parallel and never intersect
				GD.Print("parallel");
				if(verticies[indicies[i]].X == pt1.X) sameLine = true;
				intersect = false;
			}

			if(intersect) intersect = CheckIntersectionInRange(verticies[indicies[i]], verticies[indicies[index2]], new Vector2(x, y));
			else continue;

			if(intersect)
			{
				intersectPts.Add(new Vector2(x, y));
				intersectPtsLineIndex.Add(i);
				GD.Print(verticies[indicies[i]].X +" "+ verticies[indicies[i]].Y);
				int shape = GetShapePointIsIn(lineCutSlope, pt1, verticies[indicies[i]]);
				GD.Print(shape);
				GD.Print(verticies[indicies[index2]].X +" "+ verticies[indicies[index2]].Y);
				shape = GetShapePointIsIn(lineCutSlope, pt1, verticies[indicies[index2]]);
				GD.Print(shape);
			}

			// GD.Print("X0, Y0, X1, Y1\t\t" + verticies[indicies[i]].X +","+ verticies[indicies[i]].Y +" "+ verticies[indicies[index2]].X +","+ verticies[indicies[index2]].Y);
			// GD.Print(x +", "+ y);
			// GD.Print(intersect + "\n");
		}

		//make a list of all points, intersection and old vertices
		//for single triangle shape
			//add points that are in shape, and add their index
			//recreate indicies, based on old indices -> find coorisponding vertex/intersection(the intersection on the same line between two original vertexs')
		//for 2 triangle shape
			//add points that are in shape
			//recreate indicies - start with left most point, then go right, then go to point that is lower of two remaining points
			//then do similar steps with other 3 points for second triangle
		//convert triangle info. into shape info.
			//iterate through shape and look for points that match the new list of points
				//if match found change indicies list to have the index that's in the orignal shape
			//add intersections to list of points, and convert any indecies' indexs in new shape into the mesh's indexs
	}

	int GetShapePointIsIn(float slope, Vector2 linePt, Vector2 vertex)
	{
		if(!float.IsInfinity(slope))
		{
			//reconstruct y=mx+b from slope and point -> y=mx - m(x0) + y(0) 	then plug in vertex x, and see if vertex y is greater
			if(vertex.Y > (slope*vertex.X) - (slope*linePt.X) + linePt.Y) return 1;
			else if(vertex.Y < (slope*vertex.X) - (slope*linePt.X) + linePt.Y) return -1;
			else return 0;
		}
		else //line is vertical, ex) x = 0
		{
			if(vertex.X > linePt.X) return 1;
			else if(vertex.X < linePt.X) return -1;
			else return 0;
		}
	}

	bool CheckIntersectionInRange(Vector2 pt1, Vector2 pt2, Vector2 testPoint)
	{
		bool intersect = false;
		float smallerX;
		float biggerX;
		if(pt1.X < pt2.X) { smallerX = pt1.X; biggerX = pt2.X; }
		else { biggerX = pt1.X; smallerX = pt2.X; }
		float smallerY;
		float biggerY;
		if(pt1.Y < pt2.Y) { smallerY = pt1.Y; biggerY = pt2.Y; }
		else { biggerY = pt1.Y; smallerY = pt2.Y; }

		if((testPoint.X >= smallerX && testPoint.X <= biggerX) && (testPoint.Y >= smallerY && testPoint.Y <= biggerY))
		{
			intersect = true;
		}
		else
		{
			intersect = false;
		}
		return intersect;
	}

	public override void _Process(double delta)
	{
	}
}

class Shape
{
	public List<Vector2> vertecies = new List<Vector2>();
	public List<int> indicies = new List<int>();
};