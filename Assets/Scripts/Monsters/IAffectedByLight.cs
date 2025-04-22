using UnityEngine;

public interface IAffectedByLight
{
	public void SetInLight(bool isInLight);
	public void EnteredLight();
	public void ExitLight();
}
