using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class LightScript : MonoBehaviour
{
	[SerializeField] private Light lightSource;
	[SerializeField] private LayerMask enemyLayer;
	[SerializeField] private LayerMask obstacleLayer;
	[SerializeField] private LayerMask groundLayer;
	private int obstacleLayers;
	[SerializeField] private HashSet<Transform> enemiesInLight = new HashSet<Transform>();
	[SerializeField] private Collider[] enemyColliders;
	void Start()
	{
		obstacleLayers = obstacleLayer.value | groundLayer.value;
	}

	
	void Update()
	{
		DetectEnemiesInLight();
		CheckIfEnemyExitLight();
		
	}
	
	private void DetectEnemiesInLight()
	{
		enemyColliders = Physics.OverlapSphere(lightSource.transform.position, lightSource.range, enemyLayer);
		foreach(Collider enemyCollider in enemyColliders)
		{
			IAffectedByLight monsterAffectedByLight = enemyCollider.GetComponent<IAffectedByLight>();
			if(monsterAffectedByLight != null)
			{
				if(IsDirectlyLit(enemyCollider.transform))
				{
					//Debug.Log("monster in direct light");
					if(!enemiesInLight.Contains(enemyCollider.transform))
					{
						enemiesInLight.Add(enemyCollider.transform);
						monsterAffectedByLight.EnteredLight();
					}
				}
			}
			
		}
	}
	
	private void CheckIfEnemyExitLight()
	{
		List<Transform> enemiesToRemove = new List<Transform>();
		foreach(Transform enemy in enemiesInLight)
		{
			if(enemy != null)
			{
				if(!IsDirectlyLit(enemy) || Vector3.Distance(enemy.position, lightSource.transform.position) >= lightSource.range)
				{
					enemiesToRemove.Add(enemy);
				}

			}
		}
		
		foreach (Transform enemy in enemiesToRemove)
		{
			enemy.GetComponent<IAffectedByLight>().ExitLight();
			enemiesInLight.Remove(enemy);
			//Debug.Log("enemy exit light");
			
		}
	}
	
	private bool IsDirectlyLit(Transform enemy)
	{
		Vector3 dirToEnemy = (enemy.position - lightSource.transform.position).normalized;
		float distance = Vector3.Distance(lightSource.transform.position, enemy.position);
		//Debug.DrawRay(lightSource.transform.position, dirToEnemy*distance, Color.yellow);
		if(Physics.Raycast(lightSource.transform.position, dirToEnemy, out RaycastHit hit, distance, obstacleLayers))
		{
			if(hit.collider != null)
			{
				return false;
			}
			
		}

		return true;
		
	}
}
