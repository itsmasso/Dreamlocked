using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//For more info on the algorithm read this: https://www.gorillasun.de/blog/bowyer-watson-algorithm-for-delaunay-triangulation/
public class DelaunayTriangulation
{
	private List<Vector2> points;
	private List<Triangle> triangles;
	private Vector2 bounds;  
	
	
	public DelaunayTriangulation(List<Vector2> points, Vector2 bounds)
	{
		this.points = points;
		this.bounds = bounds;
	
	}
	
	
	
	public void Triangulation()
	{
		//Step 1: Create supertriangle big enough to surround all points
		float minX = 0;
		float minY = 0;
		float maxX = bounds.x;
		float maxY = bounds.y;
		
		//Calculate dimensions for supertriangle
		
		//get width and height of bounding box
		float boundsWidth = maxX - minX;
		float boundsHeight = maxY - minY;
		
		//get largest dimension (either width or height) of bounding box to ensure super triangle is large enough no matter if bounding box is wide or tall
		float boundsMaxDimension = Mathf.Max(boundsWidth, boundsHeight);
		
		//get get points of bounding box (x, y)
		Vector2 boundsCenter = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
		
		//Define vertices of super triangle
		Vector2 vertexA = new Vector2(boundsCenter.x - 20 * boundsMaxDimension, boundsCenter.y - boundsMaxDimension);
		Vector2 vertexB = new Vector2(boundsCenter.x, boundsCenter.y + 20 * boundsMaxDimension);
		Vector2 vertexC = new Vector2(boundsCenter.x + 20 * boundsMaxDimension, boundsCenter.y - boundsMaxDimension);
		
		//initialize triangulation with supertriangle
		triangles = new List<Triangle>
		{
			new Triangle(vertexA, vertexB, vertexC)	
		};
		
		//Step 2: Add points one at a time and check for points inside circumcircle
		foreach(Vector2 point in points)
		{
			List<Triangle> badTriangles = new List<Triangle>();
			foreach(Triangle triangle in triangles)
			{
				if(triangle.IsPointInsideCircumCircle(point))
				{
					badTriangles.Add(triangle);
				}
			}
			//Find boundaries of polygonal hole (edges of polygon are not shared between rejected triangles)
			List<Edge> polygonalHole = new List<Edge>();
			foreach(Triangle triangle in badTriangles)
			{
				foreach(Edge edge in triangle.GetEdges())
				{
					//if edge in bad triangles do not share edges with each other, then that edge is a boundary for the polygon hole
					if(!edge.EdgeInMultipleTriangles(badTriangles))
					{
						polygonalHole.Add(edge);
					}
				}
			}
			//Remove the rejected triangles from the triangles list
			triangles.RemoveAll(triangle => badTriangles.Contains(triangle));
			
			//Re-triangulate the polygonal hole
			foreach(Edge edge in polygonalHole)
			{
				Triangle newTriangle = new Triangle(edge.vertexU, edge.vertexV, point);
				triangles.Add(newTriangle);
			}
		}
		
		//Step 3: Remove triangles that share vertices with supertriangle
		triangles.RemoveAll(triangle => triangle.ContainsVertex(vertexA) || triangle.ContainsVertex(vertexB) || triangle.ContainsVertex(vertexC));
		
		//Debug
		foreach (var triangle in triangles)
		{
			Debug.DrawLine(
				new Vector3(triangle.vertexA.x, 1, triangle.vertexA.y),
				new Vector3(triangle.vertexB.x, 1, triangle.vertexB.y),
				Color.green,
				2f
			);
			Debug.DrawLine(
				new Vector3(triangle.vertexB.x, 1, triangle.vertexB.y),
				new Vector3(triangle.vertexC.x, 1, triangle.vertexC.y),
				Color.green,
				2f
			);
			Debug.DrawLine(
				new Vector3(triangle.vertexC.x, 1, triangle.vertexC.y),
				new Vector3(triangle.vertexA.x, 1, triangle.vertexA.y),
				Color.green,
				2f
			);

		}
		
		

	}
	
	
	public List<Edge> GetEdges()
	{
		List<Edge> allEdges = new List<Edge>();
		foreach(Triangle triangle in triangles)
		{
			foreach(Edge edge in triangle.GetEdges())
			{
				if(!allEdges.Contains(edge))
					allEdges.Add(edge);
			}
		}
		return allEdges;
	}
	
	public class Triangle
	{
		public Vector2 vertexA, vertexB, vertexC; //vertices
		private Vector2 circumcenter;
		private float circumRadiusSqr;
		
		public Triangle(Vector2 vertexA, Vector2 vertexB, Vector2 vertexC)
		{
			this.vertexA = vertexA;
			this.vertexB = vertexB;
			this.vertexC = vertexC;	
			CalculateCircumCircle();
		}
		
		private void CalculateCircumCircle()
		{
			// Determinant for the circumcenter formula
			float D = 2 * (vertexA.x * (vertexB.y - vertexC.y) + vertexB.x * (vertexC.y - vertexA.y) + vertexC.x * (vertexA.y - vertexB.y));
			
			// Coordinates of the circumcenter
			float ux = ((vertexA.sqrMagnitude * (vertexB.y - vertexC.y)) + (vertexB.sqrMagnitude * (vertexC.y - vertexA.y)) + (vertexC.sqrMagnitude * (vertexA.y - vertexB.y))) / D;
			float uy = ((vertexA.sqrMagnitude * (vertexC.x - vertexB.x)) + (vertexB.sqrMagnitude * (vertexA.x - vertexC.x)) + (vertexC.sqrMagnitude * (vertexB.x - vertexA.x))) / D;
			
			// Store circumcenter and radius squared
			circumcenter = new Vector2(ux, uy);
			circumRadiusSqr = (circumcenter - vertexA).sqrMagnitude; // Radius squared for efficiency
		}
		
		public bool IsPointInsideCircumCircle(Vector2 point)
		{
			return (point - circumcenter).sqrMagnitude < circumRadiusSqr;
		}
		
		public List<Edge> GetEdges()
		{
			List<Edge> edgeList = new List<Edge>
			{
				new Edge(vertexA, vertexB),
				new Edge(vertexB, vertexC),
				new Edge(vertexC, vertexA)
			};
			return edgeList;
		}
		
		public bool ContainsVertex(Vector2 vertex)
		{
			return Vector2.Distance(vertex, vertexA) < 0.01f 
				|| Vector2.Distance(vertex, vertexB) < 0.01f 
				|| Vector2.Distance(vertex, vertexC) < 0.01f;
		}
		
		public bool HasEdge(Edge edge)
		{	
			return (Approximate(edge.vertexU, vertexA) && Approximate(edge.vertexV, vertexB)) ||
					(Approximate(edge.vertexU, vertexB) && Approximate(edge.vertexV, vertexC)) ||
					(Approximate(edge.vertexU, vertexC) && Approximate(edge.vertexV, vertexA)) ||
					(Approximate(edge.vertexU, vertexB) && Approximate(edge.vertexV, vertexA)) ||
					(Approximate(edge.vertexU, vertexC) && Approximate(edge.vertexV, vertexB)) ||
					(Approximate(edge.vertexU, vertexA) && Approximate(edge.vertexV, vertexC));

			
		}
		
		
		//approximate function for comparing two floats
		private bool Approximate(float x, float y)
		{
			//formula for comparing floating-point numbers with precision issues due to the limitations of floating-point arithmetic.
			return Mathf.Abs(x - y) <= float.Epsilon * Mathf.Abs(x+y) * 2 || Mathf.Abs(x - y) < float.MinValue;
		}
		
		//approximate function for comparing two vector2/vertices
		private bool Approximate(Vector2 left, Vector2 right)
		{
			return Approximate(left.x, right.x) && Approximate(left.y, right.y);
		}
		
		//overriding comparison operators so that edges with the same vertex but swapped are considered the same still
		public static bool operator ==(Triangle start, Triangle end) {
			return (start.vertexA == end.vertexA || start.vertexA == end.vertexB || start.vertexA == end.vertexC)
				&& (start.vertexB == end.vertexA || start.vertexB == end.vertexB || start.vertexB == end.vertexC)
				&& (start.vertexC == end.vertexA || start.vertexC == end.vertexB || start.vertexC == end.vertexC);
		}

		public static bool operator !=(Triangle start, Triangle end) {
			return !(start == end);
		}

		public override bool Equals(object obj) {
			if (obj is Triangle t) {
				return this == t;
			}

			return false;
		}

		public bool Equals(Triangle t) {
			return this == t;
		}

		public override int GetHashCode() {
			return vertexA.GetHashCode() ^ vertexB.GetHashCode() ^ vertexC.GetHashCode();
		}
		
		
	}
	
	
	
	public class Edge
	{
		public Vector2 vertexU, vertexV;
		public Edge(Vector2 vertexU, Vector2 vertexV)
		{
			this.vertexU = vertexU;
			this.vertexV = vertexV;
		}
		
		public bool EdgeInMultipleTriangles(List<Triangle> triangles)
		{
			//number of triangles that share the given edge
			int sharedTriangles = 0;
			foreach(Triangle triangle in triangles)
			{
				if(triangle.HasEdge(this))
					sharedTriangles++;
			}
			return sharedTriangles > 1; //if more than 1 shared triangle, there is edges shared by more than one triangle
		}
		
		//overriding comparison operators so that edges with the same vertex but swapped are considered the same still
		public static bool operator ==(Edge start, Edge end)
		{
			return (start.vertexU == end.vertexU || start.vertexU == end.vertexV)
				&& (start.vertexV == end.vertexU || start.vertexV == end.vertexV);
		}
		
		public static bool operator !=(Edge start, Edge end)
		{
			return !(start == end);
		}
		public override bool Equals(object obj)
		{
			if(obj is Edge e)
			{
				return this == e;
			}
			return false;
		}
		
		public bool Equals(Edge e)
		{
			return this == e;
		}

		public override int GetHashCode()
		{
			return vertexU.GetHashCode() ^ vertexV.GetHashCode();
		}
	}
	
		public void Clear()
		{
		    points.Clear();
		    triangles.Clear();
		}

}





