using UnityEngine;

	public class Node
	{
		public CellType cellType;
		public Vector3 pos {get; private set;}
		public int gridX;
		public int gridY;
		public int gCost; //forward cost
		public int hCost; //backward cost
		public Node previousNode;
		public Node(CellType cellType, Vector3 pos, int gridX, int gridY)
		{
			this.gridX = gridX;
			this.gridY = gridY;
			this.cellType = cellType;
			this.pos = pos;
		}
		
		public int fCost
		{
			get
			{
				return gCost + hCost;
			}
		}
		
	}