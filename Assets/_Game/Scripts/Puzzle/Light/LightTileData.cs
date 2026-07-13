using System;
using System.Collections.Generic;

[Serializable]
public class LightTileRawData
{
    public int id;
    public int initialState; 
    public List<int> targetIds = new List<int>();
}

[Serializable]
public class LightStageParsedData
{
    public int rows;
    public int cols;
    public List<LightTileRawData> tileDataList = new List<LightTileRawData>();
}

