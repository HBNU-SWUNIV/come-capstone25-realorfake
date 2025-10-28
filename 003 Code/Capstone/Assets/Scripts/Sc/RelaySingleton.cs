using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class RelaySingleton : MonoBehaviour
{
    public static RelaySingleton Instance { get; private set; }
    
    // Relay 연결 완료 이벤트
    public event Action OnRelayConnected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // NetworkManager의 연결 상태 변경 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"클라이언트 연결됨: {clientId}");
        OnRelayConnected?.Invoke();
    }

    private void OnServerStarted()
    {
        Debug.Log("서버 시작됨");
        OnRelayConnected?.Invoke();
    }

    public static async Task<string> CreateRelay()
    {
        if (Instance == null)
        {
            Debug.LogError("RelaySingleton이 초기화되지 않았습니다.");
            return null;
        }

        try
        {
            // 최대 플레이어 수 설정
            int maxConnections = 2;

            // Relay 할당 요청
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // Relay 서버에 접속하기 위한 코드 생성
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // NetworkManager 설정
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 서버 시작
            NetworkManager.Singleton.StartHost();

            Debug.Log($"Relay 서버가 생성되었습니다. Join Code: {joinCode}");
            
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay 서버 생성 중 오류 발생: {e.Message}");
            return null;
        }
    }

    public static async Task JoinRelay(string joinCode)
    {
        if (Instance == null)
        {
            Debug.LogError("RelaySingleton이 초기화되지 않았습니다.");
            return;
        }

        try
        {
            // Relay 서버에 참가
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // NetworkManager 설정
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            // 클라이언트 시작
            NetworkManager.Singleton.StartClient();

            Debug.Log($"Relay 서버에 참가했습니다. Join Code: {joinCode}");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay 서버 참가 중 오류 발생: {e.Message}");
        }
    }
}