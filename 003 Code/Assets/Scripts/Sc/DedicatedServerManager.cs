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

            // Allocation ���� (�ִ� 2��)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2, region);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Relay ����ü ����
            var allocationId = RelayAllocationId.FromByteArray(allocation.AllocationIdBytes);
            var connectionData = RelayConnectionData.FromByteArray(allocation.ConnectionData);
            var key = RelayHMACKey.FromByteArray(allocation.Key);

            // Relay ���� ��������Ʈ �Ľ�
            NetworkEndpoint endpoint;
            bool parseSuccess = NetworkEndpoint.TryParse(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                out endpoint,
                NetworkFamily.Ipv4
            );

            if (!parseSuccess)
            {
                Debug.LogError("Relay ���� �ּ� �Ľ� ����");
                return;
            }

            // ref �Ű����� ���� ������� ����
            var relayServerData = new RelayServerData(
                endpoint: ref endpoint,
                nonce: 0,
                allocationId: ref allocationId,
                connectionData: ref connectionData,
                hostConnectionData: ref connectionData,
                key: ref key,
                isSecure: true
            );

            // UTP ����
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            Debug.Log($"���� ����. ���� �ڵ�: {joinCode}");

            // ���� ���� �ڵ� �߰�
            string filePath = "C:/Users/wltjr/OneDrive/RelayJoinCode.txt";
            File.WriteAllText(filePath, joinCode);
            Debug.Log($"���� �ڵ� ���� �Ϸ�: {filePath}");

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;


            NetworkManager.Singleton.StartServer();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay ����: {e.Message}");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        _connectedPlayers++;
        Debug.Log($"�÷��̾� ����: {clientId} (���� �ο�: {_connectedPlayers}/2)");

        if (_connectedPlayers == 2)
        {
            NotifyPlayersReadyClientRpc();
        }
    }

    [ClientRpc]
    private void NotifyPlayersReadyClientRpc()
    {
        Debug.Log("���� �޽���: ��� �÷��̾ �غ�Ǿ����ϴ�!");
    }
}
