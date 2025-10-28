using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SceneChanger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rayDistance = 10f;
    [SerializeField] private string targetTag = "SceneChanger";
    [SerializeField] private GameObject presetSelectionUI;
    [SerializeField] private float uiDistance = 0.5f;

    private const string FIGHT_SCENE = "FightScene";
    private const string MAIN_SCENE = "MainScene";
    private const string CAPTURE_SCENE = "CaptureScene";
    

    private bool isHovering;
    private XRRayInteractor rayInteractor;
    private InputDevice leftController;
    private string targetSceneName;
    private GameObject currentUI;

    private bool isSceneChangePressed = false;

    private void Start()
    {
        InitializeComponents();
        isSceneChangePressed = false;
    }

    private void Update()
    {
        HandleRaycast();
        CheckTriggerInput();
    }

    private void InitializeComponents()
    {
        InitializeController();
        InitializeRayInteractor();

        // 스크립트가 속한 오브젝트의 최상위 부모 (XR Origin 등)를 찾아 씬 전환 시 파괴되지 않도록 설정
        // Transform topParent = transform.root;
        // if (topParent != null)
        // {
        //     DontDestroyOnLoad(topParent.gameObject);
        //     Debug.Log($"{topParent.gameObject.name} 오브젝트를 씬 전환 시 파괴되지 않도록 설정했습니다.");
        // }
        // else
        // {
        //     Debug.LogWarning("최상위 부모 오브젝트를 찾을 수 없습니다. DontDestroyOnLoad 적용 실패.");
        // }
    }

    private void InitializeController()
    {
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, inputDevices);
        
        if (inputDevices.Count > 0)
        {
            leftController = inputDevices[0];
        }
        else
        {
            Debug.LogWarning("왼쪽 컨트롤러를 찾을 수 없습니다.");
        }
    }

    private void InitializeRayInteractor()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
        if (rayInteractor == null)
        {
            Debug.LogWarning("레이 인터랙터를 찾을 수 없습니다.");
        }
    }

    private void HandleRaycast()
    {
        if (rayInteractor == null) return;

        if (CastRay(out RaycastHit hit))
        {
            HandleRayHit(hit);
        }
        else
        {
            HandleRayMiss();
        }
    }

    private bool CastRay(out RaycastHit hit)
    {
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance))
        {
            if (hit.collider.CompareTag(targetTag))
            {
                targetSceneName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                return true;
            }
        }
        return false;
    }

    private void HandleRayHit(RaycastHit hit)
    {
        isHovering = true;
    }

    private void HandleRayMiss()
    {
        isHovering = false;
        targetSceneName = null;
    }

    private void CheckTriggerInput()
    {
        if (!IsTriggerPressed() || !isHovering || string.IsNullOrEmpty(targetSceneName)) return;

        if (targetSceneName == FIGHT_SCENE)
        {
            ShowPresetSelectionUI();
        }
        else if (targetSceneName == MAIN_SCENE)
        {
            if (SceneManager.GetActiveScene().name == "FightScene")
            {
                NetworkManager.Singleton.Shutdown();
            }

            ChangeScene(MAIN_SCENE);
        }
        else if (targetSceneName == CAPTURE_SCENE)
        {
            if (!isSceneChangePressed)
            {
                isSceneChangePressed = true;
                FindAnyObjectByType<PlayerDataManager>().StartCompareSessionCoroutine(GoCaptureScene);
            }
        }
        else
        {
            //ChangeScene(targetSceneName);
        }
    }

    private void GoCaptureScene(int a)
    {
        ChangeScene(CAPTURE_SCENE);
    }

    private bool IsTriggerPressed()
    {
        if (!leftController.isValid) return false;
        
        bool triggerPressed = false;
        leftController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        return triggerPressed;
        
    }

    private void ShowPresetSelectionUI()
    {
        if (currentUI != null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 uiPos = cam.transform.position + cam.transform.forward * uiDistance;
        currentUI = Instantiate(presetSelectionUI, uiPos, Quaternion.identity);
        
        // UI를 카메라를 향하도록 설정
        currentUI.transform.LookAt(cam.transform);
        currentUI.transform.Rotate(0, 180f, 0);
    }

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("이동할 씬 이름이 없습니다.");
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    
}

// 씬 전환 오브젝트를 위한 컴포넌트
public class SceneChangerObject : MonoBehaviour
{
    public string targetSceneName;
}
