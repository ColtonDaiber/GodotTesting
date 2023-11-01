using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Transactions;

// [Tool]
public partial class MeshGen : Node3D
{
	[Export] RigidBody3D physBody;

	public void Create3DShape(RigidBody3D body, Shape3D shape, Material mat = null)
	{
		Create3DShape(body, shape.verticies, shape.indicies, mat);
	}
	public void Create3DShape(RigidBody3D body, List<Vector3> vertexs, List<int> indicies, Material mat = null)
	{
		Vector3[] vertArray = new Vector3[vertexs.Count];
		for(int i = 0; i < vertexs.Count; i++) vertArray[i] = vertexs[i];

		int[] indArray = new int[indicies.Count];
		for(int i = 0; i < indicies.Count; i++) indArray[i] = indicies[i];

		Create3DShape(body, vertArray, indArray, mat);
	}
	public void Create3DShape(RigidBody3D body, Vector3[] vertexs, int[] indicies, Material mat = null)
	{
		MeshInstance3D meshInstance = null;
		CollisionShape3D collisionShape = null;
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertexs;
		arrays[(int)Mesh.ArrayType.Index] = indicies;
		arrays[(int)Mesh.ArrayType.TexUV] = vertexs;

		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		for(int i = 0; i < body.GetChildCount(); i++)
		{
			collisionShape = body.GetChildOrNull<CollisionShape3D>(i);
			if(collisionShape != null) break;
		}
		for(int i = 0; i < body.GetChildCount(); i++)
		{
			meshInstance = body.GetChildOrNull<MeshInstance3D>(i);
			if(meshInstance != null) break;
		}
		if(meshInstance == null)
		{
			meshInstance = new MeshInstance3D();
			body.AddChild(meshInstance);
		}
		if(collisionShape == null)
		{
			collisionShape = new CollisionShape3D();
			body.AddChild(collisionShape);
		}
		
		meshInstance.Mesh = mesh;
		if(mat != null) meshInstance.MaterialOverride = mat;
		collisionShape.Shape = mesh.CreateConvexShape();

		body.CenterOfMassMode = RigidBody3D.CenterOfMassModeEnum.Custom;
		Vector3 centerOfMass = new Vector3(0,0,0);
		for(int i = 0; i < vertexs.Length; i++) centerOfMass += vertexs[i];
		centerOfMass = centerOfMass/vertexs.Length;
		body.CenterOfMass = centerOfMass;
	}

	public Shape3D Extrude2DShape(Shape shape, float extrudeDist)
	{
		return Extrude2DShape(shape.points, shape.indicies, extrudeDist);
	}
	public Shape3D Extrude2DShape(List<Vector2> points, List<int> indicies, float extrudeDist)
	{
		Vector2[] pointsArr = new Vector2[points.Count];
		for(int i = 0; i < points.Count; i++) pointsArr[i] = points[i];

		int[] indiciesArr = new int[indicies.Count];
		for(int i = 0; i < indicies.Count; i++) indiciesArr[i] = indicies[i];

		return Extrude2DShape(pointsArr, indiciesArr, extrudeDist);
	}
	public Shape3D Extrude2DShape(Vector2[] points, int[] indicies, float extrudeDist)
	{
		if(indicies.Length % 3 != 0) //this stores list of triangles, so if its not a multiple of 3, theres an error
		{
			GD.PushError("invalid indicies array");
			return new Shape3D();
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

		return new Shape3D(new List<Vector3>(verticies), new List<int>(indicies3D));
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

	void RemoveChildren()
	{
		var children = this.GetChildren();
		for(int i = children.Count - 1; i >= 0; i--)
		{
			children[i].QueueFree();
		}
	}

	public List<Shape> CutShapes(List<Shape> shapes, Vector2 pt1, Vector2 pt2)
	{
		List<Shape> newShapeList = new List<Shape>();

		for(int i = 0; i < shapes.Count; i++)
		{
			Shape[] tempShapes = CutShape(shapes[i], pt1, pt2);
			if(tempShapes[0].points.Count > 0) newShapeList.Add(tempShapes[0]);
			if(tempShapes[1].points.Count > 0) newShapeList.Add(tempShapes[1]);
		}

		return newShapeList;
	}

	public Shape[] CutShape(Shape shape, Vector2 pt1, Vector2 pt2)
	{
		return CutShape(shape.points, shape.indicies, pt1, pt2);
	}
	public Shape[] CutShape(List<Vector2> verticies, List<int> indicies, Vector2 pt1, Vector2 pt2)
	{
		Vector2[] verticesArray = new Vector2[verticies.Count];
		int[] indiciesArray = new int[indicies.Count];
		for(int i = 0; i < verticies.Count; i++) verticesArray[i] = verticies[i];
		for(int i = 0; i < indicies.Count; i++) indiciesArray[i] = indicies[i];
		return CutShape(verticesArray, indiciesArray, pt1, pt2);
	}
	public Shape[] CutShape(Vector2[] verticies, int[] indicies, Vector2 pt1, Vector2 pt2)
	{
		Shape[] newShapes = {new Shape(), new Shape()};

		if(indicies.Length % 3 != 0)
		{
			GD.PushError("Indicies length needs to be a multiple of three!");
			return newShapes;
		}

		for(int i = 0; i < indicies.Length; i += 3)
		{
			int[] triIndicies = {indicies[i], indicies[i+1], indicies[i+2]};

			Shape[] cutTriangles = CutTriangle(verticies, triIndicies, pt1, pt2);

			if(cutTriangles[0].indicies.Count > 0) AddTrianglesToShape(newShapes[0], cutTriangles[0]);
			if(cutTriangles[1].indicies.Count > 0) AddTrianglesToShape(newShapes[1], cutTriangles[1]);
		}

		return newShapes;
	}
	void AddTrianglesToShape(Shape shape, Shape triangles)
	{
		List<int> cutToNewShapeIndicies = new List<int>();
		for(int w = 0; w < triangles.points.Count; w++)
		{
			int pointInShape = shape.PointInShape(triangles.points[w]);
			if(pointInShape == -1)
			{
				cutToNewShapeIndicies.Add(shape.points.Count);
				shape.points.Add(triangles.points[w]);
			}
			else
			{
				cutToNewShapeIndicies.Add(pointInShape);
			}
		}
		for(int w = 0; w < triangles.indicies.Count; w++)
		{
			shape.indicies.Add(cutToNewShapeIndicies[triangles.indicies[w]]);
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

			int shapeNum = GetSideOfLinePointIsOn(lineCutSlope, pt1, verticies[indicies[i]]);
			if(shapeNum == 1) pointsInShape1.Add(verticies[indicies[i]]);
			else if(shapeNum == -1) pointsInShape2.Add(verticies[indicies[i]]);
			else if(shapeNum == 0)
			{
				int index3 = ((2+1 - i) - index2);
				int index2ShapeNum = GetSideOfLinePointIsOn(lineCutSlope, pt1, verticies[indicies[index2]]);
				int index3ShapeNum = GetSideOfLinePointIsOn(lineCutSlope, pt1, verticies[indicies[index3]]);
				bool cutDoesNotIntersectTri = false;
				if(index2ShapeNum != 0 && index2ShapeNum == index3ShapeNum) cutDoesNotIntersectTri = true;
				else if(index2ShapeNum != index3ShapeNum && (index2ShapeNum == 0 || index3ShapeNum == 0)) cutDoesNotIntersectTri = true;
				else if(index2ShapeNum != 0 && index3ShapeNum != 0 && index2ShapeNum != index3ShapeNum) //cut does intersect tri, so add vertex to both shapes
				{
					// if(index2ShapeNum == 1) pointsInShape1.Add(verticies[indicies[i]]);
					// else if(index2ShapeNum == -1) pointsInShape2.Add(verticies[indicies[i]]);
					pointsInShape1.Add(verticies[indicies[i]]);
					pointsInShape2.Add(verticies[indicies[i]]);
					continue;
				}
				else GD.PushError("Should not be here!!!");

				if(cutDoesNotIntersectTri)
				{
					pointsInShape1.Clear();
					pointsInShape2.Clear();
					int addToShape = shapeNum + index2ShapeNum + index3ShapeNum;
					if(index2ShapeNum == index3ShapeNum) addToShape = shapeNum + index2ShapeNum;
					if(addToShape != -1 && addToShape != 1) GD.PushError("Should not have happened!");
					for(int w = 0; w < 3; w++)
					{
						if(addToShape == 1) pointsInShape1.Add(verticies[indicies[w]]);
						else if(addToShape == -1) pointsInShape2.Add(verticies[indicies[w]]);
					}
					break;
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
		Shape triangles1 = new Shape();
		Shape triangles2 = new Shape();
		if(false) {
			GD.Print(pointsInShape1.Count);
			GD.Print(pointsInShape2.Count);
			for(int i = 0; i < pointsInShape1.Count; i++) GD.Print(pointsInShape1[i]);
			GD.Print("");
			for(int i = 0; i < pointsInShape2.Count; i++) GD.Print(pointsInShape2[i]);
		}
		// if(pointsInShape1.Count > 0 && pointsInShape1.Count != 3 && pointsInShape1.Count != 4)
		// {
		// 	GD.Print("bad");
		// }
		
		pointsInShape1 = ConsolidateSimilarPoints(pointsInShape1);
		pointsInShape2 = ConsolidateSimilarPoints(pointsInShape2);

		if(pointsInShape1.Count == 3 || pointsInShape1.Count == 4) triangles1 = CreateShapeFromPoints(pointsInShape1);
		if(pointsInShape2.Count == 3 || pointsInShape2.Count == 4) triangles2 = CreateShapeFromPoints(pointsInShape2);
		
		returnShapes[0] = triangles1;
		returnShapes[1] = triangles2;
		return returnShapes;

		/*
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
		*/
	}

	List<Vector2> ConsolidateSimilarPoints(List<Vector2> points)
	{
		List<Vector2> consolidatedPoints = new List<Vector2>();
		for(int i = 0; i < points.Count; i++)
		{
			bool unique = true;
			for(int w = 0; w < consolidatedPoints.Count; w++)
			{
				if(Mathf.Abs(consolidatedPoints[w].X - points[i].X) < 0.0001f && Mathf.Abs(consolidatedPoints[w].Y - points[i].Y) < 0.0001f)
				{
					unique = false;
					break;
				}
			}
			if(unique) consolidatedPoints.Add(points[i]);
		}
		return consolidatedPoints;
	}

	int GetSideOfLinePointIsOn(Vector2 pt1, Vector2 pt2, Vector2 testPoint)
	{
		float slope = (pt1.Y-pt2.Y)/(pt1.X-pt2.X);
		return GetSideOfLinePointIsOn(slope, pt1, testPoint);
	}
	int GetSideOfLinePointIsOn(float slope, Vector2 linePt, Vector2 vertex)
	{
		if(!float.IsInfinity(slope))
		{
			//reconstruct y=mx+b from slope and point -> y=mx - m(x0) + y(0) 	then plug in vertex x, and see if vertex y is greater
			if(vertex.Y - ((slope*vertex.X) - (slope*linePt.X) + linePt.Y) > 0.001 ) return 1; //effectivly if(vertex.Y > ((slope*vertex.X) - (slope*linePt.X) + linePt.Y))
			else if(((slope*vertex.X) - (slope*linePt.X) + linePt.Y) - vertex.Y > 0.001) return -1; //effectivly if(vertex.Y < ((slope*vertex.X) - (slope*linePt.X) + linePt.Y))
			else return 0; //if they are about equal
		}
		else //line is vertical, ex) x = 0
		{
			if(vertex.X - linePt.X > 0.001) return 1; //effectivly if(vertex.X > linePt.X)
			else if(linePt.X - vertex.X > 0.001 ) return -1; //effectivly if(vertex.X < linePt.X)
			else return 0; //if they are about equal
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
	public Shape CreateShapeFromPoints(List<Vector2> points)
	{
		Shape newShape = new Shape(points, new List<int>());
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
			for(int index = 0; index < 3; index++)
			{	//check all lines in first tri angle, which are points at indexes 0,1,2, and see what side the unused point is at, and 
				const int lastPointIndex = 3;
				int index2 = index != 2 ? index+1 : 0;
				int index3 = ((2+1 - index) - index2);

				int sideNumInd3 = GetSideOfLinePointIsOn(points[pointOrder[index]], points[pointOrder[index2]], points[pointOrder[index3]]);
				int sideNumInd4 = GetSideOfLinePointIsOn(points[pointOrder[index]], points[pointOrder[index2]], points[pointOrder[lastPointIndex]]);

				if(sideNumInd3 != sideNumInd4 && sideNumInd3 != 0 && sideNumInd4 != 0)
				{
					newShape.indicies.Add(pointOrder[index]);
					newShape.indicies.Add(pointOrder[lastPointIndex]);
					newShape.indicies.Add(pointOrder[index2]);
					break;
				}
			}
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

		//find first point
		int firstIndex = -1;
		float leftMost = points[0].X;
		for(int i = 0; i < points.Count; i++)
		{
			if(points[i].X < leftMost)
			{
				leftMost = points[i].X;
				firstIndex = i;
			}
		}
		bool tie = false;
		for(int i = 0; i < points.Count; i++)
		{
			if(i == firstIndex) continue;
			if(points[i].X == leftMost)
			{
				tie = true;
				break;
			}
		}
		if(tie)
		{
			float lowest = float.PositiveInfinity;
			for(int i = 0; i < points.Count; i++) if(points[i].X == leftMost && points[i].Y < lowest) firstIndex = i;
		}

		//fillout point order with first point
		pointOrder[0] = firstIndex;
		int lastIndex = 0;
		for(int i = 1; i < 4; i++)
		{
			if(lastIndex == firstIndex) lastIndex ++;
			pointOrder[i] = lastIndex;
			lastIndex++;
		}

		int sideNum = GetSideOfLinePointIsOn(points[pointOrder[0]], points[pointOrder[2]], points[pointOrder[1]]);
		
		if(sideNum == -1)
		{
			int temp = pointOrder[1];
			pointOrder[1] = pointOrder[2];
			pointOrder[2] = temp;
		}

		// for(int i = 0; i < points.Count; i++) if(i != pointOrder[0] && i != pointOrder[1] && i != pointOrder[2]) pointOrder[3] = i;
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
	public Shape(Vector2[] newVert, int[] newInd)
	{
		points = new List<Vector2>(newVert);
		indicies = new List<int>(newInd);
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

public class Shape3D
{
	public List<Vector3> verticies;
	public List<int> indicies;
	public Shape3D(List<Vector3> newVert, List<int> newInd)
	{
		verticies = newVert;
		indicies = newInd;
	}
	public Shape3D(Vector3[] newVert, int[] newInd)
	{
		verticies = new List<Vector3>(newVert);
		indicies = new List<int>(newInd);
	}
	public Shape3D()
	{
		verticies = new List<Vector3>();
		indicies = new List<int>();
	}
};

public class ShapeObject
{
	public List<Shape3D>
};