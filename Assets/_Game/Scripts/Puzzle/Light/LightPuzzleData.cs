using System;
using System.Collections.Generic;

[Serializable]
public class LightPuzzleData
{
    public int id;
    public int initialState; 
    public List<int> targetIds = new List<int>();
}

[Serializable]
public class LightPuzzleStageData
{
    public int rows;
    public int cols;
    public List<LightPuzzleData> tileDataList = new List<LightPuzzleData>();
}

