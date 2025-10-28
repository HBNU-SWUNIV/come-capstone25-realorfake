using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using static GameDataManager;
using System;
using System.Collections;
using Unity.Netcode;
// 또는 using Assets.Scripts.Managers;

public class PresetLoader : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform spawnPoint;
    public PresetCardArrangement presetCardArrangement;

    public static event Action OnPresetDataLoaded = null; // 데이터 로드 완료 이벤트

    private string _kitchenBasicJson = "{\r\n    \"oid\": \"0\",\r\n    \"uid\": \"0\",\r\n    \"bigClass\": \"Kitchen\",\r\n    \"smallClass\": \"basic\",\r\n    \"abilityType\": \"A\",\r\n    \"sellState\": \"N\",\r\n    \"cost\": \"0\",\r\n    \"expireCount\": \"-1\",\r\n    \"stat\": \"0\",\r\n    \"grade\": \"Normal\"\r\n  }";

    private void Start()
    {
        Debug.Log("PresetLoader Start 호출됨."); // Start 메서드 호출 확인

        // DontDestroyOnLoad에 있는 CardArrangement 참조 가져오기
        // FindObjectOfType은 현재 활성화된 모든 오브젝트를 대상으로 검색합니다.
        presetCardArrangement = FindAnyObjectByType<PresetCardArrangement>(); 
        if (presetCardArrangement == null)
        {
            // FindAnyObjectByType이 실패하면 FindObjectOfType 시도
            presetCardArrangement = FindObjectOfType<PresetCardArrangement>();
        }
        
        if (presetCardArrangement != null)
        {
            Debug.Log("PresetCardArrangement를 찾았습니다.");
            
            // spawnPoint가 설정되어 있는지 확인
            if (presetCardArrangement.spawnPoint == null)
            {
                Debug.LogWarning("PresetCardArrangement의 spawnPoint가 설정되지 않았습니다. Inspector에서 설정해주세요.");
            }
        }
        else
        {
            Debug.LogError("FightScene 또는 DontDestroyOnLoad에서 PresetCardArrangement를 찾지 못했습니다! PresetCardArrangement 스크립트가 연결된 오브젝트가 활성화되어 있는지 확인하세요.");
        }

        // GameDataManager에서 프리셋 데이터 로드
        if (!string.IsNullOrEmpty(GameDataManager.PresetJsonData))
        {
            Debug.Log($"GameDataManager에서 데이터 로드 시도: {GameDataManager.PresetJsonData}"); // 로드 시도 데이터 확인
            LoadPresetData(GameDataManager.PresetJsonData);
            // GameDataManager.ClearPresetData(); // 데이터 사용 후 초기화 (선택 사항)
        }
        else
        {
            Debug.LogWarning("로드할 프리셋 데이터가 GameDataManager에 없습니다."); // 데이터 없을 경우
        }
    }

    public static void Init()
    {
        OnPresetDataLoaded = null;
    }

    public void LoadPresetData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("JSON 데이터가 비어있습니다!");
            return;
        }

        try
        {
            GameManager.PresetJsonData = jsonData;
            bool isKitchen = false;
            JSONNode presetData = JSON.Parse(jsonData);

            if (presetData == null)
            {
                Debug.LogError("JSON 파싱 실패!");
                return;
            }

            // 카드 데이터 배열 처리
            if (presetData.IsArray)
            {
                foreach (JSONNode cardData in presetData.AsArray)
                {
                    SpawnCard(cardData);
                    if (cardData["bigClass"] == "Kitchen")
                    {
                        isKitchen = true;
                    }
                }

                if (isKitchen)
                {
                    JSONNode kitchenBasic = JSON.Parse(_kitchenBasicJson);
                    SpawnCard(kitchenBasic);
                    JSONNode tmp = JSON.Parse(jsonData).AsArray;
                    tmp.Add(JSON.Parse(_kitchenBasicJson));
                    GameManager.PresetJsonData = tmp.ToString();
                }

                // 모든 카드 생성 후 배치 로직 호출
                if (presetCardArrangement != null && presetCardArrangement.cards != null)
                {
                    presetCardArrangement.ArrangeCards(presetCardArrangement.cards);
                    Debug.Log("모든 카드 생성 및 CardArrangement 배치 로직 호출 완료.");
                    
                    // 이벤트 호출을 지연시켜 GameManager가 구독할 시간을 확보
                    StartCoroutine(DelayedEventInvoke());
                }
                else
                {
                     Debug.LogWarning("CardArrangement 인스턴스 또는 cards 리스트를 찾을 수 없어 카드 배치를 건너뜝니다.");
                }
            }
            else
            {
                Debug.Log("JSON 데이터가 배열 형식이 아닙니다!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"프리셋 데이터 로드 중 오류 발생: {e.Message}");
        }
    }

    private IEnumerator DelayedEventInvoke()
    {
        // 한 프레임 대기하여 GameManager의 Start 메서드가 실행될 시간을 확보
        yield return null;
        
        // 데이터 로드가 완료되었음을 알림
        if (OnPresetDataLoaded != null)
        {
            Debug.Log($"OnPresetDataLoaded 이벤트를 호출합니다. {OnPresetDataLoaded}");
            OnPresetDataLoaded?.Invoke();
        }
        else
        {
            Debug.Log("OnPresetDataLoaded 이벤트에 구독자가 없습니다.");
        }
    }

    private void SpawnCard(JSONNode cardData)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("카드 프리팹이 할당되지 않았습니다!");
            return;
        }

        if (spawnPoint == null)
        {
            if (NetworkManager.Singleton.IsHost)
                spawnPoint = GameObject.Find("HostCardSpawnPoint").transform;
            else
                spawnPoint = GameObject.Find("ClientCardSpawnPoint").transform;

            if (spawnPoint == null)
                return;
        }

        GameObject cardObject = Instantiate(cardPrefab, spawnPoint.position, Quaternion.identity);
        CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();
        
        if (cardDisplay != null)
        {
            cardDisplay.SetJsonData(cardData.ToString());
            Debug.Log($"PresetLoader CardData {cardData.ToString()}");
        }
        else
        {
            Debug.LogError("생성된 카드 오브젝트에 CardDisplay 컴포넌트가 없습니다!");
        }

        // 카드 배치 관리자에 추가
        if (presetCardArrangement != null && presetCardArrangement.cards != null)
        {
            presetCardArrangement.cards.Add(cardObject);
        }
        else
        {
            Debug.LogWarning("CardArrangement 또는 cards 리스트가 null이어서 카드를 추가할 수 없습니다.");
        }
    }
} 