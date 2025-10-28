using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;
using UnityEngine.Analytics;
using UnityGLTF.Loader;
using UnityGLTF;
using TMPro;
using UnityEngine.Events;

public class WebcamToUI : MonoBehaviour
{
    private static WebcamToUI instance;
    private WebCamTexture webcamTexture;       // 웹캠 텍스처
    private Texture2D videoFrame;             // 1024x1024 크기의 텍스처
    private string baseSavePath = "C:/RecordedFrames/"; // 기본 저장 경로
    private string localPath;  // persistent 경로로
    private string currentSessionPath;        // 현재 세션 폴더 경로
    private volatile bool isCapturing = false;
    private Coroutine captureCoroutine;
    private bool previousTriggerState = false;
    private float lastTriggerTime = 0f;
    private float triggerCooldown = 0.5f;  // 트리거 쿨다운 시간 (초)
    private string fileName;                // 현재 파일 이름 저장

    public Text elapsedTimeText;              // 경과 시간을 표시할 UI 텍스트

    public int captureCount = 0;              // 촬영 횟수 카운트
    public Text captureCountText; // 촬영 횟수를 표시할 UI 텍스트

    public GameObject captureGuide;           // CaptureGuide 프리팹 참조
    public float guideDistance = 0.5f;        // 카메라로부터의 거리

    public GameObject captureCompleteUI;      // 촬영 완료 후 표시할 UI 프리팹         // GLB 파일 생성 실패 시 표시할 UI 프리팹
    public string nextSceneName = "MainScene"; // 다음 씬 이름
    public Canvas targetCanvas;               // UI를 소환할 대상 Canvas
    public GameObject xrOriginPrefab;         // XR Origin 프리팹 참조

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
    private InputDevice rightController;
    private GameObject xrOrigin;

    public static string currentTimestamp;
    public static string firstCapturedFileName = null;

    private Process meshroomProcess; // 외부 프로세스 객체



    /*
     
    todo
    1. 사진 찍고 난 이후, 다시 찍기 또는 나가기 UI 표시
    2. 나가기 시, 씬 전환 -> 폴더명 다음 씬으로 넘겨야 함
    3. 다음 씬에서, 맨 처음 찍은 사진 -> 이미지 분류 서버 보내는거, OID 요청
    4. 받은 폴더명을 기반으로 3D 모델링 호출
    5. OID, 태그 + 랜덤 스탯... DB에 저장할 json 저장
    6. 나중에 3D 모델링이 생길건데...
    (테스트 환경에서는, 우리가 폴더에 파일 넣으면 감지되는걸로 코루틴, 10초에 한 번)
    -> 3D 모델링 파일 생성 과정에서 미리 파일이 생길수도 있음
    -> 언제 완성 되었는가??? 바꿀 수 있다.. 모델링 파일 + 완성 알림 파일 

    1. 클라이언트에서 폴더를 주기적으로 확인해서 모델링 파일이 만들어졌는지 체크
    1.5. UI 띄우기... 아이템이 생성되었습니다...
    
    -> 확인 누르면 DB 저장 로직
    2. 만들어 졌으면 모델링파일, json 파일을 DB에 저장한다. 
     
     */


    void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeWebcam();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // MainScene에서는 WebcamToUI 기능을 비활성화
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainScene")
        {
            Debug.Log("MainScene에서 WebcamToUI 비활성화");
            this.enabled = false; // 컴포넌트 비활성화
            return;
        }
        
        // localPath 초기화
        localPath = Application.persistentDataPath;
        if (!Directory.Exists(localPath))
        {
            Directory.CreateDirectory(localPath);
            Debug.Log("Meshroom 디렉토리 생성됨: " + localPath);
        }

        InitializeUI();
    }

    private void InitializeUI()
    {
        // 씬에서 XR Origin 찾기
        xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            // XR Origin에서 컨트롤러 찾기
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, inputDevices);
            if (inputDevices.Count > 0)
            {
                rightController = inputDevices[0];
            }

            // 레이 인터랙터 찾기
            rayInteractor = xrOrigin.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            if (rayInteractor != null)
            {
                Debug.Log("XR Ray Interactor를 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("XR Ray Interactor를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("씬에서 'XR Origin(Player)'를 찾을 수 없습니다.");
        }

        // 초기 경과 시간 UI 설정
        UpdateElapsedTimeUI(0f);

        // Canvas 설정 확인
        if (targetCanvas != null)
        {
            // Canvas를 World Space로 설정
            targetCanvas.renderMode = RenderMode.WorldSpace;
        }

        // CaptureGuide 위치 설정
        if (captureGuide != null)
        {
            // 메인 카메라 찾기
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 카메라 앞에 CaptureGuide 배치
                captureGuide.transform.position = mainCamera.transform.position + mainCamera.transform.forward * guideDistance;
                captureGuide.transform.rotation = mainCamera.transform.rotation;
            }
        }

        // 촬영 횟수 초기화
        captureCount = 0;
        UpdateCaptureCountUI();
    }

    private void InitializeWebcam()
    {
        // 이전 웹캠 리소스 정리
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            webcamTexture = null;
        }

        Debug.Log("웹캠 초기화 시작...");
        
        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log($"발견된 웹캠 장치 수: {devices.Length}");

        if (devices.Length > 0)
        {
            // 사용 가능한 모든 카메라 정보 출력
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"카메라 {i}: {devices[i].name} (isFrontFacing: {devices[i].isFrontFacing})");
            }

            // 첫 번째 카메라 사용
            string selectedDeviceName = devices[0].name;
            Debug.Log($"선택된 카메라: {selectedDeviceName}");
            
            try
            {
                // 웹캠 텍스처를 생성하고 시작
                webcamTexture = new WebCamTexture(selectedDeviceName);
                webcamTexture.Play();
                
                // 카메라가 시작될 때까지 잠시 대기
                StartCoroutine(WaitForWebcamStart());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"카메라 시작 실패: {e.Message}");
                ShowCameraErrorUI();
            }

            // 1024x1024 크기의 텍스처 생성
            videoFrame = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
        }
        else
        {
            Debug.LogError("사용 가능한 웹캠이 없습니다. 카메라가 연결되어 있는지 확인해주세요.");
            ShowCameraErrorUI();
        }
    }
    
    // 카메라 시작 대기 코루틴
    private IEnumerator WaitForWebcamStart()
    {
        int maxWaitTime = 30; // 최대 30초 대기
        int waitCount = 0;
        
        while (!webcamTexture.isPlaying && waitCount < maxWaitTime)
        {
            yield return new WaitForSeconds(1f);
            waitCount++;
            Debug.Log($"카메라 시작 대기 중... ({waitCount}/{maxWaitTime})");
        }
        
        if (webcamTexture.isPlaying)
        {
            Debug.Log($"카메라 시작 완료: {webcamTexture.width}x{webcamTexture.height}");
        }
        else
        {
            Debug.LogError("카메라 시작 시간 초과");
            ShowCameraErrorUI();
        }
    }
    
    // 카메라 에러 UI 표시
    private void ShowCameraErrorUI()
    {
        Debug.LogError("카메라 에러 UI 표시");
        
        // 간단한 에러 메시지 UI 생성
        if (targetCanvas != null)
        {
            GameObject errorUI = new GameObject("CameraErrorUI");
            errorUI.transform.SetParent(targetCanvas.transform, false);
            
            RectTransform rectTransform = errorUI.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(400, 200);
            
            // 배경 패널
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(errorUI.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 200);
            
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(1, 0, 0, 0.8f); // 빨간색 배경
            
            // 에러 텍스트
            GameObject textObj = new GameObject("ErrorText");
            textObj.transform.SetParent(errorUI.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 0);
            textRect.sizeDelta = new Vector2(400, 150);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "카메라를 찾을 수 없습니다.\n카메라가 연결되어 있는지 확인해주세요.";
            textComponent.fontSize = 20;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
        }
    }

    void Update()
    {
        // MainScene에서는 Update 실행하지 않음
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainScene")
        {
            return;
        }
        
        // CaptureGuide를 카메라를 따라 움직이도록 설정
        if (captureGuide != null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                captureGuide.transform.position = mainCamera.transform.position + mainCamera.transform.forward * guideDistance;
                captureGuide.transform.rotation = mainCamera.transform.rotation;
            }
        }

        // UI가 표시되어 있으면 촬영 기능 비활성화
        if (GameObject.Find("RetakeUI") != null)
        {
            return;
        }

        if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed))
        {
            float currentTime = Time.time;
            
            // 트리거가 방금 눌렸고, 쿨다운 시간이 지났을 때만 실행
            if (isTriggerPressed && !previousTriggerState && (currentTime - lastTriggerTime) > triggerCooldown)
            {
                lastTriggerTime = currentTime;  // 마지막 트리거 시간 업데이트
                
                if (!isCapturing)
                {
                    isCapturing = true;
                    // 이전 코루틴이 있다면 아무것도 하지 않음
                    if (captureCoroutine == null)
                    {
                        // 타임스탬프 기반으로 세션 폴더 생성
                        currentTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                        currentSessionPath = baseSavePath + currentTimestamp + "/";
                        if (!System.IO.Directory.Exists(currentSessionPath))
                        {
                            System.IO.Directory.CreateDirectory(currentSessionPath);
                            Debug.Log("Session folder created: " + currentSessionPath);
                        }

                        // 새로운 코루틴 시작
                        captureCoroutine = StartCoroutine(CaptureRoutine(10f));
                        Debug.Log("Capture started for 10 seconds.");
                    }
                }
                else
                {
                    Debug.Log("이미 캡처가 진행 중입니다.");
                }
            }

            previousTriggerState = isTriggerPressed;
        }
    }

    IEnumerator CaptureRoutine(float duration)
    {

        captureCount = 0; 
        UpdateCaptureCountUI();

        float startTime = Time.time;

        try
        {
            while (Time.time - startTime < duration)
            {
                float elapsedTime = Time.time - startTime;
                UpdateElapsedTimeUI(elapsedTime);

                TakeScreenshot();
                yield return new WaitForSeconds(0.1f);
            }

            UpdateElapsedTimeUI(duration);
            Debug.Log($"Capture stopped after {duration} seconds.");

            // 촬영이 완료되면 UI 표시
            ShowCaptureCompleteUI();
        }
        finally
        {
            isCapturing = false;
            captureCoroutine = null;
        }
    }


    void TakeScreenshot()
    {
        // 카메라 상태 확인
        if (webcamTexture == null || !webcamTexture.isPlaying)
        {
            Debug.LogError("카메라가 준비되지 않았습니다. 스크린샷을 찍을 수 없습니다.");
            return;
        }

        try
        {
            // 웹캠 텍스처에서 픽셀을 가져와서 리사이즈 처리
            RenderTexture rt = RenderTexture.GetTemporary(1024, 1024);
            Graphics.Blit(webcamTexture, rt, new Vector2(1, -1), new Vector2(0, 1));

            RenderTexture.active = rt;
            videoFrame.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
            videoFrame.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            // 첫 사진명 저장해서 이미지 분류 서버에 이용
            fileName = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".jpg";
            System.IO.File.WriteAllBytes(currentSessionPath + fileName, videoFrame.EncodeToJPG());

            // 첫 번째 캡처 파일명 저장
            if (firstCapturedFileName == null)
                firstCapturedFileName = fileName;

            captureCount++; // 촬영 횟수 증가
            UpdateCaptureCountUI();
            Debug.Log("Screenshot saved: " + fileName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"스크린샷 촬영 중 오류 발생: {e.Message}");
        }
    }

    void UpdateElapsedTimeUI(float elapsedTime)
    {
        if (elapsedTimeText != null)
            elapsedTimeText.text = $"경과 시간: {elapsedTime:F2}초";
        else
            Debug.LogWarning("elapsedTimeText가 할당되지 않았습니다.");
    }

    void UpdateCaptureCountUI()
    {
        if (captureCountText != null)
            captureCountText.text = $"촬영 횟수: {captureCount}장";
        else
            Debug.LogWarning("captureCountText가 할당되지 않았습니다.");
    }

    void ShowCaptureCompleteUI()
    {
        if (captureCompleteUI != null && targetCanvas != null)
        {
            GameObject uiInstance = Instantiate(captureCompleteUI, targetCanvas.transform);
            uiInstance.name = "RetakeUI";
            Debug.Log("CaptureCompleteUI 인스턴스화");

            // UI에 XR Interaction 컴포넌트 추가
            CanvasGroup canvasGroup = uiInstance.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // UI의 위치를 카메라 앞으로 설정
            RectTransform rectTransform = uiInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 카메라 앞에 위치시키기
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    // 카메라 앞에 위치시키기
                    Vector3 forward = mainCamera.transform.forward;
                    Vector3 position = mainCamera.transform.position + forward * guideDistance;
                    
                    // UI를 카메라를 향하도록 회전
                    rectTransform.position = position;
                    rectTransform.rotation = Quaternion.LookRotation(forward);
                }
            }
            
            // 각 버튼에 XR Interaction 컴포넌트 추가
            Button[] buttons = uiInstance.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                // XR UI Interaction을 위한 컴포넌트 추가
                XRUIInputModule xrInputModule = button.gameObject.AddComponent<XRUIInputModule>();
                
                if (button.name.Contains("Yes"))
                {
                    button.onClick.AddListener(OnYesButtonClicked);
                }
                else if (button.name.Contains("No"))
                {
                    button.onClick.AddListener(OnNoButtonClicked);
                }
            }

            // CaptureGuide 비활성화
            if (captureGuide != null)
            {
                captureGuide.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("captureCompleteUI 또는 targetCanvas가 설정되지 않았습니다.");
        }
    }

    void OnNoButtonClicked()
    {
        // UI 제거
        GameObject uiInstance = GameObject.Find("RetakeUI");
        if (uiInstance != null)
        {
            Destroy(uiInstance);
        }

        // CaptureGuide 다시 활성화
        if (captureGuide != null)
        {
            captureGuide.SetActive(true);
        }

        // 촬영 카운트 초기화
        captureCount = 0;
        UpdateCaptureCountUI();
    }

    private async void OnYesButtonClicked()
    {
        // 서버에서 OID를 받아와서 CardItemDataManager에 설정
        try
        {
            int oidTask = await CardItemDataManager.GetOID();
            string oid = oidTask.ToString();
            
            CardItemDataManager.lastCreatedOID = oid;
            
            Debug.Log("서버에서 받은 oid: " + oid);
            
            // UI 제거
            GameObject uiInstance = GameObject.Find("RetakeUI");
            if (uiInstance != null)
            {
                Destroy(uiInstance);
            }
            
            // 씬 전환 전에 비동기 작업 시작
            StartCoroutine(RunMeshroomProcessAndLoadScene(oid));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OID 요청 실패: {e.Message}");
            // 에러 처리: 기본값 사용 또는 에러 UI 표시
        }
    }

    private IEnumerator RunMeshroomProcessAndLoadScene(string oidStr)
    {
        // 비동기 작업 시작
        Task<(bool success, string glbFilePath, string errorMessage)> meshroomTask = RunMeshroomProcessAsAdminAsync(oidStr);
        
        // 씬 전환을 즉시 수행
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(nextSceneName);

        // 씬이 완전히 로드될 때까지 대기
        while (!asyncLoad.isDone)
            yield return null;

        // 씬이 로드된 후 CardItemDataManager를 찾아서 동작시킴
        var manager = GameObject.FindObjectOfType<CardItemDataManager>();
        if (manager != null)
        {
            // GLBViewerUI에서 BigClass가 선택될 때까지 대기
            // 여기서는 즉시 호출하지 않고, GLBViewerUI에서 선택된 후 호출되도록 함
            Debug.Log("CardItemDataManager를 찾았습니다. GLBViewerUI에서 BigClass 선택을 기다립니다.");
        }
        else
        {
            Debug.LogError("CardItemDataManager를 찾을 수 없습니다!");
        }

        // Meshroom 프로세스 결과를 메인 스레드에서 처리
        yield return new WaitUntil(() => meshroomTask.IsCompleted);
        
        if (meshroomTask.IsCompletedSuccessfully)
        {
            var result = meshroomTask.Result;
            HandleMeshroomProcessResult(result.success, result.glbFilePath, result.errorMessage);
        }
    }

    void OnEnable()
    {
        // MainScene에서는 OnEnable 실행하지 않음
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainScene")
        {
            return;
        }
        
        // 웹캠이 없을 때만 초기화
        if (webcamTexture == null)
        {
            InitializeWebcam();
        }

        // UI 초기화
        InitializeUI();
    }

    void OnDisable()
    {
        // MainScene에서는 OnDisable 실행하지 않음
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainScene")
        {
            return;
        }
        
        // 씬 전환 시 웹캠은 유지하고 코루틴만 정리
        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        // UI 요소들 정리
        if (captureGuide != null)
        {
            captureGuide.SetActive(false);
        }

        if (captureCompleteUI != null)
        {
            GameObject uiInstance = GameObject.Find("RetakeUI");
            if (uiInstance != null)
            {
                Destroy(uiInstance);
            }
        }
    }

    private void CleanupWebcam()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            webcamTexture = null;
        }

        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }
    }

    void OnDestroy()
    {
        // 실행 중인 코루틴 정리
        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }
        
        // 웹캠 리소스 정리
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            webcamTexture = null;
        }
        
        // 비디오 프레임 텍스처 정리
        if (videoFrame != null)
        {
            DestroyImmediate(videoFrame);
            videoFrame = null;
        }
        
        // 메시룸 프로세스 정리
        if (meshroomProcess != null && !meshroomProcess.HasExited)
        {
            try
            {
                meshroomProcess.Kill();
                meshroomProcess.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"메시룸 프로세스 정리 중 오류: {e.Message}");
            }
            meshroomProcess = null;
        }
        
        Debug.Log("WebcamToUI 리소스 정리 완료");
    }

    /// <summary>
    /// 메시룸 프로세스를 관리자 권한으로 비동기 실행하고 출력을 캡처하는 함수
    /// </summary>
    public async Task<(bool success, string glbFilePath, string errorMessage)> RunMeshroomProcessAsAdminAsync(string oid)
    {
        string exePath = Path.Combine(localPath, "meshroom_process.exe");
        
        if (!File.Exists(exePath))
        {
            return (false, "", "오류: 'meshroom_process.exe' 파일이 다음 경로에 없습니다: " + exePath);
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{currentSessionPath}\" \"{oid}\"",  // 두 번째 매개변수로 Resources 경로 전달
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            meshroomProcess = Process.Start(startInfo);
            
            if (meshroomProcess == null)
            {
                return (false, "", "프로세스 시작 실패");
            }

            await Task.Run(() => meshroomProcess.WaitForExit());
            await Task.Yield();

            if (meshroomProcess.ExitCode == 0)
            {
                // GLB 파일 생성 여부 확인
                string objectsDir = Path.Combine(localPath, "objects");
                string glbFilePath = Path.Combine(objectsDir, $"{oid}.glb");
                
                if (File.Exists(glbFilePath))
                {
                    return (true, glbFilePath, "");
                }
                else
                {
                    return (false, glbFilePath, $"GLB 파일 생성 실패: {glbFilePath} 파일이 존재하지 않습니다.");
                }
            }
            else
            {
                return (false, "", "메시룸 프로세스 종료 실패");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            if (ex.NativeErrorCode == 1223) // 사용자가 UAC 프롬프트를 취소한 경우
            {
                return (false, "", "관리자 권한 요청이 취소되었습니다.");
            }
            else
            {
                return (false, "", $"[Unity] 메시룸 프로세스 실행 중 Win32 오류 발생: {ex.Message} (Error Code: {ex.NativeErrorCode})");
            }
        }
        catch (System.Exception ex)
        {
            if (meshroomProcess != null)
            {
                if (!meshroomProcess.HasExited)
                {
                    meshroomProcess.Kill();
                }
                meshroomProcess.Dispose();
                meshroomProcess = null;
            }
            return (false, "", $"[Unity] 메시룸 프로세스 실행 실패: {ex.Message}");
        }
    }

    // 메인 스레드에서 UI를 표시하는 메서드
    private void HandleMeshroomProcessResult(bool success, string glbFilePath, string errorMessage)
    {
        MeshroomResult.HasResult = true;
        MeshroomResult.Success = success;
        MeshroomResult.GlbFilePath = glbFilePath;
        MeshroomResult.ErrorMessage = errorMessage;
    }


    void OnApplicationQuit()
    {
        // 프로세스가 아직 실행 중이면 종료
        if (meshroomProcess != null && !meshroomProcess.HasExited)
        {
            try
            {
                meshroomProcess.Kill();
                meshroomProcess.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"프로세스 종료 중 오류 발생: {e.Message}");
            }
        }
    }

    // public void GLBLoader(string oid)
    // {   
    //     // objects 폴더 경로
    //     string objectsDir = Path.Combine(localPath, "objects");
    //     string glbFilePath = Path.Combine(objectsDir, $"{oid}.glb");

    //     UnityEngine.Debug.Log($"[Unity] 로드할 .glb 파일: {glbFilePath}");

    //     if (!Directory.Exists(objectsDir))
    //     {
    //         UnityEngine.Debug.LogError("[Unity] objects 디렉토리를 찾을 수 없습니다: " + objectsDir);
    //         return;
    //     }

    //     if (!File.Exists(glbFilePath))
    //     {
    //         UnityEngine.Debug.LogError("[Unity] GLB 파일이 존재하지 않습니다: " + glbFilePath);
    //         return;
    //     }

    //     // GLB 로딩은 코루틴으로 비동기 실행 권장
    //     StartCoroutine(LoadGLBModelCoroutine(glbFilePath));
    // }

    private IEnumerator LoadGLBModelCoroutine(string glbFilePath)
    {
        var loader = new FileLoader(Path.GetDirectoryName(glbFilePath));
        var importer = new GLTFSceneImporter(
            Path.GetFileName(glbFilePath),
            new ImportOptions { DataLoader = loader }
        );
        importer.SceneParent = new GameObject("LoadedGLB").transform;

        UnityEngine.Debug.Log("GLB 로딩 시작: " + glbFilePath);

        var loadSceneTask = importer.LoadSceneAsync();
        while (!loadSceneTask.IsCompleted)
        {
            yield return null;
        }

        if (loadSceneTask.Exception != null)
        {
            UnityEngine.Debug.LogError("GLB 로딩 실패: " + loadSceneTask.Exception.Message);
        }
        else
        {
            UnityEngine.Debug.Log("GLB 로딩 완료");
            
            // GLB 로딩 완료 후 결과 표시 UI 생성
            ShowGLBResultUI(importer.SceneParent.gameObject);
            
            // CardObjectSpawner에 로딩 완료 알림
            CardObjectSpawner cardSpawner = FindAnyObjectByType<CardObjectSpawner>();
            if (cardSpawner != null)
            {
                // GLB 파일명에서 objectId 추출
                string fileName = Path.GetFileNameWithoutExtension(glbFilePath);
                GameObject loadedObject = importer.SceneParent.gameObject;
                
                // CardObjectSpawner의 ReplaceTempObjectWithGLB 메서드 호출
                cardSpawner.ReplaceTempObjectWithGLB(fileName, loadedObject);
            }
        }
    }

    // GLB 결과를 표시하는 UI 생성
    private void ShowGLBResultUI(GameObject loadedObject)
    {
        // MainScene에서 Canvas를 동적으로 찾기
        Canvas targetCanvas = FindAnyObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogError("MainScene에서 Canvas를 찾을 수 없습니다!");
            return;
        }

        // Canvas를 Screen Space - Overlay로 설정 (더 간단한 UI 표시)
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // GLB 결과 표시용 UI 생성
        GameObject resultUI = new GameObject("GLBResultUI");
        resultUI.transform.SetParent(targetCanvas.transform, false);
        
        // UI 컴포넌트 추가
        RectTransform rectTransform = resultUI.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(400, 200);
        
        // 배경 패널 추가
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(resultUI.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(400, 200);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // 텍스트 추가
        GameObject textObj = new GameObject("ResultText");
        textObj.transform.SetParent(resultUI.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 30);
        textRect.sizeDelta = new Vector2(400, 100);
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = "3D 모델이 생성되었습니다!\n저장하시겠습니까?";
        textComponent.fontSize = 24;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        
        // 버튼들 생성
        CreateButton(resultUI, "Yes", new Vector2(-100, -50), OnSaveGLBYes);
        CreateButton(resultUI, "No", new Vector2(100, -50), OnSaveGLBNo);
        
        // 로드된 오브젝트를 UI와 함께 저장 (간단한 방식)
        resultUI.name = "GLBResultUI_" + loadedObject.name;
    }

    // 버튼 생성 헬퍼 메서드
    private void CreateButton(GameObject parent, string text, Vector2 position, UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(text + "Button");
        buttonObj.transform.SetParent(parent.transform, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(80, 40);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        
        // 버튼 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(80, 40);
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 18;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
    }
    
    // 저장 Yes 버튼 클릭 시
    private void OnSaveGLBYes()
    {
        Debug.Log("GLB 저장을 선택했습니다.");
        // CardItemDataManager를 찾아서 저장 프로세스 시작
        var manager = FindAnyObjectByType<CardItemDataManager>();
        if (manager != null)
        {
            // GLBViewerUI에서 BigClass가 선택된 후 호출되도록 함
            // 여기서는 즉시 호출하지 않음
            Debug.Log("CardItemDataManager를 찾았습니다. GLBViewerUI에서 BigClass 선택을 기다립니다.");
        }
        else
        {
            Debug.LogError("CardItemDataManager를 찾을 수 없습니다!");
        }
        
        // UI 제거
        GameObject resultUI = GameObject.Find("GLBResultUI");
        if (resultUI != null)
        {
            Destroy(resultUI);
        }
    }
    
    // 저장 No 버튼 클릭 시
    private void OnSaveGLBNo()
    {
        Debug.Log("GLB 저장을 취소했습니다.");
        
        // UI 제거
        GameObject resultUI = GameObject.Find("GLBResultUI");
        if (resultUI != null)
        {
            Destroy(resultUI);
        }
        
        // 로드된 GLB 오브젝트도 제거
        GameObject loadedGLB = GameObject.Find("LoadedGLB");
        if (loadedGLB != null)
        {
            Destroy(loadedGLB);
        }
    }
}