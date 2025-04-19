using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
public class DetectEnemyInLights : NetworkBehaviour
{
    private int obstacleLayers;
    private float lightRange;
    [SerializeField] private Light lightSource;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactableMoveables;
    [SerializeField] private HashSet<NetworkObject> enemiesInLight = new HashSet<NetworkObject>();
    [SerializeField] private Collider[] enemyColliders;
    [SerializeField] private LayerMask enemyLayer;
    void Start()
    {
        lightRange = lightSource.range;
        obstacleLayers = obstacleLayer.value | groundLayer.value | interactableMoveables;
    }

    [Rpc(SendTo.Server)]
    public void ServerSeesEnemyEnteredLightRpc(NetworkObjectReference enemyNetObjRef)
    {
        if (enemyNetObjRef.TryGet(out NetworkObject enemyNetObj))
        {
            if (enemyNetObj.TryGetComponent(out IAffectedByLight enemy))
            {
                enemy.EnteredLight();
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void ServerSeesEnemyExitLightRpc(NetworkObjectReference enemyNetObjRef)
    {
        if (enemyNetObjRef.TryGet(out NetworkObject enemyNetObj))
        {
            if (enemyNetObj.TryGetComponent(out IAffectedByLight enemy))
            {
                enemy.ExitLight();
            }
        }
    }

    public void DetectEnemiesInLight()
    {
        enemyColliders = Physics.OverlapSphere(lightSource.transform.position, lightSource.range, enemyLayer);
        foreach (Collider enemyCollider in enemyColliders)
        {
            NetworkObject enemyNetObj = enemyCollider.GetComponent<NetworkObject>();
            if (enemyNetObj != null && enemyNetObj.IsSpawned)
            {
                IAffectedByLight monsterAffectedByLight = enemyCollider.GetComponent<IAffectedByLight>();
                if (monsterAffectedByLight != null)
                {
                    if (!IsDirectlyLit(enemyCollider.transform)) continue;

                    // This check could possibly lead to bugs
                    // I think it check enemyNetObj not NetworkObject
                    if (enemiesInLight.Contains(enemyNetObj)) continue;

                    enemiesInLight.Add(enemyNetObj);
                    monsterAffectedByLight.EnteredLight();
                    ServerSeesEnemyEnteredLightRpc(enemyNetObj);
                }
            }
        }
    }

    public void DetectEnemiesInFlashLight()
    {
        enemyColliders = Physics.OverlapSphere(lightSource.transform.position, lightSource.range, enemyLayer);
        foreach (Collider enemyCollider in enemyColliders)
        {
            NetworkObject enemyNetObj = enemyCollider.GetComponent<NetworkObject>();
            if (enemyNetObj != null && enemyNetObj.IsSpawned)
            {
                if (IsInSpotlight(enemyNetObj.transform))
                {
                    IAffectedByLight monsterAffectedByLight = enemyCollider.GetComponent<IAffectedByLight>();
                    if (monsterAffectedByLight != null)
                    {
                        if (!IsDirectlyLit(enemyCollider.transform)) continue;

                        // This check could possibly lead to bugs
                        // I think it check enemyNetObj not NetworkObject
                        if (enemiesInLight.Contains(enemyNetObj)) continue;

                        enemiesInLight.Add(enemyNetObj);
                        monsterAffectedByLight.EnteredLight();
                        ServerSeesEnemyEnteredLightRpc(enemyNetObj); //maybe add a bool where this line only calls once. not good to update rpc in update
                    }
                }
            }
        }
    }

    private bool IsInSpotlight(Transform target)
    {
        Vector3 dirToTarget = target.position - lightSource.transform.position;
        float distance = dirToTarget.magnitude;

        //check distance

        if (distance > lightSource.range)
            return false;

        //check angle

        float angleToTarget = Vector3.Angle(lightSource.transform.forward, dirToTarget);
        if (angleToTarget > lightSource.spotAngle / 2f)
            return false;
        return true;
    }
    public void CheckIfEnemyExitLight()
    {
        foreach (NetworkObject enemy in enemiesInLight.ToList())
        {
            // Ensure the enemy is not null before processing
            if (enemy == null)
            {
                enemiesInLight.Remove(enemy);
                continue;
            }
            IAffectedByLight affectedByLight = enemy.GetComponent<IAffectedByLight>();
            if (affectedByLight != null)
            {
                if (enemy != null && !IsDirectlyLit(enemy.transform) || Vector3.Distance(enemy.transform.position, lightSource.transform.position) >= lightSource.range)
                {
                    affectedByLight.ExitLight();
                    enemiesInLight.Remove(enemy);
                    ServerSeesEnemyExitLightRpc(enemy);
                }
            }
        }
    }

    public bool IsDirectlyLit(Transform enemy)
    {
        Vector3 dirToEnemy = (enemy.position - lightSource.transform.position).normalized;
        float distance = Vector3.Distance(lightSource.transform.position, enemy.position);
        //Debug.DrawRay(lightSource.transform.position, dirToEnemy*distance, Color.yellow);
        if (Physics.Raycast(lightSource.transform.position, dirToEnemy, out RaycastHit hit, distance, obstacleLayers))
        {
            if (hit.collider != null)
            {
                return false;
            }

        }

        return true;

    }
}
