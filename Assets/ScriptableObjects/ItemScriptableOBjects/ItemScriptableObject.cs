
using UnityEngine;

[CreateAssetMenu(fileName = "ItemScriptableObject", menuName = "Scriptable Objects/ItemScriptableObject")]
public class ItemScriptableObject : ScriptableObject
{
	public int id;
	public string itemName;
	public bool isUseable;
	public float itemCharge;
	public int usesRemaining;
	public GameObject visualPrefab;
	public GameObject droppablePrefab;
	public GameObject useablePrefab;
	public Sprite icon;
}

