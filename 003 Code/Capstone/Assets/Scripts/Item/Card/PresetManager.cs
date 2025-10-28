using UnityEngine;
using UnityEngine.XR; // XR 관련 네임스페이스
using System.Collections.Generic; // List 사용을 위함
using UnityEngine.UI; // UI.Button 사용을 위함
using TMPro; // TextMeshPro 사용을 위함
using SimpleJSON; // SimpleJSON 라이브러리 사용을 위함
using UnityEngine.XR.Interaction.Toolkit.Interactors; // XRRayInteractor 사용을 위함
using System.IO; // 파일 입출력(Save/Load)을 위함
using static GameDataManager; // GameDataManager의 static 멤버 사용을 위함
using UnityEngine.SceneManagement;
using System.Linq; // Add this for FindIndex
using System; // Add this for ArgumentException

#if UNITY_EDITOR
using UnityEditor; // 에디터 기능(AssetDatabase.Refresh) 사용을 위함
#endif

// 프리셋 파일 저장을 위한 데이터 구조 클래스
[System.Serializable]
public class PresetSaveData
{
    public List<string> presetJsonList = new List<string>();
}

public class PresetManager : MonoBehaviour
{
    [Header("XR Interaction")]
    public XRRayInteractor rayInteractor;

    [Header("UI & Prefabs")]
    public GameObject presetUIPrefab;
    public GameObject presetPrefab;

    [Header("Visuals")]
    public Material highlightMaterial; // 이 변수는 현재 코드에서 사용되지 않지만, 필요시를 위해 남겨둡니다.

    [Header("Settings")]
    public int maxSelection = 3;
    public int maxPresets = 3;
    public float presetSpacing = 0.3f;
    public Transform presetSpawnAnchor;
    public Transform itemCaseTransform; // ItemCase 오브젝트의 Transform 참조 [추가됨]

    // 내부 관리용 변수들
    private List<GameObject> selectedCards = new List<GameObject>();
    private List<GameObject> createdPresets = new List<GameObject>();
    private List<string> presetJsonDataList = new List<string>(); // 저장/로드를 위한 JSON 데이터 리스트
    private GameObject currentUI;
    private int currentPresetCount = 0;
    private string savePath;

    private void Awake()
    {
        string fileName = "Preset.json";
        savePath = Path.Combine(Application.persistentDataPath, fileName);

        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Debug.Log($"프리셋 저장 경로가 설정되었습니다: {savePath}");
        // [수정됨] LoadPresetsFromFile() 호출을 Start로 이동 (ItemCase 참조 후 로드하기 위해)
        // LoadPresetsFromFile();
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ItemCaseTransform 다시 찾기 (씬 전환 시 필요할 수 있음)
        FindItemCaseTransform();

        if (scene.name != "MainScene")
        {
            if (presetSpawnAnchor != null)
            {
                presetSpawnAnchor.gameObject.SetActive(false);
                Debug.Log("MainScene이 아니므로 프리셋 오브젝트를 숨깁니다.");
            }
        }
        else
        {
            if (presetSpawnAnchor != null)
            {
                presetSpawnAnchor.gameObject.SetActive(true);
                LoadPresetsFromFile(); // 씬이 로드될 때마다 파일에서 다시 로드
                Debug.Log("MainScene이므로 프리셋 오브젝트를 다시 표시합니다.");
            }
        }
    }

    private void Start()
    {
        // ItemCase Transform 찾기 [추가됨]
        FindItemCaseTransform();

        if (presetSpawnAnchor == null)
        {
            GameObject anchor = new GameObject("PresetSpawnAnchor");
            anchor.transform.SetParent(transform);
            Vector3 spawnPos = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward : transform.position + transform.forward;
            anchor.transform.position = spawnPos;
            presetSpawnAnchor = anchor.transform;
        }

        // [추가됨] ItemCase 참조 후 프리셋 로드
        LoadPresetsFromFile();
    }

    // [추가됨] ItemCase Transform 찾는 함수
    private void FindItemCaseTransform()
    {
         // 이미 할당되어 있으면 다시 찾지 않음
        if (itemCaseTransform != null) return;

        // Inspector에서 할당되지 않았으면 이름으로 찾기
        GameObject itemCaseObject = GameObject.Find("ItemCase"); // ItemCase 오브젝트 이름 확인 필요
        if (itemCaseObject != null)
        {
            itemCaseTransform = itemCaseObject.transform;
            Debug.Log("ItemCase Transform을 찾았습니다.");
        }
        else
        {
            Debug.LogError("ItemCase 오브젝트를 찾을 수 없습니다! Hierarchy에서 이름을 확인하거나 Inspector에서 직접 할당해주세요.");
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
            if (cardDisplay != null)
            {
                GameObject card = cardDisplay.gameObject;
                if (!selectedCards.Contains(card))
                {
                    SelectCard(card);
                    string json = cardDisplay.GetJsonData();
                    Debug.Log($"카드 선택됨. 총 {selectedCards.Count}개. 선택된 카드 JSON: {json}");
                }
                else
                {
                    Debug.Log("이미 선택된 카드입니다.");
                }
            }
        }
    }

    void SelectCard(GameObject card)
    {
        Outline outline = card.GetComponent<Outline>() ?? card.AddComponent<Outline>();
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 5f;
        selectedCards.Add(card);
    }

    void ShowPresetUI()
    {
        if (currentUI != null) return;

        Camera cam = Camera.main;
        Vector3 uiPos = cam.transform.position + cam.transform.forward * 0.7f;

        Quaternion uiRotation = Quaternion.LookRotation(cam.transform.forward);
        currentUI = Instantiate(presetUIPrefab, uiPos, uiRotation);

        var buttons = currentUI.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.CompareTag("ConfirmButton"))
            {
                btn.onClick.AddListener(ConfirmPresetCreation);
            }
            else if (btn.CompareTag("CancelButton"))
            {
                btn.onClick.AddListener(CancelPreset);
            }
        }
    }

    public void ConfirmPresetCreation()
    {
        if (currentPresetCount >= maxPresets)
        {
            Debug.LogWarning("최대 프리셋 개수에 도달하여 더 이상 생성할 수 없습니다.");
            CancelPreset();
            return;
        }

        JSONArray combinedData = new JSONArray();
        foreach (var card in selectedCards)
        {
            var cardDisplay = card.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                // JSON 파싱 시 null 체크 추가
                string cardJsonString = cardDisplay.GetJsonData();
                if (!string.IsNullOrEmpty(cardJsonString))
                {
                    JSONNode cardJsonNode = JSON.Parse(cardJsonString);
                    if (cardJsonNode != null)
                    {
                       combinedData.Add(cardJsonNode);
                    } else {
                        Debug.LogWarning($"카드 {card.name}의 JSON 데이터 파싱 실패.");
                    }
                } else {
                     Debug.LogWarning($"카드 {card.name}의 JSON 데이터가 비어있습니다.");
                }
            }
        }

        string newPresetJson = combinedData.ToString();

        presetJsonDataList.Add(newPresetJson);
        CreatePresetObject(newPresetJson, currentPresetCount);
        currentPresetCount++;

        SavePresetsToFile();
        ClearSelection();
    }

    public void CancelPreset()
    {
        ClearSelection();
    }

    void ClearSelection()
    {
        foreach (var card in selectedCards)
        {
            if (card != null && card.TryGetComponent<Outline>(out var outline))
            {
                Destroy(outline);
            }
        }
        selectedCards.Clear();

        if (currentUI != null)
        {
            Destroy(currentUI);
            currentUI = null;
        }
    }

    // [수정됨] itemCaseTransform.right 사용
    private void CreatePresetObject(string json, int index)
    {
        if (presetSpawnAnchor == null)
        {
            Debug.LogError("presetSpawnAnchor가 설정되지 않아 프리셋 오브젝트를 생성할 수 없습니다.");
            return;
        }
        if (itemCaseTransform == null) // itemCaseTransform null 체크
        {
             Debug.LogError("itemCaseTransform이 설정되지 않아 프리셋 정렬 방향을 결정할 수 없습니다.");
             return;
        }

        // itemCaseTransform.right (ItemCase의 로컬 X축) 사용
        Vector3 offset = itemCaseTransform.right * (index * presetSpacing);
        // 시작 위치는 presetSpawnAnchor 사용, 방향은 ItemCase 기준
        Vector3 spawnPosition = presetSpawnAnchor.position + offset;

        // 생성 시 회전은 ItemCase의 회전을 따르도록 설정 (선택 사항)
        GameObject preset = Instantiate(presetPrefab, spawnPosition, itemCaseTransform.rotation, presetSpawnAnchor);

        while (createdPresets.Count <= index)
        {
            createdPresets.Add(null);
        }
        createdPresets[index] = preset;

        TextMeshPro presetNameText = preset.GetComponentInChildren<TextMeshPro>();
        if (presetNameText != null)
        {
            presetNameText.text = $"Preset {index + 1}";
        }

        var container = preset.GetComponent<JSONDataContainer>();
        if (container != null)
        {
            container.SaveData(json);
        }
    }

    #region --- Save & Load ---

    private void SavePresetsToFile()
    {
        PresetSaveData saveData = new PresetSaveData();
        saveData.presetJsonList = presetJsonDataList.Where(json => json != null).ToList();

        string jsonString = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, jsonString);

        Debug.Log($"{saveData.presetJsonList.Count}개의 프리셋을 파일에 저장했습니다: {savePath}");
    }

    private void LoadPresetsFromFile()
    {
        if (presetSpawnAnchor == null || !presetSpawnAnchor.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("presetSpawnAnchor가 준비되지 않아 프리셋 로드를 연기합니다.");
            return;
        }
        // [추가됨] ItemCaseTransform 준비 확인
        if (itemCaseTransform == null)
        {
             Debug.LogWarning("itemCaseTransform이 준비되지 않아 프리셋 로드를 연기합니다.");
             return;
        }


        ClearAllPresetObjects();
        presetJsonDataList.Clear();
        createdPresets.Clear();
        currentPresetCount = 0;

        if (File.Exists(savePath))
        {
            string jsonString = File.ReadAllText(savePath);

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Debug.LogWarning($"{Path.GetFileName(savePath)} 파일이 비어있어 로드를 건너뜁니다.");
                return;
            }
            if (string.IsNullOrEmpty(jsonString))
            {
                 Debug.LogWarning($"{Path.GetFileName(savePath)} 파일 내용이 비어있습니다.");
                 return;
            }

            PresetSaveData loadedData = JsonUtility.FromJson<PresetSaveData>(jsonString);

             if (loadedData == null)
            {
               Debug.LogError($"{Path.GetFileName(savePath)} 파일 파싱 실패. 파일 내용을 확인해주세요.");
               return;
            }

            presetJsonDataList = loadedData.presetJsonList ?? new List<string>();

            Debug.Log($"{presetJsonDataList.Count}개의 프리셋을 파일에서 불러옵니다: {savePath}");

            for (int i = 0; i < presetJsonDataList.Count; i++)
            {
                if (!string.IsNullOrEmpty(presetJsonDataList[i]))
                {
                   if (currentPresetCount < maxPresets)
                   {
                       CreatePresetObject(presetJsonDataList[i], currentPresetCount);
                       currentPresetCount++;
                   }
                }
            }
            RearrangePresetObjects();
        }
        else
        {
            Debug.Log($"저장된 프리셋 파일({Path.GetFileName(savePath)})이 없습니다. 새로 시작합니다.");
        }
    }

    #endregion

    #region --- Preset Deletion ---

    public void DeletePreset(GameObject presetToDelete)
    {
        int indexToDelete = createdPresets.IndexOf(presetToDelete);

        if (indexToDelete != -1)
        {
            Destroy(presetToDelete);
            createdPresets.RemoveAt(indexToDelete);
            // JSON 데이터 리스트에서도 해당 인덱스 제거
            if (indexToDelete < presetJsonDataList.Count) // 인덱스 유효성 검사
            {
                presetJsonDataList.RemoveAt(indexToDelete);
            } else {
                 Debug.LogWarning($"presetJsonDataList에서 인덱스 {indexToDelete}의 데이터를 찾을 수 없어 제거하지 못했습니다.");
            }
            currentPresetCount--;
            SavePresetsToFile();
            RearrangePresetObjects();
            Debug.Log($"프리셋 {indexToDelete + 1} 삭제 완료.");
        }
        else
        {
            Debug.LogWarning("삭제할 프리셋을 리스트에서 찾을 수 없습니다.");
        }
    }

    // [수정됨] itemCaseTransform.right 사용
    private void RearrangePresetObjects()
    {
        if (itemCaseTransform == null) // itemCaseTransform null 체크
        {
             Debug.LogError("itemCaseTransform이 설정되지 않아 프리셋 재정렬 방향을 결정할 수 없습니다.");
             return;
        }

        for (int i = 0; i < createdPresets.Count; i++)
        {
            GameObject preset = createdPresets[i];
            if (preset != null)
            {
                // itemCaseTransform.right (ItemCase의 로컬 X축) 사용
                Vector3 offset = itemCaseTransform.right * (i * presetSpacing);
                 // 시작 위치는 presetSpawnAnchor 사용, 방향은 ItemCase 기준
                Vector3 newPosition = presetSpawnAnchor.position + offset;

                preset.transform.position = newPosition;
                // 회전도 ItemCase에 맞추기 (선택 사항)
                preset.transform.rotation = itemCaseTransform.rotation;

                TextMeshPro presetNameText = preset.GetComponentInChildren<TextMeshPro>();
                if (presetNameText != null)
                {
                    presetNameText.text = $"Preset {i + 1}";
                }
            }
        }
    }

    #endregion

    #region --- Public Management Functions ---

    public void SelectPresetForFight(int presetIndex)
    {
        if (presetIndex >= 0 && presetIndex < createdPresets.Count && createdPresets[presetIndex] != null)
        {
            var container = createdPresets[presetIndex].GetComponent<JSONDataContainer>();
            if (container != null)
            {
                GameDataManager.PresetJsonData = container.data;
                Debug.Log($"전투용 프리셋 {presetIndex + 1} 선택됨: {container.data}");
            }
        }
         else
        {
            Debug.LogWarning($"유효하지 않은 프리셋 인덱스({presetIndex}) 또는 해당 프리셋이 존재하지 않습니다.");
        }
    }

    public string GetSelectedPresetOid(int presetIndex)
    {
        if (presetIndex >= 0 && presetIndex < presetJsonDataList.Count && !string.IsNullOrEmpty(presetJsonDataList[presetIndex]))
        {
            JSONArray ret = new JSONArray();
            JSONNode json = JSON.Parse(presetJsonDataList[presetIndex]);

            if (json != null && json.IsArray)
            {
                foreach (JSONNode node in json.AsArray)
                {
                    if (node != null && node["oid"] != null)
                    {
                       ret.Add(node["oid"].Value);
                    } else {
                        Debug.LogWarning($"프리셋 {presetIndex + 1}의 JSON 데이터 항목에 'oid'가 없거나 null입니다.");
                    }
                }
                return ret.ToString();
            } else {
                 Debug.LogWarning($"프리셋 {presetIndex + 1}의 JSON 데이터가 배열 형식이 아닙니다: {presetJsonDataList[presetIndex]}");
            }
        }
         else
        {
             Debug.LogWarning($"유효하지 않은 프리셋 인덱스({presetIndex}) 또는 해당 JSON 데이터가 비어있습니다.");
        }
        return "[]";
    }

    private void ClearAllPresetObjects()
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

    public void ClearAllPresetsAndData()
    {
        ClearAllPresetObjects();
        presetJsonDataList.Clear();

        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"모든 프리셋 데이터와 저장 파일({Path.GetFileName(savePath)})이 삭제되었습니다.");
        }
    }

    #endregion
}