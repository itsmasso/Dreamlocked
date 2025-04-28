using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sounds2DSOList", menuName = "Scriptable Objects/Sounds2DSOList")]
public class Sounds2DSOList : ScriptableObject
{
    public List<Sound2DSO> sound2DSOList = new List<Sound2DSO>();
}
