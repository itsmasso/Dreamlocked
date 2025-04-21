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
        HashSet<NetworkObject> seenThisFrame = new();

        foreach (Collider enemyCollider in enemyColliders)
        {
            NetworkObject enemyNetObj = enemyCollider.GetComponent<NetworkObject>();
            if (enemyNetObj == null || !enemyNetObj.IsSpawned) continue;

            if (!IsDirectlyLit(enemyCollider.transform)) continue;

            IAffectedByLight affected = enemyCollider.GetComponent<IAffectedByLight>();
            if (affected == null) continue;

            seenThisFrame.Add(enemyNetObj);

            if (!enemiesInLight.Contains(enemyNetObj))
            {
                enemiesInLight.Add(enemyNetObj);
                affected.AddLightSource(this);
                ServerSeesEnemyEnteredLightRpc(enemyNetObj);
            }
        }

        foreach (var enemy in enemiesInLight.ToList())
        {
            if (!seenThisFrame.Contains(enemy))
            {
                IAffectedByLight affected = enemy.GetComponent<IAffectedByLight>();
                if (affected != null)
                {
                    affected.RemoveLightSource(this);
                    ServerSeesEnemyExitLightRpc(enemy);
                }

                enemiesInLight.Remove(enemy);
            }
        }
    }

    public void DetectEnemiesInFlashLight()
    {
        enemyColliders = Physics.OverlapSphere(lightSource.transform.position, lightSource.range, enemyLayer);
        HashSet<NetworkObject> seenThisFrame = new();

        foreach (Collider enemyCollider in enemyColliders)
        {
            NetworkObject enemyNetObj = enemyCollider.GetComponent<NetworkObject>();
            if (enemyNetObj == null || !enemyNetObj.IsSpawned) continue;

            if (!IsInSpotlight(enemyNetObj.transform)) continue;
            if (!IsDirectlyLit(enemyCollider.transform)) continue;

            IAffectedByLight affected = enemyCollider.GetComponent<IAffectedByLight>();
            if (affected == null) continue;

            seenThisFrame.Add(enemyNetObj);

            if (!enemiesInLight.Contains(enemyNetObj))
            {
                enemiesInLight.Add(enemyNetObj);
                affected.AddLightSource(this);
                ServerSeesEnemyEnteredLightRpc(enemyNetObj);
            }
        }

        foreach (var enemy in enemiesInLight.ToList())
        {
            if (!seenThisFrame.Contains(enemy))
            {
                IAffectedByLight affected = enemy.GetComponent<IAffectedByLight>();
                if (affected != null)
                {
                    affected.RemoveLightSource(this);
                    ServerSeesEnemyExitLightRpc(enemy);
                }

                enemiesInLight.Remove(enemy);
            }
        }
    }

    private bool IsInSpotlight(Transform target)
    {
        Vector3 dirToTarget = (target.position - lightSource.transform.position).normalized;
        float distance = Vector3.Distance(lightSource.transform.position, target.position);

        if (distance > lightSource.range)
            return false;

        float angle = Vector3.Angle(lightSource.transform.forward, dirToTarget);
        if (angle > lightSource.spotAngle / 2f)
            return false;

        return true;
    }
    // public void CheckIfEnemyExitLight()
    // {
    //     foreach (NetworkObject enemy in enemiesInLight.ToList())
    //     {
    //         // Ensure the enemy is not null before processing
    //         if (enemy == null)
    //         {
    //             enemiesInLight.Remove(enemy);
    //             continue;
    //         }
    //         IAffectedByLight affectedByLight = enemy.GetComponent<IAffectedByLight>();
    //         if (affectedByLight != null)
    //         {
    //             if (enemy != null && !IsDirectlyLit(enemy.transform) || Vector3.Distance(enemy.transform.position, lightSource.transform.position) >= lightSource.range)
    //             {
    //                 affectedByLight.ExitLight();
    //                 enemiesInLight.Remove(enemy);
    //                 ServerSeesEnemyExitLightRpc(enemy);
    //             }
    //         }
    //     }
    // }

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
