using UnityEngine;

[CreateAssetMenu(fileName = "ItemScriptableObject", menuName = "Scriptable Objects/ItemScriptableObject")]
public class ItemScriptableObject : ScriptableObject
{
	public GameObject physicalItemPrefab;
	public GameObject visualItemPrefab;
}
