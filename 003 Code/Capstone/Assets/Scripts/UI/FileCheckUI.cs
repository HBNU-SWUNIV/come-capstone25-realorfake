using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class FileCheckUI : MonoBehaviour
{
    [Header("UI Components")]
    public Text logTitle;        // "GLB 파일생성에 실패하였습니다!" 텍스트
    public Text logDetail;       // Unity 콘솔 로그를 표시할 텍스트
    public Button closeButton;              // UI 종료 버튼

    [Header("Settings")]
    public int maxLogLines = 20;            // 표시할 최대 로그 라인 수
    public float logUpdateInterval = 0.5f;  // 로그 업데이트 간격

    private List<string> logMessages = new List<string>();
    private bool isInitialized = false;

    void Start()
    {
        InitializeUI();
        StartLogCapture();
    }

    // UI 초기화
    public void InitializeUI()
    {
        if (isInitialized) return;

        // 컴포넌트 찾기
        if (logTitle == null)
            logTitle = transform.Find("LogTitle")?.GetComponent<Text>();
        
        if (logDetail == null)
            logDetail = transform.Find("LogDetail")?.GetComponent<Text>();
        
        if (closeButton == null)
            closeButton = transform.Find("Button")?.GetComponent<Button>();

        // 버튼 이벤트 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // 초기 텍스트 설정
        if (logTitle != null)
        {
            logTitle.text = "GLB 파일생성에 실패하였습니다!";
        }

        if (logDetail != null)
        {
            logDetail.text = "로그를 수집 중입니다...";
        }

        isInitialized = true;
    }

    // 로그 캡처 시작
    public void StartLogCapture()
    {
        // Unity 콘솔 로그를 캡처하기 위해 Application.logMessageReceived 이벤트 등록
        Application.logMessageReceived += OnLogMessageReceived;
        
        // 주기적으로 로그 업데이트
        InvokeRepeating(nameof(UpdateLogDisplay), 0f, logUpdateInterval);
    }

    // 로그 메시지 수신 처리
    private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        // 에러나 경고 로그만 수집 (선택적)
        if (type == LogType.Error || type == LogType.Warning || type == LogType.Exception)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {type}: {logString}";
            
            logMessages.Add(logEntry);
            
            // 최대 라인 수 제한
            if (logMessages.Count > maxLogLines)
            {
                logMessages.RemoveAt(0);
            }
        }
    }

    // 로그 표시 업데이트
    private void UpdateLogDisplay()
    {
        if (logDetail != null && logMessages.Count > 0)
        {
            string displayText = string.Join("\n", logMessages.ToArray());
            logDetail.text = displayText;
        }
        else if (logDetail != null && logMessages.Count == 0)
        {
            logDetail.text = "로그가 없습니다.\nGLB 파일 생성 과정에서 오류가 발생했습니다.";
        }
    }

    // 특정 로그 메시지 추가 (외부에서 호출 가능)
    public void AddLogMessage(string message, LogType type = LogType.Error)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {type}: {message}";
        
        logMessages.Add(logEntry);
        
        if (logMessages.Count > maxLogLines)
        {
            logMessages.RemoveAt(0);
        }
    }

    // GLB 파일 체크 결과 설정
    public void SetFileCheckResult(bool success, string filePath = "")
    {
        if (logTitle != null)
        {
            if (success)
            {
                logTitle.text = "GLB 파일생성에 성공하였습니다!";
            }
            else
            {
                logTitle.text = "GLB 파일생성에 실패하였습니다!";
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    AddLogMessage($"파일 경로: {filePath}");
                }
            }
        }
    }

    // UI 종료 버튼 클릭 처리
    public void OnCloseButtonClicked()
    {
        CloseUI();
    }

    // UI 종료
    public void CloseUI()
    {
        // 로그 캡처 중지
        Application.logMessageReceived -= OnLogMessageReceived;
        CancelInvoke(nameof(UpdateLogDisplay));
        
        // UI 제거
        Destroy(gameObject);
    }

    // 외부에서 UI 종료 호출 가능
    public static void CloseFileCheckUI()
    {
        FileCheckUI fileCheckUI = FindObjectOfType<FileCheckUI>();
        if (fileCheckUI != null)
        {
            fileCheckUI.CloseUI();
        }
    }

    void OnDestroy()
    {
        // 이벤트 해제
        Application.logMessageReceived -= OnLogMessageReceived;
    }
} 