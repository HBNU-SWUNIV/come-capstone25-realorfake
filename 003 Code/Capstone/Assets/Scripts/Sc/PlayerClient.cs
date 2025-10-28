using System.Net;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

[RequireComponent(typeof(Rigidbody))]
public class PlayerClient : NetworkBehaviour
{

    public NetworkVariable<Vector3> NetPos = new NetworkVariable<Vector3>();
    public NetworkVariable<Vector3> NetVel = new NetworkVariable<Vector3>();
    public NetworkVariable<Quaternion> NetRot = new NetworkVariable<Quaternion>();

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;
    private bool isTurn = false;
    private bool isStarted = false;
    private Rigidbody rb;
    private NetworkRigidbody nrb;
    public InputActionAsset XRIInputs;
    private InputActionMap XRIRightHand;
    private InputActionMap XRILeftHand;

    void Awake()
    {

        rb = GetComponent<Rigidbody>();
        nrb = GetComponent<NetworkRigidbody>();
        // NetworkTransform이 붙어 있으면 Rigidbody의 isKinematic은 상황에 따라 조정
        // 일반적으로 Rigidbody와 NetworkTransform을 같이 쓰면 예측/충돌 이슈가 있을 수 있으니 주의
    }

    private void Start()
    {
        hostSpawnPoint = GameObject.Find("HostSpawnPoint").transform;
        clientSpawnPoint = GameObject.Find("ClientSpawnPoint").transform;

        if (IsHost)
        {
            if (IsOwner)
            {
                transform.position = hostSpawnPoint.position;
                transform.rotation = hostSpawnPoint.rotation;
            }
        }
        else
        {
            if (IsOwner)
            {
                RequestStartPositionServerRpc(clientSpawnPoint.position);
                RequestRotationServerRpc(clientSpawnPoint.rotation);
                Debug.Log("PlayerClient Start");
            } else
            {
                transform.position = clientSpawnPoint.position;
            }
        }

        XRIRightHand = XRIInputs.FindActionMap("XRI Right Locomotion");
        XRILeftHand = XRIInputs.FindActionMap("XRI Left Locomotion");

    }

    public override void OnNetworkSpawn()
    {

        rb.linearVelocity = new Vector3(0, 0, 0);
        nrb.SetLinearVelocity(new Vector3(0, 0, 0));
        nrb.SetAngularVelocity(new Vector3(0, 0, 0));

        if (IsHost)
        {
            if (hostSpawnPoint != null && IsOwner)
            {
                transform.position = hostSpawnPoint.position;
                transform.rotation = hostSpawnPoint.rotation;
                FindAnyObjectByType<GameManager>()._cardSpawner = GetComponentInChildren<CardObjectSpawner>();
                Debug.Log("Host OnNetworkSpawn");
            }
            DisableXRComponents();
        }
        else
        {
            if (clientSpawnPoint != null && IsOwner)
            {
                RequestStartPositionServerRpc(clientSpawnPoint.position);
                RequestRotationServerRpc(clientSpawnPoint.rotation);
                FindAnyObjectByType<GameManager>()._cardSpawner = GetComponentInChildren<CardObjectSpawner>();
                Debug.Log("Client OnNetworkSpawn");
            }
            DisableXRComponents();
        }
    }

    /*
     
    회전 직후 이동 시 살짝 느려지는 문제 발생
    velocity 문제인거 같긴함.
    일단 이동 회전 다 동기화 됐으니까 후순위로 미룸

    다음 해야할 것
    - 소환 위치 제대로 맞추기
    - Start, OnNetworkSpawn 함수 호출 순서가 매번 다르니까 위치가 달라지는 듯 함
     
     */

    /*
     
    우진이랑 맞춰야할 것

    1. 호스트, 클라이언트 둘 다 카드 프리셋, 시계 오브젝트 표출
    2. 물체 설치 공간 동기화
    3. VR 캐릭터와 손에 NetworkTransform을 붙여서 움직임 동기화
    4. 시계와 카드 프리셋의 콜라이더 제거 -> 이거때매 플레이어가 돌아감
        - 아니면 isKinematic을 활성화 하던가 해야할듯...

    변경점
    - PlayerClient 스크립트
    - Player 프리팹
        - Locomotion/Move/DynamicMoveProvider의 LeftHandMoveInput, RightHandMoveInput을 None으로
        - Locomotion/Turn/DynamicSnapTurnProvider의 RightHandSnapTurn을 None으로 설정
        -> XROrigin 자체의 이동과 회전을 꺼서 네트워크로만 해결
     */

    void FixedUpdate()
    {
        Debug.Log($"{IsOwner}, {IsHost}");
        SyncTransform();

        InputAction lookAction = XRIRightHand.FindAction("Snap Turn", true);
        Vector2 seeDir = lookAction.ReadValue<Vector2>();
        if (seeDir != Vector2.zero)
        {
            if (IsOwner && !isTurn)
            {
                Quaternion newRot = Quaternion.Euler(0, 45f * Mathf.Sign(seeDir.x), 0) * transform.rotation;
                //transform.rotation = newRot;
                rb.MoveRotation(newRot);
                isTurn = true;
                RequestRotationServerRpc(newRot);
            }
        } else
        {
            isTurn = false;
        }

        InputAction moveAction = XRILeftHand.FindAction("Move", true);
        Vector2 inputVector = moveAction.ReadValue<Vector2>();

        if (inputVector == Vector2.zero)
            return;

        Vector3 localDir = new Vector3(inputVector.x, 0, inputVector.y);
        Vector3 moveDir = transform.TransformDirection(localDir);

        if (IsOwner)
        {
            RequestPositionServerRpc(moveDir);
        }

    }

    void DisableXRComponents()
    {
        if (IsOwner)
        {
            /*
             
            InputActionManager는 싱글톤 객체라서 한쪽에서 비활성화하면 다른쪽에 영향을 끼치게됨
            미리 비활성화 해놓고 주인 객체에만 활성화 하는 것이 맞는 방법
             
             */

            var inputActionManger = GetComponent<InputActionManager>();
            inputActionManger.enabled = true;
            return;
        }

        var xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin != null) xrOrigin.enabled = false;

        var xrInputModalityManager = GetComponent<XRInputModalityManager>();
        if (xrInputModalityManager != null) xrInputModalityManager.enabled = false;

        var cam = GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = false;

        var audioListener = GetComponentInChildren<AudioListener>();
        if (audioListener != null) audioListener.enabled = false;

        var all = GetComponentsInChildren<Behaviour>(true);
        foreach (var c in all)
        {
            if (c.gameObject == gameObject)
                continue;

            c.enabled = false;
        }

        var playerClient = GetComponent<PlayerClient>();
        playerClient.enabled = false;

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name == "Ray Interactor")
            {
                child.gameObject.SetActive(false);
                return;
            }
        }
    }

    [ServerRpc]
    void RequestStartPositionServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        GameObject reqPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;

        if (reqPlayer != null)
        {
            reqPlayer.transform.position = pos;
            NetPos.Value = transform.position;
        }
    }

    [ServerRpc]
    void RequestPositionServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        rb.linearVelocity = pos.normalized * moveSpeed;

        rb.MovePosition(rb.position + pos.normalized * Time.fixedDeltaTime);

        NetVel.Value = rb.linearVelocity;
        NetPos.Value = rb.position;
    }

    void SyncTransform()
    {
        if (!IsOwner)
        {
            rb.position = Vector3.Lerp(rb.position, NetPos.Value, 0.2f);
            rb.rotation = Quaternion.Normalize(NetRot.Value);
            rb.linearVelocity = NetVel.Value;
        }
    }

    [ServerRpc]
    void RequestRotationServerRpc(Quaternion rot, ServerRpcParams rpcParams = default)
    {
        NetRot.Value = rot;

        ulong clientId = rpcParams.Receive.SenderClientId;
        GameObject reqPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;

        if (reqPlayer != null)
        {
            reqPlayer.transform.rotation = rot;
        }
    }
}
