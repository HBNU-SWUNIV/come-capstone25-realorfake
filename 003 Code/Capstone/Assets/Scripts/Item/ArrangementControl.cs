using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Add this
using SimpleJSON; // JSON 파싱을 위해 추가

public class ArrangementControl : MonoBehaviour
{
    private CardManager cardManager;
    private CardArrangement cardArrangement;
    private XRRayInteractor rayInteractor;
    private int currentPage = 0;
    private const int CARDS_PER_PAGE = 18;
    private InputDevice leftController;
    private InputDevice rightController;
    private bool isLeftControllerInitialized = false;
    private bool isRightControllerInitialized = false;
    [SerializeField] private float rayDistance = 10f;

    private PresetManager presetManager;

    // --- [추가됨] 프리셋 하이라이트 관련 변수 ---
    public Color presetHighlightColor = Color.cyan; // 하이라이트 색상 (Inspector에서 변경 가능)
    private GameObject currentlyHoveredPreset = null; // 현재 레이가 조준 중인 프리셋
    private List<Outline> currentlyHighlightedOutlines = new List<Outline>(); // 현재 하이라이트된 카드들의 Outline 컴포넌트
    // ---------------

    void Start()
    {
        // [수정됨] 씬 이름을 확인하여 StartScene이 아닐 때만 초기화 진행
        if (SceneManager.GetActiveScene().name != "StartScene")
        {
            InitializeComponents();
            InitializeControllers();

            // --- [추가됨] ---
            // PresetManager 인스턴스 찾기
            presetManager = FindObjectOfType<PresetManager>();
            if (presetManager == null)
            {
                Debug.LogError("ArrangementControl: PresetManager를 찾을 수 없습니다!");
            }
            // ---------------
        }
    }

    private void InitializeComponents()
    {
        // [수정됨] GameObject.Find 대신 FindObjectOfType 사용 및 null 체크 강화
        cardManager = FindObjectOfType<CardManager>();
        if (cardManager == null)
        {
            Debug.LogError("CardManager를 찾을 수 없습니다!");
            // return; // Start 메서드이므로 여기서 바로 return 하기보다 다른 초기화는 계속 진행
        }

        rayInteractor = GetComponent<XRRayInteractor>();
        if (rayInteractor == null)
        {
            Debug.LogError("XRRayInteractor를 찾을 수 없습니다!");
            // return;
        }
    }

    private void InitializeControllers()
    {
        var inputDevices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, inputDevices);
        if (inputDevices.Count > 0)
        {
            leftController = inputDevices[0];
            isLeftControllerInitialized = true;
        }
        else
        {
            StartCoroutine(InitializeLeftController());
        }

        inputDevices.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);
        if (inputDevices.Count > 0)
        {
            rightController = inputDevices[0];
            isRightControllerInitialized = true;
        }
        else
        {
            StartCoroutine(InitializeRightController());
        }
    }

    private System.Collections.IEnumerator InitializeLeftController()
    {
        while (!isLeftControllerInitialized)
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, inputDevices);
            if (inputDevices.Count > 0)
            {
                leftController = inputDevices[0];
                isLeftControllerInitialized = true;
            }
            yield return null;
        }
    }

    private System.Collections.IEnumerator InitializeRightController()
    {
        while (!isRightControllerInitialized)
        {
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);
            if (inputDevices.Count > 0)
            {
                rightController = inputDevices[0];
                isRightControllerInitialized = true;
            }
            yield return null;
        }
    }

    void Update()
    {
        // cardManager가 null이면 Update 로직 실행 중단 (오류 방지)
        if (cardManager == null) return;
        // PresetManager가 없으면 프리셋 삭제 로직 실행 불가
        // if (presetManager == null) return; // 주석 처리: 다른 기능은 작동해야 할 수 있음

        // allCardData 로드 확인 강화
        if (cardManager.allCardData == null) return;

        if (cardArrangement == null)
        {
            // [수정됨] FindObjectOfType 사용
            cardArrangement = FindObjectOfType<CardArrangement>();
            // [수정됨] cardArrangement가 null이어도 UpdateCardDisplay 호출 시도 방지
            if (cardArrangement != null)
            {
                 UpdateCardDisplay();
            }
        }

        // --- [수정됨] 레이캐스트 로직: 히트 여부에 따라 하이라이트 처리 ---
        if (CastRay(out RaycastHit hit))
        {
            HandleRaycastHit(hit.collider.gameObject);
        }
        else
        {
            // 레이가 아무것도 맞추지 못하면 기존 하이라이트 제거
            HandleRaycastMiss();
        }
        // ---------------
    }

    // [수정됨] 히트 시 처리 로직
    private void HandleRaycastHit(GameObject hitObject)
    {
        // 페이지 넘김 UI 처리
        if (hitObject.layer == LayerMask.NameToLayer("InteractiveUI"))
        {
            // 기존 UI 하이라이트가 있다면 제거 (선택적)
            StopPresetHighlight();
            if (IsAnyTriggerPressed())
            {
                switch (hitObject.tag)
                {
                    case "NextItem": ShowNextPage(); break;
                    case "PrevItem": ShowPreviousPage(); break;
                    case "QuitItem": DeactivateAll(); break;
                }
            }
        }
        // 프리셋 삭제 버튼 처리
        else if (hitObject.CompareTag("PresetDel"))
        {
            // 기존 프리셋 하이라이트가 있다면 제거
            StopPresetHighlight();
            if (IsAnyTriggerPressed())
            {
                if (presetManager == null)
                {
                    Debug.LogError("ArrangementControl: PresetManager 참조가 없습니다!");
                    return;
                }
                Transform presetToDelete = hitObject.transform.parent;
                if (presetToDelete != null)
                {
                    presetManager.DeletePreset(presetToDelete.gameObject);
                }
                else
                {
                    Debug.LogWarning("삭제 버튼의 부모(프리셋 오브젝트)를 찾을 수 없습니다.");
                }
            }
        }
        // --- [수정됨] 태그 변경: "PresetDisplay" -> "Preset" ---
        else if (hitObject.CompareTag("Preset")) // 프리팹에 설정한 "Preset" 태그 사용
        // ---------------
        {
            // 이전에 다른 프리셋을 조준하고 있었다면 그 하이라이트 제거
            if (currentlyHoveredPreset != hitObject)
            {
                StopPresetHighlight();
                StartPresetHighlight(hitObject);
            }
            // 이미 현재 프리셋을 조준 중이면 아무것도 안 함
        }
        // 그 외의 오브젝트를 맞췄을 경우
        else
        {
            // 기존 하이라이트 제거
            StopPresetHighlight();
        }
    }

    // --- [추가됨] 레이가 아무것도 맞추지 못했을 때 처리 ---
    private void HandleRaycastMiss()
    {
        // 모든 프리셋 하이라이트 제거
        StopPresetHighlight();
    }
    // ---------------

    // --- [추가됨] 프리셋 카드 하이라이트 시작 ---
    private void StartPresetHighlight(GameObject presetObject)
    {
        if (cardManager == null || cardManager.currentActiveCards == null) return;

        currentlyHoveredPreset = presetObject; // 현재 조준 중인 프리셋 저장

        // 1. 프리셋에서 JSON 데이터 가져오기
        JSONDataContainer container = presetObject.GetComponent<JSONDataContainer>();
        if (container == null || string.IsNullOrEmpty(container.data)) return;

        JSONNode presetJson = JSON.Parse(container.data);
        if (presetJson == null || !presetJson.IsArray) return;

        // 2. 프리셋에 포함된 카드 OID 목록 만들기
        List<string> oidsInPreset = new List<string>();
        foreach (JSONNode cardNode in presetJson.AsArray)
        {
            if (cardNode != null && cardNode["oid"] != null)
            {
                oidsInPreset.Add(cardNode["oid"].Value);
            }
        }

        // 3. 현재 활성화된 카드(ItemCase 영역) 중에서 OID가 일치하는 카드 찾기
        currentlyHighlightedOutlines.Clear(); // 이전 하이라이트 목록 초기화
        foreach (GameObject activeCard in cardManager.currentActiveCards)
        {
            if (activeCard == null) continue;

            CardDisplay cardDisplay = activeCard.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                string cardOid = cardDisplay.GetObjectId();
                if (oidsInPreset.Contains(cardOid))
                {
                    // 4. 일치하는 카드의 Outline 컴포넌트 활성화 및 설정
                    Outline outline = activeCard.GetComponent<Outline>();
                    if (outline == null)
                    {
                        outline = activeCard.AddComponent<Outline>();
                    }
                    outline.enabled = true;
                    outline.OutlineColor = presetHighlightColor;
                    outline.OutlineWidth = 5f; // 원하는 두께로 설정
                    currentlyHighlightedOutlines.Add(outline); // 하이라이트된 Outline 컴포넌트 저장
                }
            }
        }
        // Debug.Log($"프리셋 '{presetObject.name}' 하이라이트 시작. {currentlyHighlightedOutlines.Count}개의 카드 강조됨.");
    }
    // ---------------

    // --- [추가됨] 프리셋 카드 하이라이트 중지 ---
    private void StopPresetHighlight()
    {
        if (currentlyHoveredPreset != null)
        {
            // Debug.Log($"프리셋 '{currentlyHoveredPreset.name}' 하이라이트 중지.");
        }

        // 저장된 모든 Outline 컴포넌트 비활성화
        foreach (Outline outline in currentlyHighlightedOutlines)
        {
            if (outline != null) // Outline 컴포넌트가 파괴되었을 수 있으므로 null 체크
            {
                outline.enabled = false;
            }
        }
        currentlyHighlightedOutlines.Clear(); // 목록 비우기
        currentlyHoveredPreset = null; // 현재 조준 중인 프리셋 초기화
    }
    // ---------------


    private bool CastRay(out RaycastHit hit)
    {
        // rayInteractor가 null이면 false 반환 (오류 방지)
        if (rayInteractor == null)
        {
            hit = default; // 기본값으로 초기화
            return false;
        }
        // TryGetCurrent3DRaycastHit 사용
        return rayInteractor.TryGetCurrent3DRaycastHit(out hit);
    }

    bool IsAnyTriggerPressed()
    {
        bool leftTrigger = false;
        bool rightTrigger = false;

        // isInitialized 플래그 확인 추가
        if (isLeftControllerInitialized && leftController.isValid)
        {
            leftController.TryGetFeatureValue(CommonUsages.triggerButton, out leftTrigger);
        }

        if (isRightControllerInitialized && rightController.isValid)
        {
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out rightTrigger);
        }

        return leftTrigger || rightTrigger;
    }

    private void ShowNextPage()
    {
        // cardManager 및 allCardData null 체크 추가
        if (cardManager == null || cardManager.allCardData == null) return;

        int totalCards = cardManager.allCardData.Count;
        int maxPages = Mathf.CeilToInt((float)totalCards / CARDS_PER_PAGE);

        if (currentPage < maxPages - 1)
        {
            currentPage++;
            UpdateCardDisplay();
        }
    }

    private void ShowPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateCardDisplay();
        }
    }

    public void UpdateCardDisplay()
    {
        // cardManager 및 cardArrangement null 체크 추가
        if (cardManager == null || cardArrangement == null) return;

        int startIndex = currentPage * CARDS_PER_PAGE;
        int endIndex = startIndex + CARDS_PER_PAGE;
        cardManager.LoadAndArrangePage(startIndex, endIndex);
    }

    private void DeactivateAll()
    {
        if (cardManager != null)
        {
            // currentActiveCards 리스트가 null이 아닌지 확인
            if (cardManager.currentActiveCards != null)
            {
                foreach (var card in cardManager.currentActiveCards)
                {
                    if (card != null)
                    {
                        card.SetActive(false);
                    }
                }
                cardManager.currentActiveCards.Clear();
            }


            // spawnPoint 및 그 GameObject가 null이 아닌지 확인
            if (cardManager.spawnPoint != null && cardManager.spawnPoint.gameObject != null)
            {
                cardManager.spawnPoint.gameObject.SetActive(false);
            }
        }
    }
}