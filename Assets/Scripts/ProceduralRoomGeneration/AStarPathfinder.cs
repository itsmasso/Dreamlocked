using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class AStarPathfinder
{
	private Grid3D grid;
	private Heap<Node> openSet;
	private HashSet<Node> closedSet;
	
	
	public AStarPathfinder(Grid3D grid)
	{
		this.grid = grid;
		openSet = new Heap<Node>(grid.MaxSize); //set of nodes to be evaluated
		closedSet = new HashSet<Node>();
	}
	
	public List<Node> FindPath(Vector3 startPos, Vector3 targetPos, bool ignoreYPositions)
	{
		Node startNode = grid.GetNode(startPos);
		Node targetNode = grid.GetNode(targetPos);
		
		//Heap<Node> openSet = new Heap<Node>(grid.MaxSize); 
		openSet.Clear();
		closedSet.Clear();
		//HashSet<Node> closedSet = new HashSet<Node>(); //set of nodes already evaluated
		
		openSet.Add(startNode);
	
		while(openSet.Count > 0){
			Node currentNode = openSet.RemoveFirst();
			
			closedSet.Add(currentNode);
			if(currentNode == targetNode)
			{		
				return RetracePath(startNode, targetNode);
			}
			
			foreach(Node neighbour in grid.GetAllNeighbours(currentNode)){
				
				if(neighbour.cellType == CellType.Room || closedSet.Contains(neighbour))
				{
					continue;
				}
				
				if (neighbour.gridY != currentNode.gridY && ignoreYPositions)
	   				continue;

				
				int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
				
				if(ignoreYPositions)
				{
					if(neighbour.cellType == CellType.None)
					{
					newMovementCostToNeighbour += 5;
					}
					else if(neighbour.cellType == CellType.Hallway)
					{
					newMovementCostToNeighbour += 1;
					}
				}
				
				if(newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
				{
					neighbour.gCost = newMovementCostToNeighbour;
					neighbour.hCost = GetDistance(neighbour, targetNode);
					neighbour.previousNode = currentNode;
					
					if(!openSet.Contains(neighbour))
					{

						openSet.Add(neighbour);
			
					}
				}
				
			}
		}
		return null;
	}
	
	private List<Node> RetracePath(Node startNode, Node endNode)
	{
		List<Node> path = new List<Node>();
		Node currentNode = endNode;
		while(currentNode != startNode)
		{
			
			path.Add(currentNode);
			currentNode = currentNode.previousNode;
		}
		path.Reverse();
		return path;
	}
	
	private int GetDistance(Node nodeA, Node nodeB)
	{
		int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
		int distZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);
		
		int max = Mathf.Max(distX, Mathf.Max(distY, distZ));
		int min = Mathf.Min(distX, Mathf.Min(distY, distZ));
		int mid = distX + distY + distZ - max - min;

		return 14 * min + 10 * mid + 10 * (max - min - mid);
		
	
	}

}
