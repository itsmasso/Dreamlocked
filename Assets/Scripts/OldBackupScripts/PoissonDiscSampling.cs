using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
How it works:
https://www.youtube.com/watch?v=JZzdsVr8Zu4
 */
public static class PoissonDiscSampling
{
	public static List<Vector2> GeneratePoints(float pointRadius, Vector2 gridSize, int numSamplesBeforeRejection = 30)
	{
		//getting size of cell through pythagorean theorem (r^2 = s^2 + s^2) we solve for s to get side lengths
		float cellSize = pointRadius / Mathf.Sqrt(2); 

		//creating grid using two dimensional array: int[,]
		//determines how many cells fit in grid size by dividing grid size by cellSize.
		int[,] grid = new int[Mathf.CeilToInt(gridSize.x / cellSize), Mathf.CeilToInt(gridSize.y / cellSize)];

		//list of final points that will be used for object spawns
		List<Vector2> finalPoints = new List<Vector2>();

		//list of spawnpoints used for generating new potential spawnpoints around the spawnpoint
		List<Vector2> spawnPoints = new List<Vector2>();

		spawnPoints.Add(gridSize / 2); //adding point in the middle of the grid

		/*
		In this loop we pick a point to be a spawn point that will be used for spawning other potential candidate points around it.
		If the candidate is accepted, then we add it to the list of final points & as a spawnpoint to be used to see if we can spawn more valid candidates around that.
		If we fail to spawn a valid candidate around a spawnpoint 30 times (numSamplesBeforeRejection), then we remove the spawnpoint from the spawn points list since
		that point no longer can spawn valid candidates around it. 
		We keep going until the spawnPoints list is empty meaning that there is no space left since valid candidates can't be spawned. 
		*/
		while (spawnPoints.Count > 0)
		{
			//choosing a random spawnpoint from the spawnPoints list
			int spawnIndex = Random.Range(0, spawnPoints.Count);

			//the chosen spawnPoint
			Vector2 spawnCenter = spawnPoints[spawnIndex];
			bool candidateAccepted = false;

			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{
				float angle = Random.value * Mathf.PI * 2;
				//determine a random direction
				Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

				//Random.Range(radius, 2 * radius); indicates that we spawn the candidate point outside the cell because the minimum is the radius
				Vector2 candidate = spawnCenter + dir * Random.Range(pointRadius, 2 * pointRadius);

				if (IsValid(candidate, gridSize, cellSize, pointRadius, finalPoints, grid))
				{
					finalPoints.Add(candidate);
					spawnPoints.Add(candidate);

					//mark a cell in the grid as occupied and store the index of that point on the grid
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = finalPoints.Count;
					candidateAccepted = true;
					break;
				}
			}
			if (!candidateAccepted)
			{
				spawnPoints.RemoveAt(spawnIndex);
			}

		}

		return finalPoints;
	}

	static bool IsValid(Vector2 candidate, Vector2 gridSize, float cellSize, float pointRadius, List<Vector2> points, int[,] grid)
	{
		if (candidate.x >= 0 && candidate.x < gridSize.x && candidate.y >= 0 && candidate.y < gridSize.y)
		{
			int cellX = (int)(candidate.x / cellSize);
			int cellY = (int)(candidate.y / cellSize);
			int searchStartX = Mathf.Max(0, cellX - 2);
			int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
			int searchStartY = Mathf.Max(0, cellY - 2);
			int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

			for (int x = searchStartX; x <= searchEndX; x++)
			{
				for (int y = searchStartY; y <= searchEndY; y++)
				{
					int pointIndex = grid[x, y] - 1;
					if (pointIndex != -1)
					{
						float dist = (candidate - points[pointIndex]).sqrMagnitude;
						if (dist < pointRadius * pointRadius)
						{
							return false;
						}
					}
				}
			}
			return true;
		}
		return false;
	}

}