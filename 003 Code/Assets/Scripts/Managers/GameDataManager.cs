using UnityEngine;

public static class GameDataManager
{
    // MainScene에서 저장한 프리셋 JSON 데이터를 저장할 변수
    public static string PresetJsonData { get; set; }

    // 필요한 경우 다른 전역 데이터 변수를 여기에 추가할 수 있습니다.

    // 데이터 초기화 메서드 (선택 사항)
    public static void ClearPresetData()
    {
        PresetJsonData = null;
    }
} 