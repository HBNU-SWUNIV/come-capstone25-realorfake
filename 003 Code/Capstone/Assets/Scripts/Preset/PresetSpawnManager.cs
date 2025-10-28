using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PresetSpawnManager : MonoBehaviour
{
    [SerializeField] private Transform presetSpawnAnchor;
    [SerializeField] private GameObject presetPrefab;
    [SerializeField] private float presetSpacing = 0.3f;
    [SerializeField] private int maxPresets = 3;

    private List<GameObject> createdPresets = new List<GameObject>();
    private int currentPresetCount = 0;

    public bool CanCreateMorePresets => currentPresetCount < maxPresets;

    public void CreatePreset(string json)
    {
        if (!CanCreateMorePresets)
        {
            Debug.LogWarning("최대 프리셋 개수에 도달했습니다!");
            return;
        }

        Vector3 spawnPosition = presetSpawnAnchor.position + new Vector3(currentPresetCount * presetSpacing, 0, 0);
        GameObject preset = Instantiate(presetPrefab, spawnPosition, Quaternion.identity);
        createdPresets.Add(preset);

        SetupPresetName(preset);
        SetupPresetData(preset, json);

        currentPresetCount++;
    }

    private void SetupPresetName(GameObject preset)
    {
        TextMeshPro presetNameText = preset.GetComponentInChildren<TextMeshPro>();
        if (presetNameText != null)
        {
            presetNameText.text = $"Preset{currentPresetCount + 1}";
        }
    }

    private void SetupPresetData(GameObject preset, string json)
    {
        var container = preset.GetComponent<JSONDataContainer>();
        if (container == null)
        {
            Debug.LogError("생성된 프리셋 프리팹에 JSONDataContainer 컴포넌트가 없습니다!");
            return;
        }

        container.SaveData(json);
        Debug.Log($"[SetupPresetData] 생성된 프리팹의 JSONDataContainer에 데이터 저장됨: {container.data}");
    }

    public void ClearAllPresets()
    {
        foreach (var preset in createdPresets)
        {
            if (preset != null)
            {
                Destroy(preset);
            }
        }
        createdPresets.Clear();
        currentPresetCount = 0;
    }

    public void SelectPresetForFight(int presetIndex)
    {
        if (presetIndex >= 0 && presetIndex < createdPresets.Count)
        {
            GameObject selectedPreset = createdPresets[presetIndex];
            var container = selectedPreset.GetComponent<JSONDataContainer>();
            if (container != null)
            {
                GameDataManager.PresetJsonData = container.data;
                Debug.Log($"선택된 프리셋 {presetIndex + 1}의 데이터가 GameDataManager에 저장되었습니다.");
            }
        }
    }
} 