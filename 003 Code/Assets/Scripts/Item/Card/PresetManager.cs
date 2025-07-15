using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using SimpleJSON;
using UnityEngine.UI;
using TMPro;
using static GameDataManager;

public class PresetManager : MonoBehaviour
{
    public XRRayInteractor rayInteractor;
    public GameObject presetUIPrefab;
    public Material highlightMaterial;
    public int maxSelection = 3;
    public Transform presetSpawnAnchor;
    public GameObject presetPrefab;
    public float presetSpacing = 0.3f; // 프리셋 간 간격
    public int maxPresets = 3; // 최대 프리셋 개수

    private List<GameObject> selectedCards = new List<GameObject>();
    private GameObject currentUI;
    private List<GameObject> createdPresets = new List<GameObject>();
    private int currentPresetCount = 0;

    private void Start()
    {
        // presetSpawnAnchor가 할당되지 않은 경우 자동으로 생성
        if (presetSpawnAnchor == null)
        {
            GameObject anchor = new GameObject("PresetSpawnAnchor");
            anchor.transform.SetParent(transform);
            anchor.transform.localPosition = new Vector3(0, 0, 1); // 카메라 앞쪽에 위치
            presetSpawnAnchor = anchor.transform;
        }
    }

    public void HandleBButton()
    {
        if (selectedCards.Count >= maxSelection)
        {
            ShowPresetUI();
            return;
        }

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            CardDisplay cardDisplay = hit.collider.GetComponentInParent<CardDisplay>();
            if (cardDisplay == null) return;

            GameObject card = cardDisplay.gameObject;

            if (!selectedCards.Contains(card))
            {
                SelectCard(card);
                string json = cardDisplay.GetJsonData();
                Debug.Log($"선택된 카드 수: {selectedCards.Count}, 선택된 카드의 json: {json}");
            }
        }
    }

    void SelectCard(GameObject card)
    {
        Outline outline = card.GetComponent<Outline>();
        if (outline == null)
        {
            outline = card.AddComponent<Outline>();
        }
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 5f;

        selectedCards.Add(card);
    }

    void ShowPresetUI()
    {
        if (currentUI != null) return;

        Camera cam = Camera.main;
        Vector3 uiPos = cam.transform.position + cam.transform.forward * 0.5f + Vector3.up * -0.2f;

        currentUI = Instantiate(presetUIPrefab, uiPos, Quaternion.identity);

        Canvas canvas = currentUI.GetComponentInChildren<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        currentUI.transform.LookAt(cam.transform);
        currentUI.transform.Rotate(0, 180f, 0);

        var buttons = currentUI.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.CompareTag("ConfirmButton"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    CreatePreset();
                });
            }
            else if (btn.CompareTag("CancelButton"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    CancelPreset();
                });
            }
        }
    }

    public void CreatePreset()
    {
        JSONArray combinedData = new JSONArray();
        foreach (var card in selectedCards)
        {
            var cardDisplay = card.GetComponent<CardDisplay>();
            if (cardDisplay == null) continue;

            var jsonString = cardDisplay.GetJsonData();
            if (string.IsNullOrEmpty(jsonString)) continue;

            JSONNode cardJsonNode = JSON.Parse(jsonString);
            if (cardJsonNode != null && cardJsonNode.IsObject)
            {
                combinedData.Add(cardJsonNode);
            }
        }
        SavePreset(combinedData.ToString());
        ClearSelection();
    }

    void SavePreset(string json)
    {
        if (currentPresetCount >= maxPresets)
        {
            Debug.LogWarning("최대 프리셋 개수에 도달했습니다!");
            return;
        }

        GameDataManager.PresetJsonData = json;
        Debug.Log(GameDataManager.PresetJsonData);
        Vector3 spawnPosition = presetSpawnAnchor.position + new Vector3(currentPresetCount * presetSpacing, 0, 0);
        GameObject preset = Instantiate(presetPrefab, spawnPosition, Quaternion.identity);
        createdPresets.Add(preset);

        TextMeshPro presetNameText = preset.GetComponentInChildren<TextMeshPro>();
        if (presetNameText != null)
        {
            presetNameText.text = $"Preset{currentPresetCount + 1}";
        }

        var container = preset.GetComponent<JSONDataContainer>();
        if (container != null)
        {
            container.SaveData(json);
        }

        currentPresetCount++;

        if (currentUI != null)
        {
            Destroy(currentUI);
            currentUI = null;
        }
    }

    public void CancelPreset()
    {
        ClearSelection();
    }

    void ClearSelection()
    {
        foreach (var card in selectedCards) Destroy(card.GetComponent<Outline>());
        selectedCards.Clear();
        Destroy(currentUI);
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
            }
        }
    }

    public void ClearAllPresets()
    {
        foreach (var preset in createdPresets)
        {
            Destroy(preset);
        }
        createdPresets.Clear();
        currentPresetCount = 0;
    }
}
