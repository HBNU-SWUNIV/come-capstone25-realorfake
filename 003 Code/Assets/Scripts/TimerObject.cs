using UnityEngine;
using UnityEngine.XR;
using Unity.Netcode;

public class TimerObjectXR : MonoBehaviour
{
    [Header("시계 설정")]
    public GameObject clockPrefab;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftRayInteractor;
    public float throwForce = 15f;
    public Vector3 holdOffset = new Vector3(0, 0, 0.5f);

    private GameObject clock;
    private bool isGrabbed = false;
    private Vector3 originalPosition;
    private bool isInitialized = false;
    private GameManager gameManager;
    private bool isHost;

    void Start()
    {
        // if (!isInitialized)
        // {
        //     SpawnClock();
        //     isInitialized = true;
        // }

        clock = GameObject.Find("Clock");

        // GameManager 참조 가져오기
        gameManager = GameManager.GetInstance();
        // 호스트 여부 확인 (NetworkManager를 통해)
        isHost = NetworkManager.Singleton.IsHost;
    }

    void Update()
    {
        // if (clock == null)
        // {
        //     SpawnClock();
        //     return;
        // }

        HandleGrabInput();
        HandleThrowInput();
        CheckReset();
    }

    void SpawnClock()
    {
        if (clock != null) return;  // 이미 시계가 있다면 생성하지 않음

        clock = Instantiate(clockPrefab, transform.position, Quaternion.identity);
        originalPosition = clock.transform.position;

        // 필수 컴포넌트 확인
        if (!clock.GetComponent<Rigidbody>())
            clock.AddComponent<Rigidbody>();
        if (!clock.GetComponent<Collider>())
            clock.AddComponent<BoxCollider>();
    }

    void HandleGrabInput()
    {
        if (isGrabbed || clock == null || leftRayInteractor == null) return;

        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && isPressed)
        {
            if (leftRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.gameObject == clock)
                {
                    GrabClock();
                }
            }
        }
    }

    void GrabClock()
    {
        isGrabbed = true;
        clock.GetComponent<Rigidbody>().isKinematic = true;
        Debug.Log("시계 잡음!");
    }

    void HandleThrowInput()
    {
        if (!isGrabbed || clock == null || leftRayInteractor == null) return;

        // 컨트롤러 위치 추적
        clock.transform.position = leftRayInteractor.transform.position +
                                   leftRayInteractor.transform.TransformDirection(holdOffset);

        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && !isPressed)
        {
            ThrowClock();
        }
    }

    void ThrowClock()
    {
        isGrabbed = false;
        Rigidbody rb = clock.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(leftRayInteractor.transform.forward * throwForce, ForceMode.Impulse);
        Debug.Log("시계 던짐!");

        // 시계의 z 위치가 3 이상인지 확인
        if (clock.transform.position.z >= 3f)
        {
            // 턴 넘기기
            if (gameManager != null)
            {
                gameManager.EndTurn();
                Debug.Log("턴이 넘어갔습니다!");
            }
        }
    }

    void CheckReset()
    {
        if (clock == null) return;
        if (clock.transform.position.y < -1f)
        {
            ResetClock();
        }
    }

    void ResetClock()
    {
        if (clock == null) return;
        clock.transform.position = originalPosition;
        clock.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        Debug.Log("시계 리셋");
    }
}
