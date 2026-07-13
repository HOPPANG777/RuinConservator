using System;
using System.Collections.Generic;

[Flags]
public enum PipePuzzleDir
{
    None = 0,
    Top = 1 << 0,
    Right = 1 << 1,
    Bottom = 1 << 2,
    Left = 1 << 3
}

public enum PipePuzzleType
{
    Empty, 
    Start, 
    End, 
    Pipe_1_Straight, 
    Pipe_1_Corner, 
    Pipe_2_T, 
    Pipe_3_Cross
}

[Serializable]
public struct PipePuzzleData
{
    public PipePuzzleType type;
    public int rotationIndex;

    public PipePuzzleDir GetCurrentDirections()
    {
        PipePuzzleDir baseDir = type switch
        {
            PipePuzzleType.Start => PipePuzzleDir.Top,
            PipePuzzleType.End => PipePuzzleDir.Top,
            PipePuzzleType.Pipe_1_Straight => PipePuzzleDir.Top | PipePuzzleDir.Bottom,
            PipePuzzleType.Pipe_1_Corner => PipePuzzleDir.Left | PipePuzzleDir.Bottom,
            PipePuzzleType.Pipe_2_T => PipePuzzleDir.Top | PipePuzzleDir.Left | PipePuzzleDir.Right,
            PipePuzzleType.Pipe_3_Cross => PipePuzzleDir.Top | PipePuzzleDir.Bottom | PipePuzzleDir.Left | PipePuzzleDir.Right,
            _ => PipePuzzleDir.None
        };

        int dirVal = (int)baseDir;
        for (int i = 0; i < rotationIndex; i++)
        {
            dirVal = ((dirVal << 1) | (dirVal >> 3)) & 0x0F;
        }

        return (PipePuzzleDir)dirVal;
    }
}

public class PipePuzzleTileData
{
    public string typeStr;
    public int rotationIndex;
}

public class PipePuzzleStageData
{
    public int rows;
    public int cols;
    public List<PipePuzzleTileData> tileDataList = new List<PipePuzzleTileData>();
}