using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PresetCardArrangement : NetworkBehaviour
{
    public List<GameObject> cards = new List<GameObject>(); // 카드 리스트
    public Transform hostSpawnPoint;   // SpawnPoint 위치 (CardDeckPosition)
    public Transform clientSpawnPoint;   // SpawnPoint 위치 (CardDeckPosition)
    public Transform spawnPoint;
    public float yOffset = -0.02f; // Y 오프셋
    public float xOffset = 1.5f;   // X 오프셋 (카드 간격)

    public int maxCardsPerRow = 6; // 각 행에 배치될 최대 카드 수

    public string specificSceneName = "FightScene";

    public static PresetCardArrangement Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost && hostSpawnPoint != null)
            spawnPoint = hostSpawnPoint;
        else if (NetworkManager.Singleton.IsClient && clientSpawnPoint != null)
        {
            spawnPoint = clientSpawnPoint;

            foreach (GameObject card in cards)
            {
                Vector3 tmpPos = card.transform.position;
                card.transform.position = new Vector3(-tmpPos.x, tmpPos.y, -tmpPos.z);
                // ★ 변경점: 회전 변수가 없으므로 고정된 값으로 설정
                card.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }
    }

    public void ArrangeCards(List<GameObject> cardsToArrange)
    {
        Debug.Log($"ArrangeCards {cardsToArrange.Count}, {spawnPoint.name}");
        if (cardsToArrange == null)
        {
            Debug.LogError("cardsToArrange가 null입니다!");
            return;
        }

        bool isSpecificScene = SceneManager.GetActiveScene().name == specificSceneName;

        for (int i = 0; i < cardsToArrange.Count; i++)
        {
            GameObject card = cardsToArrange[i];
            
            if (card == null)
            {
                Debug.LogWarning($"cardsToArrange[{i}]가 null입니다. 건너뜁니다.");
                continue;
            }

            int row = i / maxCardsPerRow;
            int column = i % maxCardsPerRow;

            Vector3 offset = (spawnPoint.right * column * xOffset) +   // X 오프셋은 spawnPoint의 오른쪽 방향으로
                             (spawnPoint.up * row * yOffset);           // Y 오프셋은 spawnPoint의 위쪽 방향으로
            
            Vector3 newPosition = spawnPoint.position + offset;
            card.transform.position = newPosition;

            // ★ 변경점: 회전 변수가 없으므로 SpawnPoint의 기본 회전을 따르도록 설정
            if (spawnPoint != null)
            {
                card.transform.rotation = spawnPoint.rotation;
            }

            var renderer = card.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = i;
        }
    }

    public void SetClientPos()
    {
        spawnPoint = clientSpawnPoint;
    }
}