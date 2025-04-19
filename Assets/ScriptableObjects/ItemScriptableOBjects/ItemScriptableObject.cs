
using UnityEngine;

[CreateAssetMenu(fileName = "ItemScriptableObject", menuName = "Scriptable Objects/ItemScriptableObject")]
public class ItemScriptableObject : ScriptableObject
{
	public int id;
	public string itemName;
	public float itemCharge;
	public int usesRemaining;
	public GameObject heldPrefab;
	public GameObject droppablePrefab;
	public Sprite icon;
}

