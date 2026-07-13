using System;
using System.Collections.Generic;
using UnityEngine;


namespace LJMIN
{

    [Flags]
    public enum TileDirection
    {
        None = 0,
        Top = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Left = 1 << 3
    }

    public enum TileType
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
    public struct TileData
    {
        public TileType type;
        public int rotationIndex;

        public TileDirection GetCurrentDirections()
        {
            TileDirection baseDir = type switch
            {
                TileType.Start => TileDirection.Top,
                TileType.End => TileDirection.Top,
                TileType.Pipe_1_Straight => TileDirection.Top | TileDirection.Bottom,
                TileType.Pipe_1_Corner => TileDirection.Left | TileDirection.Bottom,
                TileType.Pipe_2_T => TileDirection.Top | TileDirection.Left | TileDirection.Right,
                TileType.Pipe_3_Cross => TileDirection.Top | TileDirection.Bottom | TileDirection.Left | TileDirection.Right,
                _ => TileDirection.None
            };

            int dirVal = (int)baseDir;
            for (int i = 0; i < rotationIndex; i++)
            {
                dirVal = ((dirVal << 1) | (dirVal >> 3)) & 0x0F;
            }

            return (TileDirection)dirVal;
        }
    }

    public class PipeTileRawData
    {
        public string typeStr;
        public int rotationIndex;
    }

    public class PipeStageParsedData
    {
        public int rows;
        public int cols;
        public List<PipeTileRawData> tileDataList = new List<PipeTileRawData>();
    }

    [CreateAssetMenu(fileName = "NewPuzzleLevel", menuName = "Puzzle/Level Data")]
    public class PuzzleLevelData : ScriptableObject
    {
        public int rows = 4;
        public int cols = 4;
        public List<TileData> mapData = new List<TileData>();

        public void ParseRawData(string rawData)
        {
            mapData.Clear();
            string[] tokens = rawData.Split(new[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                TileData tile = new TileData { rotationIndex = 0 };
                tile.type = token switch
                {
                    "0" => TileType.Empty,
                    "S" => TileType.Start,
                    "E" => TileType.End,
                    "1S" => TileType.Pipe_1_Straight,
                    "1C" => TileType.Pipe_1_Corner,
                    "2" => TileType.Pipe_2_T,
                    "3" => TileType.Pipe_3_Cross,
                    _ => TileType.Empty
                };
                mapData.Add(tile);
            }
        }
    }

}