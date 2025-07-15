using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

[RequireComponent(typeof(Rigidbody))]
public class PlayerClient : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // NetworkTransform이 붙어 있으면 Rigidbody의 isKinematic은 상황에 따라 조정
        // 일반적으로 Rigidbody와 NetworkTransform을 같이 쓰면 예측/충돌 이슈가 있을 수 있으니 주의
    }

    public override void OnNetworkSpawn()
    {
        hostSpawnPoint = GameObject.Find("HostSpawnPoint").transform;
        clientSpawnPoint = GameObject.Find("ClientSpawnPoint").transform;

        if (IsHost)
        {
            if (hostSpawnPoint != null && IsOwner)
            {
                transform.position = hostSpawnPoint.position;
                transform.rotation = hostSpawnPoint.rotation;
            }
            else
            {
                // 자신이 아니거나 호스트 스폰 포인트가 없으면 XR 관련 컴포넌트 비활성화
                DisableXRComponents();
            }
        }
        else
        {
            if (clientSpawnPoint != null && IsOwner)
            {
                transform.position = clientSpawnPoint.position;
                transform.rotation = clientSpawnPoint.rotation;
            }
            else
            {
                DisableXRComponents();
            }
        }
    }

    void Update()
    {
        if (!IsOwner)
        {
            Vector3 inputVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            if (inputVector != Vector3.zero)
            {
                // NetworkTransform이 붙어 있으면 transform.position 변경만으로도 동기화됨
                transform.position += inputVector * moveSpeed * Time.deltaTime;
                // 회전도 마찬가지로 변경 가능
                // transform.rotation = Quaternion.LookRotation(inputVector);
            }
        }
    }

    void DisableXRComponents()
    {
        if (IsOwner)
            return;
        var xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin != null) xrOrigin.enabled = false;
        var inputManager = GetComponent<InputActionManager>();
        if (inputManager != null) inputManager.enabled = false;
        var inputModality = GetComponent<XRInputModalityManager>();
        if (inputModality != null) inputModality.enabled = false;
        var cam = GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = false;
        var audioListener = GetComponentInChildren<AudioListener>();
        if (audioListener != null) audioListener.enabled = false;
    }
}
