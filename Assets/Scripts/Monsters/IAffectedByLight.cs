using UnityEngine;

public interface IAffectedByLight
{
	void AddLightSource(DetectEnemyInLights lightSource);
    void RemoveLightSource(DetectEnemyInLights lightSource);
	public void EnteredLight();
	public void ExitLight();
}
