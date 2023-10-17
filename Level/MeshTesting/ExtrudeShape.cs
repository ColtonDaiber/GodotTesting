using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Transactions;

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

	public void Extrude2DShape(Shape shape, float extrudeDist)
	{
		Extrude2DShape(shape.points, shape.indicies, extrudeDist);
	}
	public void Extrude2DShape(List<Vector2> points, List<int> indicies, float extrudeDist)
	{
		Vector2[] pointsArr = new Vector2[points.Count];
		for(int i = 0; i < points.Count; i++) pointsArr[i] = points[i];

		int[] indiciesArr = new int[indicies.Count];
		for(int i = 0; i < indicies.Count; i++) indiciesArr[i] = indicies[i];

		Extrude2DShape(pointsArr, indiciesArr, extrudeDist);
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
		// Extrude2DShape(pointsInShape, indiciesInShape, .5f);

		// int[] ind = {indiciesInShape[0], indiciesInShape[1], indiciesInShape[2]};
		// CutTriangle(pointsInShape, ind, new Vector2(2,1), new Vector2(0, 1));

		// Shape newShape = CreateShapeFromPoints(new List<Vector2>(pointsInShape));
		// Extrude2DShape(newShape.points, newShape.indicies, .25f);

		Shape[] shapes = CutShape(pointsInShape, indiciesInShape, new Vector2(2,1), new Vector2(0, 1));
		for(int i = 0; i < shapes[0].points.Count; i++)
		{
			GD.Print(shapes[0].points[i]);
		}
		for(int i = 0; i < shapes[0].indicies.Count; i++)
		{
			GD.Print(shapes[0].indicies[i]);
		}
		Extrude2DShape(shapes[0], 0.5f);
	}

	void RemoveChildren()
	{
		var children = this.GetChildren();
		for(int i = children.Count - 1; i >= 0; i--)
		{
			if(children[i] != physicsBody) children[i].QueueFree();
		}
	}

	Shape[] CutShape(Vector2[] verticies, int[] indicies, Vector2 pt1, Vector2 pt2)
	{
		Shape[] newShapes = {new Shape(), new Shape()};

		if(indicies.Length % 3 != 0)
		{
			GD.PushError("Indicies length needs to be a multiple of three!");
			return newShapes;
		}

		for(int i = 0; i < indicies.Length; i += 3)
		{
			int[] triIndicies = {i, i+1, i+2};

			Shape[] cutTriangles = CutTriangle(verticies, triIndicies, pt1, pt2);

			List<int> cutIndex0 = new List<int>();
			List<int> shapeIndex0 = new List<int>();
			for(int w = 0; w < cutTriangles[0].points.Count; w++)
			{

			}
		}

		return newShapes;
	}

	void AddShapeToShape(Shape originalShape, Shape addShape)
	{
		for(int ind = 0; ind < addShape.indicies.Count; ind++)
		{
			
		}
	}

	Shape[] CutTriangle(Vector2[] verticies, int[] indicies, Vector2 pt1, Vector2 pt2)
	{
		Shape[] returnShapes = {new Shape(), new Shape()};
		if(indicies.Length != 3)
		{
			GD.PushError("expected 3 indicies!");
			return returnShapes;
		}

		/* cramer's rule
			ax + by = c
			dx + ey = f
			Dx = c*e-b*f
			Dy = a*f-c*d
			D = a*e-d*b
		*/
		float lineCutSlope = (pt2.Y-pt1.Y)/(pt2.X-pt1.X);
		float lineCutYIntercept = (-pt1.X*lineCutSlope) + pt1.Y;

		List<Vector2> pointsInShape1 = new List<Vector2>();
		List<Vector2> pointsInShape2 = new List<Vector2>();
		bool sameLine = false;
		for(int i = 0; i < 3; i++)
		{
			int index2 = (i != 2 ? i+1 : 0);

			float slope = (verticies[indicies[index2]].Y-verticies[indicies[i]].Y)/(verticies[indicies[index2]].X-verticies[indicies[i]].X);
			float yIntercept = (-verticies[indicies[i]].X*slope) + verticies[indicies[i]].Y;
			float x = 0;
			float y = 0;

			int shapeNum = GetShapePointIsIn(lineCutSlope, pt1, verticies[indicies[i]]);
			if(shapeNum == 1) pointsInShape1.Add(verticies[indicies[i]]);
			else if(shapeNum == -1) pointsInShape2.Add(verticies[indicies[i]]);
			else if(shapeNum == 0)
			{
				int index3 = ((2+1 - i) - index2);
				int index2ShapeNum = GetShapePointIsIn(lineCutSlope, pt1, verticies[indicies[index2]]);
				int index3ShapeNum = GetShapePointIsIn(lineCutSlope, pt1, verticies[indicies[index3]]);
				if( index2ShapeNum != index3ShapeNum && index2ShapeNum != 0 && index3ShapeNum != 0)
				{
					continue;
				}
			}

			bool intersect = true;
			if(float.IsInfinity(lineCutSlope) && !float.IsInfinity(slope))
			{
				// GD.Print("lineCutSlope is undef");
				x = pt1.X;
				y = (slope*pt1.X) + yIntercept;
			}
			else if(float.IsInfinity(slope) && !float.IsInfinity(lineCutSlope))
			{
				// GD.Print("slope is undef");
				x = verticies[indicies[i]].X;
				y = (lineCutSlope * verticies[indicies[i]].X) + lineCutYIntercept;
			}
			else if(!float.IsInfinity(slope) && !float.IsInfinity(lineCutSlope))
			{
				if(slope == lineCutSlope && yIntercept == lineCutYIntercept) {sameLine = true; GD.PrintErr("shouldnt be here");} //should never get here
				// GD.Print("Cramers Rule");
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
				if(verticies[indicies[i]].X == pt1.X) {sameLine = true; GD.PrintErr("shouldnt be here");} //should never get here
				intersect = false;
			}

			if(intersect) intersect = CheckIntersectionInRange(verticies[indicies[i]], verticies[indicies[index2]], new Vector2(x, y));
			else continue;

			if(intersect)
			{
				pointsInShape1.Add(new Vector2(x,y));
				pointsInShape2.Add(new Vector2(x,y));
			}
		}

		// GD.Print(pointsInShape1.Count);
		// GD.Print(pointsInShape2.Count);
		// for(int i = 0; i < pointsInShape1.Count; i++) GD.Print(pointsInShape1[i]);

		Shape triangles1 = new Shape();
		Shape triangles2 = new Shape();
		if(pointsInShape1.Count > 0) triangles1 = CreateShapeFromPoints(pointsInShape1);
		if(pointsInShape2.Count > 0) triangles2 = CreateShapeFromPoints(pointsInShape2);

		returnShapes[0] = triangles1;
		returnShapes[1] = triangles2;

		// for(int i = 0; i < triangles2.points.Count; i++) GD.Print(triangles2.points[i]);
		// for(int i = 0; i < triangles2.indicies.Count; i++) GD.Print(triangles2.indicies[i]);
		// for(int i = 0; i < triangles1.points.Count; i++) GD.Print(triangles1.points[i]);
		// for(int i = 0; i < triangles1.indicies.Count; i++) GD.Print(triangles1.indicies[i]);
		// Extrude2DShape(triangles2, .5f);
		return returnShapes;

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

	/*	expects 3 or 4 points
		any 3 points can not for a triangle that sorrounds the other point
		points must not be colinear */
	Shape CreateShapeFromPoints(List<Vector2> points)
	{
		Shape newShape = new Shape(points, new List<int>());
		//TODO check for colinearity??
		if(points.Count != 3 && points.Count != 4)
		{
			GD.PushError("Points are not valid to make a new shape");
			return newShape;
		}

		int[] pointOrder = TriangleGetFirstSecondPoint(points);
		newShape.indicies.Add(pointOrder[0]);
		newShape.indicies.Add(pointOrder[1]);
		newShape.indicies.Add(pointOrder[2]);
		
		if(points.Count == 4)
		{
			newShape.indicies.Add(pointOrder[2]);
			newShape.indicies.Add(pointOrder[1]);
			newShape.indicies.Add(pointOrder[3]);
		}
		return newShape;
	}

	int[] TriangleGetFirstSecondPoint(List<Vector2> points)
	{
		int[] pointOrder = {-1, -1, -1, -1};

		if(points.Count != 3 && points.Count != 4)
		{
			GD.PushError("Points are not valid to make a new shape!");
			return pointOrder;
		}

		int firstPoint = -1; //lowest -if tie lowest leftmost
		float lowest = points[0].Y;
		for(int i = 1; i < points.Count; i++)
		{
			if(points[i].Y < lowest) lowest = points[i].Y;
		}
		//check for ties
		int lowCnt = 0;
		for(int i = 0; i < points.Count; i++)
		{
			if(points[i].Y == lowest)
			{
				lowCnt++;
				firstPoint = i;
			}
		}
		if(lowCnt > 1) //if tie
		{
			float leftMostLowest = points[firstPoint].X;
			for(int i = 0; i < points.Count; i++)
			{
				if(points[i].Y == lowest && points[i].X < leftMostLowest) //if this is never true, the the leftMostLowest is already firstPoint, which is what we want
				{
					firstPoint = i;
					break; //there will only ever be a tie between two points, or the points are in a line and do not for a triangle
				}
			}
		}

		int secondPoint = -1; //leftmost -if tie leftmost highest
		float leftMost = points[firstPoint == 0 ? 1 : 0].X; //this need to not be the inital point, bc this will become the next point
		for(int i = 1; i < points.Count; i++)
		{
			if(points[i].X < leftMost && i != firstPoint) leftMost = points[i].X;
		}
		//check for ties
		int leftCnt = 0;
		for(int i = 0; i < points.Count; i++)
		{
			if(points[i].X == leftMost && i != firstPoint)
			{
				leftCnt++;
				secondPoint = i;
			}
		}
		if(leftCnt > 1) //if tie
		{
			float highestLeftMost = points[secondPoint].Y;
			for(int i = 0; i < points.Count; i++)
			{
				if(points[i].X == leftMost && points[i].Y > highestLeftMost && i != firstPoint)
				{
					secondPoint = i;
					break;
				}
			}
		}

		pointOrder[0] = firstPoint;
		pointOrder[1] = secondPoint;
		for(int i = 0; i < points.Count; i++) if(i != firstPoint && i != secondPoint) pointOrder[2] = i;
		if(points.Count == 4)
		{
			for(int i = 0; i < points.Count; i++) if(i != firstPoint && i != secondPoint && i != pointOrder[2]) pointOrder[3] = i;
		}
		return pointOrder;
	}

	public override void _Process(double delta)
	{
	}
}

public class Shape
{
	public List<Vector2> points;
	public List<int> indicies;
	public Shape(List<Vector2> newVert, List<int> newInd)
	{
		points = newVert;
		indicies = newInd;
	}
	public Shape()
	{
		points = new List<Vector2>();
		indicies = new List<int>();
	}

	//return index if point is in shape, otherwise returns -1
	public int PointInShape(Vector2 point)
	{
		for(int i = 0; i < this.points.Count; i++)
		{
			if(this.points[i].X == point.X && this.points[i].Y == point.Y)
			{
				return i;
			}
		}
		return -1;
	}
};