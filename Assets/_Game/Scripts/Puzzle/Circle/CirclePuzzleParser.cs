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
                // 주석(#)이거나 빈 줄이거나, 'Circle Count' 문자열이 포함된 줄은 건너뜁니다.
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.Contains("Circle Count")) continue;

                string[] tokens = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // 데이터 숫자가 하나만 적힌 줄(예: 원의 개수 '3')도 매핑에서 제외합니다.
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
            Debug.LogError($"[CircleParser 예외] {e.Message}");
            return null;
        }
    }
}