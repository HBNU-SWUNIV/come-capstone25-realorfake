using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class DedicatedServerManager : NetworkBehaviour
{
    [SerializeField] private string region = "asia-northeast3";
    private int _connectedPlayers = 0;

    async void Start()
    {

        if (!IsServer) return;

        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Allocation 생성 (최대 2명)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2, region);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Relay 구조체 추출
            var allocationId = RelayAllocationId.FromByteArray(allocation.AllocationIdBytes);
            var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData);
            var key = RelayHMACKey.FromByteArray(allocation.Key);

            // Relay 서버 엔드포인트 파싱
            NetworkEndpoint endpoint;
            bool parseSuccess = NetworkEndpoint.TryParse(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                out endpoint,
                NetworkFamily.Ipv4
            );

            if (!parseSuccess)
            {
                Debug.LogError("Relay 서버 주소 파싱 실패");
                return;
            }

            // ref 매개변수 전달 방식으로 수정
            var relayServerData = new RelayServerData(
                endpoint: ref endpoint,
                nonce: 0,
                allocationId: ref allocationId,
                connectionData: ref connectionData,
                hostConnectionData: ref connectionData,
                key: ref key,
                isSecure: true
            );

            // UTP 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            Debug.Log($"서버 시작. 참가 코드: {joinCode}");

            // 파일 저장 코드 추가
            string filePath = "C:/Users/wltjr/OneDrive/RelayJoinCode.txt";
            File.WriteAllText(filePath, joinCode);
            Debug.Log($"참가 코드 저장 완료: {filePath}");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;


            NetworkManager.Singleton.StartServer();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay 오류: {e.Message}");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        _connectedPlayers++;
        Debug.Log($"플레이어 연결: {clientId} (현재 인원: {_connectedPlayers}/2)");

        if (_connectedPlayers == 2)
        {
            NotifyPlayersReadyClientRpc();
        }
    }

    [ClientRpc]
    private void NotifyPlayersReadyClientRpc()
    {
        Debug.Log("서버 메시지: 모든 플레이어가 준비되었습니다!");
    }
}
