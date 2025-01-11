using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
	private Grid2D grid;
	public AStarPathfinder(Grid2D grid)
	{
		this.grid = grid;
	}
	
	public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
	{

		List<Node> path = new List<Node>();
		Node startNode = grid.GetNode(startPos);
		Node targetNode = grid.GetNode(targetPos);
		
		List<Node> openSet = new List<Node>(); //set of nodes to be evaluated
		HashSet<Node> closedSet = new HashSet<Node>(); //set of nodes already evaluated
		
		openSet.Add(startNode);
	
		while(openSet.Count > 0){
			Node currentNode = openSet[0];
			for(int i = 1; i < openSet.Count; i++)
			{
				if(openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
				{
					currentNode = openSet[i];
				}
			}
			
			openSet.Remove(currentNode);
			closedSet.Add(currentNode);
			if(currentNode == targetNode)
			{
				path = RetracePath(startNode, targetNode);
				
				return path;
			}
			
			foreach(Node neighbour in grid.GetNeighbours(currentNode)){
				if(closedSet.Contains(neighbour))
				{
					continue;
				}
				
				int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
				
				if(neighbour.cellType == CellType.Room)
				{
					newMovementCostToNeighbour += 10;
				}
				else if(neighbour.cellType == CellType.None)
				{
					newMovementCostToNeighbour += 5;
				}
				else if(neighbour.cellType == CellType.Hallway)
				{
					newMovementCostToNeighbour += 1;
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
		return path;
	}
	
	private List<Node> RetracePath(Node startNode, Node endNode)
	{
		List<Node> path = new List<Node>();
		Node currentNode = endNode;
		while(currentNode != startNode)
		{
			
			path.Add(currentNode);
			currentNode = currentNode.previousNode;
			currentNode.cellType = CellType.Hallway;
		}
		path.Reverse();
		return path;
	}
	
	private int GetDistance(Node nodeA, Node nodeB)
	{
		int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
		
		if(distX > distY)
		{
			return 14*distY + 10*(distX-distY);
		}
		return 14*distX + 10*(distY-distX);
	
	}

}
