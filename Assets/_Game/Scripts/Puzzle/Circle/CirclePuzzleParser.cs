using System;
using System.Collections.Generic;
using UnityEngine;

public static class CirclePuzzleParser
{
    public static List<CirclePuzzleData> Parse(string textData)
    {
        if (string.IsNullOrEmpty(textData)) return null;

        try
        {
            string[] lines = textData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<CirclePuzzleData> ringList = new List<CirclePuzzleData>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.Contains("Circle Count")) continue;

                string[] tokens = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length < 2) continue;

                ringList.Add(new CirclePuzzleData
                {
                    prefabIndex = int.Parse(tokens[0]),
                    segments = int.Parse(tokens[1])
                });
            }

            return ringList;
        }
        catch (Exception e)
        {
            Debug.LogError($"{e.Message}");
            return null;
        }
    }
}