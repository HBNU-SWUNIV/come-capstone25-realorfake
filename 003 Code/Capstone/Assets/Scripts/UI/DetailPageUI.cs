using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using SimpleJSON;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections;

[System.Serializable]
public class AuctionResponse
{
    public bool success;
    public List<CardItemData> data;
}
public class DetailPageUI : MonoBehaviour
{
    [Header("UI References")]
    public Button backButton;
    public GameObject mainPanel;

    [Header("Card List Section")]
    public ScrollRect cardScrollView;
    public Transform cardContent;
    public Button leftArrowButton;
    public Button rightArrowButton;
    public GameObject cardPrefab;

    [Header("Page Settings")]
    public int cardsPerPage = 6;

    [Header("Detail Section")]
    public Image selectedCardImage;
    public TextMeshProUGUI selectedCardCategoryText;
    public TextMeshProUGUI selectedCardStatsText;
    public TextMeshProUGUI selectedCardGradeText;

    [Header("Price Section")]
    public InputField priceInput;
    public TextMeshProUGUI priceDisplayText;

    [Header("Action Buttons")]
    public Button RegistrationButton;
    public Button goBackButton;

    [Header("Data")]
    public List<CardItemData> registeredItems = new List<CardItemData>();

    private CardItemDataManager dataManager;
    private int currentPageIndex = 0;
    private List<GameObject> currentCards = new List<GameObject>();
    private CardItemData selectedCardData;

    string path;
    private static string url = PlayerDataManager._serverUrl;

    void Awake()
    {
        path = Path.Combine(Application.persistentDataPath, "myJson.json");
    }

    void Start()
    {
        SetupButtons();
        LoadRegisteredItems();
        ClearCardDetails();

        if (registeredItems.Count > 0)
        {
            StartCoroutine(GetUserRegisteredItemData(PlayerDataManager._uid));
        }
        UpdateNavigationButtons();
    }

    void SetupButtons()
    {
        backButton?.onClick.AddListener(BackToMain);
        leftArrowButton?.onClick.AddListener(PreviousPage);
        rightArrowButton?.onClick.AddListener(NextPage);
        RegistrationButton?.onClick.AddListener(Registration); //버튼 컴포넌트는 이거로 연결 >> Registration 함수 호출
        goBackButton?.onClick.AddListener(BackToMain);
        
        priceInput?.onValueChanged.AddListener(OnPriceInputChanged);
    }

    void OnPriceInputChanged(string newPrice)
    {
        if (priceDisplayText != null)
        {
            priceDisplayText.text = FormatPrice(newPrice);
        }
    }

    void LoadRegisteredItems()
    {
        // // db에서 해당 uid 사용자의 경매장 아이템을 호출해 가져옴
        // var list = await GetUserAuctionItemsAsync(PlayerDataManager._uid);
        // registeredItems.Clear();

        // if (list == null)
        // {
        //     Debug.LogWarning("등록된 아이템을 불러오지 못했습니다.");
        //     return;
        // }

        // foreach (var item in list)
        // {
        //     Debug.Log($"OID: {item.oid}, Cost: {item.cost}, Class: {item.bigClass}/{item.smallClass}, Stat: {item.stat}");
        //     registeredItems.Add(item);
        // }

        Debug.Log($"JSON 파일 로드 시도: {path}");
        // TextAsset jsonFile = Resources.Load<TextAsset>(path);
        string jsonText = File.ReadAllText(path);
        TextAsset jsonFile = new TextAsset(jsonText);
        if (jsonFile != null)
        {
            CardItemDataList cardList = JsonUtility.FromJson<CardItemDataList>("{\"items\":" + jsonFile.text + "}");
            registeredItems = cardList.items;
            Debug.Log($"Loaded {registeredItems.Count} registered items");
        }
        else
        {
            Debug.LogError("Failed to load JSON file"); // 
        }
    }

    void DisplayCurrentCards()
    {
        ClearCurrentCards();

        int startIndex = currentPageIndex * cardsPerPage;
        int endIndex = Mathf.Min(startIndex + cardsPerPage, registeredItems.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            CreateCard(registeredItems[i], i);
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

    void CreateCard(CardItemData itemData, int index)
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
        
        // <<-- 수정됨: InputField 오브젝트를 활성화(보이게) 합니다.
        if (priceInput != null)
        {
            priceInput.gameObject.SetActive(true);
        }
        // <<-- 수정됨: 가격 표시 텍스트도 함께 활성화합니다.
        if (priceDisplayText != null)
        {
            priceDisplayText.gameObject.SetActive(true);
        }
    }

    void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            DisplayCurrentCards();
        }
    }

    void NextPage()
    {
        int maxPages = Mathf.CeilToInt((float)registeredItems.Count / cardsPerPage);
        if (currentPageIndex < maxPages - 1)
        {
            currentPageIndex++;
            DisplayCurrentCards();
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

    void Registration() 
    {
        if (selectedCardData == null)
        {
            Debug.LogWarning("등록할 아이템이 선택되지 않았습니다.");
            return;
        }
        Debug.Log($"Registration for item: {selectedCardData.smallClass}");

        dataManager = FindFirstObjectByType<CardItemDataManager>();
        if (dataManager == null)
        {
            Debug.LogError("CardItemDataManager를 찾을 수 없습니다.");
            return;
        }

        string registrationPrice = priceInput.text;

        if (string.IsNullOrEmpty(registrationPrice))
        {
            Debug.LogWarning("가격이 입력되지 않았습니다.");
            return;
        }
        
        dataManager.RegisterAuctionItem(selectedCardData.oid, registrationPrice, true);
        Debug.Log("등록 완료");

        // 로직
        StartCoroutine(GetUserRegisteredItemData(PlayerDataManager._uid));
    }

    public IEnumerator GetUserRegisteredItemData(int uid) {
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

            foreach (var item in list)
            {
                int idx = registeredItems.FindIndex(x => x.oid == item.oid);
                if (idx != -1) {
                    registeredItems.RemoveAt(idx);
                }
            }
        }

        DisplayCurrentCards();
        ClearCardDetails();
    }

    

    void BackToMain()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);

        gameObject.SetActive(false);
    }

    #region Utility Functions

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
        jsonObject["expireCount"] = itemData.expireCount;
        jsonObject["stat"] = itemData.stat;
        jsonObject["grade"] = itemData.grade;
        return jsonObject;
    }

    void DisplayCardDetails(CardItemData itemData)
    {
        if (selectedCardImage != null)
        {
            // 카드 이미지 설정 로직
        }
        if (selectedCardCategoryText != null)
            selectedCardCategoryText.text = $"{itemData.smallClass}";
        if (selectedCardStatsText != null)
            selectedCardStatsText.text = $"{itemData.stat}";
        if (selectedCardGradeText != null)
            selectedCardGradeText.text = $"{itemData.grade}";
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

        if (priceInput != null)
        {
            priceInput.text = "";
            priceInput.gameObject.SetActive(false); // <<-- 수정됨: InputField 오브젝트를 비활성화(숨김)
        }
        if (priceDisplayText != null)
        {
            priceDisplayText.text = "0";
            priceDisplayText.gameObject.SetActive(false); // <<-- 수정됨: 가격 표시 텍스트도 함께 비활성화
        }
    }

    #endregion
}