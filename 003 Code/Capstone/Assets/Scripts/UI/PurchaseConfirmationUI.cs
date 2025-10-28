using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using SimpleJSON;
using TMPro;

public class PurchaseConfirmationUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI majorCategoryText;
    public TextMeshProUGUI subcategoryText;
    public TextMeshProUGUI statText;
    public TextMeshProUGUI priceText;
    public Button yesButton;
    public Button noButton;
    public TextMeshProUGUI _result; // 요청 결과를 표시할 TextMeshProUGUI

    private CardItemData currentItemData;

    void Start()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesButtonClicked);
            // 이게 먼 코드임ㅋㅋ아 아닌데 엄
            //저서 세션관련 코드잖아
            // 이게 안먹힌건가? 이게 yes버튼 이벤트 연결하는 코드 ㅇㅇ 이게 여기서 연결하는건데 근데 그렇다고 하기엔 No버튼 눌러서 UI끄는건 잘됌
            // 그럼 디버깅 해보죠???? 
            // ㅋㅋ 연결 되있는데 왜 안뜨노
            // 클릭 한거지
            // ㅇㅇ

            Debug.Log("yes버튼 연결됨");
        }
        if (noButton != null)
        {
            noButton.onClick.AddListener(CloseUI);
        }
    }

    public void Setup(CardItemData itemData)
    {
        currentItemData = itemData;
        majorCategoryText.text = "Major category: " + itemData.bigClass;
        subcategoryText.text = "Subcategory: " + itemData.smallClass;
        statText.text = "Stat: " + itemData.stat;
        priceText.text = "Price: " + itemData.cost;
    }

    private void OnYesButtonClicked()
    {
        Debug.Log("Yes버튼 눌림");
        // 이거 왜 안뜨냐???
        //일단 이 디버그가 안뜬다
        //이 메서드가 호출이 안된다는거니까
        if (currentItemData != null)
        {
            StartCoroutine(PurchaseAndUpdateCoroutine());
        }
    }

    private IEnumerator PurchaseAndUpdateCoroutine()
    {
        // --- [디버깅] 코루틴 시작 ---
        Debug.Log("== PurchaseAndUpdateCoroutine 시작 ==");
        if (_result != null) _result.text = "구매 절차 시작...";

        // 1. 아이템 구매 요청 (POST)
        Debug.Log("[1/3] 아이템 구매 요청 시도...");
        var purchaseJson = new JSONObject();
        purchaseJson["uid"] = PlayerDataManager._uid;
        purchaseJson["oid"] = currentItemData.oid;

        string purchaseUrl = PlayerDataManager._serverUrl + "/shop/buy";
        Debug.Log($"  - 요청 URL: {purchaseUrl}");
        Debug.Log($"  - 보내는 데이터: {purchaseJson.ToString()}");

        UnityWebRequest purchaseRequest = new UnityWebRequest(purchaseUrl, "POST");
        byte[] purchaseBodyRaw = Encoding.UTF8.GetBytes(purchaseJson.ToString());
        purchaseRequest.uploadHandler = new UploadHandlerRaw(purchaseBodyRaw);
        purchaseRequest.downloadHandler = new DownloadHandlerBuffer();
        purchaseRequest.SetRequestHeader("Content-Type", "application/json");

        yield return purchaseRequest.SendWebRequest();

        if (purchaseRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[구매 실패] 에러: {purchaseRequest.error}");
            Debug.LogError($"  - 응답 코드: {purchaseRequest.responseCode}");
            Debug.LogError($"  - 응답 내용: {purchaseRequest.downloadHandler.text}");
            if (_result != null) _result.text = "구매 실패! (로그 확인)";
            yield return new WaitForSeconds(3);
            CloseUI();
            yield break;
        }

        Debug.Log("[1/3] 아이템 구매 요청 성공!");
        Debug.Log($"  - 응답 내용: {purchaseRequest.downloadHandler.text}");
        if (_result != null) _result.text = "구매 성공! 아이템 목록 갱신 중...";
        
        // --- 2. 사용자 아이템 목록 갱신 요청 (GET) ---
        Debug.Log("[2/3] 사용자 아이템 목록 갱신 요청 시도...");
        string listUrl = PlayerDataManager._serverUrl + $"/instance/list/{PlayerDataManager._uid}";
        Debug.Log($"  - 요청 URL: {listUrl}");

        UnityWebRequest request = UnityWebRequest.Get(listUrl); // GET 방식이므로 Get 사용

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[2/3] 아이템 목록 갱신 요청 성공!");
            Debug.Log($"  - 받은 데이터: {request.downloadHandler.text}");

            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                var retJson = JSON.Parse(request.downloadHandler.text);
                if (retJson["success"] == true) // 이걸 식별을 못해서 success;false라고 우김
                // 근데 200 받은거보면 잘 왓는데
                {
                    Debug.Log("[3/3] 데이터 파싱 성공. 아이템 목록 로컬에 저장 시작...");
                    if (_result != null) _result.text = "목록 갱신 완료. 저장 중...";

                    PlayerDataManager._oids = retJson["oid"].ToString();

                    PlayerDataManager pm = GameObject.FindAnyObjectByType<PlayerDataManager>();
                    if (pm != null)
                    {
                        pm.StartSaveItemCoroutine();
                        Debug.Log("  - PlayerDataManager.StartSaveItemCoroutine() 호출 완료");
                    }
                    else
                    {
                        Debug.LogError("  - PlayerDataManager를 찾지 못해 아이템을 로컬에 저장할 수 없습니다.");
                    }
                }
                else
                {
                    Debug.LogError("[3/3] 서버에서 success:false 를 반환했습니다.");
                    if (_result != null) _result.text = "Result : Fail (서버 응답 오류)";
                }
            }
            else
            {
                Debug.LogWarning("[3/3] 서버 응답이 비어있습니다.");
            }
        }
        else
        {
            Debug.LogError($"[2/3] 아이템 목록 갱신 요청 실패. 에러: {request.error}");
            Debug.LogError($"  - 응답 코드: {request.responseCode}");
            if (_result != null) _result.text = "목록 갱신 실패! (로그 확인)";
        }

        Debug.Log("== PurchaseAndUpdateCoroutine 종료 ==");
        yield return new WaitForSeconds(3); // 결과 확인을 위해 3초 대기
        CloseUI();
    }

    private void CloseUI()
    {
        Destroy(gameObject);
    }
}