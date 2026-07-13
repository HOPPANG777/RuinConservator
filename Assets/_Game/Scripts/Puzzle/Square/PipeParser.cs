using System;
using System.Collections.Generic;
using UnityEngine;

public static class PipeParser
{
    public static PipeStageParsedData Parse(string textData)
    {
        if (string.IsNullOrEmpty(textData)) return null;

        try
        {
            string[] lines = textData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> validLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                validLines.Add(trimmed);
            }

            if (validLines.Count < 2) return null;

            PipeStageParsedData stageData = new PipeStageParsedData();

            // 크기 파싱
            string[] sizeTokens = validLines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            stageData.rows = int.Parse(sizeTokens[0]);
            stageData.cols = int.Parse(sizeTokens[1]);

            // 타일 데이터들 행 단위 파싱
            for (int i = 1; i < validLines.Count; i++)
            {
                string[] tokens = validLines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // 가로로 나열된 토큰들을 2개씩 쌍으로 묶어서 처리 (타일타입, 회전)
                for (int j = 0; j < tokens.Length; j += 2)
                {
                    if (j + 1 >= tokens.Length) break;

                    stageData.tileDataList.Add(new PipeTileRawData
                    {
                        typeStr = tokens[j],
                        rotationIndex = int.Parse(tokens[j + 1])
                    });
                }
            }

            return stageData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PipeParser 예외] {e.Message}");
            return null;
        }
    }
}