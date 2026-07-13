using System;
using System.Collections.Generic;
using UnityEngine;

public class CircleRingRawData
{
    public string ringName;
    public int prefabIndex;
    public int segments;
}

public static class CirclePuzzleParser
{
    public static List<CircleRingRawData> Parse(string textData)
    {
        if (string.IsNullOrEmpty(textData)) return null;

        try
        {
            string[] lines = textData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<CircleRingRawData> ringList = new List<CircleRingRawData>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                string[] tokens = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 3) continue;

                ringList.Add(new CircleRingRawData
                {
                    ringName = tokens[0],
                    prefabIndex = int.Parse(tokens[1]),
                    segments = int.Parse(tokens[2])
                });
            }

            return ringList;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CircleParser 예외] {e.Message}");
            return null;
        }
    }
}