using Oculus.Platform.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AG.Network.AGLobby
{
    public class LobbySingleton : MonoBehaviour
    {
        public static LobbySingleton instance { get; private set; }

        public event Action<List<Lobby>> lobbyListChangedEvent;

        // 플레이어가 참가하거나 나갈 때 발생하는 이벤트
        public event Action<Lobby> joinLobbyEvent;
        // 참가자가 나갈 때 발생하는 이벤트
        public event Action leaveLobbyEvent;
        // 참가 중 플레이어가 강퇴될 때 발생하는 이벤트
        public event Action kickedFromLobbyEvent;
        // GameStart 함수에서 발생 -> 모든 참가자들이 Relay 서버에 접속해야 함
        // TODO : 게임 시작하면 참가자들이 모두 플레이어 Relay 서버에 접속 -> 이렇게 해야 함
        //          Relay 서버를 사용해서 게임을 동기화 -> NetCode 사용하면 됨
        //          gameStartEvent 발생 후 LoadScene(게임씬) -> 네트워크 매니저, 네트워크 오브젝트 사용해서 게임 시작 시 동기화
        public event Action gameStartEvent;

        public Lobby joinedLobby;

        private string playerName;

        private float lobbyMaintainTimer = 0.0f;

        private float lobbyInfomationUpdateTimer = 0.0f;

        public bool createdLobby = false;

        public bool isMigrated = false;

        private async void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                try
                {
                    // Unity Services가 초기화되어 있는지 확인
                    if (!UnityServices.InitializeAsync().IsCompleted)
                    {
                        Debug.Log("Unity Services 초기화 중...");
                        await UnityServices.InitializeAsync();
                    }

                    // 인증 상태 확인 및 필요시 재인증
                    if (!AuthenticationService.Instance.IsSignedIn)
                    {
                        Debug.Log("인증이 필요합니다. 재인증 시도...");
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unity Services 초기화 중 오류 발생: {e.Message}");
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            MaintainLobbyAlive();
            RefreshLobbyInfomation();
        }

        private void OnEnable()
        {
            // 이벤트 구독 제거
        }

        private void OnDisable()
        {
            // 이벤트 구독 제거
        }

        private void OnDestroy()
        {
            // 씬 전환 시 불필요한 이벤트 구독 해제
            if (lobbyListChangedEvent != null)
            {
                lobbyListChangedEvent = null;
            }
            if (joinLobbyEvent != null)
            {
                joinLobbyEvent = null;
            }
            if (leaveLobbyEvent != null)
            {
                leaveLobbyEvent = null;
            }
            if (kickedFromLobbyEvent != null)
            {
                kickedFromLobbyEvent = null;
            }
            if (gameStartEvent != null)
            {
                gameStartEvent = null;
            }
        }

        public async Task Authenticate(string playerName)
        {
            this.playerName = playerName;

            try
            {
                // Unity Services가 초기화되어 있는지 확인
                if (!UnityServices.InitializeAsync().IsCompleted)
                {
                    Debug.Log("Unity Services 초기화 중...");
                    await UnityServices.InitializeAsync();
                }

                // 이미 로그인되어 있는지 확인
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("이미 로그인되어 있습니다.");
                    return;
                }

                InitializationOptions initializationOptions = new InitializationOptions();
                initializationOptions.SetProfile(playerName);

                await UnityServices.InitializeAsync(initializationOptions);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                
                Debug.Log($"인증 상태: {AuthenticationService.Instance.IsSignedIn}");
                Debug.Log($"플레이어 ID: {AuthenticationService.Instance.PlayerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"인증 중 오류 발생: {e.Message}");
            }
        }

        public void ClearSession()
        {
            playerName = "";
            AuthenticationService.Instance.SignOut(true);
        }

        public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
        {
            try
            {
                // Unity Services가 초기화되어 있는지 확인
                if (!UnityServices.InitializeAsync().IsCompleted)
                {
                    Debug.Log("Unity Services 초기화 중...");
                    await UnityServices.InitializeAsync();
                }

                // 인증 상태 확인 및 필요시 재인증
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("인증이 필요합니다. 재인증 시도...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                CreateLobbyOptions createOptions = new CreateLobbyOptions
                {
                    Player = GetPlayer(),
                    IsPrivate = isPrivate,
                    Data = new Dictionary<string, DataObject>{
                        { NetworkConstants.GAMEMODE_KEY, new DataObject(DataObject.VisibilityOptions.Public, "DefaultGameMode") },
                        { NetworkConstants.GAMESTART_KEY, new DataObject(DataObject.VisibilityOptions.Member, NetworkConstants.GAMESTART_KEY_DEFAULT) },
                        { "HostOid", new DataObject(DataObject.VisibilityOptions.Member, "") },
                        { "ClientOid", new DataObject(DataObject.VisibilityOptions.Member, "") },
                        { "MapId", new DataObject(DataObject.VisibilityOptions.Member, UnityEngine.Random.Range(1, 6).ToString()) }
                    }
                };

                var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
                joinedLobby = lobby;

                joinLobbyEvent?.Invoke(joinedLobby);

                Debug.Log($"Created Lobby {joinedLobby.LobbyCode}");

                createdLobby = true;
                int selectedIndex = FindAnyObjectByType<PresetSelectionUI>().GetSelectedIndex();
                string oidList = FindAnyObjectByType<PresetManager>().GetSelectedPresetOid(selectedIndex);

                // Host OID 설정
                UpdateLobbyOptions updateOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "HostOid", new DataObject(DataObject.VisibilityOptions.Member, oidList) }
                    }
                };

                joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, updateOptions);
            
            }
            catch (Exception e)
            {
                Debug.LogError($"로비 생성 중 오류 발생: {e.Message}");
            }
        }

        public async void MaintainLobbyAlive()
        {
            if (!IsLobbyhost()) return;

            lobbyMaintainTimer += Time.deltaTime;
            if (lobbyMaintainTimer < NetworkConstants.LOBBY_MAINTAIN_TIME) return;

            lobbyMaintainTimer = 0.0f;
            await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
        }

        public async void RefreshLobbyInfomation()
        {
            if (joinedLobby == null) return;


            lobbyInfomationUpdateTimer += Time.deltaTime;
            if (lobbyInfomationUpdateTimer < NetworkConstants.LOBBY_INFO_UPDATE_TIME) return;

            lobbyInfomationUpdateTimer = 0.0f;


            Debug.Log($"LobbySingleton : {IsLobbyhost()}");

            var lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            joinedLobby = lobby;
            Debug.Log($"GetLobbyAsync ClientOid after update: {joinedLobby.Data["ClientOid"].Value}");

            // PlayerTest의 텍스트 업데이트


            // PresetSelectionUI의 텍스트 업데이트
            var presetUI = FindAnyObjectByType<PresetSelectionUI>();
            if (presetUI != null && presetUI.PlayerText != null)
            {
                presetUI.PlayerText.text = $"플레이어 수: {joinedLobby.Players.Count} / 2\n";
            }



            // 로비에 2명이 전부 들어왔을 때
            if (presetUI != null && joinedLobby.Players.Count == 2)
            {
                if (IsLobbyhost())
                {
                    await MigrateHost();
                    SetClientOidServerRpc();
                }

                if (!string.IsNullOrEmpty(joinedLobby.Data["ClientOid"].Value))
                {
                    presetUI.StartGameStartCoroutine(createdLobby);
                }
            }


            if (!IsPlayerInLobby())
            {
                joinedLobby = null;
                kickedFromLobbyEvent?.Invoke();
                return;
            }
            if (joinedLobby.Data[NetworkConstants.GAMESTART_KEY].Value != NetworkConstants.GAMESTART_KEY_DEFAULT)
            {
                if (!IsLobbyhost())
                {
                    if (RelaySingleton.Instance == null) {
                        GameObject relayObj = new GameObject("RelaySingleton");
                        relayObj.AddComponent<RelaySingleton>();
                        Debug.Log("RelaySingleton이 생성되었습니다.");
                    }

                    await RelaySingleton.JoinRelay(joinedLobby.Data[NetworkConstants.GAMESTART_KEY].Value);
                    // 추가 코드
                    
                    if (presetUI != null && presetUI.LogText != null)
                    {
                        presetUI.LogText.text += $"Relay 코드 {joinedLobby.Data[NetworkConstants.GAMESTART_KEY].Value} 게임 시작\n";
                    }

                    //SceneManager.LoadScene("GameScene", LoadSceneMode.Additive);
                    // 호스트 RPC 호출
                }


                joinedLobby = null;

                gameStartEvent?.Invoke();
                return;
            }

            joinLobbyEvent?.Invoke(joinedLobby);
        }

        public async void JoinLobbyByUI(Lobby lobby)
        {
            var joinOption = new JoinLobbyByIdOptions { Player = GetPlayer() };
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, joinOption);

            joinLobbyEvent?.Invoke(joinedLobby);
        }

        public async void JoinLobbyByCode(string lobbyCode)
        {
            var joinOption = new JoinLobbyByCodeOptions { Player = GetPlayer() };
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOption);

            joinLobbyEvent?.Invoke(joinedLobby);
        }

        public async void QuickMatch(Text t)
        {
            try
            {
                // Unity Services가 초기화되어 있는지 확인
                if (!UnityServices.InitializeAsync().IsCompleted)
                {
                    Debug.Log("Unity Services 초기화 중...");
                    await UnityServices.InitializeAsync();
                }

                // 인증 상태 확인
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("인증이 필요합니다. 먼저 Authenticate를 호출해주세요.");
                    return;
                }

                QuickJoinLobbyOptions options = new QuickJoinLobbyOptions { Player = GetPlayer() };
                joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

                joinLobbyEvent?.Invoke(joinedLobby);
                t.text = $"참가한 로비 {joinedLobby.LobbyCode}";
                Debug.Log($"참가한 로비 {joinedLobby.LobbyCode}");

                

            }
            catch (LobbyServiceException e)
            {
                if (e.Reason == LobbyExceptionReason.NoOpenLobbies)
                {
                    t.text = $"열린 로비가 없습니다";
                    Debug.Log($"열린 로비가 없습니다");
                }
                Debug.Log(e);
            }
            catch (Exception e)
            {
                t.text = $"QuickMatch 중 오류 발생: {e.Message}";
                Debug.LogError($"QuickMatch 중 오류 발생: {e.Message}");
            }
        }

        public async void GetLobbyList()
        {
            try
            {
                QueryLobbiesOptions options = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>{
                        new QueryFilter(
                            field: QueryFilter.FieldOptions.AvailableSlots,
                            op: QueryFilter.OpOptions.GT,
                            value: "0"
                        )
                    },
                    Order = new List<QueryOrder>{
                        new QueryOrder(
                            asc: false,
                            field: QueryOrder.FieldOptions.Created
                        )
                    }
                };

                QueryResponse lobbyListQueryResponse = await LobbyService.Instance.QueryLobbiesAsync();
                lobbyListChangedEvent?.Invoke(lobbyListQueryResponse.Results);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"{e}");
            }
        }

        public async void LeaveLobby()
        {
            if (joinedLobby == null) return;

            try
            {
                await MigrateHost();
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;

                leaveLobbyEvent?.Invoke();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"{e}");
            }
        }

        public async void KickPlayer(string playerId)
        {
            if (!IsLobbyhost()) return;
            if (playerId == AuthenticationService.Instance.PlayerId) return;

            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"{e}");
            }
        }

        public async Task MigrateHostAgain()
        {
            if (!IsLobbyhost() || joinedLobby.Players.Count <= 1 || createdLobby) return;

            Debug.Log($"MigrateHostAgain {createdLobby}, Current Host : {joinedLobby.HostId}");

            try
            {
                string hostId = joinedLobby.Players[0].Id;
                if (hostId == joinedLobby.HostId)
                    hostId = joinedLobby.Players[1].Id;

                Debug.Log($"MigrateHostAgain New Host : {hostId}");

                joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    HostId = hostId
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"{e}");
            }
        }

        public async Task StartGame()
        {
            Debug.Log($"Lobby Singleton StartGame, IsLobbyHost : {IsLobbyhost()}, createLobby : {createdLobby}");

            if (!createdLobby)
                return;

            while (!IsLobbyhost())
            {
                await Task.Delay(100);
            }

            try
            {
                // RelaySingleton이 없으면 생성
                if (RelaySingleton.Instance == null)
                {
                    GameObject relayObj = new GameObject("RelaySingleton");
                    relayObj.AddComponent<RelaySingleton>();
                    Debug.Log("RelaySingleton이 생성되었습니다.");
                }

                // RelaySingleton이 초기화될 때까지 잠시 대기
                int maxAttempts = 10;
                int currentAttempt = 0;
                while (RelaySingleton.Instance == null && currentAttempt < maxAttempts)
                {
                    await Task.Delay(100);
                    currentAttempt++;
                }

                if (RelaySingleton.Instance == null)
                {
                    Debug.LogError("RelaySingleton 초기화에 실패했습니다.");
                    return;
                }

                string relayCode = await RelaySingleton.CreateRelay();
                if (string.IsNullOrEmpty(relayCode))
                {
                    Debug.LogError("Relay 서버 생성에 실패했습니다.");
                    return;
                }

                Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>{
                        { NetworkConstants.GAMESTART_KEY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                joinedLobby = lobby;

                // 씬 전환 전에 필요한 컴포넌트들이 로드되었는지 확인
                await EnsureComponentsLoaded();

                // 씬 전환 시 로딩 화면 표시
                Debug.Log("LobbySingleton FightScene");
                //SceneManager.LoadScene("FightScene", LoadSceneMode.Additive);

                //FindAnyObjectByType<SceneChanger>()?.ChangeScene("FightScene");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"게임 시작 중 오류 발생: {e.Message}");
            }
        }

        private async Task EnsureComponentsLoaded()
        {
            // 필요한 매니저들이 로드될 때까지 대기
            int maxAttempts = 10;
            int currentAttempt = 0;

            var cardManager = FindAnyObjectByType<CardManager>();
            var presetManager = FindAnyObjectByType<PresetLoader>();

            while (currentAttempt < maxAttempts)
            {
                cardManager = FindAnyObjectByType<CardManager>();
                presetManager = FindAnyObjectByType<PresetLoader>();

                if (cardManager != null && presetManager != null)
                {
                    return;
                }

                currentAttempt++;
                await Task.Delay(100); // 100ms 대기
            }
            
            Debug.Log("CardManager : " + cardManager);
            Debug.Log("PreserManager : " + presetManager);
            Debug.LogWarning("일부 매니저가 로드되지 않았습니다. 게임을 계속 진행합니다.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "FightScene")
            {
                // 씬이 로드된 후 필요한 초기화 작업 수행
                StartCoroutine(InitializeFightScene());
            }
        }

        private IEnumerator InitializeFightScene()
        {
            // 씬 로드 후 1프레임 대기
            yield return null;

            var cardManager = FindAnyObjectByType<CardManager>();
            var presetManager = FindAnyObjectByType<PresetManager>();

            if (cardManager == null)
            {
                Debug.LogError("CardManager를 찾을 수 없습니다.");
            }

            if (presetManager == null)
            {
                Debug.LogError("PresetManager를 찾을 수 없습니다.");
            }
        }

        private Player GetPlayer()
        {
            return new Player
            {
                Data = new Dictionary<string, PlayerDataObject>{
                    {NetworkConstants.PLAYERNAME_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
                }
            };
        }

        private bool IsPlayerInLobby()
        {
            if (joinedLobby == null || joinedLobby.Players == null) return false;

            foreach (var player in joinedLobby.Players)
            {
                if (player.Id != AuthenticationService.Instance.PlayerId) continue;
                return true;
            }

            return false;
        }

        private async Task MigrateHost()
        {
            if (!IsLobbyhost() || joinedLobby.Players.Count <= 1 || !createdLobby || isMigrated) return;

            isMigrated = true;

            try
            {
                joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    HostId = joinedLobby.Players[1].Id
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"{e}");
            }
        }

        private bool IsLobbyhost()
        {
            return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
        }

        public Lobby GetJoinedLobby()
        {
            return joinedLobby;
        }

        public bool IsAuthenticated()
        {
            return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
        }

        public string GetPlayerId()
        {
            return IsAuthenticated() ? AuthenticationService.Instance.PlayerId : string.Empty;
        }

        public async Task<bool> EnsureAuthenticated()
        {
            try
            {
                if (!IsAuthenticated())
                {
                    Debug.Log("인증이 필요합니다. 재인증 시도...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                return IsAuthenticated();
            }
            catch (Exception e)
            {
                Debug.LogError($"인증 확인 중 오류 발생: {e.Message}");
                return false;
            }
        }

        [ServerRpc]
        public void SetClientOidServerRpc()
        {
            // Client OID 설정
            int selectedIndex = FindAnyObjectByType<PresetSelectionUI>().GetSelectedIndex();
            string oidList = FindAnyObjectByType<PresetManager>().GetSelectedPresetOid(selectedIndex);

            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                    {
                        { "ClientOid", new DataObject(DataObject.VisibilityOptions.Member, oidList.ToString()) }
                    }
            };

            LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, updateOptions);
        }

    }
}