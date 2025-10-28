using UnityEngine;
using UnityEngine.UI; // Image 사용에 필요
using SimpleJSON;
using TMPro;
using System.IO;      // Path, File 사용에 필요
using UnityEngine.Networking; // UnityWebRequestTexture 사용에 필요
using System.Collections;   // IEnumerator 사용에 필요

 public class CardDisplay_UI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI smallClassText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI gradeText;
    public Image cardImage; // <-- Image 컴포넌트 참조 추가

    [Header("Image Settings")]
    public Sprite defaultSprite; // <-- 선택 사항: 인스펙터에서 기본 이미지 할당

    private JSONNode myCardData;
    private string cardId;
    private string objectId;

    public void SetCardData(JSONNode cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("카드 데이터가 null입니다!");
            return;
        }

        myCardData = cardData;

        // 텍스트 필드 업데이트
        UpdateTextUI(smallClassText, cardData["smallClass"], "smallClass");
        UpdateTextUI(costText, cardData["cost"], "cost");
        UpdateTextUI(gradeText, cardData["grade"], "grade");

        // ID 추출 (.Value 사용 및 null 체크 강화)
        cardId = cardData["uid"] != null ? cardData["uid"].Value ?? "unknown_uid" : "unknown_uid";
        objectId = cardData["oid"] != null ? cardData["oid"].Value ?? "unknown_oid" : "unknown_oid";

        // --- 이미지 로드 및 적용 ---
        if (cardImage != null && !string.IsNullOrEmpty(objectId) && objectId != "unknown_oid")
        {
            // 기존 코루틴 중지 (중복 실행 방지)
            StopCoroutine("LoadImageCoroutine"); // 문자열 인자로 코루틴 중지
            StartCoroutine(LoadImageCoroutine(objectId));
        }
        else if (cardImage != null)
        {
            // oid가 유효하지 않거나 Image 컴포넌트가 있을 때 기본 스프라이트 적용
            cardImage.sprite = defaultSprite;
            cardImage.enabled = (defaultSprite != null); // 기본 이미지가 있을 때만 활성화
            // 유효하지 않은 경우 로그 추가
            if(string.IsNullOrEmpty(objectId) || objectId == "unknown_oid")
            {
                Debug.LogWarning($"유효하지 않은 objectId [{objectId}] 또는 cardImage 문제로 기본 스프라이트를 사용합니다.");
            }
        }
        else
        {
            // cardImage 자체가 null인 경우 에러 로그
            Debug.LogError("Card Image 컴포넌트가 할당되지 않았습니다!");
        }
        // --------------------------
    }

    private IEnumerator LoadImageCoroutine(string oid)
    {
        // --- [디버그 로그 추가] ---
        Debug.Log($"코루틴 시작: LoadImageCoroutine for oid: {oid}");
        // --- [수정] 확장자를 다시 .jpg로 변경 ---
        string imagePath = Path.Combine(Application.persistentDataPath, "images", oid + ".jpg");
        // ------------------------------------
        Debug.Log($"이미지 경로: {imagePath}");
        string url = "file://" + imagePath; // 로컬 파일 접근을 위한 URL 형식
        Debug.Log($"URL: {url}");
        // --------------------------

        // --- [디버그 로그 추가] ---
        // 파일 존재 여부 확인 로그 개선 (파일 이름 포함)
        bool fileExists = File.Exists(imagePath); // 결과를 변수에 저장
        Debug.Log($"파일 존재 여부 ({Path.GetFileName(imagePath)}): {fileExists}");
        // --------------------------
        if (fileExists) // 변수 사용
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            // --- [디버그 로그 추가] ---
            // 웹 요청 결과 로그 개선 (oid 포함)
            Debug.Log($"웹 요청 결과 ({oid}): {request.result}");
            // 웹 요청 에러 발생 시 상세 로그 추가
            if(request.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"웹 요청 에러 ({oid}): {request.error} (Code: {request.responseCode})");
            }
            // --------------------------

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                // --- [디버그 로그 추가] ---
                // 텍스처 로드 성공 여부 로그 개선 (oid 포함)
                Debug.Log($"텍스처 로드 성공 여부 ({oid}): {texture != null}");
                // --------------------------
                if (texture != null)
                {
                    // 로드된 Texture2D로부터 Sprite 생성
                    Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f); // Pixels per unit 추가 (기본값 100)
                    if (cardImage != null) // cardImage null 체크 추가
                    {
                        cardImage.sprite = sprite;
                        cardImage.enabled = true; // 이미지 표시
                        Debug.Log($"UI 이미지 로드 및 적용 성공: {imagePath}"); // 성공 로그 추가
                    }
                    else
                    {
                        Debug.LogError($"cardImage 참조가 null입니다. 스프라이트를 적용할 수 없습니다. ({oid})");
                        // 로드된 텍스처 메모리 해제 고려
                        Destroy(texture); // 스프라이트 생성 실패 시 텍스처 해제
                    }
                }
                else
                {
                    Debug.LogError($"텍스처 로드 실패 (null): {imagePath}");
                    if (cardImage != null) // cardImage null 체크 추가
                    {
                        cardImage.sprite = defaultSprite;
                        cardImage.enabled = (defaultSprite != null);
                    }
                }
            }
            else
            {
                // 에러 로그는 위에서 이미 출력됨
                if (cardImage != null) // cardImage null 체크 추가
                {
                    cardImage.sprite = defaultSprite;
                    cardImage.enabled = (defaultSprite != null);
                }
            }
            // 리소스 해제를 위해 request Dispose 호출
            if (request != null) // request null 체크 추가
            {
                request.Dispose(); // using 문을 사용하지 않으므로 수동 Dispose
            }
        }
        else
        {
            // 파일이 없을 경우 기본 스프라이트 적용
            if (cardImage != null) // cardImage null 체크 추가
            {
                cardImage.sprite = defaultSprite;
                cardImage.enabled = (defaultSprite != null);
            }
        }
    }


    private void UpdateTextUI(TextMeshProUGUI textElement, JSONNode nodeData, string fieldName)
    {
        if (textElement == null)
        {
            // 인스펙터 연결 안 됐을 때 경고 (필요 시 주석 해제)
            // Debug.LogError($"{fieldName} TextMeshProUGUI 요소가 할당되지 않았습니다!");
            return;
        }

        if (nodeData != null)
        {
            // JSON 문자열에서 앞뒤 큰따옴표 제거 (.Value 사용)
            textElement.text = nodeData.Value; // .Value 사용 시 따옴표 자동 제거
        }
        else
        {
            textElement.text = ""; // 데이터 없으면 비움
            Debug.LogWarning($"JSON 데이터에 '{fieldName}' 필드가 없습니다.");
        }
    }

    // 나머지 메서드는 동일
    public void SetJsonData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
                Debug.LogWarning("SetJsonData에 전달된 JSON 문자열이 비어있습니다.");
                return;
        }

        try
        {
            JSONNode cardData = JSON.Parse(jsonData);
                if (cardData == null)
            {
                Debug.LogError("JSON 파싱 실패! 결과가 null입니다. 원본 JSON: " + jsonData);
                return;
            }
            SetCardData(cardData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 데이터 파싱 중 오류 발생: {e.Message}\n원본 JSON: {jsonData}");
        }
    }

    public string GetJsonData()
    {
        return myCardData != null ? myCardData.ToString() : null;
    }

    // 카드 ID 반환
    public string GetCardId()
    {
        return cardId;
    }

    // 오브젝트 ID 반환
    public string GetObjectId()
    {
        return objectId;
    }

    // 개별 필드 접근자 (Null 조건 연산자 ?. 사용 및 기본값 제공)
    public string GetOid() => myCardData?["oid"]?.Value ?? "unknown_oid";
    public string GetSmallClass() => myCardData?["smallClass"]?.Value ?? "unknown";
    public int GetCost() => myCardData?["cost"]?.AsInt ?? 0; // AsInt 사용 및 기본값 0
    // GetGrade는 문자열일 수 있으므로 .Value 사용, 숫자인 경우 AsInt 사용 고려
    public string GetGrade() => myCardData?["grade"]?.Value ?? "Normal";
}