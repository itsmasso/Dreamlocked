using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
public class DetectEnemyInLights : NetworkBehaviour
{
    private int obstacleLayers;
    [SerializeField] private Light lightSource;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactableMoveables;
    [SerializeField] private HashSet<NetworkObject> enemiesInLight = new HashSet<NetworkObject>();
    [SerializeField] private Collider[] enemyColliders;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool isFlashlight;
    void Start()
    {

        obstacleLayers = obstacleLayer.value | groundLayer.value | interactableMoveables;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    
    public float GetLightRange()
    {
        return lightSource.range;
    }

    public void TrackEnemiesInLight(bool isFlashlight)
    {
        enemyColliders = Physics.OverlapSphere(lightSource.transform.position, lightSource.range, enemyLayer);

        foreach (Collider enemyCollider in enemyColliders)
        {
            NetworkObject enemyNetObj = enemyCollider.GetComponent<NetworkObject>();
            if (enemyNetObj == null || !enemyNetObj.IsSpawned) continue;

            if (isFlashlight && !IsInSpotlight(enemyNetObj.transform)) continue;
            if (!IsVisibleFromLight(enemyCollider.transform)) continue;

            IAffectedByLight affected = enemyCollider.GetComponent<IAffectedByLight>();
            if (affected == null) continue;

            affected.SetInLight(true);
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

    public bool IsVisibleFromLight(Transform enemy)
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
