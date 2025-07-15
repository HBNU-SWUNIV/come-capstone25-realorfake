using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardPool : MonoBehaviour
{
    public GameObject cardPrefab; // ī 
    public int poolSize = 20;     // Ǯ ũ��
    private Queue<GameObject> cardPool = new Queue<GameObject>();

    public Transform poolParent; // Ǯ���� ������Ʈ�� ���� �θ�
    private string poolParentName = "CardPoolParent"; // 풀 부모 오브젝트의 이름

    public static CardPool Instance;

    void Awake()
    {
        Debug.Log("CardPool Awake called"); // 디버그 로그 추가
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 바뀌어도 이 오브젝트 유지

            // 씬 전환 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 초기 풀 부모 찾기
            FindPoolParent();

            // 카드 풀 초기화
            InitializeCardPool();
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

    void OnDestroy()
    {
        // 씬 전환 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"씬 로드됨: {scene.name}");
        FindPoolParent();
        
        // 씬 전환 시 카드 풀 재초기화
        if (cardPool.Count == 0)
        {
            Debug.Log("카드 풀이 비어있어 재초기화합니다.");
            InitializeCardPool();
        }
    }

    private void FindPoolParent()
    {
        // 기존 풀 부모 찾기
        GameObject parentObj = GameObject.Find(poolParentName);
        if (parentObj != null)
        {
            poolParent = parentObj.transform;
            Debug.Log($"풀 부모 찾음: {poolParent.name}");
        }
        else
        {
            Debug.LogWarning($"풀 부모를 찾을 수 없습니다: {poolParentName}");
            // 풀 부모가 없으면 새로 생성
            parentObj = new GameObject(poolParentName);
            poolParent = parentObj.transform;
            Debug.Log("새 풀 부모 생성됨");
        }
    }

    private void InitializeCardPool()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("카드 프리팹이 할당되지 않았습니다!");
            return;
        }

        // 기존 카드들 정리
        while (cardPool.Count > 0)
        {
            GameObject card = cardPool.Dequeue();
            if (card != null)
            {
                Destroy(card);
            }
        }

        // 새 카드 생성
        for (int i = 0; i < poolSize; i++)
        {
            GameObject card = Instantiate(cardPrefab);
            if (card != null)
            {
                card.SetActive(false);
                if (poolParent != null)
                {
                    card.transform.SetParent(poolParent);
                }
                cardPool.Enqueue(card);
                Debug.Log($"카드 생성됨: {i + 1}/{poolSize}");
            }
            else
            {
                Debug.LogError($"카드 생성 실패: {i + 1}번째");
            }
        }
        Debug.Log($"카드 풀 초기화 완료: {cardPool.Count}개의 카드 생성됨");
    }

    // 기존 Start 메서드는 이제 비워둡니다.
    void Start()
    {
        
    }

    public GameObject GetCard()
    {
        if (cardPool.Count > 0)
        {
            GameObject card = cardPool.Dequeue();
            if (card != null)
            {
                card.SetActive(true); // 활성화
                card.transform.SetParent(null); // 부모로부터 분리 (필요 시 이동)
                return card;
            }
            else
            {
                Debug.LogWarning("카드 오브젝트가 null입니다!");
                // 카드 풀 재초기화 시도
                InitializeCardPool();
                return null;
            }
        }
        else
        {
            Debug.LogWarning("카드 풀이 비어있습니다! 재초기화를 시도합니다.");
            InitializeCardPool();
            return null;
        }
    }

    public void ReturnCard(GameObject card)
    {
        if (card == null) return; // null 체크 추가

        try
        {
            card.SetActive(false); // 비활성화
            if (poolParent != null)
            {
                card.transform.SetParent(poolParent); // 부모로 이동
            }
            cardPool.Enqueue(card); // 다시 풀에 추가
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"카드 반환 중 오류 발생: {e.Message}");
        }
    }
}


