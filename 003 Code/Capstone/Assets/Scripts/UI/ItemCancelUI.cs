using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using SimpleJSON;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections;

public class ItemCancelUI : MonoBehaviour
{
    [Header("UI References")]
    public Button backButton;
    public GameObject mainPanel;
    
    [Header("Card List Section")]
    public ScrollRect cardScrollView;
    public Transform cardContent; // Horizontal Layout Group이 있는 Transform
    public Button leftArrowButton;
    public Button rightArrowButton;
    public GameObject cardPrefab;

    [Header("Page Settings")]
    public int cardsPerPage = 7; // 페이지당 보여줄 카드 개수 (Inspector에서 조절 가능)
    
    [Header("Detail Section")]
    public Image selectedCardImage;
    public TextMeshProUGUI selectedCardCategoryText;
    public TextMeshProUGUI selectedCardStatsText;
    public TextMeshProUGUI selectedCardGradeText;
    public TextMeshProUGUI selectedCardPriceText; // <<-- 가격 표시용 텍스트 (기존)
    
    // 👇 추가된 부분 시작
    [Header("Price Section")]
    public InputField priceInput;
    public TextMeshProUGUI priceDisplayText;
    // 👆 추가된 부분 끝

    [Header("Action Buttons")]
    public Button cancelRegistrationButton;
    public Button goBackButton;
    
    [Header("Data")]
    public List<CardItemData> registeredItems = new List<CardItemData>();
    
    private int currentPageIndex = 0;
    private List<GameObject> currentCards = new List<GameObject>();
    private CardItemData selectedCardData;
    
    string path;
    private static string url = PlayerDataManager._serverUrl;

    private CardItemDataManager dataManager;

    void Awake()
    {
        path = Path.Combine(Application.persistentDataPath, "myJson.json");
    }

    void Start()
    {
        SetupButtons();
        //LoadRegisteredItems();
        StartCoroutine(GetUserAuctionItemCoroutine(PlayerDataManager._uid));
        ClearCardDetails(); // 시작 시 상세 정보 및 InputField 숨김
        
    }
    
    void SetupButtons()
    {
        backButton?.onClick.AddListener(BackToMain);
        leftArrowButton?.onClick.AddListener(PreviousPage);
        rightArrowButton?.onClick.AddListener(NextPage);
        cancelRegistrationButton?.onClick.AddListener(CancelRegistration);
        goBackButton?.onClick.AddListener(BackToMain);

        // 👇 추가된 부분: InputField 값 변경 리스너 등록
        priceInput?.onValueChanged.AddListener(OnPriceInputChanged);
    }
    
    // 👇 추가된 부분: InputField 값이 변경될 때마다 호출되어 priceDisplayText를 업데이트
    void OnPriceInputChanged(string newPrice)
    {
        if (priceDisplayText != null)
        {
            priceDisplayText.text = FormatPrice(newPrice);
        }
    }
    // 👆 추가된 부분 끝

    async void LoadRegisteredItems()
    {
        // db에서 해당 uid 사용자의 경매장 아이템을 호출해 가져옴
        var list = await GetUserAuctionItemsAsync(PlayerDataManager._uid);
        registeredItems.Clear();

        if (list == null)
        {
            Debug.LogWarning("등록된 아이템을 불러오지 못했습니다.");
            return;
        }

        foreach (var item in list)
        {
            Debug.Log($"OID: {item.oid}, Cost: {item.cost}, Class: {item.bigClass}/{item.smallClass}, Stat: {item.stat}");
            registeredItems.Add(item);
        }


        //     TextAsset jsonFile = Resources.Load<TextAsset>(path);
        //     if (jsonFile != null)
        // {
        //     CardItemDataList cardList = JsonUtility.FromJson<CardItemDataList>("{\"items\":" + jsonFile.text + "}");
        //     registeredItems = cardList.items;
        //     Debug.Log($"Loaded {registeredItems.Count} registered items");
        // }
        // else
        // {
        //     Debug.LogError("Failed to load JSON file");
        // }
    }

    public IEnumerator GetUserAuctionItemCoroutine(int uid) {
        string shop_url = $"{url}/shop/{uid}";
        using (UnityWebRequest req = UnityWebRequest.Get(shop_url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            yield return req.SendWebRequest();

            // 네트워크 실패
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GetMyAuctionItemsAsync] 요청 실패: {req.error}");
                yield break;
            }

            // JSON 파싱
            string json = req.downloadHandler.text;
            AuctionResponse response = JsonUtility.FromJson<AuctionResponse>(json);

            if (!response.success)
            {
                Debug.LogWarning("[GetMyAuctionItemsAsync] 서버 응답 실패");
                yield break;
            }

            Debug.Log($"[GetMyAuctionItemsAsync] {response.data.Count}개의 매물 데이터를 불러왔습니다.");
            var list = response.data;

            registeredItems.Clear();

            if (list == null)
            {
                Debug.LogWarning("등록된 아이템을 불러오지 못했습니다.");
                yield break;
            }

            foreach (var item in list)
            {
                Debug.Log($"OID: {item.oid}, Cost: {item.cost}, Class: {item.bigClass}/{item.smallClass}, Stat: {item.stat}");
                registeredItems.Add(item);
            }
        }

        if (registeredItems.Count > 0)
        {
            DisplayCurrentPageCards();
        }
        UpdateNavigationButtons();
    }


    /// <summary>
    /// 특정 UID 사용자가 올린 모든 매물 정보를 가져옵니다.
    /// </summary>
    /// <param name="uid">유저 UID</param>
    /// <returns>해당 UID가 올린 경매장 아이템 리스트</returns>
    public static async Task<List<CardItemData>> GetUserAuctionItemsAsync(int uid)
    {
        string shop_url = $"{url}/shop/{uid}";
        using (UnityWebRequest req = UnityWebRequest.Get(shop_url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            var operation = req.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            // 네트워크 실패
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GetMyAuctionItemsAsync] 요청 실패: {req.error}");
                return null;
            }

            // JSON 파싱
            string json = req.downloadHandler.text;
            AuctionResponse response = JsonUtility.FromJson<AuctionResponse>(json);

            if (!response.success)
            {
                Debug.LogWarning("[GetMyAuctionItemsAsync] 서버 응답 실패");
                return null;
            }

            Debug.Log($"[GetMyAuctionItemsAsync] {response.data.Count}개의 매물 데이터를 불러왔습니다.");
            return response.data;
        }
    }
    
    void DisplayCurrentPageCards()
    {
        ClearCurrentCards();
        
        int startIndex = currentPageIndex * cardsPerPage;
        int endIndex = Mathf.Min(startIndex + cardsPerPage, registeredItems.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateCard(registeredItems[i], i, startIndex);
        }
        
        UpdateNavigationButtons();

        if (endIndex > startIndex)
        {
            OnCardClicked(registeredItems[startIndex]);
        }
        else
        {
            ClearCardDetails();
        }
    }
    
    void CreateCard(CardItemData itemData, int itemIndex, int pageStartIndex)
    {
        if (cardPrefab == null || cardContent == null) return;
        
        GameObject card = Instantiate(cardPrefab, cardContent);
        currentCards.Add(card);
        
        var cardDisplay = card.GetComponent<CardDisplay_UI>(); 
        if (cardDisplay != null)
        {
            JSONNode jsonNode = ConvertToJSONNode(itemData);
            cardDisplay.SetCardData(jsonNode);
        }
        else
        {
            Debug.LogError("Card Prefab에 CardDisplay_UI 컴포넌트가 없습니다!");
        }
        
        Button cardButton = card.GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(() => OnCardClicked(itemData));
        }
    }
    
    void ClearCurrentCards()
    {
        foreach (GameObject card in currentCards)
        {
            if (card != null)
                Destroy(card);
        }
        currentCards.Clear();
    }

    void OnCardClicked(CardItemData itemData)
    {
        selectedCardData = itemData;
        DisplayCardDetails(itemData);
        HighlightSelectedCard(itemData);

        // 👇 추가된 부분: InputField 및 가격 표시 UI 활성화 및 값 로드
        if (priceInput != null)
        {
            priceInput.gameObject.SetActive(true);
            // InputField에 현재 등록된 가격(customPrice)을 로드
            string priceToLoad = !string.IsNullOrEmpty(itemData.customPrice) ? itemData.customPrice : itemData.cost;
            priceInput.text = priceToLoad;
        }
        if (priceDisplayText != null)
        {
            priceDisplayText.gameObject.SetActive(true);
        }
        // 👆 추가된 부분 끝
    }
    
    void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            DisplayCurrentPageCards();
        }
    }
    
    void NextPage()
    {
        int maxPages = Mathf.CeilToInt((float)registeredItems.Count / cardsPerPage);
        if (currentPageIndex < maxPages - 1)
        {
            currentPageIndex++;
            DisplayCurrentPageCards();
        }
    }
    
    void UpdateNavigationButtons()
    {
        if (registeredItems.Count == 0 || cardsPerPage <= 0) return;
        int maxPages = Mathf.CeilToInt((float)registeredItems.Count / cardsPerPage);
        
        if (leftArrowButton != null)
            leftArrowButton.interactable = currentPageIndex > 0;
            
        if (rightArrowButton != null)
            rightArrowButton.interactable = currentPageIndex < maxPages - 1;
    }
    
    void BackToMain()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);
            
        gameObject.SetActive(false);
    }

    #region Utility Functions (데이터 변환 및 UI 업데이트)
    
    JSONNode ConvertToJSONNode(CardItemData itemData)
    {
        JSONObject jsonObject = new JSONObject();
        jsonObject["oid"] = itemData.oid;
        jsonObject["uid"] = itemData.uid;
        jsonObject["bigClass"] = itemData.bigClass;
        jsonObject["smallClass"] = itemData.smallClass;
        jsonObject["abilityType"] = itemData.abilityType;
        jsonObject["sellState"] = itemData.sellState;
        jsonObject["cost"] = itemData.cost;
        jsonObject["customPrice"] = itemData.customPrice; // <<-- customPrice도 변환에 포함
        jsonObject["expireCount"] = itemData.expireCount;
        jsonObject["stat"] = itemData.stat;
        jsonObject["grade"] = itemData.grade;
        return jsonObject;
    }

    void DisplayCardDetails(CardItemData itemData)
    {
        if (selectedCardImage != null)
        {
            // 카드 이미지 설정 로직 (예시: Resources.Load 등)
        }
        
        if (selectedCardCategoryText != null)
            selectedCardCategoryText.text = $"{itemData.smallClass}";
        
        if (selectedCardStatsText != null)
            selectedCardStatsText.text = $"{itemData.stat}";
            
        if (selectedCardGradeText != null)
            selectedCardGradeText.text = $"{itemData.grade}";
            
        // 👇 수정된 부분: 가격 텍스트를 InputField의 현재 값으로 업데이트
        if (selectedCardPriceText != null)
        {
            selectedCardPriceText.text = FormatPrice(
                priceInput != null && priceInput.gameObject.activeSelf ? priceInput.text : 
                !string.IsNullOrEmpty(itemData.customPrice) ? itemData.customPrice : itemData.cost
            );
        }
    }

    void HighlightSelectedCard(CardItemData itemData)
    {
        // 선택된 카드 하이라이트 로직
    }
    
    string FormatPrice(string price)
    {
        if (string.IsNullOrEmpty(price)) return "0";
        if (int.TryParse(price, out int priceValue))
        {
            return priceValue.ToString("N0");
        }
        return price;
    }

    void ClearCardDetails()
    {
        if (selectedCardImage != null) selectedCardImage.sprite = null;
        if (selectedCardCategoryText != null) selectedCardCategoryText.text = "";
        if (selectedCardStatsText != null) selectedCardStatsText.text = "";
        if (selectedCardGradeText != null) selectedCardGradeText.text = "";
        
        if (selectedCardPriceText != null)
            selectedCardPriceText.text = "0";

        // 👇 추가된 부분: InputField 및 가격 표시 텍스트 비활성화
        if (priceInput != null)
        {
            priceInput.text = "";
            priceInput.gameObject.SetActive(false);
        }
        if (priceDisplayText != null)
        {
            priceDisplayText.text = "";
            priceDisplayText.gameObject.SetActive(false);
        }
        // 👆 추가된 부분 끝
    }

    void CancelRegistration()
    {
        if (selectedCardData == null)
        {
            Debug.LogWarning("취소할 아이템이 선택되지 않았습니다.");
            return;
        }

        dataManager = FindFirstObjectByType<CardItemDataManager>();
        if (dataManager == null)
        {
            Debug.LogError("CardItemDataManager를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"Cancel Registration for item: {selectedCardData.smallClass}, {selectedCardData.oid}");
        // TODO: 여기에 실제 등록 취소 로직을 구현해야 합니다.
        dataManager.RegisterAuctionItem(selectedCardData.oid, "-1", false);

        StartCoroutine(GetUserAuctionItemCoroutine(PlayerDataManager._uid));
        ClearCardDetails();
    }

    #endregion
}