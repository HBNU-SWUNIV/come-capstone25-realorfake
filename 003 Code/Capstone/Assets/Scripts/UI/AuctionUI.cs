using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Unity.Android.Types;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class AuctionUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject mainPanel;
    public GameObject detailPage;
    public GameObject itemCancelPage;

    public ScrollRect leftView;
    public ScrollRect rightView;

    public Button nextButton;
    public Button prevButton;
    public Button registationButton;
    public Button registeredItemsButton;

    [Header("Card")]
    public GameObject cardPrefab;

    [Header("Pagination")]
    public int currentPage = 0;
    // 페이지당 아이템 개수를 Inspector에서 직접 설정하도록 변경합니다.
    // 예를 들어, 6열 3줄로 페이지를 구성하고 싶다면 18로 설정하세요.
    public int itemsPerPage = 18;

    private readonly List<CardItemData> allItems = new(); // 옥션에 진입하면 경매장에 올라온 모든 아이템 목록
    private readonly List<GameObject> currentCards = new();

    private RectTransform leftContent, rightContent;
    
    // 서버 응답 구조
    [System.Serializable]
    public class AuctionResponse
    {
        public bool success;
        public List<CardItemData> data;
    }

    private static string url = PlayerDataManager._serverUrl;

    public GameObject purchaseConfirmationPrefab;

    void Awake()
    {
        leftContent  = leftView.content;
        rightContent = rightView.content;

        var leftGrid = leftContent.GetComponent<GridLayoutGroup>() ?? leftContent.gameObject.AddComponent<GridLayoutGroup>();
        var rightGrid = rightContent.GetComponent<GridLayoutGroup>() ?? rightContent.gameObject.AddComponent<GridLayoutGroup>();
    }


    void Start()
    {
        //LoadJSONData();
        StartCoroutine(GetAllOnSaleItemCoroutine());
        SetupButtons();
        //DisplayCurrentPage();

        /// search 화면 진입 : 모든 아이템 조회, 
        /// 받은 json 데이터 이용해서 아이템 리스트 화면에 표시 (대분류, 소분류, 능력치, 가격)
        // Task 실행만 던지고, 기다리지는 않음
    
    }

    async void LoadJSONData()
    {
        // 이제 db의 모든 유저의 경매장 아이템 가져옴
        var list = await GetAllOnSaleItemsAsync();
        allItems.Clear();

        if (list == null)
        {
            Debug.LogWarning("경매장 전체 매물을 불러오지 못했습니다.");
            return;
        }

        foreach (var item in list)
        {
            Debug.Log($"OID: {item.oid}, Cost: {item.cost}, Class: {item.bigClass}/{item.smallClass}, Stat: {item.stat}");

            allItems.Add(item);
        }
        Debug.Log($"불러온 아이템 개수: {list.Count}");

        // TextAsset jsonFile = Resources.Load<TextAsset>("JSON/TestJSON/test");
        // if (jsonFile != null)
        // {
        //     // 파일 내용은 제대로 읽혔는지 확인
        //     Debug.Log("JSON 파일 로드 성공. 내용 길이: " + jsonFile.text.Length);
            
        //     var list = JsonUtility.FromJson<CardItemDataList>("{\"items\":" + jsonFile.text + "}");
        //     allItems.Clear();
        //     allItems.AddRange(list.items);

        //     // 최종적으로 리스트에 몇 개의 아이템이 들어왔는지 확인
        //     Debug.Log($"총 {allItems.Count}개의 아이템이 리스트에 추가되었습니다.");
        // }
        // else
        // {
        //     Debug.LogError("JSON/TestJSON/test.json 파일을 찾을 수 없습니다! 경로를 확인해주세요.");
        // }
    }

    void SetupButtons()
    {
        nextButton?.onClick.AddListener(NextPage);
        prevButton?.onClick.AddListener(PrevPage);
        registationButton?.onClick.AddListener(ShowDetailPage);
        registeredItemsButton?.onClick.AddListener(ShowItemCancelPage);
    }

    void DisplayCurrentPage()
    {
        ClearCurrentCards();

        // 페이지 계산 로직이 양쪽 뷰를 합쳐서 처리하므로 itemsPerPage * 2 를 사용합니다.
        int startIdx   = currentPage * itemsPerPage * 2;
        int leftStart  = startIdx;
        int rightStart = startIdx + itemsPerPage;

        // Left View
        for (int i = 0; i < itemsPerPage && leftStart + i < allItems.Count; i++)
            CreateCard(allItems[leftStart + i], leftContent);

        // Right View
        for (int i = 0; i < itemsPerPage && rightStart + i < allItems.Count; i++)
            CreateCard(allItems[rightStart + i], rightContent);

        UpdateButtonStates();
    }

    void CreateCard(CardItemData data, Transform parent) //이거 호출되는 걸 봐야할거 같은데 
    {
        Debug.Log("카드 제작"); // 나 시프트가 안돼 상ㅋㅋ
        if (!cardPrefab) return;
        // Instantiate만 하면 GridLayoutGroup이 알아서 위치를 잡아줍니다.
        var go = Instantiate(cardPrefab, parent);
        currentCards.Add(go);

        var cd = go.GetComponent<CardDisplay_UI>();
        if (cd != null) cd.SetCardData(ConvertToJSONNode(data));

        var btn = go.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => OnCardClicked(data));
    }

    JSONNode ConvertToJSONNode(CardItemData d)
    {
        var o = new JSONObject();
        o["oid"]=d.oid; o["uid"]=d.uid; o["bigClass"]=d.bigClass; o["smallClass"]=d.smallClass;
        o["abilityType"]=d.abilityType; o["sellState"]=d.sellState; o["cost"]=d.cost;
        o["expireCount"]=d.expireCount; o["stat"]=d.stat; o["grade"]=d.grade;
        return o;
    }

    void ClearCurrentCards()
    {
        foreach (var c in currentCards) if (c) Destroy(c);
        currentCards.Clear();
    }

    void NextPage()
    {
        int maxPages = Mathf.CeilToInt((float)allItems.Count / (itemsPerPage * 2));
        if (currentPage < maxPages - 1)
        {
            currentPage++;
            DisplayCurrentPage();
        }
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            DisplayCurrentPage();
        }
    }

    void UpdateButtonStates()
    {
        int maxPages = Mathf.CeilToInt((float)allItems.Count / (itemsPerPage * 2));
        if (prevButton) prevButton.interactable = currentPage > 0;
        if (nextButton) nextButton.interactable = currentPage < maxPages - 1;
    }

    void ShowDetailPage()
    {
        mainPanel?.SetActive(false);
        detailPage?.SetActive(true);
        itemCancelPage?.SetActive(false);
    }

    void ShowItemCancelPage()
    {
        mainPanel?.SetActive(false);
        detailPage?.SetActive(false);
        itemCancelPage?.SetActive(true);
    }

    void OnCardClicked(CardItemData itemData)
    {
        Debug.Log($"Card clicked: {itemData.smallClass} (OID: {itemData.oid})");

        if (purchaseConfirmationPrefab != null)
        {
            // 구매 확인 UI 생성
            GameObject uiInstance = Instantiate(purchaseConfirmationPrefab);

            // 사용자 앞에 UI를 띄우기 위해 카메라 위치를 기준으로 설정
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                uiInstance.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 2.0f; // 2미터 앞에 표시
                uiInstance.transform.rotation = Quaternion.LookRotation(uiInstance.transform.position - mainCamera.transform.position);
            }

            // 생성된 UI에 카드 정보 전달
            var purchaseUI = uiInstance.GetComponent<PurchaseConfirmationUI>();
            if (purchaseUI != null)
            {
                purchaseUI.Setup(itemData);
            }
            else
            {
                Debug.LogError("PurchaseConfirmationPrefab에 PurchaseConfirmationUI 스크립트가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("PurchaseConfirmationPrefab이 AuctionUI에 할당되지 않았습니다.");
        }
    }

    public IEnumerator GetAllOnSaleItemCoroutine() {

        string onsale_url = $"{url}/shop/onsale";

        using (UnityWebRequest req = UnityWebRequest.Get(onsale_url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            yield return req.SendWebRequest();
            

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GetAllOnSaleItemsAsync] 요청 실패: {req.error}");
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log(json);
            AuctionResponse response = JsonUtility.FromJson<AuctionResponse>(json);

            if (!response.success)
            {
                Debug.LogWarning("[GetAllOnSaleItemsAsync] 서버 응답 실패");
                yield break;
            }

            Debug.Log($"[GetAllOnSaleItemsAsync] {response.data.Count}개의 매물이 로드되었습니다.");
            var list = response.data;

            allItems.Clear();

            if (list == null)
            {
                Debug.LogWarning("경매장 전체 매물을 불러오지 못했습니다.");
                yield break;
            }


            foreach (var item in list)
            {
                Debug.Log($"UID ; {item.uid}, OID: {item.oid}, Cost: {item.cost}, Class: {item.bigClass}/{item.smallClass}, Stat: {item.stat}");
                FindAnyObjectByType<FileManager>().DownloadImage(item.oid);
                if (PlayerDataManager._uid != int.Parse(item.uid))
                    allItems.Add(item);
            }
            Debug.Log($"불러온 아이템 개수: {list.Count}");
            StartCoroutine(CheckImages());
        }
    }

    IEnumerator CheckImages()
    {
        int total = allItems.Count;
        while (true)
        {
            int cur = 0;
            foreach (var e in allItems)
            {
                string imgPath = Path.Combine(Application.persistentDataPath, "images", e.oid + ".jpg");
                if (File.Exists(imgPath))
                    cur++;
            }
            if (cur == total)
            {
                DisplayCurrentPage();
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public static async Task<List<CardItemData>> GetAllOnSaleItemsAsync()
    {
        string onsale_url = $"{url}/shop/onsale";

        using (UnityWebRequest req = UnityWebRequest.Get(onsale_url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            var operation = req.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GetAllOnSaleItemsAsync] 요청 실패: {req.error}");
                return null;
            }

            string json = req.downloadHandler.text;
            AuctionResponse response = JsonUtility.FromJson<AuctionResponse>(json);

            if (!response.success)
            {
                Debug.LogWarning("[GetAllOnSaleItemsAsync] 서버 응답 실패");
                return null;
            }

            Debug.Log($"[GetAllOnSaleItemsAsync] {response.data.Count}개의 매물이 로드되었습니다.");
            return response.data;
        }
    }


    
}