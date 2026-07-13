using System;
using System.Collections.Generic;
using UnityEngine;

public static class LightPuzzleParser
{
    public static LightStageParsedData Parse(string textData)
    {
        if (string.IsNullOrEmpty(textData))
        {
            Debug.LogError("[LightParser] 텍스트 데이터가 비어있습니다.");
            return null;
        }

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

            if (validLines.Count < 2)
            {
                Debug.LogError($"[LightParser] 유효한 데이터 라인이 부족합니다. (현재 인식된 라인 수: {validLines.Count})");
                return null;
            }

            LightStageParsedData stageData = new LightStageParsedData();

            string[] sizeTokens = validLines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            stageData.rows = int.Parse(sizeTokens[0]);
            stageData.cols = int.Parse(sizeTokens[1]);

            for (int i = 1; i < validLines.Count; i++)
            {
                string[] tokens = validLines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 3) continue;

                LightTileRawData tileData = new LightTileRawData
                {
                    id = int.Parse(tokens[0]),
                    initialState = int.Parse(tokens[1])
                };

                string[] targets = tokens[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string targetStr in targets)
                {
                    tileData.targetIds.Add(int.Parse(targetStr.Trim()));
                }

                stageData.tileDataList.Add(tileData);
            }

            return stageData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LightParser 예외] 데이터 해석 오류: {e.Message}");
            return null;
        }
    }
}