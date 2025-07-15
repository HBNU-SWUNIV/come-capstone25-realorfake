using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using AG.Network.AGLobby;
using System.Threading.Tasks;
using Unity.Services.Authentication;

public class PresetSelectionUI : MonoBehaviour
{
    public GameObject presetButtonPrefab;
    public Transform buttonContainer;
    public Button confirmButton;
    public Button cancelButton;
    public Button QuickMatchButton;
    public Button CreateLobbyButton;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI LobbyText;
    public TextMeshProUGUI PlayerText;
    public TextMeshProUGUI LogText;
    public float buttonSpacing = 10f; // 버튼 간 간격

    private PresetManager presetManager;
    private List<Button> presetButtons = new List<Button>();

    void Start()
    {
        presetManager = FindAnyObjectByType<PresetManager>();
        if (presetManager == null)
        {
            Debug.LogError("PresetManager를 찾을 수 없습니다!");
            return;
        }

        // 인증 상태 확인 및 텍스트 초기화
        if (LobbySingleton.instance != null)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                if (LobbyText != null) LobbyText.text = $"인증됨 (ID: {AuthenticationService.Instance.PlayerId})\n";
                if (PlayerText != null) PlayerText.text = "로비 참가 대기 중...\n";
                if (LogText != null) LogText.text = "시스템 준비 완료\n";
            }
            else
            {
                if (LobbyText != null) LobbyText.text = "인증되지 않음\n";
                if (PlayerText != null) PlayerText.text = "인증이 필요합니다\n";
                if (LogText != null) LogText.text = "시스템 초기화 중...\n";
            }
        }
        else
        {
            if (LobbyText != null) LobbyText.text = "LobbySingleton을 찾을 수 없음\n";
            if (PlayerText != null) PlayerText.text = "시스템 오류\n";
            if (LogText != null) LogText.text = "초기화 실패\n";
        }

        SetupUI();
        SetupLobbyButtons();
    }

    void SetupUI()
    {
        // 기존 버튼들 제거
        foreach (var button in presetButtons)
        {
            Destroy(button.gameObject);
        }
        presetButtons.Clear();

        // 버튼 컨테이너의 RectTransform 가져오기
        RectTransform containerRect = buttonContainer.GetComponent<RectTransform>();
        float containerWidth = containerRect.rect.width;
        float buttonWidth = presetButtonPrefab.GetComponent<RectTransform>().rect.width;
        
        // 버튼들의 시작 위치 계산 (중앙 정렬)
        float startX = -(containerWidth - buttonWidth) / 2f;

        // 프리셋 버튼 생성
        for (int i = 0; i < 3; i++)
        {
            GameObject buttonObj = Instantiate(presetButtonPrefab, buttonContainer);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            
            // 버튼 위치 설정
            buttonRect.anchoredPosition = new Vector2(startX + (buttonWidth + buttonSpacing) * i, 0);
            
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                buttonText.text = $"Preset {i + 1}";
            }

            int presetIndex = i; // 클로저를 위한 로컬 변수
            button.onClick.AddListener(() => OnPresetSelected(presetIndex));
            
            presetButtons.Add(button);
        }

        // 확인 버튼 설정
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        // 취소 버튼 설정
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    void SetupLobbyButtons()
    {
        if (CreateLobbyButton != null)
        {
            CreateLobbyButton.onClick.AddListener(() => {
                LobbySingleton.instance.CreateLobby("test", 2, false);
                if (LobbyText != null) LobbyText.text += "Created Lobby\n";
                if (LogText != null) LogText.text += "Created Lobby\n";
            });
        }

        if (QuickMatchButton != null)
        {
            QuickMatchButton.onClick.AddListener(() => {
                LobbySingleton.instance.QuickMatch();
                if (LobbyText != null) LobbyText.text += "Quick Match\n";
                if (LogText != null) LogText.text += "Quick Match\n";
            });
        }
    }

    void OnPresetSelected(int presetIndex)
    {
        // 선택된 버튼 하이라이트
        for (int i = 0; i < presetButtons.Count; i++)
        {
            presetButtons[i].GetComponent<Image>().color = (i == presetIndex) ? Color.yellow : Color.white;
        }
    }

    void OnConfirmClicked()
    {
        // 선택된 프리셋 찾기
        int selectedIndex = -1;
        for (int i = 0; i < presetButtons.Count; i++)
        {
            if (presetButtons[i].GetComponent<Image>().color == Color.yellow)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex != -1)
        {
            presetManager.SelectPresetForFight(selectedIndex);
            LobbySingleton.instance.StartGame();
            if (LobbyText != null) LobbyText.text += "Game Start\n";
            if (LogText != null) LogText.text += "Game Start\n";
            // 씬 전환 로직 호출
            FindAnyObjectByType<SceneChanger>()?.ChangeScene("FightScene");
        }
        else
        {
            Debug.LogWarning("프리셋이 선택되지 않았습니다!");
        }
    }

    void OnCancelClicked()
    {
        gameObject.SetActive(false);
    }
} 