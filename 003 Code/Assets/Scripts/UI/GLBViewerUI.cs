using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using UnityGLTF.Loader;
using UnityGLTF;

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
    
    private GameObject currentModel;
    private bool isRotating = true;
    private string currentModelPath;
    
    // 이벤트 콜백
    public System.Action<bool> OnSaveDecision;
    
    void Start()
    {
        InitializeRenderTexture();
        SetupCamera();
        SetupButtons();
        ShowLoading(true);
    }
    
    void Update()
    {
        if (isRotating && currentModel != null)
        {
            // 모델 자동 회전
            currentModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void SetupButtons()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
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
            renderTexture.Create();
        }
        
        if (displayImage != null)
        {
            displayImage.texture = renderTexture;
        }
    }
    
    private void SetupCamera()
    {
        if (renderCamera == null)
        {
            // 카메라가 없으면 새로 생성
            GameObject cameraObj = new GameObject("RenderCamera");
            renderCamera = cameraObj.AddComponent<Camera>();
            cameraObj.transform.SetParent(transform);
        }
        
        renderCamera.targetTexture = renderTexture;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear;
        renderCamera.cullingMask = LayerMask.GetMask("UI3DModel"); // 특정 레이어만 렌더링
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = 2f;
    }
    
    public void LoadModel()
    {
        if (modelPrefab == null) return;
        
        // 기존 모델 제거
        if (currentModel != null)
        {
            DestroyImmediate(currentModel);
        }
        
        // 새 모델 생성
        currentModel = Instantiate(modelPrefab, modelContainer);
        currentModel.layer = LayerMask.NameToLayer("UI3DModel");
        
        // 카메라 위치 조정
        if (renderCamera != null)
        {
            Bounds bounds = GetModelBounds(currentModel);
            float distance = bounds.size.magnitude * 2f;
            renderCamera.transform.position = bounds.center + cameraOffset.normalized * distance;
            renderCamera.transform.LookAt(bounds.center);
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
    
    private Bounds GetModelBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds();
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
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
    private void OnSaveClicked()
    {
        Debug.Log("저장 버튼 클릭됨");
        OnSaveDecision?.Invoke(true);
    }
    
    // 취소 버튼 클릭 이벤트
    private void OnCancelClicked()
    {
        Debug.Log("취소 버튼 클릭됨");
        OnSaveDecision?.Invoke(false);
    }
    
    public void LoadGLBToUI(string glbFilePath)
    {
        StartCoroutine(LoadGLBModelCoroutine(glbFilePath));
    }

    private IEnumerator LoadGLBModelCoroutine(string glbFilePath)
    {
        if (!File.Exists(glbFilePath))
        {
            Debug.LogError($"GLB 파일이 존재하지 않습니다: {glbFilePath}");
            yield break;
        }

        var loader = new FileLoader(Path.GetDirectoryName(glbFilePath));
        var importer = new GLTFSceneImporter(
            Path.GetFileName(glbFilePath),
            new ImportOptions { DataLoader = loader }
        );
        
        // modelContainer가 지정되어 있으면 그 하위에 생성
        if (modelContainer != null)
            importer.SceneParent = modelContainer;
        else
            importer.SceneParent = new GameObject("LoadedGLB").transform;

        Debug.Log("GLB 로딩 시작: " + glbFilePath);

        var loadSceneTask = importer.LoadSceneAsync();
        while (!loadSceneTask.IsCompleted)
        {
            yield return null;
        }

        if (loadSceneTask.Exception != null)
        {
            Debug.LogError("GLB 로딩 실패: " + loadSceneTask.Exception.Message);
        }
        else
        {
            Debug.Log("GLB 로딩 완료");
            // 필요시 오브젝트 위치/스케일 조정
            if (modelContainer != null)
            {
                // 첫 번째 자식 오브젝트를 가져와서 정렬
                if (modelContainer.childCount > 0)
                {
                    var loadedObj = modelContainer.GetChild(0).gameObject;
                    loadedObj.transform.localPosition = Vector3.zero;
                    loadedObj.transform.localRotation = Quaternion.identity;
                    loadedObj.transform.localScale = Vector3.one;
                }
            }
        }
    }
    
    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
} 