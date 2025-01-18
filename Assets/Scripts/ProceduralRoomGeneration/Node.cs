using UnityEngine;

	public class Node : IHeapItem<Node>
	{
		public CellType cellType;
		public Vector3 pos {get; private set;}
		public int gridX;
		public int gridY;
		public int gridZ;
		public int gCost; //forward cost
		public int hCost; //backward cost
		public Node previousNode;
		int heapIndex;
		public Node(CellType cellType, Vector3 pos, int gridX, int gridY, int gridZ)
		{
			this.gridX = gridX;
			this.gridY = gridY;
			this.gridZ = gridZ;
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
		
		public int HeapIndex
		{
			get
			{
				return heapIndex;
			}
			set
			{
				heapIndex = value;
			}
		}
		
		public int CompareTo(Node nodeToCompare)
		{
			int compare = fCost.CompareTo(nodeToCompare.fCost);
			if(compare == 0){
				compare = hCost.CompareTo(nodeToCompare.hCost);
			}
			return -compare;
		}
		
	}