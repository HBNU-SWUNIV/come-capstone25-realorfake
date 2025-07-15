using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;
using System.Security.Cryptography;
using SimpleJSON;
using System.Linq;
using System.Numerics;
using System.Collections;
using Unity.Collections;

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

    public struct BUFF
    {
        public BUFTYPE type;
        public float value;
        public int turn;
    }

    private struct PLAYERINFO
    {
        public int uid;
        public int hp;
        public int shield;
        public int stamina;
        public List<GameObject> itemList;
        public List<GameObject> installedList;
        public List<BUFF> bufList;
    }

    private static GameManager _instance;
    private GameManager() { } // 싱글톤 생성

     public static string PresetJsonData { get; set; } // 프리셋 데이터 

    // 플레이어 정보
    private static PLAYERINFO hostPlayer;
    private static PLAYERINFO clientPlayer;

    private NetworkVariable<PLAYERTYPE> _turn;
    private int _turnCount;

    private NetworkVariable<float> _currentLeftTime; // 지금 턴의 남은 시간
    private NetworkVariable<int> _currentStamina;
    private NetworkVariable<FixedString128Bytes> _hostPresetJsonData;
    private NetworkVariable<FixedString128Bytes> _clientPresetJsonData;

    private const int _minTime = 30;
    private const int _maxTime = 60;

    private const int _maxHP = 100;

    private bool _host;
    private volatile bool _isGameStarted = false; // 게임 시작 상태를 관리하는 변수
    private CardObjectSpawner _cardSpawner; // 카드 스포너 참조

    private bool _isPresetDataLoaded = false;


    private void Start()
    {
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

        // NetworkVariable 초기화를 가장 먼저 수행
        _turn = new NetworkVariable<PLAYERTYPE>();
        _currentLeftTime = new NetworkVariable<float>();
        _currentStamina = new NetworkVariable<int>();
        _hostPresetJsonData = new NetworkVariable<FixedString128Bytes>();
        _clientPresetJsonData = new NetworkVariable<FixedString128Bytes>();

        // 카드 스포너 찾기
        _cardSpawner = FindAnyObjectByType<CardObjectSpawner>();
        if (_cardSpawner == null)
        {
            Debug.LogWarning("CardObjectSpawner를 찾을 수 없습니다.");
        }

        // PresetLoader의 데이터 로드 완료 이벤트 구독
        PresetLoader.OnPresetDataLoaded += OnPresetDataLoaded;

        // 플레이어 초기화
        hostPlayer = new PLAYERINFO();
        clientPlayer = new PLAYERINFO();
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
    }

    private void InitializeGame()
    {
        Debug.Log("Initialize Game");
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("isHost");
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                _hostPresetJsonData.Value = new FixedString128Bytes(PresetJsonData);
            }
            else
            {
                Debug.LogWarning("PresetJsonData가 비어있습니다.");
                _hostPresetJsonData.Value = new FixedString128Bytes("{}");
            }
            _currentLeftTime.Value = 1000 * 120;

            // hostPlayer 정보를 먼저 초기화
            hostPlayer.uid = 0;
            hostPlayer.hp = 10;
            hostPlayer.shield = 0;
            hostPlayer.stamina = 10;
            hostPlayer.itemList = new List<GameObject>();
            hostPlayer.installedList = new List<GameObject>();
            hostPlayer.bufList = new List<BUFF>();

            // clientPlayer 정보를 모두 초기화
            clientPlayer.uid = 0;
            clientPlayer.hp = 10;
            clientPlayer.shield = 0;
            clientPlayer.stamina = 10;
            clientPlayer.itemList = new List<GameObject>();
            clientPlayer.installedList = new List<GameObject>();
            clientPlayer.bufList = new List<BUFF>();

            // 스태미나 설정
            _currentStamina.Value = clientPlayer.stamina;

            Debug.Log("Data Set Finish");
            // json 데이터 불러오기
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                try
                {
                    Debug.Log("Host Json Data Load");
                    var jsonData = JSON.Parse(PresetJsonData);
                    
                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> otherOidList = new List<int>();
                    foreach (JSONNode node in jsonData["items"].AsArray)
                    {
                        otherOidList.Add(node["oid"].AsInt);
                    }
                    otherOidList.Sort();

                    foreach (int _oid in otherOidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData["items"].AsArray)
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
                            GameObject itemObj = new GameObject($"Item_{_oid}");
                            
                            itemObj = AddItemComponent(itemObj, itemData["bigClass"].Value, itemData["smallClass"].Value);
                            
                            // 아이템 정보 설정
                            BaseItem item = itemObj.GetComponent<BaseItem>();
                            if (item != null)
                            {
                                int stat = itemData["stat"].AsInt;
                                int expireCount = itemData["expireCount"].AsInt;
                                item.Init(_oid, stat, expireCount);
                            }

                            // 비활성화
                            itemObj.SetActive(false);

                            hostPlayer.itemList.Add(itemObj);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
                }
            }

            // _clientPresetJsonData 이용해서 clientPlayer 정보 채우기
            if (!string.IsNullOrEmpty(_clientPresetJsonData.Value.ToString()))
            {
                try
                {
                    Debug.Log("Client Json Load");

                    var jsonData = JSON.Parse(_clientPresetJsonData.Value.ToString());
                    
                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> otherOidList = new List<int>();
                    foreach (JSONNode node in jsonData["items"].AsArray)
                    {
                        otherOidList.Add(node["oid"].AsInt);
                    }
                    otherOidList.Sort();

                    foreach (int _oid in otherOidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData["items"].AsArray)
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
                            GameObject itemObj = new GameObject($"Item_{_oid}");
                            
                            itemObj = AddItemComponent(itemObj, itemData["bigClass"].Value, itemData["smallClass"].Value);

                            // 아이템 정보 설정
                            BaseItem item = itemObj.GetComponent<BaseItem>();
                            if (item != null)
                            {
                                int stat = itemData["stat"].AsInt;
                                int expireCount = itemData["expireCount"].AsInt;
                                item.Init(_oid, stat, expireCount);
                            }

                            // 비활성화
                            itemObj.SetActive(false);

                            clientPlayer.itemList.Add(itemObj);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
                }
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                _clientPresetJsonData.Value = new FixedString128Bytes(PresetJsonData);
            }
            else
            {
                Debug.LogWarning("PresetJsonData가 비어있습니다.");
                _clientPresetJsonData.Value = new FixedString128Bytes("{}");
            }

            // clientPlayer 정보를 먼저 초기화
            clientPlayer.uid = 0;
            clientPlayer.hp = 10;
            clientPlayer.shield = 0;
            clientPlayer.stamina = 10;
            clientPlayer.itemList = new List<GameObject>();
            clientPlayer.installedList = new List<GameObject>();
            clientPlayer.bufList = new List<BUFF>();

            // json 데이터 불러오기
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                try
                {
                    var jsonData = JSON.Parse(PresetJsonData);

                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> oidList = new List<int>();
                    foreach (JSONNode node in jsonData["items"].AsArray)
                    {
                        oidList.Add(node["oid"].AsInt); 
                    }
                    oidList.Sort();

                    foreach (int _oid in oidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData["items"].AsArray)
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
                            GameObject itemObj = CreateItemObject(_oid, itemData);
                            if (itemObj != null)
                            {
                                itemObj.SetActive(false);
                                clientPlayer.itemList.Add(itemObj);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
                }
            }


            clientPlayer.bufList = new List<BUFF>();

            // hostPlayer 정보를 모두 초기화
            hostPlayer.uid = 0;
            hostPlayer.hp = 10;
            hostPlayer.shield = 0;
            hostPlayer.stamina = 10;
            hostPlayer.itemList = new List<GameObject>();
            hostPlayer.bufList = new List<BUFF>();

            // json 데이터 불러오기
            if (!string.IsNullOrEmpty(PresetJsonData))
            {
                try
                {
                    var jsonData = JSON.Parse(PresetJsonData);
                    
                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> otherOidList = new List<int>();
                    foreach (JSONNode node in jsonData["items"].AsArray)
                    {
                        otherOidList.Add(node["oid"].AsInt);
                    }
                    otherOidList.Sort();

                    foreach (int _oid in otherOidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData["items"].AsArray)
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
                            GameObject itemObj = new GameObject($"Item_{_oid}");
                            
                            itemObj = AddItemComponent(itemObj, itemData["bigClass"].Value, itemData["smallClass"].Value);

                            // 아이템 정보 설정
                            BaseItem item = itemObj.GetComponent<BaseItem>();
                            if (item != null)
                            {
                                int stat = itemData["stat"].AsInt;
                                int expireCount = itemData["expireCount"].AsInt;
                                item.Init(_oid, stat, expireCount);
                            }

                            // 비활성화
                            itemObj.SetActive(false);

                            clientPlayer.itemList.Add(itemObj);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
                }
            }

            // _hostPresetJsonData 이용해서 hostPlayer 정보 채우기

            if (!string.IsNullOrEmpty(_hostPresetJsonData.Value.ToString()))
            {
                try
                {
                    var jsonData = JSON.Parse(_hostPresetJsonData.Value.ToString());
                    
                    // JSON에서 oid 값들을 가져와서 배열로 구성
                    List<int> otherOidList = new List<int>();
                    foreach (JSONNode node in jsonData["items"].AsArray)
                    {
                        otherOidList.Add(node["oid"].AsInt);
                    }
                    otherOidList.Sort();

                    foreach (int _oid in otherOidList)
                    {
                        // OID에 해당하는 아이템 데이터 찾기
                        JSONNode itemData = null;
                        foreach (JSONNode node in jsonData["items"].AsArray)
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
                            GameObject itemObj = new GameObject($"Item_{_oid}");
                            
                            itemObj = AddItemComponent(itemObj, itemData["bigClass"].Value, itemData["smallClass"].Value);

                            // 아이템 정보 설정
                            BaseItem item = itemObj.GetComponent<BaseItem>();
                            if (item != null)
                            {
                                int stat = itemData["stat"].AsInt;
                                int expireCount = itemData["expireCount"].AsInt;
                                item.Init(_oid, stat, expireCount);
                            }

                            // 비활성화
                            itemObj.SetActive(false);

                            clientPlayer.itemList.Add(itemObj);
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
            _turn.Value = PLAYERTYPE.HOST;
        }
        else
        {
            _turn.Value = PLAYERTYPE.CLIENT;
        }
        _turnCount = 1;

        // 게임 시작
        GameStart();
    }

    private void GameStart()
    {
        Debug.Log("Game Start");
        // 게임 시작 플래그 설정
        _isGameStarted = true;

        // 초기 턴 설정
        _turn.Value = PLAYERTYPE.HOST;
        _turnCount = 1;

        // 초기 시간 설정 - 0으로 설정하여 자동으로 턴이 넘어가도록 함
        _currentLeftTime.Value = 0;

        // 초기 스태미나 설정
        if (NetworkManager.Singleton.IsHost)
        {
            _currentStamina.Value = hostPlayer.stamina;
        }
        else
        {
            _currentStamina.Value = clientPlayer.stamina;
        }

        Debug.Log("게임이 시작되었습니다!");
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
        if (_currentLeftTime == null || _turn == null || _currentStamina == null || _isGameStarted == null)
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

        // 남은 시간이 다 되면 턴이 바뀌어야 함
        // 게임 종료
        if (IsGameOver() != 0)
        {
            int winner = IsGameOver();
            if (winner == 1)
            {
                // hostPlayer Win
            }
            else
            {
                // clientPlayer win
            }
        }

        Debug.Log("in");
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
                // 설치된 아이템 효과 적용
                ApplyInstalledItemsEffects();

                // 시간 초기화
                _currentLeftTime.Value = _minTime + 3 * _turnCount++;
                if (_currentLeftTime.Value >= _maxTime)
                    _currentLeftTime.Value = _maxTime;

                // 턴 교체
                _turn.Value = (PLAYERTYPE)(((int)_turn.Value + 1) % 2);
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
    private void ApplyInstalledItemsEffects()
    {
        if (_turn.Value == PLAYERTYPE.HOST)
        {
            for (int i = hostPlayer.installedList.Count - 1; i >= 0; i--)
            {
                GameObject itemObj = hostPlayer.installedList[i];
                BaseItem item = itemObj.GetComponent<BaseItem>();
                
                if (item != null)
                {
                    // 아이템 효과 적용
                    item.InstallPassive();
                    
                    // 만료 카운트 감소
                    item.DecreaseExpireCount();
                    
                    // 만료된 아이템 제거
                    if (item.IsExpired())
                    {
                        item.Uninstall();
                        hostPlayer.installedList.RemoveAt(i);
                    }
                }
            }
        }
        else
        {
            for (int i = clientPlayer.installedList.Count - 1; i >= 0; i--)
            {
                GameObject itemObj = clientPlayer.installedList[i];
                BaseItem item = itemObj.GetComponent<BaseItem>();
                
                if (item != null)
                {
                    // 아이템 효과 적용
                    item.InstallPassive();
                    
                    // 만료 카운트 감소
                    item.DecreaseExpireCount();
                    
                    // 만료된 아이템 제거
                    if (item.IsExpired())
                    {
                        item.Uninstall();
                        clientPlayer.installedList.RemoveAt(i);
                    }
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
                hostPlayer.installedList.Add(itemObj);
            }
            else
            {
                clientPlayer.installedList.Add(itemObj);
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

    /// <summary>
    /// 아이템 사용 (스태미나 체크 포함)
    /// </summary>
    public bool Use(GameObject itemObj, bool isHost)
    {
        BaseItem item = itemObj.GetComponent<BaseItem>();
        if (item != null && !item.IsShooted())
        {
            if (CheckCanUseItemAndDecreaseStamina(isHost, item.GetStamina()))
            {
                item.Use();
                return true;
            }
        }
        return false;
    }

    public static GameManager GetInstance()
    {
        if (_instance == null)
            _instance = new GameManager();

        return _instance;
    }

    public int GetStamina(bool isHost)
    {
        if (isHost)
        {
            return hostPlayer.stamina;
        }
        else
        {
            return clientPlayer.stamina;
        }
    }

    public bool Attack(int damage, int stamina, bool isHost)
    {
        if (isHost)
        {
            if (CheckCanUseItemAndDecreaseStamina(isHost, stamina))
                return false; // 사용 불가

            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamage(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in hostPlayer.bufList)
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
            for (int i = 0; i < clientPlayer.bufList.Count; i++)
            {
                BUFF buf = clientPlayer.bufList[i];
                if (buf.type == BUFTYPE.REFLECT)
                {
                    if (hostPlayer.shield > 0)
                    {
                        if (hostPlayer.shield > (int)realDamage)
                            hostPlayer.shield -= (int)realDamage;
                        else
                        {
                            hostPlayer.shield = 0;
                            hostPlayer.hp -= (int)realDamage;
                        }
                    } else
                    {
                        hostPlayer.hp -= (int)realDamage;
                    }

                    realDamage = 0;
                    buf.value--;

                    if (buf.value == 0)
                        clientPlayer.bufList.RemoveAt(i);
                    else
                        clientPlayer.bufList[i] = buf;

                    return true;
                }
            }


            if (clientPlayer.shield > 0)
            {
                if (clientPlayer.shield > (int)realDamage)
                    clientPlayer.shield -= (int)realDamage;
                else
                {
                    clientPlayer.shield = 0;
                    clientPlayer.hp -= ((int)realDamage - clientPlayer.shield);
                }
            }
            else
            {
                clientPlayer.hp -= (int)realDamage;
            }

            return true;
            // 성공
        } else
        {
            if(CheckCanUseItemAndDecreaseStamina(isHost, stamina))
                return false; // 사용 불가

            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamage(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in clientPlayer.bufList)
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
            for (int i = 0; i < hostPlayer.bufList.Count; i++)
            {
                BUFF buf = hostPlayer.bufList[i];
                if (buf.type == BUFTYPE.REFLECT)
                {
                    if (clientPlayer.shield > 0)
                    {
                        if (clientPlayer.shield > (int)realDamage)
                            clientPlayer.shield -= (int)realDamage;
                        else
                        {
                            clientPlayer.shield = 0;
                            clientPlayer.hp -= (int)realDamage;
                        }
                    }
                    else
                    {
                        clientPlayer.hp -= (int)realDamage;
                    }

                    realDamage = 0;
                    buf.value--;

                    if (buf.value == 0)
                        hostPlayer.bufList.RemoveAt(i);
                    else
                        hostPlayer.bufList[i] = buf;

                    return true;
                }
            }

            if (hostPlayer.shield > 0)
            {
                if (hostPlayer.shield > (int)realDamage)
                    hostPlayer.shield -= (int)realDamage;
                else
                {
                    hostPlayer.shield = 0;
                    hostPlayer.hp -= ((int)realDamage - clientPlayer.shield);
                }
            }
            else
            {
                hostPlayer.hp -= (int)realDamage;
            }

            return true;
        }
    }

    public bool IgnoreGuardAttack(int damage, int stamina, bool isHost)
    {
        if (isHost)
        {
            if (CheckCanUseItemAndDecreaseStamina(isHost, stamina))
                return false; // 사용 불가

            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamageWithoutGuard(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in hostPlayer.bufList)
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

            if (clientPlayer.shield > 0)
            {
                if (clientPlayer.shield > (int)realDamage)
                    clientPlayer.shield -= (int)realDamage;
                else
                {
                    clientPlayer.shield = 0;
                    clientPlayer.hp -= ((int)realDamage - clientPlayer.shield);
                }
            }
            else
            {
                clientPlayer.hp -= (int)realDamage;
            }

            return true;
            // 성공
        }
        else
        {
            if (CheckCanUseItemAndDecreaseStamina(isHost, stamina))
                return false; // 사용 불가

            float realDamage = damage;

            // 버프 적용, 방어 적용
            realDamage *= ProcBuffAndCalcDamageWithoutGuard(isHost);

            // ATTACKMISS 버프가 있는 경우
            foreach (BUFF buf in clientPlayer.bufList)
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

            if (hostPlayer.shield > 0)
            {
                if (hostPlayer.shield > (int)realDamage)
                    hostPlayer.shield -= (int)realDamage;
                else
                {
                    hostPlayer.shield = 0;
                    hostPlayer.hp -= ((int)realDamage - clientPlayer.shield);
                }
            }
            else
            {
                hostPlayer.hp -= (int)realDamage;
            }

            return true;
        }
    }

    public bool CheckCanUseItemAndDecreaseStamina(bool isHost, int stamina)
    {
        if (isHost)
        {
            if (hostPlayer.stamina < stamina)
                return false;

            hostPlayer.stamina -= stamina;
        }
        else
        {
            if (clientPlayer.stamina < stamina)
                return false;

            clientPlayer.stamina -= stamina;
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
            hostPlayer.bufList.Add(buf);
        else
            clientPlayer.bufList.Add(buf);
    }

    public void RemoveBuff(BUFTYPE type, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostPlayer.bufList.Count - 1; i >= 0; i--)
            {
                if (hostPlayer.bufList[i].type == type)
                {
                    hostPlayer.bufList.RemoveAt(i);
                }
            }
        }
        else
        {
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                if (clientPlayer.bufList[i].type == type)
                {
                    clientPlayer.bufList.RemoveAt(i);
                }
            }
        }
    }

    public void RemoveDeBuff(BUFTYPE type, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostPlayer.bufList.Count - 1; i >= 0; i--)
            {
                if (hostPlayer.bufList[i].type == type)
                {
                    if (hostPlayer.bufList[i].value < 0)
                        hostPlayer.bufList.RemoveAt(i);
                }
            }
        }
        else
        {
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                if (clientPlayer.bufList[i].type == type)
                {
                    if (clientPlayer.bufList[i].value < 0)
                        clientPlayer.bufList.RemoveAt(i);
                }
            }
        }
    }

    public void ReverseBuff(bool isHost)
    {
        if (isHost)
        {
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostPlayer.bufList[i];
                if (buf.type == BUFTYPE.INCATTACK || buf.type == BUFTYPE.INCDEFENSE)
                {
                    buf.value = -buf.value;
                }
                clientPlayer.bufList[i] = buf;
            }
        }
        else
        {
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientPlayer.bufList[i];
                if (buf.type == BUFTYPE.INCATTACK || buf.type == BUFTYPE.INCDEFENSE)
                {
                    buf.value = -buf.value;
                }
                clientPlayer.bufList[i] = buf;
            }
        }
    }

    public void SetShield(int shield, bool isHost)
    {
        if (isHost)
        {
            hostPlayer.shield += shield;
        } 
        else
        {
            clientPlayer.shield += shield;
        }
    }

    private int IsGameOver()
    {
        // 0 : false, 1: 첫번째 플레이어가 승리, 2: 두번째 플레이어가 승리
        // update 에서 계속 확인

        if (hostPlayer.hp <= 0)
            return 2;
        else if (clientPlayer.hp <= 0)
            return 1;
        else
            return 0;
    }

    /*
     
    아이템에 필요한 데이터를 (데이터, 효과 적용)
     
     */
    private static float ProcBuffAndCalcDamage(bool isHost)
    {
        float ret = 1;
        if (isHost)
        {
            // INCATTACK, INCDEFENSE
            foreach (BUFF buf in hostPlayer.bufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in clientPlayer.bufList)
            {
                if (buf.type == BUFTYPE.INCDEFENSE)
                {
                    ret -= buf.value;
                }
            }

            // GUARD
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientPlayer.bufList[i];
                if (buf.type == BUFTYPE.GUARD)
                {
                    buf.value--;
                    ret = 0;
                }
                clientPlayer.bufList[i] = buf;

                if (clientPlayer.bufList[i].value == 0)
                {
                    clientPlayer.bufList.RemoveAt(i);
                }
            }
        }
        else
        {
            // INCATTACK, INCDEFENSE
            foreach (BUFF buf in clientPlayer.bufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in hostPlayer.bufList)
            {
                if (buf.type == BUFTYPE.INCDEFENSE)
                {
                    ret -= buf.value;
                }
            }

            // GUARD
            for (int i = hostPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostPlayer.bufList[i];
                if (buf.type == BUFTYPE.GUARD)
                {
                    buf.value--;
                    ret = 0;
                }
                hostPlayer.bufList[i] = buf;

                if (hostPlayer.bufList[i].value == 0)
                {
                    hostPlayer.bufList.RemoveAt(i);
                }
            }


        }

        return ret;
    }

    /*
     
   방어 무시 적용 

     */
    private static float ProcBuffAndCalcDamageWithoutGuard(bool isHost)
    {
        float ret = 1;
        if (isHost)
        {
            // INCATTACK, INCDEFENSE
            foreach (BUFF buf in hostPlayer.bufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in clientPlayer.bufList)
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
            foreach (BUFF buf in clientPlayer.bufList)
            {
                if (buf.type == BUFTYPE.INCATTACK)
                {
                    ret += buf.value;
                }
            }

            foreach (BUFF buf in hostPlayer.bufList)
            {
                if (buf.type == BUFTYPE.INCDEFENSE)
                {
                    ret -= buf.value;
                }
            }
        }

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
            foreach (BUFF buf in hostPlayer.bufList)
            {
                if (buf.type == BUFTYPE.UNHEAL)
                {
                    unheal = true;
                    break;
                }
            }

            // HEAL, ZEROSTAMINA, BLEED
            foreach (BUFF buf in hostPlayer.bufList)
            {
                switch (buf.type)
                {
                    case BUFTYPE.HEAL:
                        if (!unheal)
                        {
                            hostPlayer.hp += (int)(buf.value * hostPlayer.hp);
                            if (hostPlayer.hp > _maxHP)
                                hostPlayer.hp = _maxHP;
                        }
                        break;
                    case BUFTYPE.ZEROSTAMINA:
                        hostPlayer.stamina = 0;
                        break;
                    case BUFTYPE.BLEED:
                        hostPlayer.hp -= (int)buf.value;
                        break;
                    case BUFTYPE.INCSTAMINA:
                        hostPlayer.stamina += (int)buf.value;
                        break;
                    case BUFTYPE.SHIELD:
                        SetShield((int)buf.value, isHost);
                        break;
                }
            }

            // TURN Decrease
            for (int i = hostPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostPlayer.bufList[i];
                buf.turn--;
                hostPlayer.bufList[i] = buf;

                if (hostPlayer.bufList[i].turn == 0)
                {
                    hostPlayer.bufList.RemoveAt(i);
                }
            }
        } 
        else
        {
            foreach (BUFF buf in clientPlayer.bufList)
            {
                if (buf.type == BUFTYPE.UNHEAL)
                {
                    unheal = true;
                    break;
                }
            }

            // HEAL, ZEROSTAMINA, BLEED
            foreach (BUFF buf in clientPlayer.bufList)
            {
                switch (buf.type)
                {
                    case BUFTYPE.HEAL:
                        if (!unheal)
                        {
                            clientPlayer.hp += (int)(buf.value * clientPlayer.hp);
                            if (clientPlayer.hp > _maxHP)
                                clientPlayer.hp = _maxHP;
                        }
                        break;
                    case BUFTYPE.ZEROSTAMINA:
                        clientPlayer.stamina = 0;
                        break;
                    case BUFTYPE.BLEED:
                        clientPlayer.hp -= (int)buf.value;
                        break;
                    case BUFTYPE.INCSTAMINA:
                        clientPlayer.stamina -= (int)buf.value;
                        break;
                    case BUFTYPE.SHIELD:
                        SetShield((int)buf.value, isHost);
                        break;
                }
            }

            // TURN Decrease
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientPlayer.bufList[i];
                buf.turn--;
                clientPlayer.bufList[i] = buf;

                if (clientPlayer.bufList[i].turn == 0)
                {
                    clientPlayer.bufList.RemoveAt(i);
                }
            }
        }
    }

    public void IncreaseBuffValue(BUFTYPE type, float value, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostPlayer.bufList[i];
                if (buf.type == type)
                {
                    buf.value *= (value + 1);
                }
                hostPlayer.bufList[i] = buf;
            }
        }
        else
        {
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientPlayer.bufList[i];
                if (buf.type == type)
                {
                    buf.value *= (value + 1);
                }
                clientPlayer.bufList[i] = buf;
            }
        }

    }

    public void IncreaseAllItemsExpireCount(int value, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostPlayer.itemList.Count - 1; i >= 0; i--)
            {
                GameObject item = hostPlayer.itemList[i];
                item.GetComponent<BaseItem>().IncreaseExpireCount();
                hostPlayer.itemList[i] = item;
            }
        }
        else
        {
            for (int i = clientPlayer.itemList.Count - 1; i >= 0; i--)
            {
                GameObject item = clientPlayer.itemList[i];
                item.GetComponent<BaseItem>().IncreaseExpireCount();
                clientPlayer.itemList[i] = item;
            }
        }
    }

    public void IncreaseAllItemsStats(float value, bool isHost)
    {
        if (isHost)
        {
            for (int i = hostPlayer.itemList.Count - 1; i >= 0; i--)
            {
                GameObject item = hostPlayer.itemList[i];
                item.GetComponent<BaseItem>().IncreaseStats(value);
                hostPlayer.itemList[i] = item;
            }
        }
        else
        {
            for (int i = clientPlayer.itemList.Count - 1; i >= 0; i--)
            {
                GameObject item = clientPlayer.itemList[i];
                item.GetComponent<BaseItem>().IncreaseStats(value);
                clientPlayer.itemList[i] = item;
            }
        }
    }

    public bool IsX2(bool isHost)
    {
        if (isHost)
        {
            for (int i = hostPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = hostPlayer.bufList[i];
                if (buf.type == BUFTYPE.X2)
                {
                    hostPlayer.bufList.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        else
        {
            for (int i = clientPlayer.bufList.Count - 1; i >= 0; i--)
            {
                BUFF buf = clientPlayer.bufList[i];
                if (buf.type == BUFTYPE.X2)
                {
                    clientPlayer.bufList.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }

    /*
     
    설치된 아이템의 효과를 관리하는 배열로 변경해야 함

     */
    public void DestroyItem(int count, bool isHost)
    {
        int itemCount = -1;

        if (isHost)
        {   
            while (true)
            {
                itemCount = hostPlayer.itemList.Count;

                if (count <= 0 || itemCount == 0)
                    break;

                int randomIdx = UnityEngine.Random.Range(0, itemCount);
                hostPlayer.itemList.RemoveAt(randomIdx);
                count--;
            }
        }
        else
        {
            while (true)
            {
                itemCount = clientPlayer.itemList.Count;

                if (count <= 0 || itemCount == 0)
                    break;

                int randomIdx = UnityEngine.Random.Range(0, itemCount);
                clientPlayer.itemList.RemoveAt(randomIdx);
                count--;
            }
        }
    }

    public void EndTurn()
    {
        if (NetworkManager.Singleton.IsHost && _turn.Value == PLAYERTYPE.HOST)
        {
            _turn.Value = PLAYERTYPE.CLIENT;
            _currentLeftTime.Value = 0;
        } else if (NetworkManager.Singleton.IsClient && _turn.Value == PLAYERTYPE.CLIENT)
        {
            _turn.Value = PLAYERTYPE.HOST;
            _currentLeftTime.Value = 0;
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

    private GameObject CreateItemObject(int oid, JSONNode itemData)
    {
        // 아이템 오브젝트 생성
        GameObject itemObj = new GameObject($"Item_{oid}");
        
        // bigClass와 smallClass에 따른 컴포넌트 추가
        string bigClass = itemData["bigClass"].Value.ToLower();
        string smallClass = itemData["smallClass"].Value.ToLower();
        itemObj = AddItemComponent(itemObj, bigClass, smallClass);

        // 아이템 정보 설정
        BaseItem item = itemObj.GetComponent<BaseItem>();
        if (item != null)
        {
            int stat = itemData["stat"].AsInt;
            int expireCount = itemData["expireCount"].AsInt;
            item.Init(oid, stat, expireCount);
        }

        return itemObj;
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
        if (_cardSpawner != null)
        {
            _cardSpawner.SetCardInteraction(enable);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (RelaySingleton.Instance != null)
        {
            //RelaySingleton.Instance.OnRelayConnected -= OnRelayConnected;
        }
        
        // PresetLoader 이벤트 구독 해제
        PresetLoader.OnPresetDataLoaded -= OnPresetDataLoaded;
    }
}
