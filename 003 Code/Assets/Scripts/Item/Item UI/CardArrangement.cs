using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardArrangement : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>(); // 카드 리스트
    public Transform spawnPoint;   // SpawnPoint 위치 (CardDeckPosition)
    public float yOffset = -0.02f; // Y 오프셋
    public float zOffset = -0.01f; // Z 오프셋
    public float xOffset = 1.5f;   // X 오프셋 (카드 간격)
    public float xRotation = 0f;   // X축 회전 각도
    public int maxCardsPerColumn = 6; // 각 열에 배치될 최대 카드 수

    public string specificSceneName = "FightScene";

    public static CardArrangement Instance;

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

    // Update is called once per frame
    void Update()
    {

    }

    public void ArrangeCards(List<GameObject> cardsToArrange)
    {
        // null 체크 추가
        if (cardsToArrange == null)
        {
            Debug.LogError("cardsToArrange가 null입니다!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("spawnPoint가 null입니다! CardArrangement의 spawnPoint를 설정해주세요.");
            return;
        }

        // 현재 씬이 특정 씬인지 확인
        bool isSpecificScene = SceneManager.GetActiveScene().name == specificSceneName;

        for (int i = 0; i < cardsToArrange.Count; i++)
        {
            GameObject card = cardsToArrange[i];
            
            if (card == null)
            {
                Debug.LogWarning($"cardsToArrange[{i}]가 null입니다. 건너뜁니다.");
                continue;
            }

            int column = i / maxCardsPerColumn;
            int row = i % maxCardsPerColumn;

            //if (isSpecificScene)
            //{
            //    // 특정 씬에서는 컨트롤러의 자식으로 설정
            //    card.transform.SetParent(spawnPoint, false);

            //    // 로컬 좌표로 배치 (부모 기준)
            //    Vector3 localPos = new Vector3(column * xOffset, row * yOffset, row * zOffset);
            //    card.transform.localPosition = localPos;
            //}
            //else
            {
                // 다른 씬에서는 월드 좌표 사용
                // 기존 Z 오프셋 계산 방식을 누적 방식으로 변경
                Vector3 newPosition = spawnPoint.position + new Vector3(column * xOffset, row * yOffset, row * zOffset);
                card.transform.position = newPosition;
                card.transform.rotation = Quaternion.Euler(xRotation, 0f, 0f); // X축 회전 적용
            }

            var renderer = card.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = i;
        }
    }

    // 기존 ArrangeCards 메서드는 더 이상 사용하지 않음
    // public void ArrangeCards()
    // {
    //     ArrangeCards(cards);
    // }
}






