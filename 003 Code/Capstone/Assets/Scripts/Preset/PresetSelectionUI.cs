using AG.Network.AGLobby;
using OVRSimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGLTF.Interactivity.Schema;

public class PresetSelectionUI : MonoBehaviour
{
    public GameObject presetButtonPrefab;
    public Transform buttonContainer;
    public Button confirmButton;
    public Button cancelButton;
    public Button QuickMatchButton;
    public Button CreateLobbyButton;

    public Text titleText;
    public Text LobbyText;
    public Text PlayerText;
    public Text LogText;
    public float buttonSpacing = 10f; // 버튼 간 간격

    private PresetManager presetManager;
    private List<Button> presetButtons = new List<Button>();
    private FileManager fileManager;

    // 게임 종료 후 새로 로드되면서 이거 초기화 되는지 확인필요
    private bool isCoroutineStarted = false;

    void Start()
    {
        presetManager = FindAnyObjectByType<PresetManager>();
        if (presetManager == null)
        {
            Debug.LogError("PresetManager를 찾을 수 없습니다!");
            return;
        }

        fileManager = FindAnyObjectByType<FileManager>();
        if (fileManager == null)
        {
            Debug.LogError("FileManager 찾을 수 없습니다!");
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
        StartCoroutine(WaitLobby());
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
            confirmButton.interactable = false;
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
            CreateLobbyButton.onClick.AddListener(() => { FindAnyObjectByType<PlayerDataManager>().StartCompareSessionCoroutine(OnCreateLobbyClicked); });
            //CreateLobbyButton.onClick.AddListener(() => { OnCreateLobbyClicked(1); });
        }

        if (QuickMatchButton != null)
        {
            QuickMatchButton.onClick.AddListener(() => { FindAnyObjectByType<PlayerDataManager>().StartCompareSessionCoroutine(OnQuickMatchClicked); });
            //QuickMatchButton.onClick.AddListener(() => { OnQuickMatchClicked(1); });
        }
    }

    void OnCreateLobbyClicked(int a)
    {
        LobbySingleton.instance.CreateLobby("test", 2, false);
        if (LobbyText != null) LobbyText.text += "Created Lobby\n";
        if (LogText != null) LogText.text += "Created Lobby\n";
        QuickMatchButton.GetComponent<Button>().interactable = false;
        confirmButton.interactable = true;
    }

    void OnQuickMatchClicked(int a)
    {
        LobbySingleton.instance.QuickMatch(LobbyText);
        if (LobbyText != null) LobbyText.text += "Quick Match\n";
        if (LogText != null) LogText.text += "Quick Match\n";
    }

    void OnPresetSelected(int presetIndex)
    {
        // 선택된 버튼 하이라이트
        for (int i = 0; i < presetButtons.Count; i++)
        {
            presetButtons[i].GetComponent<Image>().color = (i == presetIndex) ? Color.yellow : Color.white;
        }
    }

    public int GetSelectedIndex()
    {
        int selectedIndex = -1;
        for (int i = 0; i < presetButtons.Count; i++)
        {
            if (presetButtons[i].GetComponent<Image>().color == Color.yellow)
            {
                selectedIndex = i;
                break;
            }
        }
        return selectedIndex;
    }

    async void OnConfirmClicked()
    {
        Debug.Log("OnConfirmClicked");
        // 선택된 프리셋 찾기
        int selectedIndex = GetSelectedIndex();

        if (selectedIndex != -1)
        {
            presetManager.SelectPresetForFight(selectedIndex);
            if (LobbyText != null) LobbyText.text += "Game Start\n";
            if (LogText != null) LogText.text += "Game Start\n";
            await LobbySingleton.instance.MigrateHostAgain();
            LobbySingleton.instance.StartGame();
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
        LobbySingleton.instance.LeaveLobby();
        gameObject.SetActive(false);
    }

    // 클라이언트 로비 입장 시
    // Yes 버튼 비활성화, 5초 뒤 Cancel도 비활성화
    // 이후 서로의 OID 공유 -> LobbyOptions 이용 -> DB에서 모델링 다운로드
    public IEnumerator StartConfirmCountdown(bool createdLobby)
    {
        // 1. confirmButton 비활성화
        if (confirmButton != null) 
            confirmButton.interactable = false;
        
        int seconds = 5;

        // 2. confirm 버튼 텍스트에 카운트다운 표시
        while (seconds > 0)
        {
            if (confirmButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = $"{seconds}";
            yield return new WaitForSeconds(1f);
            seconds--;
        }

        if (confirmButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "게임 시작";

        // 3. 5초 뒤 cancelButton도 비활성화
        if (cancelButton != null) cancelButton.interactable = false;

        // 오브젝트 다운 로직
        if (LogText != null) 
            LogText.text = "모델링 다운을 시작합니다\n";
        // 1. OID 공유 -> LobbySingleton에서 진행

        // 2. 다운
        List<string> oids = new List<string>();
        if (createdLobby)
        {
            if (LobbySingleton.instance.joinedLobby.Data.TryGetValue("ClientOid", out DataObject dataObject))
            {
                string oidList = dataObject.Value;
                Debug.Log(oidList);
                JSONNode oid = JSON.Parse(oidList);
                foreach (JSONNode node in oid.AsArray)
                {
                    string oidstr = node.ToString();
                    fileManager.DownloadFile(Regex.Replace(oidstr, @"\D", ""));
                    oids.Add(Regex.Replace(oidstr, @"\D", ""));
                }
            }
        }
        else
        {
            if (LobbySingleton.instance.joinedLobby.Data.TryGetValue("HostOid", out DataObject dataObject))
            {
                string oidList = dataObject.Value;
                Debug.Log(oidList);
                JSONNode oid = JSON.Parse(oidList);
                foreach (JSONNode node in oid.AsArray)
                {
                    string oidstr = node.ToString();
                    fileManager.DownloadFile(Regex.Replace(oidstr, @"\D", ""));
                    oids.Add(Regex.Replace(oidstr, @"\D", ""));
                }
            }
        }


            // 3. 다운 완료 후 게임 시작

            StartCoroutine(WaitDownload(oids));
    }

    public void StartGameStartCoroutine(bool createLobby)
    {
        if (isCoroutineStarted) 
            return;
        isCoroutineStarted = true;
        StartCoroutine(StartConfirmCountdown(createLobby));
    }

    public IEnumerator WaitLobby()
    {
        CreateLobbyButton.interactable = false;
        QuickMatchButton.interactable = false;
        confirmButton.interactable = false;
        cancelButton.interactable = false;

        int seconds = 10;
        while (seconds > 0)
        {
            LogText.text = $"{seconds}초 이후 활성화";
            yield return new WaitForSeconds(1);
            seconds--;
        }
        LogText.text = "";
        CreateLobbyButton.interactable = true;
        QuickMatchButton.interactable = true;
        confirmButton.interactable = true;
        cancelButton.interactable = true;
    }

    public IEnumerator WaitDownload(List<string> list)
    {
        Debug.Log($"WaitDownload List Count : {list.Count}");
        List<string> pathStrings = new List<string>();
        foreach (string str in list)
        {
            string savePath = Path.Combine(Application.persistentDataPath, "objects", str + ".glb");
            Debug.Log($"savePath : {savePath}");
            pathStrings.Add(savePath);
        }

        int FileCount = pathStrings.Count;
        int curCount = 0;

        while (true)
        {
            curCount = 0;
            foreach(string str in pathStrings)
            {
                if (File.Exists(str))
                {
                    curCount++;
                }
            }

            Debug.Log($"Downloaded Count : {curCount}");

            if (curCount == FileCount)
            {
                break;
            }

            if (LogText != null)
                LogText.text += $"{curCount} / {FileCount} 다운 완료\n";

            yield return new WaitForSeconds(1);
        }

        OnConfirmClicked();
    }
} 