using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CirclePuzzleData
{
    public string ringName = "Ring_Outer";
    public int prefabIndex; 
    public int segments = 8; 
}

[CreateAssetMenu(fileName = "NewCircleStage", menuName = "Puzzle/Circle Stage Data")]
public class CirclePuzzleStageData : ScriptableObject
{
    public string stageName = "Circle_Stage_1";
    public List<GameObject> ringPrefabs = new List<GameObject>();
    public List<CirclePuzzleData> ringDataList = new List<CirclePuzzleData>();
}