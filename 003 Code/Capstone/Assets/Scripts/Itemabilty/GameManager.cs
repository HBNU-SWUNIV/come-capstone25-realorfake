using AG.Network.AGLobby;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Lobbies.Models;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityGLTF;
using UnityGLTF.Loader;
using Object = UnityEngine.Object;

/// <summary>
/// 게임의 핵심 관리 클래스
/// 네트워크 동기화, 턴 관리, 아이템 관리, 전투 시스템을 담당
/// </summary>
public class GameManager : NetworkBehaviour
{
    /*
     * 네트워크 동기화 데이터
     * - NetworkVariable을 사용하여 동기화
     * - NetworkObject 컴포넌트 필요
     * - 오브젝트 풀링과 Spawn/Despawn 사용
     * 
     * 동기화되는 데이터:
     * - turn: 현재 턴
     * - currentLeftTime: 남은 시간
     * - currentStamina: 현재 스태미나
     * - hostPresetJsonData: 호스트 프리셋
     * - clientPresetJsonData: 클라이언트 프리셋
     */

    /*
     * 아이템 시스템
     * 
     * 1. 아이템 효과 적용
     * - 처음 사용 시 플래그 true
     * - 다음 턴에서 _expireCount 확인
     * - _isShooted 플래그로 사용 여부 체크
     * 
     * 2. 설치된 아이템 관리
     * - installedList로 설치된 아이템 관리
     * - 턴마다 InstallPassive 함수 호출
     * - _expireCount가 0이면 효과 제거
     * 
     * 3. 아이템 기본 흐름
     * - Init: 초기화
     * - Use: 턴마다 호출 (설치 시)
     * - InstallPassive: 설치된 효과 턴마다 적용
     * 
     * 4. 클라이언트 제한
     * - 자신의 턴이 아니면 카드 선택/사용 불가
     * - 자신의 턴이 아니면 카드 비활성화
     * - 설치된 효과 중복 적용 방지
     */

    /*
     

    설치된 아이템의 효과를 한번만 호출해야 한다
    -> 처음 사용할 때 플래그 true
    -> 다음 턴에서 _expireCount 확인해서 0이 아니면 플래그 false 변경
    -> _isShooted 플래그 사용하기! 처음 사용할 때 true로 바꿈
    -> InstallPassive를 bool 타입으로 만들고, _isShooted를 체크해서 바꿔야 함


    설치된 아이템의 효과 사용 횟수 제한 -> 배열로 관리(installedLIst)
    -> 한 번만 설치된 아이템의 효과 적용.. 턴마다 InstallPassive 함수 호출
    -> Use는 설치와 동시에 사용
    -> 다음 턴에서 배열을 순회해서 _expireCount가 0이면 효과를 제거


    DestroyItem 함수 추가 필요
    IncreaseAllItems 함수 추가 필요 -> itemList에 있는 모든 아이템의 설치된 효과나 사용된 효과를 모두 증가시켜야 함



    모든 스크립트 -> Attack을 호출할 때 체력이 0이하인지 체크해야 함.
     
     */

    /*
     
    아이템의 기본 흐름 : 처음 Init를 사용해서 초기화, 턴마다 Use(턴마다 호출, 설치 시) 호출, 설치된 아이템의 효과를 턴마다 InstallPassive를 호출한다.
    + 모든 아이템의 효과가 끝나면 다시 사용할 수 있게 턴마다 카드가 다시 생성되거나 있다. 

    클라이언트 제한 : 자신의 턴이 아니면 카드 선택과 사용 불가, 자신의 턴이 아니면 카드 표시하고 비활성화..(턴마다 호출)
    + 설치된 아이템은 이미 설치된 효과를 다시 적용하면 안됨, 모든 아이템의 효과를 다시 적용하면 안됨..
    + 턴마다 호출되는 함수를 자신의 턴인지 체크해서 모든 효과를 적용하면 안됨

     */

    /*
     
    테스트 순서
    
    1. 네트워크 플래그 설정 확인과 동기화

    2. 턴 제한 테스트 (시간 제한이나 턴 제한)
    - 시간제한이 끝나면 턴이 바뀌어야 함. 설치된 효과 적용,, 턴이 바뀌어야 함..

    3. 아이템 효과 테스트
    - 아이템의 효과가 제대로 적용되는지..
    - 버프 찾기
     
     */

    public enum BUFTYPE
    {
        INCATTACK,
        INCDEFENSE,
        ATTACKMISS,
        GUARD,
        HEAL,
        ZEROSTAMINA,
        BLEED,
        INCSTAMINA,
        X2,
        SHIELD,
        UNHEAL,
        REFLECT
    }

    public enum PLAYERTYPE
    {
        HOST,
        CLIENT,
        NONE
    }

    public struct BUFF : INetworkSerializable, IEquatable<BUFF>
    {
        public BUFTYPE type;
        public float value;
        public int turn;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref value);
            serializer.SerializeValue(ref turn);
        }

        public bool Equals(BUFF other)
        {
            return type == other.type && value == other.value && turn == other.turn;
        }
    }

    public struct PLAYERINFO : INetworkSerializable
    {
        public int uid;
        public int hp;
        public int shield;
        public int stamina;
        public int maxStamina;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref uid);
            serializer.SerializeValue(ref hp);
            serializer.SerializeValue(ref shield);
            serializer.SerializeValue(ref stamina);
            serializer.SerializeValue(ref maxStamina);
        }
    }

    private static GameManager _instance;
    private GameManager() { } // 싱글톤 생성

     public static string PresetJsonData { get; set; } // 프리셋 데이터 

    // 플레이어 정보 -> uid의 경우 접속하면서 받고, 갱신을 해줘야 하지 않을까?
    private static NetworkVariable<PLAYERINFO> hostPlayer = new NetworkVariable<PLAYERINFO>(new PLAYERINFO { uid = 0, hp = 10, shield = 0, stamina = 10 });
    private static NetworkVariable<PLAYERINFO> clientPlayer = new NetworkVariable<PLAYERINFO>(new PLAYERINFO { uid = 0, hp = 10, shield = 0, stamina = 10 });

    // 사실상 호스트만 들고 있음. 클라이언트로 버프 정보를 알 수 있어야 함
    private List<GameObject> hostItemList = new List<GameObject>();
    public List<GameObject> hostInstalledList = new List<GameObject>();
    public static List<BUFF> hostBufList = new List<BUFF>();

    private List<GameObject> clientItemList = new List<GameObject>();
    public List<GameObject> clientInstalledList = new List<GameObject>();
    public static List<BUFF> clientBufList = new List<BUFF>();

    // 조회용 버프 정보
    private NetworkList<BUFF> _networkHostBufList = new NetworkList<BUFF>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkList<BUFF> _networkClientBufList = new NetworkList<BUFF>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<PLAYERTYPE> _turn = new NetworkVariable<PLAYERTYPE>(PLAYERTYPE.NONE, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int _turnCount;

    public Dictionary<int, GameObject> _cardArray = new Dictionary<int, GameObject>();

    private NetworkVariable<float> _currentLeftTime = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // 지금 턴의 남은 시간
    private NetworkVariable<int> _currentStamina = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString4096Bytes> _hostPresetJsonData = new NetworkVariable<FixedString4096Bytes>();
    private NetworkVariable<int> _mapId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private const int _minTime = 30;
    private const int _maxTime = 60;

    private const int _maxHP = 1000;
    private const int _maxStamina = 10;

    private bool _host;
    private volatile bool _isGameStarted = false; // 게임 시작 상태를 관리하는 변수
    public CardObjectSpawner _cardSpawner; // 카드 스포너 참조

    private bool _isPresetDataLoaded = false;
    private bool _isInitialized = false;

    private GameObject _itemParent;
    private GameObject _staminaParent;
    private GameObject _floor;

    private GameObject _enemy;
    private List<BaseItem> _particleList = new List<BaseItem>();

    public GameObject _userUI;
    public GameObject _hostUIPosition;
    public GameObject _clientUIPosition;

    public GameObject _hostAvarta;
    public GameObject _clientAvarta;

    void Awake()
    {
        //_hostAvarta.SetActive(false);
        //_clientAvarta.SetActive(false);
    }

    private void Start()
    {

        //_userUI = GameObject.Find("UserUI");
        //_hostUIPosition = GameObject.Find("HostUISpawnPoint");
        //_clientUIPosition = GameObject.Find("ClientUISpawnPoint");
        // NetworkManager 초기화 확인
        if (NetworkManager.Singleton == null)
        {
            Debug.Log("NetworkManager가 초기화되지 않았습니다.");
            return;
        }

        // NetworkObject 컴포넌트 확인 및 추가
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = gameObject.AddComponent<NetworkObject>();
            Debug.Log("NetworkObject 컴포넌트가 추가되었습니다.");
        }

        // 카드 스포너 찾기
        _cardSpawner = FindAnyObjectByType<CardObjectSpawner>();

        // PresetLoader의 데이터 로드 완료 이벤트 구독
        
        PresetLoader.OnPresetDataLoaded -= OnPresetDataLoaded;
        PresetLoader.OnPresetDataLoaded += OnPresetDataLoaded;
        // 네트워크 종료 이벤트 등록
        NetworkManager.OnClientDisconnectCallback += OnDisconnect;

        _itemParent = GameObject.Find("ItemParent");
        _staminaParent = GameObject.Find("Stamina");
        _floor = GameObject.Find("Cube");
    }

    public void OnDisconnect(ulong clientId)
    {
        PresetLoader.Init();
        if (NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.Shutdown();
        FindAnyObjectByType<SceneChanger>()?.ChangeScene("MainScene");
    }

    public void OnPresetDataLoaded()
    {
        Debug.Log("PresetLoader에서 데이터 로드가 완료되었습니다.");
        _isPresetDataLoaded = true;
        
        // 이벤트 구독 해제
        PresetLoader.OnPresetDataLoaded -= OnPresetDataLoaded;
        
        InitializeGame();
    }

    public override void OnNetworkSpawn() 
    {
        // 이미 데이터가 로드되었는지 확인
        if (_isPresetDataLoaded)
        {
            InitializeGame();
        }
        else
        {
            // 아직 데이터가 로드되지 않았다면 이벤트 구독
            PresetLoader.OnPresetDataLoaded += OnPresetDataLoaded;
        }
        Debug.Log(PresetJsonData);

        Debug.Log($"CardObjectSpawner {_cardSpawner}");
    }

    private void InitializeGame()
    {
        if (_isInitialized)
            return;

        // 스태미나 객체 초기화
        SetStaminaObject(1);

        // 리스트 초기화
        hostBufList.Clear(); 
        clientBufList.Clear();
        //_networkClientBufList.Clear();
        //_networkHostBufList.Clear();

        if (NetworkManager.Singleton.IsHost && IsOwner)
        {
            if (_userUI != null)
            {
                _userUI.transform.position = _hostUIPosition.transform.position;
                _userUI.transform.rotation = _hostUIPosition.transform.rotation;
            }

            _isInitialized = true;
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                _hostPresetJsonData.Value = new FixedString4096Bytes(PresetJsonData);
            }
            else
            {
                Debug.LogWarning("PresetJsonData가 비어있습니다.");
                _hostPresetJsonData.Value = new FixedString4096Bytes("{}");
            }

            // 카드 리스트 설정
            _cardArray.Clear();
            CardDisplay[] _allCards = Object.FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);

            JSONNode jj = JSON.Parse(PresetJsonData);
            foreach (JSONNode n in jj)
            {
                foreach (CardDisplay cardDisplay in _allCards)
                {
                    string json = cardDisplay.GetJsonData();
                    JSONNode node = JSON.Parse(json);
                    Debug.Log($"{int.Parse(node["oid"])}, {int.Parse(n["oid"])}");
                    Debug.Log($"{int.Parse(node["oid"])} == {int.Parse(n["oid"])} : {int.Parse(node["oid"]) == int.Parse(n["oid"])}");
                    if (int.Parse(node["oid"]) == int.Parse(n["oid"]))
                    {
                        Debug.Log($"CardList Add {node["oid"]} : {cardDisplay.gameObject.name}");
                        _cardArray.Add(node["oid"], cardDisplay.gameObject);
                    }
                }
            }

            // json 데이터 불러오기
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                try
                {
                    var jsonData = JSON.Parse(PresetJsonData);
                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> otherOidList = new List<int>();
                    foreach (JSONNode node in jsonData.AsArray)
                    {
                        otherOidList.Add(node["oid"].AsInt);
                    }
                    otherOidList.Sort();

                    foreach (int _oid in otherOidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData.AsArray)
                        {
                            if (node["oid"].AsInt == _oid)
                            {
                                itemData = node;
                                break;
                            }
                        }

                        if (itemData != null)
                        {
                            // 아이템 오브젝트 생성
                            string path = Application.persistentDataPath + $"/objects/{_oid}.glb";
                            StartCoroutine(LoadGLBModelCoroutine(path, _oid, itemData, true));
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
                }
            }

            // _clientPresetJsonData 관련 아이템 생성 로직은 TransmitClientJsonDataServerRpc에서 처리

        }
        else if (NetworkManager.Singleton.IsClient)
        {
            if (_userUI != null)
            {
                _userUI.transform.position = _clientUIPosition.transform.position;
                _userUI.transform.rotation = _clientUIPosition.transform.rotation;
            }
            Debug.Log($"GameManager, Initialize, IsHost {NetworkManager.Singleton.IsHost} IsClient {NetworkManager.Singleton.IsClient} IsOwner {IsOwner}");
            Debug.Log($"GameManager, Initialize, PresetJsonData : {PresetJsonData}");
            _isInitialized = true;
            // 이 부분은 그냥 RPC로 상대에게 넘겨주고, 상대는 바로 아이템 생성하는 로직으로 만들자...
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                // _clientPresetJsonData.Value = new FixedString128Bytes(PresetJsonData);
                TransmitClientJsonDataServerRpc(new FixedString4096Bytes(PresetJsonData));
            }
            else
            {
                Debug.LogWarning("PresetJsonData가 비어있습니다.");
                TransmitClientJsonDataServerRpc(new FixedString4096Bytes("{}"));
            }

            // 카드 리스트 설정
            _cardArray.Clear();
            CardDisplay[] _allCards = Object.FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);

            JSONNode jj = JSON.Parse(PresetJsonData);
            foreach (JSONNode n in jj)
            {
                foreach (CardDisplay cardDisplay in _allCards)
                {
                    string json = cardDisplay.GetJsonData();
                    JSONNode node = JSON.Parse(json);
                    Debug.Log($"{int.Parse(node["oid"])}, {int.Parse(n["oid"])}");
                    Debug.Log($"{int.Parse(node["oid"])} == {int.Parse(n["oid"])} : {int.Parse(node["oid"]) == int.Parse(n["oid"])}");
                    if (int.Parse(node["oid"]) == int.Parse(n["oid"]))
                    {
                        Debug.Log($"CardList Add {node["oid"]} : {cardDisplay.gameObject.name}");
                        _cardArray.Add(node["oid"], cardDisplay.gameObject);
                    }
                }
            }

            // json 데이터 불러오기
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                try
                {
                    var jsonData = JSON.Parse(PresetJsonData);
                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> oidList = new List<int>();
                    foreach (JSONNode node in jsonData.AsArray)
                    {
                        oidList.Add(node["oid"].AsInt); 
                    }
                    oidList.Sort();

                    foreach (int _oid in oidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData.AsArray)
                        {
                            if (node["oid"].AsInt == _oid)
                            {
                                itemData = node;
                                break;
                            }
                        }

                        if (itemData != null)
                        {
                            // 아이템 오브젝트 생성
                            string path = Application.persistentDataPath + $"/objects/{_oid}.glb";
                            StartCoroutine(LoadGLBModelCoroutine(path, _oid, itemData, false));
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
                }
            }

            // _hostPresetJsonData 이용해서 hostPlayer 정보 채우기
            Debug.Log($"GameManager, Initialize, hostPresetJsonData : {_hostPresetJsonData.Value}");
            if (!string.IsNullOrEmpty(_hostPresetJsonData.Value.ToString()))
            {
                try
                {
                    var jsonData = JSON.Parse(_hostPresetJsonData.Value.ToString());
                    
                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> otherOidList = new List<int>();
                    foreach (JSONNode node in jsonData.AsArray)
                    {
                        otherOidList.Add(node["oid"].AsInt);
                    }
                    otherOidList.Sort();

                    foreach (int _oid in otherOidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData.AsArray)
                        {
                            if (node["oid"].AsInt == _oid)
                            {
                                itemData = node;
                                break;
                            }
                        }

                        if (itemData != null)
                        {
                            // 아이템 오브젝트 생성
                            string path = Application.persistentDataPath + $"/objects/{_oid}.glb";
                            StartCoroutine(LoadGLBModelCoroutine(path, _oid, itemData, true));
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
                }
            }
        }

        Debug.Log("Game Start Setting");
        // 게임 시작 설정
        if (NetworkManager.Singleton.IsHost)
        {
            // 게임 시작
            GameStart();
        }
        _isGameStarted = true;

        if (NetworkManager.Singleton.IsHost&& LobbySingleton.instance.joinedLobby.Data.TryGetValue("MapId", out DataObject dataObject))
        {
            _mapId.Value = int.Parse(dataObject.Value);
        }
 
        string mapPath = $"Map/FightField{_mapId.Value}";
        GameObject obj = Resources.Load(mapPath) as GameObject;
        GameObject map = null;
        if (obj != null) 
            map = Instantiate(obj);

        switch (_mapId.Value)
        {
            case 1:
                map.transform.position = new Vector3(7, -17, 20);
                map.transform.rotation = Quaternion.Euler(0, 90, 0);
                _floor.GetComponent<Renderer>().material = Resources.Load("Material/DessertFloor") as Material;
                break;
            case 2:
                map.transform.position = new Vector3(0, -6, -2);
                map.transform.rotation = Quaternion.Euler(0, 0, 0);
                _floor.GetComponent<Renderer>().material = Resources.Load("Material/SpaceFloor") as Material;
                break;
            case 3:
                map.transform.position = new Vector3(-10, 17.5f, 22);
                map.transform.rotation = Quaternion.Euler(0, 90, 0);
                _floor.GetComponent<Renderer>().material = Resources.Load("Material/SoccerFloor") as Material;
                break;
            case 4:
                map.transform.position = new Vector3(-11, 6, 8);
                map.transform.rotation = Quaternion.Euler(0, 90, 0);
                _floor.GetComponent<Renderer>().material = Resources.Load("Material/CampingFloor") as Material;
                break;
            case 5:
                map.transform.position = new Vector3(-67, 20, -36);
                map.transform.rotation = Quaternion.Euler(0, 90, 0);
                _floor.GetComponent<Renderer>().material = Resources.Load("Material/DessertFloor") as Material;
                break;
        }

        StartCoroutine(CheckAndSetAvarta());

    }

    private void GameStart()
    {
        Debug.Log("Game Start");

        // 호스트 플레이어 정보 설정
        PLAYERINFO hinfo;
        hinfo.hp = _maxHP;
        hinfo.shield = 0;
        hinfo.stamina = 0;
        hinfo.maxStamina = 0;
        hinfo.uid = 0;
        hostPlayer.Value = hinfo;

        // 클라이언트 플레이어 정보 설정
        PLAYERINFO cinfo;
        cinfo.uid = 1;
        cinfo.stamina = 0;
        cinfo.maxStamina = 0;
        cinfo.shield = 0;
        cinfo.hp = _maxHP;
        clientPlayer.Value = cinfo;

        // 버프 UI 초기화
        

        // 초기 턴 설정
        _turn.Value = PLAYERTYPE.HOST;
        _turnCount = 1;

        // 초기 시간 설정 - 0으로 설정하여 자동으로 턴이 넘어가도록 함
        _currentLeftTime.Value = 0;

        // 초기 스태미나 설정
        _currentStamina.Value = hostPlayer.Value.stamina;

        Debug.Log("게임이 시작되었습니다!");

        GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.RemoveAllBuff(true);
        GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.RemoveAllBuff(false);
    }

    private void FixedUpdate()
    {
        // NetworkManager 초기화 확인
        if (NetworkManager.Singleton == null)
        {
            Debug.Log("NetworkManager가 초기화되지 않았습니다.");
            return;
        }

        // 네트워크 연결 상태 확인
        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.Log("네트워크 연결이 아직 설정되지 않았습니다.");
            return;
        }

        // NetworkVariable 초기화 확인
        if (_currentLeftTime == null || _turn == null || _currentStamina == null || !_isGameStarted)
        {
            Debug.Log("NetworkVariable이 초기화되지 않았습니다.");
            return;
        }

        // 게임이 시작되지 않았으면 리턴
        if (!_isGameStarted)
        {
            Debug.Log("게임이 아직 시작되지 않았습니다." + this.gameObject.name);
            return;
        }


        Debug.Log($"Test2 {NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values.Count}");

        // 남은 시간이 다 되면 턴이 바뀌어야 함
        // 게임 종료
        PLAYERTYPE winner = IsGameOver();
        if (winner != PLAYERTYPE.NONE)
        {
            if (winner == PLAYERTYPE.HOST)
            {
                // host win
                Debug.Log("Host Win");
                NetworkManager.Singleton.Shutdown();
                FindAnyObjectByType<SceneChanger>()?.ChangeScene("MainScene");
            } else
            {
                // client win
                Debug.Log("Client Win");
                NetworkManager.Singleton.Shutdown();
                FindAnyObjectByType<SceneChanger>()?.ChangeScene("MainScene");
            }
        }

        // GameManager 상태 확인
        DebugUI.SetTurnText($"{_turn.Value}");
        DebugUI.SetTimeText(_currentLeftTime.Value);

        if (_turn.Value == PLAYERTYPE.HOST)
        {
            DebugUI.SetHpText(hostPlayer.Value.hp);
            DebugUI.SetShieldText(hostPlayer.Value.shield);
            DebugUI.SetStaminaText(hostPlayer.Value.stamina);
            DebugUI.ClearBuffText();
            /*
            foreach (BUFF buf in hostBufList)
            {
                DebugUI.AddBuffText($"{buf.type.ToString()}, val : {buf.value}, turn : {buf.turn}");
            }
            */
            foreach (BUFF buf in _networkHostBufList)
            {
                DebugUI.AddBuffText($"{buf.type.ToString()}, val : {buf.value}, turn : {buf.turn}");
            }
        }
        else
        {
            DebugUI.SetHpText(clientPlayer.Value.hp);
            DebugUI.SetShieldText(clientPlayer.Value.shield);
            DebugUI.SetStaminaText(clientPlayer.Value.stamina);
            DebugUI.ClearBuffText();
            /*
            foreach (BUFF buf in clientBufList)
            {
                DebugUI.AddBuffText($"{buf.type.ToString()}, val : {buf.value}, turn : {buf.turn}");
            }
            */
            foreach (BUFF buf in _networkClientBufList)
            {
                DebugUI.AddBuffText($"{buf.type.ToString()}, val : {buf.value}, turn : {buf.turn}");
            }
        }

        // 날아간 아이템 확인 및 비활성화
        foreach (GameObject go in hostItemList)
        {
            if (Mathf.Abs(go.transform.position.z) > 20)
            {
                go.SetActive(false);
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.Euler(0, 0, 0);
                go.GetComponent<Rigidbody>().useGravity = false;
                go.GetComponent<Rigidbody>().isKinematic = true;
                go.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                go.GetComponent<BaseItem>().ClearShoot();
            }
        }

        foreach (GameObject go in clientItemList)
        {
            if (Mathf.Abs(go.transform.position.z) > 20)
            {
                go.SetActive(false);
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.Euler(0, 180, 0);
                go.GetComponent<Rigidbody>().useGravity = false;
                go.GetComponent<Rigidbody>().isKinematic = true;
                go.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                go.GetComponent<BaseItem>().ClearShoot();
            }
        }

        if (NetworkManager.Singleton.IsHost)
        { // 해당 턴
            if (_turn.Value == PLAYERTYPE.HOST)
            {
                // 자신의 턴이면 카드 선택과 사용 가능
                EnableCardSelection(true);
            }
            else
            {
                // 자신의 턴이 아니면 카드 선택과 사용 불가능
                EnableCardSelection(false);
                // 설치된 오브젝트 턴마다 호출 불가
            }

            // 시간 감소를 호스트에서만 NetworkVariable에 적용
            _currentLeftTime.Value -= Time.deltaTime;

            if (_currentLeftTime.Value <= 0)
            {
                _particleList.Clear();

                // 시간 초기화
                _currentLeftTime.Value = _minTime + 3 * _turnCount++;
                if (_currentLeftTime.Value >= _maxTime)
                    _currentLeftTime.Value = _maxTime;


                // 턴 교체
                _turn.Value = (PLAYERTYPE)(((int)_turn.Value + 1) % 2);

                // 스태미나 증가 로직
                if (_turn.Value == PLAYERTYPE.HOST)
                {
                    // 설치된 아이템 효과 적용
                    ApplyInstalledItemsEffectsClientRpc();

                    PLAYERINFO hinfo = hostPlayer.Value;
                    hinfo.maxStamina++;
                    if (hinfo.maxStamina > _maxStamina)
                        hinfo.maxStamina = _maxStamina;
                    

                    int stamina = CheckStaminaBuffAndDecreaseCount(true);

                    if (stamina == -1)
                        hinfo.stamina = hinfo.maxStamina;
                    else
                        hinfo.stamina = stamina;

                    hostPlayer.Value = hinfo;
                    SetStaminaObject(hinfo.stamina);
                    SetStaminaObjectClientRpc(hinfo.stamina);

                    // 가진 버프 효과 적용
                    ProcBuff(false);
                }
                else
                {
                    // 설치된 아이템 효과 적용
                    ApplyInstalledItemsEffects(true);

                    PLAYERINFO cinfo = clientPlayer.Value;
                    cinfo.maxStamina++;
                    if (cinfo.maxStamina > _maxStamina)
                        cinfo.maxStamina = _maxStamina;

                    int stamina = CheckStaminaBuffAndDecreaseCount(false);

                    if (stamina == -1)
                        cinfo.stamina = cinfo.maxStamina;
                    else
                        cinfo.stamina = stamina;

                    clientPlayer.Value = cinfo;
                    SetStaminaObject(cinfo.stamina);
                    SetStaminaObjectClientRpc(cinfo.stamina);

                    // 가진 버프 효과 적용
                    ProcBuff(true);
                }
            }
        }
        else
        {
            if (_turn.Value == PLAYERTYPE.CLIENT)
            {
                // 자신의 턴이면 카드 선택과 사용 가능
                EnableCardSelection(true);
            }
            else
            {
                // 자신의 턴이 아니면 카드 선택과 사용 불가능
                EnableCardSelection(false);
                // 설치된 오브젝트 턴마다 호출 불가
            }
        }
    }

    /// <summary>
    /// 설치된 아이템들의 효과를 적용
    /// </summary>
    private void ApplyInstalledItemsEffects(bool isHost)
    {
        bool isCoroutineStarted = false;
        if (isHost)
        {
            for (int i = hostInstalledList.Count - 1; i >= 0; i--)
            {
                GameObject itemObj = hostInstalledList[i];
                BaseItem item = itemObj.GetComponent<BaseItem>();

                if (item != null)
                {
                    // 아이템 효과 적용
                    item.InstallPassive();
                    item.ClearShoot();
                    // 파티클이 한번에 터지면 맵이 복잡해짐.. 하나씩 터지는게 맞지 않나..
                    _particleList.Add(item.GetComponent<BaseItem>());
                    if (!isCoroutineStarted){
                        isCoroutineStarted = true;
                        StartCoroutine(PlayParticleCoroutine());
                    }

                    // 만료된 아이템 제거
                    if (item.IsExpired())
                    {
                        itemObj.SetActive(false);
                        FindAnyObjectByType<CardObjectSpawner>()?.DeleteOccupiedPosition(itemObj.transform.position);
                        item.Uninstall();
                        hostInstalledList.RemoveAt(i);
                        DespawnItemObjectClientRpc(item.GetOid(), item.GetInteractionType());
                        if (NetworkManager.Singleton.IsHost)
                            EnableCard(item.name.Split('_')[1]);
                    }
                }
            }

            if (NetworkManager.Singleton.IsHost)
            {
                foreach (GameObject obj in _cardArray.Values)
                {
                    string json = obj.GetComponent<CardDisplay>().GetJsonData();
                    JSONNode node = JSON.Parse(json);
                    int oid = node["oid"];
                    if (GameObject.Find($"Item_{oid}") == null)
                        obj.SetActive(true);
                }
            }
            
        }
        else
        {
            for (int i = clientInstalledList.Count - 1; i >= 0; i--)
            {
                GameObject itemObj = clientInstalledList[i];
                BaseItem item = itemObj.GetComponent<BaseItem>();

                if (item != null)
                {
                    // 아이템 효과 적용
                    item.InstallPassive();
                    item.ClearShoot();
                    _particleList.Add(item.GetComponent<BaseItem>());
                    if (!isCoroutineStarted)
                    {
                        isCoroutineStarted = true;
                        StartCoroutine(PlayParticleCoroutine());
                    }

                    // 만료된 아이템 제거
                    if (item.IsExpired())
                    {
                        itemObj.SetActive(false);
                        FindAnyObjectByType<CardObjectSpawner>()?.DeleteOccupiedPosition(itemObj.transform.position);
                        item.Uninstall();
                        clientInstalledList.RemoveAt(i);
                        DespawnItemObjectServerRpc(item.GetOid(), item.GetInteractionType());
                        if (!NetworkManager.Singleton.IsHost)
                            EnableCard(item.name.Split('_')[1]);
                    }
                }
            }

            if (!NetworkManager.Singleton.IsHost)
            {
                foreach (GameObject obj in _cardArray.Values)
                {
                    string json = obj.GetComponent<CardDisplay>().GetJsonData();
                    JSONNode node = JSON.Parse(json);
                    int oid = node["oid"];
                    if (GameObject.Find($"Item_{oid}") == null)
                        obj.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// 아이템 설치
    /// </summary>
    public void InstallItem(GameObject itemObj, bool isHost)
    {
        BaseItem item = itemObj.GetComponent<BaseItem>();
        if (item != null && !item.IsInstalled())
        {
            item.Install();
            
            if (isHost)
            {
                hostInstalledList.Add(itemObj);
            }
            else
            {
                clientInstalledList.Add(itemObj);
            }
        }
    }

    /// <summary>
    /// 아이템 사용
    /// </summary>
    public bool UseItem(GameObject itemObj, bool isHost)
    {
        BaseItem item = itemObj.GetComponent<BaseItem>();
        if (item != null && !item.IsShooted())
        {
            item.Use();
            return true;
        }
        return false;
    }

    public static GameManager GetInstance()
    {
        if (_instance == null)
        {
            _instance = FindAnyObjectByType<GameManager>();
        }

        return _instance;
    }

    public int GetStamina(bool isHost)
    {
        if (isHost)
        {
            return hostPlayer.Value.stamina;
        }
        else
        {
            return clientPlayer.Value.stamina;
        }
    }

    public int GetMaxStamina(bool isHost)
    {
        if (isHost)
            return hostPlayer.Value.maxStamina;
        else
            return clientPlayer.Value.maxStamina;
    }

    public bool Attack(int damage, int stamina, bool isHost)
    {
        if (isHost)
        {
            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamage(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in hostBufList)
            {
                if (buf.type == BUFTYPE.ATTACKMISS)
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                        realDamage = 0;
                    else
                        break;
                }
            }

            // REFLECT 버프가 있는 경우
            for (int i = 0; i < clientBufList.Count; i++)
            {
                BUFF buf = clientBufList[i];
                PLAYERINFO hinfo = hostPlayer.Value;
                if (buf.type == BUFTYPE.REFLECT)
                {
                    if (hostPlayer.Value.shield > 0)
                    {
                        if (hostPlayer.Value.shield > (int)realDamage)
                            hinfo.shield -= (int)realDamage;
                        else
                        {
                            hinfo.hp -= ((int)realDamage - hinfo.shield);
                            hinfo.shield = 0;
                        }
                    } else
                    {
                        hinfo.hp -= (int)realDamage;
                    }

                    hostPlayer.Value = hinfo;

                    realDamage = 0;
                    buf.value--;

                    if (buf.value == 0)
                        clientBufList.RemoveAt(i);
                    else
                        clientBufList[i] = buf;

                    return true;
                }
            }

            PLAYERINFO cinfo = clientPlayer.Value;
            if (clientPlayer.Value.shield > 0)
            {
                if (clientPlayer.Value.shield > (int)realDamage)
                    cinfo.shield -= (int)realDamage;
                else
                {
                    cinfo.hp -= ((int)realDamage - cinfo.shield);
                    cinfo.shield = 0;
                }
            }
            else
            {
                cinfo.hp -= (int)realDamage;
            }
            clientPlayer.Value = cinfo;
            return true;
            // 성공
        } else
        {
            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamage(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in clientBufList)
            {
                if (buf.type == BUFTYPE.ATTACKMISS)
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                        realDamage = 0;
                    else
                        break;
                }
            }

            // REFLECT 버프가 있는 경우
            for (int i = 0; i < hostBufList.Count; i++)
            {
                BUFF buf = hostBufList[i];
                PLAYERINFO cinfo = clientPlayer.Value;
                if (buf.type == BUFTYPE.REFLECT)
                {
                    if (clientPlayer.Value.shield > 0)
                    {
                        if (clientPlayer.Value.shield > (int)realDamage)
                        {
                            AddClientShieldServerRpc(-(int)realDamage);
                        }
                        else
                        {
                            AddClientHpServerRpc(-((int)realDamage - cinfo.shield));
                            AddClientShieldServerRpc(-cinfo.shield);
                        }
                    }
                    else
                    {
                        AddClientHpServerRpc(-(int)realDamage);
                    }

                    realDamage = 0;
                    buf.value--;

                    if (buf.value == 0)
                        hostBufList.RemoveAt(i);
                    else
                        hostBufList[i] = buf;

                    return true;
                }
            }

            PLAYERINFO hinfo = hostPlayer.Value;
            if (hostPlayer.Value.shield > 0)
            {
                if (hostPlayer.Value.shield > (int)realDamage)
                {
                    AddHostShieldServerRpc(-(int)realDamage);
                }
                else
                {
                    AddHostHpServerRpc(-((int)realDamage - hinfo.shield));
                    AddHostShieldServerRpc(-hinfo.shield);
                }
            }
            else
            {
                AddHostHpServerRpc(-(int)realDamage);
            }

            return true;
        }
    }

    public bool IgnoreGuardAttack(int damage, int stamina, bool isHost)
    {
        if (isHost)
        {
            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamageWithoutGuard(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in hostBufList)
            {
                if (buf.type == BUFTYPE.ATTACKMISS)
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                        realDamage = 0;
                    else
                        break;
                }
            }

            // 방어 무시 적용
            PLAYERINFO cinfo = clientPlayer.Value;
            if (clientPlayer.Value.shield > 0)
            {
                if (cinfo.shield > (int)realDamage)
                    cinfo.shield -= (int)realDamage;
                else
                {
                    cinfo.shield = 0;
                    cinfo.hp -= ((int)realDamage - cinfo.shield);
                }
            }
            else
            {
                cinfo.hp -= (int)realDamage;
            }
            clientPlayer.Value = cinfo;

            return true;
            // 성공
        }
        else
        {
            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamageWithoutGuard(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in clientBufList)
            {
                if (buf.type == BUFTYPE.ATTACKMISS)
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                        realDamage = 0;
                    else
                        break;
                }
            }

            // 방어 무시 적용
            PLAYERINFO hinfo = hostPlayer.Value;
            if (hinfo.shield > 0)
            {
                if (hinfo.shield > (int)realDamage)
                {
                    AddHostShieldServerRpc(-(int)realDamage);
                }
                else
                {
                    AddHostShieldServerRpc(-hinfo.shield);
                    AddHostHpServerRpc(-((int)realDamage - hinfo.shield));
                }
            }
            else
            {
                AddHostHpServerRpc(-(int)realDamage);
            }

            return true;
        }
    }

    public bool CheckCanUseItemAndDecreaseStamina(bool isHost, int stamina)
    {

        Debug.Log($"CheckCanUseItemAndDecreaseStamina {isHost} {stamina}");

        if (isHost)
        {
            PLAYERINFO hinfo = hostPlayer.Value;
            Debug.Log($"CheckCanUseItemAndDecreaseStamina {hinfo.stamina} {stamina}");
            if (hinfo.stamina < stamina)
                return false;

            hinfo.stamina -= stamina;
            hostPlayer.Value = hinfo;
        }
        else
        {
            PLAYERINFO cinfo = clientPlayer.Value;
            if (cinfo.stamina < stamina)
                return false;

            AddClientStaminaServerRpc(-stamina);
        }

        return true;
    }

    public void SetBuff(BUFTYPE type, float value, int turn, bool isHost)
    {
        BUFF buf = new BUFF();
        buf.type = type;
        buf.value = value;
        buf.turn = turn;

        if (isHost)
        {
            hostBufList.Add(buf);
            _networkHostBufList.Add(buf);
        }
        else
        {
            clientBufList.Add(buf);
            _networkClientBufList.Add(buf);
        }
        UpdateBuffStatus();
    }

    public void RemoveBuff(BUFTYPE type, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostBufList.Count - 1; i >= 0; i--)
            {
                if (hostBufList[i].type == type)
                {
                    hostBufList.RemoveAt(i);
                    _networkHostBufList.RemoveAt(i);
                }
            }
        }
        else
        {
            for (int i = clientBufList.Count - 1; i >= 0; i--)
            {
                if (clientBufList[i].type == type)
                {
                    clientBufList.RemoveAt(i);
                    _networkClientBufList.RemoveAt(i);
                }
            }
        }
        UpdateBuffStatus();
    }

    public void RemoveDeBuff(BUFTYPE type, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostBufList.Count - 1; i >= 0; i--)
            {
                if (hostBufList[i].type == type)
                {
                    if (hostBufList[i].value < 0 || hostBufList[i].type == BUFTYPE.BLEED || hostBufList[i].type == BUFTYPE.UNHEAL)
                    {
                        hostBufList.RemoveAt(i);
                        _networkHostBufList.RemoveAt(i);
                    }
                }
            }
        }
        else
        {
            for (int i = clientBufList.Count - 1; i >= 0; i--)
            {
                if (clientBufList[i].type == type || clientBufList[i].type == BUFTYPE.BLEED || clientBufList[i].type == BUFTYPE.UNHEAL)
                {
                    if (clientBufList[i].value < 0)
                    {
                        clientBufList.RemoveAt(i);
                        _networkClientBufList.RemoveAt(i);
                    }
                }
            }
        }
        UpdateBuffStatus();
    }

    public void ReverseBuff(bool isHost)
    {
        if (isHost)
        {
            for (int i = hostBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostBufList[i];
                if (buf.type == BUFTYPE.INCATTACK || buf.type == BUFTYPE.INCDEFENSE)
                {
                    buf.value = -buf.value;
                    hostBufList[i] = buf;
                    _networkHostBufList[i] = buf;
                }
            }
        }
        else
        {
            for (int i = clientBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientBufList[i];
                if (buf.type == BUFTYPE.INCATTACK || buf.type == BUFTYPE.INCDEFENSE)
                {
                    buf.value = -buf.value;
                    clientBufList[i] = buf;
                    _networkClientBufList[i] = buf;
                }
            }
        }
        UpdateBuffStatus();
    }

    public void SetShield(int shield, bool isHost)
    {
        Debug.Log($"SetShield {shield}, {isHost}");
        if (isHost)
        {
            PLAYERINFO hinfo = hostPlayer.Value;
            hinfo.shield += shield;
            Debug.Log($"SetShield value : {hinfo.shield}");
            hostPlayer.Value = hinfo;
        } 
        else
        {
            PLAYERINFO cinfo = clientPlayer.Value;
            cinfo.shield += shield;
            AddClientShieldServerRpc(shield);
        }
    }

    private PLAYERTYPE IsGameOver()
    {
        // 0 : false, 1: 첫번째 플레이어가 승리, 2: 두번째 플레이어가 승리
        // update 에서 계속 확인

        if (hostPlayer.Value.hp <= 0)
            return PLAYERTYPE.CLIENT;
        else if (clientPlayer.Value.hp <= 0)
            return PLAYERTYPE.HOST;
        else
            return PLAYERTYPE.NONE;
    }

    /*
     
    아이템에 필요한 데이터를 (데이터, 효과 적용)
     
     */
    private float ProcBuffAndCalcDamage(bool isHost)
    {
        float ret = 1;
        if (isHost)
        {
            // INCATTACK, INCDEFENSE
            foreach (BUFF buf in hostBufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in clientBufList)
            {
                if (buf.type == BUFTYPE.INCDEFENSE)
                {
                    ret -= buf.value;
                }
            }

            // GUARD
            for (int i = clientBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientBufList[i];
                if (buf.type == BUFTYPE.GUARD)
                {
                    buf.value--;
                    ret = 0;
                }
                clientBufList[i] = buf;

                if (clientBufList[i].value == 0)
                {
                    clientBufList.RemoveAt(i);
                    _networkClientBufList.RemoveAt(i);
                }
            }
        }
        else
        {
            // INCATTACK, INCDEFENSE
            foreach (BUFF buf in clientBufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in hostBufList)
            {
                if (buf.type == BUFTYPE.INCDEFENSE)
                {
                    ret -= buf.value;
                }
            }

            // GUARD
            for (int i = hostBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostBufList[i];
                if (buf.type == BUFTYPE.GUARD)
                {
                    buf.value--;
                    ret = 0;
                }
                hostBufList[i] = buf;

                if (hostBufList[i].value == 0)
                {
                    hostBufList.RemoveAt(i);
                    _networkHostBufList.RemoveAt(i);
                }
            }


        }
        UpdateBuffStatus();

        return ret;
    }

    /*
     
   방어 무시 적용 

     */
    private float ProcBuffAndCalcDamageWithoutGuard(bool isHost)
    {
        float ret = 1;
        if (isHost)
        {
            // INCATTACK, INCDEFENSE
            foreach (BUFF buf in hostBufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in clientBufList)
            {
                if (buf.type == BUFTYPE.INCDEFENSE)
                {
                    ret -= buf.value;
                }
            }
        }
        else
        {
            // INCATTACK, INCDEFENSE
            foreach (BUFF buf in clientBufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in hostBufList)
            {
                if (buf.type == BUFTYPE.INCDEFENSE)
                {
                    ret -= buf.value;
                }
            }
        }
        UpdateBuffStatus();

        return ret;
    }


    /*
     
    턴마다 호출
    턴, 버프, 효과 적용
     
     */

    public void ProcBuff(bool isHost)
    {
        bool unheal = false;
        if (isHost)
        {
            foreach (BUFF buf in hostBufList)
            {
                if (buf.type == BUFTYPE.UNHEAL)
                {
                    unheal = true;
                    break;
                }
            }


            PLAYERINFO hinfo = hostPlayer.Value;
            // HEAL, ZEROSTAMINA, BLEED
            foreach (BUFF buf in hostBufList)
            {
                switch (buf.type)
                {
                    case BUFTYPE.HEAL:
                        if (!unheal)
                        {
                            hinfo.hp += (int)(buf.value * hinfo.hp);
                            if (hinfo.hp > _maxHP)
                                hinfo.hp = _maxHP;
                        }
                        break;
                    case BUFTYPE.BLEED:
                        hinfo.hp -= (int)buf.value;
                        break;
                    case BUFTYPE.SHIELD:
                        hinfo.shield += (int)buf.value;
                        break;
                }
            }
            hostPlayer.Value = hinfo;

            // TURN Decrease
            for (int i = hostBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostBufList[i];
                if (buf.type != BUFTYPE.ZEROSTAMINA && buf.type != BUFTYPE.INCSTAMINA && buf.type != BUFTYPE.GUARD)
                    buf.turn--;
                hostBufList[i] = buf;
                _networkHostBufList[i] = buf;

                if (hostBufList[i].turn == 0)
                {
                    hostBufList.RemoveAt(i);
                    _networkHostBufList.RemoveAt(i);
                }
            }
        } 
        else
        {
            foreach (BUFF buf in clientBufList)
            {
                if (buf.type == BUFTYPE.UNHEAL)
                {
                    unheal = true;
                    break;
                }
            }

            PLAYERINFO cinfo = clientPlayer.Value;
            // HEAL, ZEROSTAMINA, BLEED
            foreach (BUFF buf in clientBufList)
            {
                switch (buf.type)
                {
                    case BUFTYPE.HEAL:
                        if (!unheal)
                        {
                            cinfo.hp += (int)(buf.value * cinfo.hp);
                            if (cinfo.hp > _maxHP)
                                cinfo.hp = _maxHP;
                        }
                        break;
                    case BUFTYPE.BLEED:
                        cinfo.hp -= (int)buf.value;
                        break;
                    case BUFTYPE.SHIELD:
                        cinfo.shield += (int)buf.value;
                        break;
                }
            }
            clientPlayer.Value = cinfo;

            // TURN Decrease
            for (int i = clientBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientBufList[i];
                if (buf.type != BUFTYPE.ZEROSTAMINA && buf.type != BUFTYPE.INCSTAMINA && buf.type != BUFTYPE.GUARD)
                    buf.turn--;
                clientBufList[i] = buf;
                _networkClientBufList[i] = buf;

                if (clientBufList[i].turn == 0)
                {
                    clientBufList.RemoveAt(i);

                    _networkClientBufList.RemoveAt(i);
                }
            }
        }
        UpdateBuffStatus();
    }

    public int CheckStaminaBuffAndDecreaseCount(bool isHost)
    {
        if (isHost)
        {
            for (int i = 0; i < hostBufList.Count; i++)
            {
                BUFF buf = hostBufList[i];
                if (buf.type == BUFTYPE.ZEROSTAMINA)
                {
                    buf.turn--;
                    hostBufList[i] = buf;
                    _networkHostBufList[i] = buf;
                    return 0;
                } else if (buf.type == BUFTYPE.INCSTAMINA)
                {
                    buf.turn--;
                    PLAYERINFO hinfo = hostPlayer.Value;
                    hinfo.stamina = hinfo.maxStamina + (int)buf.value + 1;
                    if (hinfo.stamina < 0)
                        hinfo.stamina = 0;
                    else if (hinfo.stamina > 10)
                        hinfo.stamina = 10;

                    hostBufList[i] = buf;
                    _networkHostBufList[i] = buf;
                    hostPlayer.Value = hinfo;
                    return hinfo.stamina;
                }
            }
        }
        else
        {
            for (int i = 0; i < clientBufList.Count; i++)
            {
                BUFF buf = clientBufList[i];
                if (buf.type == BUFTYPE.ZEROSTAMINA)
                {
                    buf.turn--;
                    clientBufList[i] = buf;
                    _networkClientBufList[i] = buf;
                    return 0;
                }
                else if (buf.type == BUFTYPE.INCSTAMINA)
                {
                    buf.turn--;
                    PLAYERINFO cinfo = clientPlayer.Value;
                    cinfo.stamina = cinfo.maxStamina + (int)buf.value + 1;
                    if (cinfo.stamina < 0)
                        cinfo.stamina = 0;
                    else if (cinfo.stamina > 10)
                        cinfo.stamina = 10;
                    clientBufList[i] = buf;
                    _networkClientBufList[i] = buf;
                    clientPlayer.Value = cinfo;
                    return cinfo.stamina;
                }
            }
        }
        UpdateBuffStatus();

        return -1;
    }

    public void IncreaseBuffValue(BUFTYPE type, float value, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostBufList[i];
                if (buf.type == type)
                {
                    buf.value *= (value + 1);
                    hostBufList[i] = buf;
                    _networkHostBufList[i] = buf;
                }
            }
        }
        else
        {
            for (int i = clientBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientBufList[i];
                if (buf.type == type)
                {
                    buf.value *= (value + 1);
                    clientBufList[i] = buf;
                    _networkClientBufList[i] = buf;
                }
            }
        }
        UpdateBuffStatus();

    }

    public void IncreaseAllItemsExpireCount(int value, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostItemList.Count - 1; i >= 0; i--)
            {
                GameObject item = hostItemList[i];
                item.GetComponent<BaseItem>().IncreaseExpireCount();
                hostItemList[i] = item;
            }
        }
        else
        {
            for (int i = clientItemList.Count - 1; i >= 0; i--)
            {
                GameObject item = clientItemList[i];
                item.GetComponent<BaseItem>().IncreaseExpireCount();
                clientItemList[i] = item;
            }
        }
    }

    public void IncreaseAllItemsStats(float value, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostItemList.Count - 1; i >= 0; i--)
            {
                GameObject item = hostItemList[i];
                item.GetComponent<BaseItem>().IncreaseStats(value);
                hostItemList[i] = item;
            }
        }
        else
        {
            for (int i = clientItemList.Count - 1; i >= 0; i--)
            {
                GameObject item = clientItemList[i];
                item.GetComponent<BaseItem>().IncreaseStats(value);
                clientItemList[i] = item;
            }
        }
    }

    public bool IsX2(bool isHost)
    {
        if (isHost)
        {
            for (int i = hostBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostBufList[i];
                if (buf.type == BUFTYPE.X2)
                {
                    hostBufList.RemoveAt(i);
                    _networkHostBufList.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        else
        {
            for (int i = clientBufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientBufList[i];
                if (buf.type == BUFTYPE.X2)
                {
                    clientBufList.RemoveAt(i);
                    _networkClientBufList.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }

    /*
     
    설치된 아이템의 효과를 관리하는 배열로 변경해야 함

     */
    public void RandomDestroyItem(int count, bool isHost)
    {
        int itemCount = -1;

        if (isHost)
        {   
            while (true)
            {
                itemCount = hostInstalledList.Count;

                if (count <= 0 || itemCount == 0)
                    break;

                int randomIdx = UnityEngine.Random.Range(0, itemCount);

                hostInstalledList[randomIdx].SetActive(false);
                _cardSpawner.DeleteOccupiedPosition(hostInstalledList[randomIdx].transform.position);
                RemoveHostItemServerRpc(hostInstalledList[randomIdx].name);
                hostInstalledList.RemoveAt(randomIdx);

                count--;
            }
        }
        else
        {
            while (true)
            {
                itemCount = clientInstalledList.Count;

                if (count <= 0 || itemCount == 0)
                    break;

                int randomIdx = UnityEngine.Random.Range(0, itemCount);
                clientInstalledList[randomIdx].SetActive(false);
                _cardSpawner.DeleteOccupiedPosition(clientInstalledList[randomIdx].transform.position);
                RemoveClientItemClientRpc(clientInstalledList[randomIdx].name);
                clientInstalledList.RemoveAt(randomIdx);

                count--;
            }
        }
    }

    public void EndTurn(bool isHost)
    {
        // 나중에 지우기
        _currentLeftTime.Value = 0;

        if ((_turn.Value == PLAYERTYPE.HOST && isHost))
            _currentLeftTime.Value = 0;
        else if ((_turn.Value == PLAYERTYPE.CLIENT && !isHost))
        {
            EndTurnServerRpc();
        }
    }

    // OID로부터 bigClass 가져오기
    private string GetBigClassFromOid(int oid)
    {
        if (!string.IsNullOrEmpty(PresetJsonData))
        {
            try
            {
                var jsonData = JSON.Parse(PresetJsonData);
                foreach (JSONNode node in jsonData["items"].AsArray)
                {
                    if (node["oid"].AsInt == oid)
                    {
                        return node["bigClass"].Value;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
            }
        }
        return "WriteInst"; // 기본값
    }

    // OID로부터 smallClass 가져오기
    private string GetSmallClassFromOid(int oid)
    {
        if (!string.IsNullOrEmpty(PresetJsonData))
        {
            try
            {
                var jsonData = JSON.Parse(PresetJsonData);
                foreach (JSONNode node in jsonData["items"].AsArray)
                {
                    if (node["oid"].AsInt == oid)
                    {
                        return node["smallClass"].Value;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
            }
        }
        return "Glue"; // 기본값
    }

    // OID로부터 stat 값 가져오기
    private int GetStatFromOid(int oid)
    {
        if (!string.IsNullOrEmpty(PresetJsonData))
        {
            try
            {
                var jsonData = JSON.Parse(PresetJsonData);
                foreach (JSONNode node in jsonData["items"].AsArray)
                {
                    if (node["oid"].AsInt == oid)
                    {
                        return node["stat"].AsInt;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
            }
        }
        return 10; // 기본값
    }

    // OID로부터 expireCount 값 가져오기
    private int GetExpireCountFromOid(int oid)
    {
        if (!string.IsNullOrEmpty(PresetJsonData))
        {
            try
            {
                var jsonData = JSON.Parse(PresetJsonData);
                foreach (JSONNode node in jsonData["items"].AsArray)
                {
                    if (node["oid"].AsInt == oid)
                    {
                        return node["expireCount"].AsInt;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
            }
        }
        return 3; // 기본값
    }

    private GameObject CreateItemObject(int oid, JSONNode itemData, GameObject obj = null)
    {
        // 아이템 오브젝트 생성
        obj.name = $"Item_{oid}";
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        obj.AddComponent<BoxCollider>();
        
        // bigClass와 smallClass에 따른 컴포넌트 추가
        string bigClass = itemData["bigClass"].Value.ToLower();
        string smallClass = itemData["smallClass"].Value.ToLower();
        obj = AddItemComponent(obj, bigClass, smallClass);

        // 아이템 정보 설정
        BaseItem item = obj.GetComponent<BaseItem>();
        if (item != null)
        {
            int stat = itemData["stat"].AsInt;
            int expireCount = itemData["expireCount"].AsInt;

            Debug.Log($"CreateItemObject {oid} {stat} {expireCount}");

            item.Init(oid, stat, expireCount);
        }

        return obj;
    }

    public IEnumerator LoadGLBModelCoroutine(string glbFilePath, int oid, JSONNode data, bool isHost)
    {
        var loader = new FileLoader(Path.GetDirectoryName(glbFilePath));
        var importer = new GLTFSceneImporter(
            Path.GetFileName(glbFilePath),
            new ImportOptions { DataLoader = loader }
        );
        importer.SceneParent = new GameObject("LoadedGLB").transform;

        UnityEngine.Debug.Log("GLB 로딩 시작: " + glbFilePath);

        var loadSceneTask = importer.LoadSceneAsync();
        while (!loadSceneTask.IsCompleted)
        {
            yield return null;
        }

        if (loadSceneTask.Exception != null)
        {
            UnityEngine.Debug.LogError("GLB 로딩 실패: " + loadSceneTask.Exception.Message);
        }
        else
        {

            // GLB 로딩 완료 후 결과 표시 UI 생성
            // ShowGLBResultUI(importer.SceneParent.gameObject);

            // CardObjectSpawner에 로딩 완료 알림
            //CardObjectSpawner cardSpawner = FindAnyObjectByType<CardObjectSpawner>();
            UnityEngine.Debug.Log($"GLB 로딩 완료 {oid}, cardSpawner {_cardSpawner}");

            // 이 아래 if (_cardSpawner != null) 조건문 삭제. 아무리 봐도 CardObjectSpawer를 쓰는 코드가 안보임
            // GLB 파일명에서 objectId 추출
            string fileName = Path.GetFileNameWithoutExtension(glbFilePath);
            GameObject loadedObject = importer.SceneParent.gameObject;

            // CardObjectSpawner의 ReplaceTempObjectWithGLB 메서드 호출
            GameObject newObj = CreateItemObject(oid, data, loadedObject);
            newObj.transform.SetParent(_itemParent.transform);
            newObj.SetActive(false);
            newObj.GetComponent<Rigidbody>().isKinematic = true;
            newObj.GetComponent<Rigidbody>().useGravity = false;

            // 디버깅용
            // Debug.Log($"{newObj.GetComponent<BaseItem>().GetOid()} : {newObj.GetComponent<BaseItem>().GetStat()}, {newObj.GetComponent<BaseItem>().GetStamina()}");

            if (isHost)
            {
                hostItemList.Add(newObj);
            }
            else
            {
                newObj.transform.rotation = Quaternion.Euler(0, 180, 0);
                clientItemList.Add(newObj);
            }
        }
    }

    private GameObject AddItemComponent(GameObject itemObj, string bigClass, string smallClass) {
        // bigClass에 따른 Passive 컴포넌트 추가
        switch (bigClass)
        {
            case "clean":
                itemObj.AddComponent<CleanPassive>();
                break;
            case "kitchen":
                itemObj.AddComponent<KitchenPassive>();
                break;
            case "writeinst":
                itemObj.AddComponent<WriteInstPassive>();
                break;
        }

        // smallClass에 따른 아이템 스크립트 추가
        itemObj = AddSmallItemComponent(itemObj, smallClass);
    
        return itemObj;
    }

    private GameObject AddSmallItemComponent(GameObject itemObj, string smallClass) {
        // smallClass에 따른 아이템 스크립트 추가
        switch (smallClass)
        {
            // Clean 아이템들
            case "airgun":
                itemObj.AddComponent<CleanAirgun>();
                break;
            case "broom":
                itemObj.AddComponent<CleanBroom>();
                break;
            case "bucket":
                itemObj.AddComponent<CleanBucket>();
                break;
            case "dishcloth":
                itemObj.AddComponent<CleanDishCloth>();
                break;
            case "duster":
                itemObj.AddComponent<CleanDuster>();
                break;
            case "dustpan":
                itemObj.AddComponent<CleanDustpan>();
                break;
            case "gloves":
                itemObj.AddComponent<CleanGloves>();
                break;
            case "mop":
                itemObj.AddComponent<CleanMop>();
                break;
            case "mopsqueezer":
                itemObj.AddComponent<CleanMopSqueezer>();
                break;
            case "sponge":
                itemObj.AddComponent<CleanSponge>();
                break;
            case "spray":
                itemObj.AddComponent<CleanSpray>();
                break;
            case "squeezer":
                itemObj.AddComponent<CleanSqueezer>();
                break;
            case "tapecleaner":
                itemObj.AddComponent<CleanTapeCleaner>();
                break;
            case "toiletbrush":
                itemObj.AddComponent<CleanToiletBrush>();
                break;
            case "vacuum":
                itemObj.AddComponent<CleanVacuum>();
                break;

            // Kitchen 아이템들
            case "basic":
                itemObj.AddComponent<KitchenBasic>();
                break;
            case "coffeepot":
                itemObj.AddComponent<KitchenCoffeepot>();
                break;
            case "container":
                itemObj.AddComponent<KitchenContainer>();
                break;
            case "cookpot":
                itemObj.AddComponent<KitchenCookpot>();
                break;
            case "cup":
                itemObj.AddComponent<KitchenCup>();
                break;
            case "knife":
                itemObj.AddComponent<KitchenKnife>();
                break;
            case "ladle":
                itemObj.AddComponent<KitchenLadle>();
                break;
            case "mbowl":
                itemObj.AddComponent<KitchenMbowl>();
                break;
            case "mcup":
                itemObj.AddComponent<KitchenMcup>();
                break;
            case "microwave":
                itemObj.AddComponent<KitchenMicrowave>();
                break;
            case "mspoon":
                itemObj.AddComponent<KitchenMspoon>();
                break;
            case "pan":
                itemObj.AddComponent<KitchenPan>();
                break;
            case "plate":
                itemObj.AddComponent<KitchenPlate>();
                break;
            case "ptowel":
                itemObj.AddComponent<KitchenPtowel>();
                break;
            case "scale":
                itemObj.AddComponent<KitchenScale>();
                break;
            case "spatula":
                itemObj.AddComponent<KitchenSpatula>();
                break;
            case "spoon":
                itemObj.AddComponent<KitchenSpoon>();
                break;
            case "strainer":
                itemObj.AddComponent<KitchenStrainer>();
                break;
            case "toaster":
                itemObj.AddComponent<KitchenToaster>();
                break;
            case "tongs":
                itemObj.AddComponent<KitchenTongs>();
                break;

            // WriteInst 아이템들
            case "ballpen":
                itemObj.AddComponent<WriteInstBallpen>();
                break;
            case "compass":
                itemObj.AddComponent<WriteInstCompass>();
                break;
            case "crayon":
                itemObj.AddComponent<WriteInstCrayon>();
                break;
            case "eraser":
                itemObj.AddComponent<WriteInstEraser>();
                break;
            case "fountainpen":
                itemObj.AddComponent<WriteInstFountainpen>();
                break;
            case "glue":
                itemObj.AddComponent<WriteInstGlue>();
                break;
            case "highlighterpen":
                itemObj.AddComponent<WriteInstHighlighterpen>();
                break;
            case "pencil":
                itemObj.AddComponent<WriteInstPencil>();
                break;
            case "ruler":
                itemObj.AddComponent<WriteInstRuler>();
                break;
            case "scissors":
                itemObj.AddComponent<WriteInstScissors>();
                break;
            case "tape":
                itemObj.AddComponent<WriteInstTape>();
                break;
        }

        return itemObj;
    }

    private void EnableCardSelection(bool enable)
    {
        Debug.Log($"EnableCardSelection {enable}, {_cardSpawner}");
        if (_cardSpawner != null)
        {
            _cardSpawner.SetCardInteraction(enable);
        }
    }

    public void SetStaminaObject(int stamina)
    {
        // 스태미나 객체 초기화
        for (int i = 0; i < _staminaParent.transform.childCount; i++)
        {
            GameObject child = _staminaParent.transform.GetChild(i).gameObject;
            child.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", Color.red);
        }

        // 스태미나 객체 색깔 변경
        for (int i = 0; i < stamina; i++)
        {
            GameObject child = _staminaParent.transform.GetChild(i).gameObject;
            child.GetComponent<MeshRenderer>().material.color = Color.green;
        }
    }

    public void AddHostInstallItemList(GameObject obj)
    {
        hostInstalledList.Add(obj);
    }

    public void AddClientInstallItemList(GameObject obj)
    {
        clientInstalledList.Add(obj);
    }

    private void EnableCard(string oid)
    {
        _cardArray[int.Parse(oid)].SetActive(true);
    }

    private void CheckExpireCount(bool isHost)
    {
        if (isHost)
        {
            foreach (GameObject go in hostInstalledList)
            {
                if (go.GetComponent<BaseItem>().GetExpireCount() <= 0)
                {
                    Destroy(go);
                }
            }
        }
        else
        {
            foreach (GameObject go in clientInstalledList)
            {
                if (go.GetComponent<BaseItem>().GetExpireCount() <= 0)
                {
                    Destroy(go);
                }
            }
        }
    }

    public Transform GetEnemy()
    {
        if (_enemy != null)
            return _enemy.transform;
        return null;
    }

    IEnumerator PlayParticleCoroutine()
    {
        while (_particleList.Count != 0)
        {
            _particleList[0].PlayUseParticle(GetEnemy());
            Debug.Log($"PlayParticle {_particleList[0].GetOid()}");
            if (NetworkManager.Singleton.IsHost)
                PlayParticleClientRpc(_particleList[0].GetOid());
            else
                PlayParticleServerRpc(_particleList[0].GetOid());

                yield return new WaitForSeconds(5);
            _particleList.RemoveAt(0);
        }
    }

    IEnumerator CheckAndSetAvarta()
    {
        while (true)
        {
            if (NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.SpawnedObjects != null)
                break;

            yield return new WaitForSeconds(0.1f);
        }

        if (NetworkManager.Singleton.IsHost)
        {
            foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                // 방금 스폰된 오브젝트가 내꺼라면
                if (netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId && netObj.GetComponent<XROrigin>() != null)
                {

                    var list = netObj.transform.GetComponentsInChildren<TargetSc>();
                    foreach (var c in list)
                    {
                        if (c.gameObject.name == "MainCamera_target")
                        {
                            _hostAvarta.GetComponent<VRRig>().head.vrTarget = c.gameObject.transform;
                        }
                        else if (c.gameObject.name == "LeftController_target")
                        {
                            _hostAvarta.GetComponent<VRRig>().leftHand.vrTarget = c.gameObject.transform;
                        }
                        else if (c.gameObject.name == "RightController_target")
                        {
                            _hostAvarta.GetComponent<VRRig>().rightHand.vrTarget = c.gameObject.transform;
                        }
                    }

                    //_clientAvarta.GetComponent<AnimationController>().move = null;
                    _cardSpawner = netObj.GetComponentInChildren<CardObjectSpawner>();
                }
                else if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId && netObj.GetComponent<XROrigin>() != null)
                {
                    Debug.Log($"Network Obj {netObj.name}");

                    // 굳이 설정해 줄 필요 있나? 
                }
            }
            StartCoroutine(SetAvatarOwnership());
            yield break;
        }
        else
        {
            while (true)
            {
                foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
                {
                    // 방금 스폰된 오브젝트가 내꺼라면
                    if (netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId && netObj.GetComponent<XROrigin>() != null)
                    {
                        Debug.Log($"Network Obj {netObj.name} Success");

                        var list = netObj.transform.GetComponentsInChildren<TargetSc>();

                        Transform head = null;
                        Transform left = null;
                        Transform right = null;

                        foreach (var c in list)
                        {
                            if (c.gameObject.name == "MainCamera_target")
                            {
                                _clientAvarta.GetComponent<VRRig>().head.vrTarget = c.gameObject.transform;
                                head = c.gameObject.transform;
                            }
                            else if (c.gameObject.name == "LeftController_target")
                            {
                                _clientAvarta.GetComponent<VRRig>().leftHand.vrTarget = c.gameObject.transform;
                                left = c.gameObject.transform;
                            }
                            else if (c.gameObject.name == "RightController_target")
                            {
                                _clientAvarta.GetComponent<VRRig>().rightHand.vrTarget = c.gameObject.transform;
                                right = c.gameObject.transform;
                            }
                        }

                        //_hostAvarta.GetComponent<AnimationController>().move = null;
                        _cardSpawner = netObj.GetComponentInChildren<CardObjectSpawner>();

                        //AvatarSyncManager.SetTransform(netObj.transform, head, left, right);

                        FindAnyObjectByType<PresetCardArrangement>().SetClientPos();
                        FindAnyObjectByType<PresetCardArrangement>().ArrangeCards(FindAnyObjectByType<PresetLoader>().presetCardArrangement.cards);

                        yield break;
                    }
                    else if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId && netObj.GetComponent<XROrigin>() != null)
                    {
                        Debug.Log($"Network Obj {netObj.name}");

                        // 굳이 설정해 줄 필요 있나? 
                    }

                    if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                    {
                        _enemy = netObj.gameObject;
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    IEnumerator SetAvatarOwnership()
    {
        while (true)
        {
            foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log($"NetworkID : {netObj.OwnerClientId} {NetworkManager.Singleton.LocalClientId}");
                    _enemy = netObj.gameObject;

                    NetworkObject[] transforms = _clientAvarta.GetComponentsInChildren<NetworkObject>();
                    foreach (var t in transforms)
                    {
                        t.ChangeOwnership(netObj.OwnerClientId);
                    }
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public override void OnDestroy()
    {
        // 이벤트 구독 해제
        if (RelaySingleton.Instance != null)
        {
            //RelaySingleton.Instance.OnRelayConnected -= OnRelayConnected;
        }
        
        // PresetLoader 이벤트 구독 해제
        PresetLoader.OnPresetDataLoaded -= OnPresetDataLoaded;
    }
    void UpdateBuffStatus() {

        // 수정 필요

        GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.RemoveAllBuff(true);
        GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.RemoveAllBuff(false);

        if (NetworkManager.Singleton.IsHost)
        {
            foreach (BUFF buf in _networkHostBufList)
            {
                GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.AddBuff(buf.type, buf.turn, true);
            }
            foreach (BUFF buf in _networkClientBufList)
            {
                GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.AddBuff(buf.type, buf.turn, false);
            }
            GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.SetHp(((float)hostPlayer.Value.hp / 1000), true);
            GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.SetHp(((float)clientPlayer.Value.hp / 1000), false);
        } else
        {
            foreach (BUFF buf in _networkClientBufList)
            {
                GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.AddBuff(buf.type, buf.turn, true);
            }
            foreach (BUFF buf in _networkHostBufList)
            {
                GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.AddBuff(buf.type, buf.turn, false);
            }

            GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.SetHp(((float)hostPlayer.Value.hp / 1000), false);
            GameObject.FindAnyObjectByType<BuffTooltipUIManager>()?.SetHp(((float)clientPlayer.Value.hp / 1000), true);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    void SetStaminaObjectClientRpc(int stamina)
    {
        if (IsOwner)
            return;
        SetStaminaObject(stamina);
    }

    [ServerRpc(RequireOwnership=false)]
    void EndTurnServerRpc()
    {
        
        _currentLeftTime.Value = 0;
    }

    [ServerRpc(RequireOwnership = false)]
    void TransmitClientJsonDataServerRpc(FixedString4096Bytes _json)
    {
        if (string.IsNullOrEmpty(_json.ToString()))
            return;

        try
        {
            var jsonData = JSON.Parse(_json.ToString());
            // JSON에서 oid 값들을 가져와서 배열로 구성
            List<int> otherOidList = new List<int>();
            foreach (JSONNode node in jsonData.AsArray)
            {
                otherOidList.Add(node["oid"].AsInt);
            }

            otherOidList.Sort();

            foreach (int _oid in otherOidList)
            {
                // OID에 해당하는 아이템 데이터 찾기
                JSONNode itemData = null;
                foreach (JSONNode node in jsonData.AsArray)
                {
                    if (node["oid"].AsInt == _oid)
                    {
                        itemData = node;
                        break;
                    }
                }

                if (itemData != null)
                {
                    // 아이템 오브젝트 생성
                    string path = Application.persistentDataPath + $"/objects/{_oid}.glb";
                    StartCoroutine(LoadGLBModelCoroutine(path, _oid, itemData, false));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void AddClientHpServerRpc(int hp)
    {
        PLAYERINFO cinfo = clientPlayer.Value;
        cinfo.hp += hp;
        if (cinfo.hp > _maxHP)
            cinfo.hp = _maxHP;
        clientPlayer.Value = cinfo;
    }

    [ServerRpc(RequireOwnership = false)]
    void AddClientShieldServerRpc(int shield)
    {
        PLAYERINFO cinfo = clientPlayer.Value;
        cinfo.shield += shield;
        clientPlayer.Value = cinfo;
    }

    [ServerRpc(RequireOwnership = false)]
    void AddClientStaminaServerRpc(int stamina)
    {
        PLAYERINFO cinfo = clientPlayer.Value;
        cinfo.stamina += stamina;
        if (cinfo.stamina > cinfo.maxStamina)
            cinfo.stamina = cinfo.maxStamina;
        clientPlayer.Value = cinfo;
    }

    [ServerRpc(RequireOwnership = false)]
    void AddHostHpServerRpc(int hp)
    {
        PLAYERINFO hinfo = hostPlayer.Value;
        hinfo.hp += hp;
        if (hinfo.hp > _maxHP)
            hinfo.hp = _maxHP;
        hostPlayer.Value = hinfo;
    }

    [ServerRpc(RequireOwnership = false)]
    void AddHostShieldServerRpc(int shield)
    {
        PLAYERINFO hinfo = hostPlayer.Value;
        hinfo.shield += shield;
        hostPlayer.Value = hinfo;
    }

    [ServerRpc(RequireOwnership = false)]
    void AddHostStaminaServerRpc(int stamina)
    {
        PLAYERINFO hinfo = hostPlayer.Value;
        hinfo.stamina += stamina;
        if (hinfo.stamina > hinfo.maxStamina)
            hinfo.stamina = hinfo.maxStamina;
        hostPlayer.Value = hinfo;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetBuffServerRpc(BUFTYPE type, float value, int turn, bool isHost)
    {
        SetBuff(type, value, turn, isHost);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveBuffServerRpc(BUFTYPE type, bool isHost)
    {
        RemoveBuff(type, isHost);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveDeBuffServerRpc(BUFTYPE type, bool isHost)
    {
        RemoveDeBuff(type, isHost);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReverseBuffServerRpc(bool isHost)
    {
        ReverseBuff(isHost);
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseBuffValueServerRpc(BUFTYPE type, float value, bool isHost)
    {
        IncreaseBuffValue(type, value, isHost);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AttackServerRpc(int damage, int stamina, bool self = false)
    {
        Attack(damage, stamina, self);

    }

    [ServerRpc(RequireOwnership = false)]
    public void IgnoreGuardAttackServerRpc(int damage, int stamina, bool isHost)
    {
        IgnoreGuardAttack(damage, stamina, isHost);
    }

    [ClientRpc]
    public void RemoveClientItemClientRpc(string itemName)
    {
        if (IsOwner)
            return;

        foreach (GameObject go in clientInstalledList)
        {
            if (go.name == itemName)
            {
                go.GetComponent<BaseItem>().Uninstall();
                go.SetActive(false);
                clientInstalledList.Remove(go);
                break;
            }
        }

        
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveHostItemServerRpc(string itemName)
    {
        foreach (GameObject go in hostInstalledList)
        {
            if (go.name == itemName)
            {
                go.GetComponent<BaseItem>().Uninstall();
                go.SetActive(false);
                hostInstalledList.Remove(go);
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseAllItemsExpireCountServerRpc(int value)
    {
        IncreaseAllItemsExpireCount(value, false);
    }

    [ClientRpc]
    public void IncreaseAllItemsExpireCountClientRpc(int value)
    {
        if (IsOwner)
            return;
        IncreaseAllItemsExpireCount(value, true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseAllItemsStatsServerRpc(float value)
    {
        IncreaseAllItemsStats(value, false);
    }

    [ClientRpc]
    public void IncreaseAllItemsStatsClientRpc(float value)
    {
        if (IsOwner)
            return;
        IncreaseAllItemsStats(value, true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnItemObjectServerRpc(string oid, UnityEngine.Vector3 position, BaseItem.InteractionType type) 
    {
        // 클라이언트가 서버 플레이어에게 소환한 것을 전파

        foreach (GameObject obj in clientItemList)
        {
            if (obj.name == $"Item_{oid}")
            {
                obj.SetActive(true);
                obj.GetComponent<Rigidbody>().isKinematic = true;
                obj.transform.position = position;
                if (type == BaseItem.InteractionType.Install)
                    clientInstalledList.Add(obj);
                return;
            }
        }
    }

    [ClientRpc]
    public void SpawnItemObjectClientRpc(string oid, UnityEngine.Vector3 position, BaseItem.InteractionType type)
    {
        // 서버가 클라이언트 플레이어에게 소환한 것을 전파
        if (IsOwner)
            return;

        foreach (GameObject obj in hostItemList)
        {
            if (obj.name == $"Item_{oid}")
            {
                obj.SetActive(true);
                obj.GetComponent<Rigidbody>().isKinematic = true;
                obj.transform.position = position;
                if (type == BaseItem.InteractionType.Install)
                    hostInstalledList.Add(obj);
                return;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnItemObjectServerRpc(int oid, BaseItem.InteractionType type)
    {
        // 클라이언트가 서버 플레이어에게 소환한 것을 전파
        foreach (GameObject obj in clientItemList)
        {
            if (obj.name == $"Item_{oid}")
            {
                obj.SetActive(false);
                obj.GetComponent<Rigidbody>().isKinematic = true;
                obj.transform.position = Vector3.zero;
                if (type == BaseItem.InteractionType.Install)
                    clientInstalledList.Remove(obj);
                return;
            }
        }
    }

    [ClientRpc]
    public void DespawnItemObjectClientRpc(int oid, BaseItem.InteractionType type)
    {
        // 서버가 클라이언트 플레이어에게 소환한 것을 전파
        if (IsOwner)
            return;

        foreach (GameObject obj in hostItemList)
        {
            if (obj.name == $"Item_{oid}")
            {
                obj.SetActive(false);
                obj.GetComponent<Rigidbody>().isKinematic = true;
                obj.transform.position = Vector3.zero;
                if (type == BaseItem.InteractionType.Install)
                    hostInstalledList.Remove(obj);
                return;
            }
        }
    }

    [ClientRpc]
    void ApplyInstalledItemsEffectsClientRpc()
    {
        if (IsOwner)
            return;
        ApplyInstalledItemsEffects(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShootServerRpc(int oid)
    {
        foreach (GameObject go in clientItemList)
        {
            if (go.name == $"Item_{oid}")
            {
                go.GetComponent<BaseItem>().Shoot();
                return;
            }
        }
    }

    [ClientRpc]
    public void ShootClientRpc(int oid)
    {
        if (IsOwner)
            return;

        foreach (GameObject go in hostItemList)
        {
            if (go.name == $"Item_{oid}")
            {
                go.GetComponent<BaseItem>().Shoot();
                return;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayParticleServerRpc(int oid)
    {
        foreach (GameObject go in clientItemList)
        {
            if (go.name == $"Item_{oid}")
            {
                go.GetComponent<BaseItem>().PlayUseParticle(_enemy.transform);
                return;
            }
        }
    }

    [ClientRpc]
    public void PlayParticleClientRpc(int oid)
    {
        if (IsOwner)
            return;

        foreach (GameObject go in hostItemList)
        {
            if (go.name == $"Item_{oid}")
            {
                go.GetComponent<BaseItem>().PlayUseParticle(_enemy.transform);
                return;
            }
        }
    }
}
