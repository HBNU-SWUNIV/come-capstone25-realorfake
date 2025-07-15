using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

public class ModelResultManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private GameObject glbViewerUIPrefab;
    
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
        
        // GLB 파일 생성 완료 감지 시작
        if (!string.IsNullOrEmpty(WebcamToUI.currentTimestamp))
        {
            string glbFilePath = $"C:/RecordedFrames/{WebcamToUI.currentTimestamp}/output/texturedMeshed.glb";
            StartCoroutine(CheckGLBFileAndShowUI(glbFilePath));
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
            yield return new WaitForSeconds(10f); // 10초마다 체크
        }
        
        if (checkCount >= maxChecks)
        {
            Debug.LogError("GLB 파일 생성 시간 초과");
        }
    }
    
    private void ShowResultUI(string glbFilePath)
    {
        // 기존 UI가 있으면 제거
        if (currentUI != null)
        {
            Destroy(currentUI);
        }
        
        // GLBViewerUI 프리팹 생성
        if (glbViewerUIPrefab != null)
        {
            currentUI = Instantiate(glbViewerUIPrefab);
            
            // GLBViewerUI 컴포넌트 가져오기
            GLBViewerUI glbViewer = currentUI.GetComponent<GLBViewerUI>();
            if (glbViewer != null)
            {
                // 저장/취소 결정 이벤트 연결
                glbViewer.OnSaveDecision += OnSaveDecision;
                
                // GLB 파일을 직접 로드
                glbViewer.LoadGLBToUI(glbFilePath);
            }
        }
        else
        {
            Debug.LogError("GLBViewerUI 프리팹이 설정되지 않았습니다!");
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
        // CardItemDataManager를 통해 저장 프로세스 실행
        CardItemDataManager cardManager = FindObjectOfType<CardItemDataManager>();
        if (cardManager != null)
        {
            // 기존 CardItemDataManager의 OnCardInfoReceived 콜백 사용
            string imagePath = $"C:/RecordedFrames/{WebcamToUI.currentTimestamp}/{WebcamToUI.currentTimestamp}.jpg";
            string bigClass = "kitchen"; // 기본값 또는 실제 분석 결과
            
            // CardItemDataManager의 기존 로직 활용
            cardManager.OnCardInfoReceived(true, bigClass);
        }
        else
        {
            Debug.LogError("CardItemDataManager를 찾을 수 없습니다!");
            ShowErrorUI();
        }
        
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
        CloseResultUI();
    }
} 