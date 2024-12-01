using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Prims_MST
{
	public class Edge
	{
		public Vector2 vertexU, vertexV;
		public float distance;
		public Edge(Vector2 vertexU, Vector2 vertexV)
		{
			this.vertexU = vertexU;
			this.vertexV = vertexV;
			distance = Vector2.Distance(vertexU, vertexV);
		}
		
		//overriding comparison operators so that edges with the same vertex but swapped are considered the same still
		public static bool operator ==(Edge start, Edge end)
		{
			return (start.vertexU == end.vertexU && start.vertexV == end.vertexV)
				|| (start.vertexU == end.vertexV && start.vertexV == end.vertexU);
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
	private List<Edge> potentialEdges;
	public Prims_MST(List<Edge> potentialEdges)
	{
		this.potentialEdges = potentialEdges;
	}
	
	private Edge FindMinEdge(HashSet<Vector2> mstVertices, List<Edge> potentialEdges)
	{
		//Find the edge with the smallest weight out of potential edges
		Edge minEdge = null;
		float minWeight = float.PositiveInfinity;

		foreach (Edge edge in potentialEdges)
		{
			if ((mstVertices.Contains(edge.vertexU) && !mstVertices.Contains(edge.vertexV)) ||
				(mstVertices.Contains(edge.vertexV) && !mstVertices.Contains(edge.vertexU)))
			{
				float weight = edge.distance;
				if (weight < minWeight)
				{
					minWeight = weight;
					minEdge = edge;
				}
			}
		}
		return minEdge;
	}
		
	public List<Edge> GenerateMST()
	{
		//vertices in the minimum spanning tree
		HashSet<Vector2> mstVertices = new HashSet<Vector2>();
		HashSet<Vector2> totalVertices = new HashSet<Vector2>();
		List<Edge> mstEdges = new List<Edge>();
		foreach(Edge edge in potentialEdges)
		{
			totalVertices.Add(edge.vertexU);
			totalVertices.Add(edge.vertexV);
		}
				
		//Select the first vertex to start the tree
		Vector2 startingPoint = potentialEdges[0].vertexU;
		mstVertices.Add(startingPoint);
			
		int iterations = 0;
		
		while(mstVertices.Count < totalVertices.Count && iterations < 1000)
		{
			iterations++;
			//Find edge with the smallest distance
			Edge minEdge = FindMinEdge(mstVertices, potentialEdges);
			
			//add smallest edge to mst
			mstEdges.Add(minEdge);
			
			//add edge's vertices to list of visited vertices
			mstVertices.Add(minEdge.vertexU);
			mstVertices.Add(minEdge.vertexV);

			//remove minimum edge from the potential edges list
			potentialEdges.Remove(minEdge);
			
			if(iterations == 1000)
			{
				Debug.LogWarning("Max iterations reached! MST may not be complete.");
			}
		}
		
		
		foreach (Edge edge in mstEdges)
		{
			Debug.DrawLine(
				new Vector3(edge.vertexU.x, 1, edge.vertexU.y),
				new Vector3(edge.vertexV.x, 1, edge.vertexV.y),
				Color.blue,
				10f
			);
		}
		return mstEdges;
		
		
	}
}
