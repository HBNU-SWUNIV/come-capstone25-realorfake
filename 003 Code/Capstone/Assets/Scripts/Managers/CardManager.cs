using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using Unity.Netcode;
using System.IO;

public class CardManager : NetworkBehaviour
{
    public Transform spawnPoint;  // 카드 생성 위치
    private CardArrangement cardArrangement; // CardArrangement 참조를 private으로 변경
    public CardPool cardPool;     // CardPool 참조
    public GameObject cardPrefab; // 카드 프리팹 연결 (CardPool에서 사용)

    public static CardManager Instance;

    public JSONNode allCardData; // 모든 카드 데이터를 저장할 변수
    public List<GameObject> currentActiveCards = new List<GameObject>(); // 현재 활성화된 카드 리스트

    void Awake()
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

    void Start()
    {
        if (cardPool == null)
        {
            cardPool = FindAnyObjectByType<CardPool>();
            if (cardPool == null)
            {
                Debug.Log("CardPool 컴포넌트를 찾을 수 없습니다!");
                return;
            }
        }

        // spawnPoint가 할당되지 않은 경우에만 찾기
        if (spawnPoint == null)
        {
            GameObject spawnPointObj = null;
            Debug.Log($"CardManager, IsHost {NetworkManager.Singleton.IsHost}");

            if (NetworkManager.Singleton.IsHost)
                spawnPointObj = GameObject.Find("HostCardSpawnPoint");
            else
                spawnPointObj = GameObject.Find("ClientCardSpawnPoint");

            if (spawnPointObj != null)
            {
                spawnPoint = spawnPointObj.transform;
                cardArrangement.spawnPoint = spawnPoint;
            }
            else
            {
                Debug.Log("CardSpawnPoint를 찾을 수 없습니다. 아이템 케이스가 활성화될 때 spawnPoint가 설정됩니다.");
            }
        }

        StartCoroutine(LoadCardDataAsync());
    }

    private System.Collections.IEnumerator LoadCardDataAsync()
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "myJson.json");
        Debug.Log($"JSON 파일 로드 시도: {jsonPath}");
        
        //ResourceRequest request = Resources.LoadAsync<TextAsset>(jsonPath);
        //yield return request;

        string jsonText = File.ReadAllText(jsonPath);

        TextAsset jsonFile = new TextAsset(jsonText);

        if (jsonFile != null)
        {
            Debug.Log($"JSON 파일 내용: {jsonFile.text}");
            allCardData = JSON.Parse(jsonFile.text);
            
            if (allCardData != null)
            {
                if (allCardData.IsArray)
                {
                    Debug.Log($"JSON 파일 로드 완료. 총 {allCardData.Count}개의 카드 데이터 로드.");
                }
                else
                {
                    Debug.Log($"JSON 데이터가 배열 형식이 아닙니다. 현재 형식: {allCardData.GetType()}");
                }
            }
            else
            {
                Debug.Log("JSON 파싱 실패: allCardData가 null입니다.");
            }
        }
        else
        {
            Debug.Log($"JSON 파일을 찾을 수 없습니다. 경로: {jsonPath}");
        }

        yield return null;
    }

    public void FindCardArrangement()
    {
        if (cardArrangement == null)
        {
            cardArrangement = FindAnyObjectByType<CardArrangement>();
            if (cardArrangement == null)
            {
                Debug.LogError("CardArrangement 컴포넌트를 찾을 수 없습니다!");
                return;
            }
        }
    }

    public void LoadAndArrangePage(int startIndex, int endIndex)
    {
        Debug.Log($"LoadAndArrangePage 호출: startIndex={startIndex}, endIndex={endIndex}");
        
        if (allCardData == null)
        {
            Debug.Log("카드 데이터가 로드되지 않았습니다. (allCardData is null)");
            return;
        }

        if (!allCardData.IsArray)
        {
            Debug.Log($"카드 데이터가 배열 형식이 아닙니다. 현재 형식: {allCardData.GetType()}");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.Log("spawnPoint가 설정되지 않았습니다. 아이템 케이스가 활성화되어 있는지 확인해주세요.");
            return;
        }

        if (cardPool == null)
        {
            Debug.Log("cardPool이 할당되지 않았습니다!");
            return;
        }

        FindCardArrangement();

        if (cardArrangement == null)
        {
            Debug.Log("cardArrangement가 할당되지 않았습니다!");
            return;
        }

        Debug.Log("기존 카드 정리 시작");
        foreach (var card in currentActiveCards)
        {
            if (card != null)
            {
                cardPool.ReturnCard(card);
            }
        }
        currentActiveCards.Clear();
        cardArrangement.cards.Clear();
        Debug.Log("기존 카드 정리 완료");

        Debug.Log("새 카드 생성 시작");
        for (int i = startIndex; i < endIndex && i < allCardData.Count; i++)
        {
            JSONNode cardData = allCardData[i];
            GameObject newCard = cardPool.GetCard();

            if (newCard != null)
            {
                CardDisplay display = newCard.GetComponent<CardDisplay>();
                if (display != null)
                {
                    display.SetCardData(cardData);
                }
                else
                {
                    Debug.LogError($"생성된 카드에 CardDisplay 컴포넌트가 없습니다: {newCard.name}");
                    continue;
                }

                newCard.transform.position = spawnPoint.position;
                newCard.transform.rotation = Quaternion.identity;
                newCard.transform.SetParent(null);

                currentActiveCards.Add(newCard);
                if (cardArrangement != null)
                {
                    cardArrangement.cards.Add(newCard);
                }
                Debug.Log($"카드 생성 완료: {i}번째 카드");
            }
            else
            {
                Debug.Log($"카드 풀에서 카드를 가져오지 못했습니다: {i}번째 카드");
            }
        }
        Debug.Log($"새 카드 생성 완료. 총 {currentActiveCards.Count}개의 카드 생성됨");

        if (cardArrangement != null)
        {
            Debug.Log("카드 정렬 시작");
            cardArrangement.ArrangeCards(currentActiveCards);
            Debug.Log("카드 정렬 완료");
        }
    }
}
