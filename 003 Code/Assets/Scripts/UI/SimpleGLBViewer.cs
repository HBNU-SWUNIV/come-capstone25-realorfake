using UnityEngine;
using UnityEngine.UI;

public class SimpleGLBViewer : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button resetButton;
    
    [Header("Model Settings")]
    [SerializeField] private GameObject[] modelPrefabs;
    [SerializeField] private int currentModelIndex = 0;
    
    private Camera renderCamera;
    private RenderTexture renderTexture;
    private GameObject currentModel;
    private bool isRotating = true;
    
    void Start()
    {
        SetupUI();
        CreateRenderCamera();
        LoadModel(currentModelIndex);
    }
    
    void Update()
    {
        if (isRotating && currentModel != null)
        {
            currentModel.transform.Rotate(0, 50f * Time.deltaTime, 0);
        }
    }
    
    private void SetupUI()
    {
        // 버튼 이벤트 연결
        if (rotateButton != null)
            rotateButton.onClick.AddListener(ToggleRotation);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetRotation);
    }
    
    private void CreateRenderCamera()
    {
        // 렌더 텍스처 생성
        renderTexture = new RenderTexture(256, 256, 16);
        renderTexture.Create();
        
        // UI 이미지에 텍스처 할당
        if (displayImage != null)
            displayImage.texture = renderTexture;
        
        // 렌더 카메라 생성
        GameObject cameraObj = new GameObject("UI Render Camera");
        cameraObj.transform.SetParent(transform);
        cameraObj.transform.localPosition = new Vector3(0, 0, -5);
        
        renderCamera = cameraObj.AddComponent<Camera>();
        renderCamera.targetTexture = renderTexture;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0); // 투명 배경
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = 1.5f;
        renderCamera.cullingMask = 1 << LayerMask.NameToLayer("UI3DModel");
    }
    
    public void LoadModel(int index)
    {
        if (modelPrefabs == null || index < 0 || index >= modelPrefabs.Length)
            return;
        
        // 기존 모델 제거
        if (currentModel != null)
            DestroyImmediate(currentModel);
        
        // 새 모델 생성
        currentModel = Instantiate(modelPrefabs[index], transform);
        currentModel.layer = LayerMask.NameToLayer("UI3DModel");
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        
        currentModelIndex = index;
    }
    
    public void LoadNextModel()
    {
        int nextIndex = (currentModelIndex + 1) % modelPrefabs.Length;
        LoadModel(nextIndex);
    }
    
    public void LoadPreviousModel()
    {
        int prevIndex = (currentModelIndex - 1 + modelPrefabs.Length) % modelPrefabs.Length;
        LoadModel(prevIndex);
    }
    
    public void ToggleRotation()
    {
        isRotating = !isRotating;
    }
    
    public void ResetRotation()
    {
        if (currentModel != null)
        {
            currentModel.transform.localRotation = Quaternion.identity;
        }
    }
    
    public void LoadModelFromResources(string path)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab != null)
        {
            // 기존 모델 제거
            if (currentModel != null)
                DestroyImmediate(currentModel);
            
            // 새 모델 생성
            currentModel = Instantiate(prefab, transform);
            currentModel.layer = LayerMask.NameToLayer("UI3DModel");
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError($"Resources에서 모델을 찾을 수 없습니다: {path}");
        }
    }
    
    void OnDestroy()
    {
        if (renderTexture != null)
            renderTexture.Release();
    }
} 