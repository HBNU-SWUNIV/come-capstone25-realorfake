using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System;

public static class MeshroomResult
{
    public static bool HasResult = false;
    public static bool Success = false;
    public static string GlbFilePath = "";
    public static string ErrorMessage = "";
}

public class ModelResultManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private GameObject glbViewerUIPrefab;
    [SerializeField] private GameObject fileCheckUIPrefab;
    [Header("Model Settings")]
    private GameObject currentUI;
    private string currentModelPath;
    
    void Start()
    {
        // MainScene에서만 실행
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainScene")
        {
            return;
        }
        
        Debug.Log("ModelResultManager 시작됨");
        Debug.Log($"WebcamToUI.currentTimestamp: {WebcamToUI.currentTimestamp}");
        Debug.Log($"CardItemDataManager.lastCreatedOID: {CardItemDataManager.lastCreatedOID}");
        
        // MeshroomResult에 결과가 있으면 UI 표시
        if (MeshroomResult.HasResult)
        {
            Debug.Log($"MeshroomResult 발견: Success={MeshroomResult.Success}, Path={MeshroomResult.GlbFilePath}");
            if (MeshroomResult.Success)
            {
                ShowResultUI(MeshroomResult.GlbFilePath);
            }
            else
            {
                ShowFileCheckUI(MeshroomResult.GlbFilePath);
            }
            MeshroomResult.HasResult = false; // 재사용 방지
        }
        else if (!string.IsNullOrEmpty(WebcamToUI.currentTimestamp))
        {
            Debug.Log("WebcamToUI.currentTimestamp가 설정됨 - GLB 파일 감지 시작");
            // 올바른 GLB 파일 경로 설정
            string objectsPath = Path.Combine(Application.persistentDataPath, "objects");
            Debug.Log($"Objects 경로: {objectsPath}");
            
            // OID가 설정되어 있으면 해당 파일을 찾고, 없으면 모든 .glb 파일을 찾음
            if (!string.IsNullOrEmpty(CardItemDataManager.lastCreatedOID))
            {
                string glbFilePath = Path.Combine(objectsPath, $"{CardItemDataManager.lastCreatedOID}.glb");
                Debug.Log($"OID 기반 GLB 파일 경로: {glbFilePath}");
                StartCoroutine(CheckGLBFileAndShowUI(glbFilePath));
            }
            else
            {
                // OID가 없으면 objects 폴더에서 가장 최근 .glb 파일을 찾음
                Debug.LogWarning("OID가 설정되지 않았습니다. objects 폴더에서 최근 GLB 파일을 찾습니다.");
                StartCoroutine(FindLatestGLBFileAndShowUI(objectsPath));
            }
        }
        else
        {
            Debug.LogWarning("WebcamToUI.currentTimestamp가 설정되지 않았습니다.");
        }
    }
    
    private IEnumerator CheckGLBFileAndShowUI(string filePath)
    {
        Debug.Log($"GLB 파일 감지 시작: {filePath}");
        
        int checkCount = 0;
        const int maxChecks = 120; // 20분 (10초 * 120)
        
        while (checkCount < maxChecks)
        {
            if (File.Exists(filePath))
            {
                Debug.Log("GLB 파일 생성 완료! UI 표시 시작");
                ShowResultUI(filePath);
                break;
            }
            
            checkCount++;
            Debug.Log($"GLB 파일 대기 중... ({checkCount}/{maxChecks}) - {filePath}");
            yield return new WaitForSeconds(10f); // 10초마다 체크
        }
        
        if (checkCount >= maxChecks)
        {
            Debug.LogError($"GLB 파일 생성 시간 초과: {filePath}");
            ShowFileCheckUI(filePath);
        }
    }

    private IEnumerator FindLatestGLBFileAndShowUI(string directoryPath)
    {
        Debug.Log($"최신 GLB 파일 찾기 시작: {directoryPath}");
        string latestGlbFile = null;
        DateTime latestCreationTime = DateTime.MinValue;

        if (Directory.Exists(directoryPath))
        {
            foreach (string file in Directory.GetFiles(directoryPath, "*.glb"))
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime > latestCreationTime)
                {
                    latestCreationTime = fileInfo.CreationTime;
                    latestGlbFile = file;
                }
            }
        }

        if (latestGlbFile != null)
        {
            Debug.Log($"최신 GLB 파일 발견: {latestGlbFile}");
            ShowResultUI(latestGlbFile);
        }
        else
        {
            Debug.LogError("최신 GLB 파일을 찾을 수 없습니다.");
            ShowFileCheckUI(directoryPath); // 디렉토리 자체를 파일 체크 UI에 표시
        }
        yield return null;
    }
    
    private void ShowResultUI(string glbFilePath)
    {
        // 기존 UI가 있으면 제거
        if (currentUI != null)
        {
            Destroy(currentUI);
        }
        
        // Canvas 찾기
        Canvas targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogWarning("씬에서 Canvas를 찾을 수 없습니다. 새로운 Canvas를 생성합니다.");
            // 새로운 Canvas 생성
            GameObject canvasObj = new GameObject("DynamicCanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.WorldSpace; // World Space로 설정
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        Debug.Log($"Canvas 발견: {targetCanvas.name}, RenderMode: {targetCanvas.renderMode}");
        
        // GLBViewerUI 프리팹 생성
        if (glbViewerUIPrefab != null)
        {
            currentUI = Instantiate(glbViewerUIPrefab, targetCanvas.transform);
            
            // XR Origin 앞에 UI 배치
            PositionUIInFrontOfXROrigin(currentUI);
            
            // GLBViewerUI 컴포넌트 가져오기
            GLBViewerUI glbViewer = currentUI.GetComponent<GLBViewerUI>();
            if (glbViewer != null)
            {
                // 저장/취소 결정 이벤트 연결
                glbViewer.OnSaveDecision += OnSaveDecision;
                
                // BigClass 선택 이벤트 연결
                glbViewer.OnBigClassSelected += OnBigClassSelected;
                
                // GLB 파일을 직접 로드 (초기화 완료 후)
                StartCoroutine(LoadGLBAfterInitialization(glbViewer, glbFilePath));
                
                Debug.Log("GLBViewerUI 생성 및 설정 완료");
            }
            else
            {
                Debug.LogError("GLBViewerUI 컴포넌트를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("GLBViewerUI 프리팹이 설정되지 않았습니다!");
        }
    }
    
    // GLBViewerUI 초기화 완료 후 GLB 파일 로드
    private System.Collections.IEnumerator LoadGLBAfterInitialization(GLBViewerUI glbViewer, string glbFilePath)
    {
        // GLBViewerUI의 Start 메서드가 완료될 때까지 대기
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // 추가 대기 시간
        
        Debug.Log($"GLB 파일 로드 시작: {glbFilePath}");
        glbViewer.LoadGLBToUI(glbFilePath);
    }

    // XR Origin 앞에 UI 배치
    private void PositionUIInFrontOfXROrigin(GameObject uiObject)
    {
        // XR Origin 찾기
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin");
        }
        
        if (xrOrigin != null)
        {
            // 메인 카메라 찾기
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 카메라 앞에 UI 배치
                Vector3 forward = mainCamera.transform.forward;
                Vector3 position = mainCamera.transform.position + forward * 2f; // 2미터 앞에 배치
                
                uiObject.transform.position = position;
                uiObject.transform.rotation = Quaternion.LookRotation(forward);
                
                // UI 크기 설정
                RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(800, 600);
                }
                
                Debug.Log($"UI를 XR Origin 앞에 배치: {position}");
            }
            else
            {
                Debug.LogWarning("메인 카메라를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("XR Origin을 찾을 수 없습니다.");
        }
    }

    private void ShowFileCheckUI(string glbFilePath)
    {
        if (currentUI != null)
        {
            Destroy(currentUI);
        }
        
        // Canvas 찾기
        Canvas targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogWarning("씬에서 Canvas를 찾을 수 없습니다. 새로운 Canvas를 생성합니다.");
            // 새로운 Canvas 생성
            GameObject canvasObj = new GameObject("DynamicCanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.WorldSpace; // World Space로 설정
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        Debug.Log($"Canvas 발견: {targetCanvas.name}, RenderMode: {targetCanvas.renderMode}");
        
        if (fileCheckUIPrefab != null)
        {
            currentUI = Instantiate(fileCheckUIPrefab, targetCanvas.transform);
            
            // XR Origin 앞에 UI 배치
            PositionUIInFrontOfXROrigin(currentUI);
            
            var fileCheckComponent = currentUI.GetComponent<FileCheckUI>();
            if (fileCheckComponent != null)
            {
                fileCheckComponent.SetFileCheckResult(false, glbFilePath);
                fileCheckComponent.AddLogMessage($"GLB 파일 생성 실패: {glbFilePath}", LogType.Error);
            }
        }
        else
        {
            Debug.LogWarning("fileCheckUIPrefab이 설정되지 않았습니다.");
        }
    }
    
    private void OnSaveDecision(bool shouldSave)
    {
        if (shouldSave)
        {
            Debug.Log("사용자가 저장을 선택했습니다.");
            StartCoroutine(SaveModelProcess());
        }
        else
        {
            Debug.Log("사용자가 취소를 선택했습니다.");
            CloseResultUI();
        }
    }
    
    // BigClass 선택 이벤트 핸들러
    private void OnBigClassSelected(string bigClass)
    {
        Debug.Log($"선택된 BigClass: {bigClass}");
        StartCoroutine(SaveModelWithBigClass(bigClass));
    }
    
    // 선택된 BigClass로 카드 생성
    private IEnumerator SaveModelWithBigClass(string bigClass)
    {
        Debug.Log($"BigClass '{bigClass}'로 카드 생성 시작...");
        
        // CardItemDataManager를 통해 저장 프로세스 실행
        CardItemDataManager cardManager = FindObjectOfType<CardItemDataManager>();
        if (cardManager != null)
        {
            // 선택된 BigClass로 카드 생성
            cardManager.OnCardInfoReceived(true, bigClass);
            Debug.Log($"카드 생성 요청 완료: BigClass={bigClass}");
        }
        else
        {
            Debug.LogError("CardItemDataManager를 찾을 수 없습니다!");
            ShowErrorUI();
        }
        
        // UI 정리
        CloseResultUI();
        
        yield return null;
    }
    
    private IEnumerator SaveModelProcess()
    {
        // 저장 프로세스 시작
        Debug.Log("모델 저장 프로세스 시작...");
        
        // 비동기 처리를 위한 Task 시작
        StartCoroutine(SaveModelAsync());
        
        yield return null;
    }
    
    private IEnumerator SaveModelAsync()
    {
        // 이 메서드는 BigClass 선택 방식으로 대체되었습니다.
        // OnBigClassSelected에서 SaveModelWithBigClass를 호출합니다.
        Debug.Log("SaveModelAsync는 더 이상 사용되지 않습니다. BigClass 선택 방식을 사용하세요.");
        
        yield return null;
    }
    
    // 성공 UI 표시
    private void ShowSuccessUI()
    {
        Debug.Log("모델 저장 성공!");
        // TODO: 실제 성공 UI 표시
        CloseResultUI();
    }
    
    // 실패 UI 표시
    private void ShowErrorUI()
    {
        Debug.LogError("모델 저장에 실패했습니다.");
        // TODO: 실제 실패 UI 표시
    }
    
    private void CloseResultUI()
    {
        if (currentUI != null)
        {
            Destroy(currentUI);
            currentUI = null;
        }
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (currentUI != null)
        {
            GLBViewerUI glbViewer = currentUI.GetComponent<GLBViewerUI>();
            if (glbViewer != null)
            {
                glbViewer.OnSaveDecision -= OnSaveDecision;
                glbViewer.OnBigClassSelected -= OnBigClassSelected;
            }
        }
        
        CloseResultUI();
    }
} 