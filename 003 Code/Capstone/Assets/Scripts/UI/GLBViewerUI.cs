using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using UnityGLTF.Loader;
using UnityGLTF;
using System.Threading.Tasks; // Added for Task.Delay

public class GLBViewerUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private Camera renderCamera;
    [SerializeField] private RenderTexture renderTexture;
    
    [Header("3D Model Settings")]
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private Transform modelContainer;
    
    [Header("Camera Settings")]
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -5f);
    
    [Header("UI Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text loadingText;
    
    [Header("BigClass Selection")]
    [SerializeField] private Button kitchenButton;
    [SerializeField] private Button writeButton;
    [SerializeField] private Button cleanButton;

    private CardItemDataManager dataManager; // CardItemDataManager를 연결할 변수

    private GameObject currentModel;
    private bool isRotating = true;
    private string currentModelPath;
    private string selectedBigClass; // 선택된 BigClass 저장
    private bool isBigClassSelected = false; // BigClass 선택 여부
    private bool isInitialized = false; // 초기화 완료 여부
    private string pendingGLBPath; // 대기 중인 GLB 파일 경로
    
    // 이벤트 콜백
    public System.Action<bool> OnSaveDecision;
    public System.Action<string> OnBigClassSelected; // BigClass 선택 이벤트
    
    void Start()
    {
        Debug.Log("GLBViewerUI 시작");
        
        // UI3DModel 레이어 생성 시도
        CreateUI3DModelLayer();
        
        // 초기화 순서 중요: RenderTexture -> Camera -> Buttons
        InitializeRenderTexture();
        SetupCamera();
        SetupButtons();
        ShowLoading(true);
        
        // 초기화 완료 표시
        isInitialized = true;
        
        // 대기 중인 GLB 파일이 있으면 로드
        if (!string.IsNullOrEmpty(pendingGLBPath))
        {
            Debug.Log($"대기 중인 GLB 파일 로드: {pendingGLBPath}");
            LoadGLBToUI(pendingGLBPath);
            pendingGLBPath = null;
        }
        
        Debug.Log("GLBViewerUI 초기화 완료");
    }
    
    // UI3DModel 레이어 생성 (런타임에서는 제한적)
    private void CreateUI3DModelLayer()
    {
        int ui3DModelLayer = LayerMask.NameToLayer("UI3DModel");
        if (ui3DModelLayer == -1)
        {
            Debug.LogWarning("UI3DModel 레이어가 존재하지 않습니다. Default 레이어를 사용합니다.");
            Debug.LogWarning("레이어 생성을 위해서는 Unity 에디터에서 Edit > Project Settings > Tags and Layers에서 수동으로 추가해주세요.");
        }
        else
        {
            Debug.Log($"UI3DModel 레이어 발견: {ui3DModelLayer}");
        }
    }
    
    void Update()
    {
        if (isRotating && currentModel != null)
        {
            // 모델 자동 회전 (렌더 카메라 기준)
            currentModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        // 렌더 카메라가 RawImage에 계속 렌더링되도록 보장
        if (renderCamera != null && renderTexture != null)
        {
            // RenderTexture가 유효한지 확인
            if (!renderTexture.IsCreated())
            {
                Debug.LogWarning("RenderTexture가 생성되지 않았습니다. 재생성을 시도합니다.");
                if (!renderTexture.Create())
                {
                    Debug.LogError("RenderTexture 재생성 실패!");
                    return;
                }
            }
            
            renderCamera.targetTexture = renderTexture;
            
            // RawImage에 텍스처가 할당되어 있는지 확인
            if (displayImage != null && displayImage.texture != renderTexture)
            {
                displayImage.texture = renderTexture;
                Debug.Log("RawImage 텍스처 재할당");
            }
        }
        else if (renderCamera == null || renderTexture == null)
        {
            // 중요한 컴포넌트가 null인 경우 한 번만 로그 출력
            if (Time.frameCount % 60 == 0) // 1초마다 한 번씩만 로그 출력
            {
                Debug.LogWarning($"렌더 컴포넌트 누락 - Camera: {renderCamera != null}, Texture: {renderTexture != null}");
            }
        }
    }
    
    private void SetupButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveClicked);
            saveButton.interactable = false; // 초기에는 비활성화
        }
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
            
        // BigClass 선택 버튼 설정
        if (kitchenButton != null)
            kitchenButton.onClick.AddListener(() => OnBigClassClicked("kitchen"));
        
        if (writeButton != null)
            writeButton.onClick.AddListener(() => OnBigClassClicked("write"));
            
        if (cleanButton != null)
            cleanButton.onClick.AddListener(() => OnBigClassClicked("clean"));
            
        // 확인 버튼 설정 (초기에는 비활성화)
        // if (confirmButton != null)
        // {
        //     confirmButton.onClick.AddListener(OnConfirmClicked);
        //     confirmButton.interactable = false; // 초기에는 비활성화
        // }
    }
    
    private void ShowLoading(bool show)
    {
        if (loadingText != null)
            loadingText.gameObject.SetActive(show);
    }
    
    public void ShowUI(bool show)
    {
        gameObject.SetActive(show);
    }
    
    private void InitializeRenderTexture()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(512, 512, 16);
            renderTexture.antiAliasing = 4; // 안티앨리어싱 적용
            
            if (!renderTexture.Create())
            {
                Debug.LogError("RenderTexture 생성 실패!");
                renderTexture = null;
                return;
            }
            
            Debug.Log("RenderTexture 생성 성공");
        }
        
        if (displayImage != null && renderTexture != null)
        {
            displayImage.texture = renderTexture;
            Debug.Log("RenderTexture 초기화 완료");
        }
        else
        {
            Debug.LogWarning($"초기화 실패 - displayImage: {displayImage != null}, renderTexture: {renderTexture != null}");
        }
    }
    
    private void SetupCamera()
    {
        if (renderTexture == null)
        {
            Debug.LogError("RenderTexture가 초기화되지 않았습니다. 카메라 설정을 건너뜁니다.");
            return;
        }
        
        if (renderCamera == null)
        {
            // 카메라가 없으면 새로 생성
            GameObject cameraObj = new GameObject("RenderCamera");
            renderCamera = cameraObj.AddComponent<Camera>();
            cameraObj.transform.SetParent(transform);
            
            // 생성된 카메라를 Inspector에서 확인할 수 있도록 설정
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log("새로운 렌더 카메라 생성 완료");
        }
        
        // UI3DModel 레이어 존재 확인 및 생성
        int ui3DModelLayer = LayerMask.NameToLayer("UI3DModel");
        if (ui3DModelLayer == -1)
        {
            Debug.LogWarning("UI3DModel 레이어가 존재하지 않습니다. Default 레이어를 사용합니다.");
            ui3DModelLayer = 0; // Default 레이어 사용
        }
        
        renderCamera.targetTexture = renderTexture;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear;
        
        // 레이어 마스크 설정 - UI3DModel 레이어가 있으면 해당 레이어만, 없으면 모든 레이어
        if (ui3DModelLayer != 0) // UI3DModel 레이어가 존재하는 경우
        {
            renderCamera.cullingMask = 1 << ui3DModelLayer;
            Debug.Log($"UI3DModel 레이어만 렌더링: {ui3DModelLayer}");
        }
        else // Default 레이어를 사용하는 경우
        {
            renderCamera.cullingMask = ~0; // 모든 레이어 렌더링
            Debug.Log("모든 레이어 렌더링 (Default 레이어 사용)");
        }
        
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = 2f;
        renderCamera.nearClipPlane = 0.1f;
        renderCamera.farClipPlane = 100f;
        
        // 카메라를 UI의 자식으로 설정하여 함께 움직이도록 함
        renderCamera.transform.SetParent(transform);
        renderCamera.transform.localPosition = new Vector3(0, 0, -5f);
        renderCamera.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"렌더 카메라 설정 완료 - 레이어: {ui3DModelLayer}, 마스크: {renderCamera.cullingMask}");
    }
    
    public void LoadModel()
    {
        if (modelPrefab == null) 
        {
            Debug.LogWarning("modelPrefab이 null입니다.");
            return;
        }
        
        if (modelContainer == null)
        {
            Debug.LogWarning("modelContainer가 null입니다. 새로 생성합니다.");
            GameObject container = new GameObject("ModelContainer");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            modelContainer = container.transform;
        }
        
        // 기존 모델 제거
        if (currentModel != null)
        {
            DestroyImmediate(currentModel);
        }
        
        // 새 모델 생성
        currentModel = Instantiate(modelPrefab, modelContainer);
        
        // 레이어 설정 (UI3DModel 레이어가 없으면 Default 사용)
        int ui3DModelLayer = LayerMask.NameToLayer("UI3DModel");
        if (ui3DModelLayer == -1)
        {
            ui3DModelLayer = 0; // Default 레이어
        }
        
        SetModelLayer(currentModel, ui3DModelLayer);
        
        // 카메라 위치 조정
        if (renderCamera != null)
        {
            Bounds bounds = GetModelBounds(currentModel);
            if (bounds.size != Vector3.zero)
            {
                float distance = bounds.size.magnitude * 2f;
                renderCamera.transform.position = bounds.center + cameraOffset.normalized * distance;
                renderCamera.transform.LookAt(bounds.center);
                Debug.Log($"카메라 위치 조정 완료: 거리={distance}");
            }
            else
            {
                Debug.LogWarning("모델의 Bounds를 계산할 수 없습니다.");
            }
        }
        
        ShowLoading(false);
    }
    
    public void LoadModelFromPath(string modelPath)
    {
        currentModelPath = modelPath;
        
        // Resources 폴더에서 모델 로드
        GameObject prefab = Resources.Load<GameObject>(modelPath);
        if (prefab != null)
        {
            modelPrefab = prefab;
            LoadModel();
        }
        else
        {
            Debug.LogError($"모델을 찾을 수 없습니다: {modelPath}");
            ShowLoading(false);
        }
    }
    
    private void SetModelLayer(GameObject model, int layer)
    {
        // 모델과 모든 자식 오브젝트의 레이어를 설정
        model.layer = layer;
        foreach (Transform child in model.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = layer;
        }
        Debug.Log($"모델 레이어 설정 완료: {layer}");
    }
    
    private Bounds GetModelBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) 
        {
            Debug.LogWarning("모델에 Renderer가 없습니다.");
            return new Bounds();
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        Debug.Log($"모델 Bounds 계산: {bounds.size}");
        return bounds;
    }
    
    public void ToggleRotation()
    {
        isRotating = !isRotating;
    }
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    public void ResetModelRotation()
    {
        if (currentModel != null)
        {
            currentModel.transform.rotation = Quaternion.identity;
        }
    }
    
    // 저장 버튼 클릭 이벤트
    private async void OnSaveClicked()
    {
        if (isBigClassSelected && !string.IsNullOrEmpty(selectedBigClass))
        {
            Debug.Log($"최종 BigClass 선택: {selectedBigClass}");
            OnBigClassSelected?.Invoke(selectedBigClass);

            // CardItemDataManager를 찾아 정보전달을 요청하기 
            dataManager = FindFirstObjectByType<CardItemDataManager>();
            if (dataManager == null)
            {
                Debug.LogError("CardItemDataManager를 찾을 수 없습니다.");
                return;
            }

            dataManager.bigClass = selectedBigClass;

            // OnCardInfoReceived를 호출하여 카드 데이터 생성 및 저장 진행
            Debug.Log("카드 정보 생성 및 저장 시작...");
            dataManager.OnCardInfoReceived(true, selectedBigClass);

            ShowUI(false); // UI 숨기기
        }
        else
        {
            Debug.LogWarning("BigClass가 선택되지 않았습니다. 먼저 BigClass를 선택해주세요.");
        }
    }

    
    // 취소 버튼 클릭 이벤트
    private void OnCancelClicked()
    {
        Debug.Log("취소 버튼 클릭됨");
        OnSaveDecision?.Invoke(false);
    }
    
    // BigClass 선택 버튼 클릭 이벤트
    private void OnBigClassClicked(string bigClass)
    {
        selectedBigClass = bigClass;
        isBigClassSelected = true;
        Debug.Log($"BigClass 선택됨: {bigClass}");
        
        // 저장 버튼 활성화
        if (saveButton != null)
        {
            saveButton.interactable = true;
        }
        
        // BigClass 버튼들 비활성화 (중복 선택 방지)
        if (kitchenButton != null) kitchenButton.interactable = false;
        if (writeButton != null) writeButton.interactable = false;
        if (cleanButton != null) cleanButton.interactable = false;
    }
    
    public void LoadGLBToUI(string glbFilePath)
    {
        if (!isInitialized)
        {
            Debug.Log($"초기화가 완료되지 않았습니다. GLB 파일을 대기열에 추가: {glbFilePath}");
            pendingGLBPath = glbFilePath;
            return;
        }
        
        Debug.Log($"GLB 파일 로드 시작: {glbFilePath}");
        StartCoroutine(LoadGLBModelCoroutine(glbFilePath));
    }

    private IEnumerator LoadGLBModelCoroutine(string glbFilePath)
    {
        // renderCamera가 초기화될 때까지 대기
        while (renderCamera == null)
        {
            Debug.Log("renderCamera 초기화 대기 중...");
            yield return null;
        }

        if (!File.Exists(glbFilePath))
        {
            Debug.LogError($"GLB 파일이 존재하지 않습니다: {glbFilePath}");
            yield break;
        }

        Debug.Log($"GLB 로딩 시작: {glbFilePath}");

        var loader = new FileLoader(Path.GetDirectoryName(glbFilePath));
        var importer = new GLTFSceneImporter(
            Path.GetFileName(glbFilePath),
            new ImportOptions { DataLoader = loader }
        );
        
        // modelContainer가 없으면 렌더 카메라의 자식으로 생성
        if (modelContainer == null)
        {
            GameObject tempContainer = new GameObject("GLBContainer");
            tempContainer.transform.SetParent(renderCamera.transform);
            tempContainer.transform.localPosition = Vector3.zero;
            modelContainer = tempContainer.transform;
        }
        
        // GLB를 modelContainer에 로드
        importer.SceneParent = modelContainer;

        var loadSceneTask = importer.LoadSceneAsync();
        while (!loadSceneTask.IsCompleted)
        {
            yield return null;
        }

        if (loadSceneTask.Exception != null)
        {
            Debug.LogError("GLB 로딩 실패: " + loadSceneTask.Exception.Message);
            ShowLoading(false);
        }
        else
        {
            Debug.Log("GLB 로딩 완료");
            
            // 로드된 모델을 렌더 카메라 앞에 배치
            if (modelContainer.childCount > 0)
            {
                var loadedObj = modelContainer.GetChild(0).gameObject;
                SetupModelForRendering(loadedObj);
            }
            else
            {
                Debug.LogError("GLB 모델이 로드되지 않았습니다.");
                ShowLoading(false);
            }
        }
    }
    
    // 렌더링을 위한 모델 설정
    private void SetupModelForRendering(GameObject model)
    {
        Debug.Log("모델 렌더링 설정 시작");
        
        if (renderCamera == null || renderTexture == null)
        {
            Debug.LogError("렌더 카메라 또는 RenderTexture가 초기화되지 않았습니다.");
            ShowLoading(false);
            return;
        }
        
        // 모델을 렌더 카메라 앞에 배치
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        
        // 레이어 설정
        int ui3DModelLayer = LayerMask.NameToLayer("UI3DModel");
        if (ui3DModelLayer == -1)
        {
            Debug.LogWarning("UI3DModel 레이어가 존재하지 않습니다. Default 레이어를 사용합니다.");
            ui3DModelLayer = 0; // Default 레이어
        }
        
        SetModelLayer(model, ui3DModelLayer);
        
        // 렌더 카메라 위치 조정
        Bounds bounds = GetModelBounds(model);
        if (bounds.size != Vector3.zero)
        {
            float distance = bounds.size.magnitude * 2f;
            
            // 렌더 카메라를 모델 앞에 배치
            renderCamera.transform.localPosition = new Vector3(0, 0, -distance);
            renderCamera.transform.LookAt(bounds.center);
            
            Debug.Log($"카메라 위치 조정: 거리={distance}, 중심={bounds.center}");
        }
        else
        {
            Debug.LogWarning("모델 Bounds를 계산할 수 없어 기본 카메라 위치를 사용합니다.");
            renderCamera.transform.localPosition = new Vector3(0, 0, -5f);
        }
        
        // 렌더 카메라 설정 재확인
        renderCamera.targetTexture = renderTexture;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear;
        
        // 레이어 마스크 설정 - UI3DModel 레이어가 있으면 해당 레이어만, 없으면 모든 레이어
        if (ui3DModelLayer != 0) // UI3DModel 레이어가 존재하는 경우
        {
            renderCamera.cullingMask = 1 << ui3DModelLayer;
            Debug.Log($"UI3DModel 레이어만 렌더링: {ui3DModelLayer}");
        }
        else // Default 레이어를 사용하는 경우
        {
            renderCamera.cullingMask = ~0; // 모든 레이어 렌더링
            Debug.Log("모든 레이어 렌더링 (Default 레이어 사용)");
        }
        
        currentModel = model;
        Debug.Log($"모델 렌더링 설정 완료: 레이어={ui3DModelLayer}, 모델 크기={bounds.size}");
        
        // RawImage에 텍스처 할당 확인
        if (displayImage != null)
        {
            displayImage.texture = renderTexture;
            Debug.Log("RawImage에 RenderTexture 할당 완료");
        }
        else
        {
            Debug.LogError("displayImage가 null입니다!");
        }
        
        ShowLoading(false);
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
} 