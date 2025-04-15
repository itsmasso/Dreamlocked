using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HouseMapDifficultyListSO", menuName = "Scriptable Objects/HouseMapDifficultyListSO")]
public class HouseMapDifficultyListSO : ScriptableObject
{
    public List<HouseMapDifficultySettingsSO> difficultyListSO;
}
