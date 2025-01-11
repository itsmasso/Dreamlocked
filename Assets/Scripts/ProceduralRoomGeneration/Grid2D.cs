using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Grid2D
{
	
	public Vector2 gridWorldSize;
	private float nodeRadius;
	public Node[,] grid;
	public float nodeDiameter;
	private int gridSizeX, gridSizeY;
	private Vector3 gridCenterPos;
	
	public Grid2D(Vector2 gridWorldSize, float nodeRadius, Vector3 gridCenterPos)
	{
		this.nodeRadius = nodeRadius;
		nodeDiameter = nodeRadius*2;
		this.gridWorldSize = gridWorldSize;
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x /nodeDiameter);
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
		this.gridCenterPos = gridCenterPos;
		
	}
	
	public void CreateGrid()
	{
		grid = new Node[gridSizeX, gridSizeY];
		Vector3 worldBottomLeft = gridCenterPos - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;
		for(int x = 0; x < gridSizeX; x++)
		{
			for(int y = 0; y < gridSizeY; y++)
			{
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
				CellType cellType = CellType.None;
				grid[x,y] = new Node(cellType, worldPoint, x, y);
				//Debug.Log(worldPoint);
			}
		}
	}
	
	public List<Node> GetNeighbours(Node node){
		List<Node> neighbours = new List<Node>();
		for(int x = -1; x <= 1; x++){
			for(int y = -1; y <= 1; y++)
			{
				if(x == 0 && y == 0){
					continue;
				}
				int checkX = node.gridX + x;
				int checkY = node.gridY + y;
				
				if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY){
					neighbours.Add(grid[checkX, checkY]);
				}
			}
		}
		
		return neighbours;
	}
	
	public Node GetNode(Vector3 worldPosition)
	{
		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);
		
		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
		return grid[x,y];
	}
	
	public void SetNodeType(Vector3 worldPosition, CellType cellType)
	{
		GetNode(worldPosition).cellType = cellType;
	}
}
