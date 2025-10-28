using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Threading.Tasks;
using AG.Network.AGLobby;
using UnityEngine.UI;

public class CubeInteraction : MonoBehaviour
{
    [SerializeField] private bool isQuitButton; // 종료 버튼인지 여부
    [SerializeField] private GameObject loginUIPrefab; // UI 프리팹
    [SerializeField] private float distanceFromCamera = 2f; // 카메라로부터의 거리
    [SerializeField] private GameObject loginUI;

    private XRSimpleInteractable interactable;
    private bool isAuthenticated = false;
    private GameObject currentUI;
    private Text statusText;

    private void Start()
    {
        // XR Simple Interactable 컴포넌트 가져오기
        interactable = GetComponent<XRSimpleInteractable>();

        // 이벤트 리스너 등록
        //interactable.selectEntered.AddListener(OnSelectEntered); //이 이벤트는 로그인 성공 시 표출
        interactable.selectEntered.AddListener(ShowLoginUI);
    }

    private void ShowLoginUI(SelectEnterEventArgs args)
    {
        if (currentUI != null)
        {
            Destroy(currentUI);
        }

        // UI 생성
        currentUI = Instantiate(loginUI);

        // 카메라 앞에 배치
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            currentUI.transform.position = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
            currentUI.transform.rotation = Quaternion.LookRotation(currentUI.transform.position - mainCamera.transform.position);
            currentUI.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            currentUI.GetComponent<LoginUI>()._fp = OnSelectEntered;
        }
    }

    private void CreateUIInFrontOfCamera()
    {
        if (currentUI != null)
        {
            Destroy(currentUI);
        }

        // UI 생성
        currentUI = Instantiate(loginUIPrefab);
        
        // 카메라 앞에 배치
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            currentUI.transform.position = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
            currentUI.transform.rotation = Quaternion.LookRotation(currentUI.transform.position - mainCamera.transform.position);
        }

        // Text 컴포넌트 찾기
        statusText = currentUI.GetComponentInChildren<Text>();
    }

    public async void OnSelectEntered()
    {
        if (isQuitButton)
        {
            // 게임 종료
            StartSceneManager.Instance.QuitGame();
        }
        else
        {
            // UI 생성
            CreateUIInFrontOfCamera();

            // 로그인 시도
            if (!isAuthenticated)
            {
                if (statusText != null) statusText.text = "로그인 중...";
                
                try
                {
                    await AuthenticateUser(LoginUI._id.text);
                    if (isAuthenticated)
                    {
                        if (statusText != null) statusText.text = "로그인 성공!";
                        // 카드 json 데이터 저장 및 모델링 체크, 다운로드
                        PlayerDataManager pm = GameObject.FindAnyObjectByType<PlayerDataManager>();
                        if (pm != null)
                            pm.StartSaveItemCoroutine();
                    }
                }
                catch (System.Exception e)
                {
                    if (statusText != null) statusText.text = "로그인 실패: " + e.Message;
                    Debug.LogError($"로그인 실패: {e.Message}");
                }
            }
            else
            {
                // 이미 로그인된 상태면 바로 씬 전환
                StartSceneManager.Instance.LoadMainScene();
            }
        }
    }

    private async Task AuthenticateUser(string id)
    {
        try
        {
            await LobbySingleton.instance.Authenticate(id);
            isAuthenticated = true;
        }
        catch (System.Exception e)
        {
            isAuthenticated = false;
            throw e;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 리스너 제거
        if (interactable != null)
        {
            //interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        // UI 제거
        if (currentUI != null)
        {
            Destroy(currentUI);
        }
    }
} 