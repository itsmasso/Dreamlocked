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
	
	public List<Node> GetNeighbours(Node node){
		List<Node> neighbours = new List<Node>();
		for(int x = -1; x <= 1; x++){
			for(int y = -1; y <= 1; y++)
			{
				for(int z = -1; z <= 1; z++)
				{
					//Ignore the node itself
					if (x == 0 && y == 0 && z == 0)
					{
						continue;
					}
					//Ignore diagonals (ensures only one offset is non-zero)
			   	 	if (Mathf.Abs(x) + Mathf.Abs(y) + Mathf.Abs(z) != 1)
			   	 	{
						continue;
					}
					
					int checkX = node.gridX + x;
					int checkY = node.gridY + y;
					int checkZ = node.gridZ + z;
					
					if(checkX >= 0 && checkX < gridSizeX && 
						checkY >= 0 && checkY < gridSizeY && 
						checkZ >= 0 && checkZ < gridSizeZ)
					{
						neighbours.Add(grid[checkX, checkY, checkZ]);
					}
				}
				
			}
		}
		
		return neighbours;
	}
	
	public Node GetNode(Vector3 worldPosition)
	{
		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (worldPosition.y + gridWorldSize.y/2) / gridWorldSize.y;
		float percentZ = (worldPosition.z + gridWorldSize.z/2) / gridWorldSize.z;
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
