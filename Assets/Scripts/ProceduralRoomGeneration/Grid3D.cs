using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Grid3D
{
	
	public Vector3 gridWorldSize;
	private float nodeRadius;
	public Node[,,] grid;
	public float nodeDiameter;
	private int gridSizeX, gridSizeY, gridSizeZ;
	private Vector3 gridCenterPos;
	
	public Grid3D(Vector3 gridWorldSize, float nodeRadius, Vector3 gridCenterPos)
	{
		this.nodeRadius = nodeRadius;
		nodeDiameter = nodeRadius*2;
		this.gridWorldSize = gridWorldSize;
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x /nodeDiameter);
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
		if(gridSizeY <= 0)
			gridSizeY = 1;
		gridSizeZ = Mathf.RoundToInt(gridWorldSize.z/nodeDiameter);
		this.gridCenterPos = gridCenterPos;
		
	}
	
	public int MaxSize
	{
		get
		{
			return gridSizeX * gridSizeY * gridSizeZ;
		}
	}
	
	public void CreateGrid()
	{
		
		grid = new Node[gridSizeX, gridSizeY, gridSizeZ];
		Vector3 worldBottomLeft = gridCenterPos - Vector3.right * gridWorldSize.x/2 - Vector3.up * gridWorldSize.y/2 - Vector3.forward * gridWorldSize.z/2;
		
		for(int x = 0; x < gridSizeX; x++)
		{
			for(int y = 0; y < gridSizeY; y++)
			{
				for(int z = 0; z < gridSizeZ; z++)
				{
					Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);

					CellType cellType = CellType.None;
					grid[x,y,z] = new Node(cellType, worldPoint, x, y, z);
					
				}
				
			}
		}
	}
	
	public List<Node> GetAllNeighbours(Node node)
	{
		List<Node> neighbours = new List<Node>();

		
		int[,] directions = new int[,]
		{
			{ 1, 0, 0 },  //Right
			{ -1, 0, 0 }, //Left
			{ 0, 1, 0 },  //Up
			{ 0, -1, 0 }, //Down
			{ 0, 0, 1 },  //Forward
			{ 0, 0, -1 }  //Backward
		};

		for (int i = 0; i < directions.GetLength(0); i++)
		{
			int checkX = node.gridX + directions[i, 0];
			int checkY = node.gridY + directions[i, 1];
			int checkZ = node.gridZ + directions[i, 2];

			//Check bounds
			if (checkX >= 0 && checkX < gridSizeX &&
				checkY >= 0 && checkY < gridSizeY &&
				checkZ >= 0 && checkZ < gridSizeZ)
			{
				neighbours.Add(grid[checkX, checkY, checkZ]);
			}
		}

		return neighbours;
	}

	
	public Node GetNode(Vector3 worldPosition)
	{
		float percentX = (-gridCenterPos.x + worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (-gridCenterPos.y + worldPosition.y + gridWorldSize.y/2) / gridWorldSize.y;
		float percentZ = (-gridCenterPos.z + worldPosition.z + gridWorldSize.z/2) / gridWorldSize.z;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);
		percentZ = Mathf.Clamp01(percentZ);
		
		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
		int z = Mathf.RoundToInt((gridSizeZ-1) * percentZ);
		return grid[x,y,z];
	}
	
	public void SetNodeType(Vector3 worldPosition, CellType cellType)
	{
		GetNode(worldPosition).cellType = cellType;
	}
}
