using UnityEngine;
using SimpleJSON;
using TMPro;
using System.IO;      // Path, File 사용에 필요
using UnityEngine.Networking; // UnityWebRequestTexture 사용에 필요
using System.Collections;   // IEnumerator 사용에 필요

public class CardDisplay : MonoBehaviour
{
    [Header("UI Text References")] // TextMeshPro 참조용 헤더 추가
    public TextMeshPro smallClassText;
    public TextMeshPro statText;
    public TextMeshPro gradeText;
    public TextMeshPro costText;

    // --- 이미지 표시용 Renderer 컴포넌트 참조 추가 ---
    [Header("3D Model References")]
    [Tooltip("카드 이미지를 표시할 Mesh Renderer 컴포넌트")] // 툴팁 추가
    public Renderer cardRenderer; // 인스펙터에서 Renderer(예: MeshRenderer) 할당
    [Tooltip("이미지 로드 실패 시 표시할 기본 텍스처 (선택 사항)")] // 툴팁 추가
    public Texture defaultTexture; // 선택 사항: 기본 텍스처
    // ---------------------------------------------------


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

        // 텍스트 필드 설정...
        UpdateTextMeshPro(smallClassText, cardData["smallClass"], "smallClass");
        UpdateTextMeshPro(statText, cardData["stat"], "stat");
        UpdateTextMeshPro(gradeText, cardData["grade"], "grade");
        UpdateTextMeshPro(costText, cardData["cost"], "cost");

        // ID 추출
        cardId = cardData["uid"] != null ? cardData["uid"].ToString().Trim('"') : "unknown_uid";
        objectId = cardData["oid"] != null ? cardData["oid"].ToString().Trim('"') : "unknown_oid";

        // --- 이미지 텍스처 로드 및 적용 ---
        if (cardRenderer != null && !string.IsNullOrEmpty(objectId) && objectId != "unknown_oid")
        {
            // 기존 코루틴 중지 (중복 실행 방지)
            StopCoroutine("LoadImageCoroutine3D"); // 문자열 인자로 코루틴 중지
            StartCoroutine(LoadImageCoroutine3D(objectId));
        }
        else if (cardRenderer != null && cardRenderer.material != null)
        {
            // oid가 유효하지 않거나 renderer가 있을 때 기본 텍스처 적용
            // URP/HDRP 호환성을 위해 SetTexture 사용 고려
            if (cardRenderer.material.HasProperty("_BaseMap"))
            {
                cardRenderer.material.SetTexture("_BaseMap", defaultTexture);
            }
            else if (cardRenderer.material.HasProperty("_MainTex"))
            {
                cardRenderer.material.mainTexture = defaultTexture;
            }
            Debug.LogWarning($"유효하지 않은 objectId [{objectId}] 또는 cardRenderer 문제로 기본 텍스처를 사용합니다.");
        }
        else
        {
            Debug.LogError("Card Renderer 또는 Material이 할당되지 않았습니다!");
        }
        // ------------------------------------

        // Debug.Log("카드 정보 적용 시도 완료!"); // SetCardData 완료 시점 로그
    }

    private IEnumerator LoadImageCoroutine3D(string oid)
    {
        // --- [디버그 로그 추가] ---
        Debug.Log($"코루틴 시작: LoadImageCoroutine3D for oid: {oid}");
        string imagePath = Path.Combine(Application.persistentDataPath, "images", oid + ".jpg");
        Debug.Log($"이미지 경로: {imagePath}");
        string url = "file://" + imagePath; // 로컬 파일 접근을 위한 URL 형식
        Debug.Log($"URL: {url}");
        // --------------------------

        // --- [디버그 로그 추가] ---
        // 파일 존재 여부 확인 로그 개선 (파일 이름 포함)
        Debug.Log($"파일 존재 여부 ({Path.GetFileName(imagePath)}): {File.Exists(imagePath)}");
        // --------------------------
        if (File.Exists(imagePath))
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
                if (texture != null && cardRenderer != null && cardRenderer.material != null)
                {
                    // 기존 코드 주석 처리
                    // cardRenderer.material.mainTexture = texture;

                    // --- [수정된 머티리얼 속성 적용 코드] ---
                    if (cardRenderer.material.HasProperty("_BaseMap")) // _BaseMap 속성이 있는지 확인 (URP/HDRP)
                    {
                        cardRenderer.material.SetTexture("_BaseMap", texture);
                        Debug.Log($"_BaseMap 텍스처 설정 완료 ({oid})");
                    }
                    else if (cardRenderer.material.HasProperty("_MainTex")) // _MainTex 속성이 있는지 확인 (Standard 셰이더 등)
                    {
                        // mainTexture는 _MainTex 속성에 접근하는 편의 프로퍼티
                        cardRenderer.material.mainTexture = texture;
                        Debug.Log($"_MainTex (mainTexture) 텍스처 설정 완료 ({oid})");
                    }
                    else
                    {
                        // 머티리얼 이름과 함께 경고 메시지 표시
                        Debug.LogWarning($"머티리얼 [{cardRenderer.material.name}]에 _BaseMap 또는 _MainTex 속성을 찾을 수 없습니다. ({oid})");
                        // 필요한 경우 다른 텍스처 속성 이름 사용: cardRenderer.material.SetTexture("_YourTextureName", texture);
                    }
                    // ------------------------------------
                }
                else
                {
                    // 실패 원인 명시 (텍스처 null, 렌더러 null, 머티리얼 null)
                    string reason = texture == null ? "texture null" : (cardRenderer == null ? "renderer null" : "material null");
                    Debug.LogError($"3D 카드 텍스처 적용 실패 ({reason}): {imagePath}");
                    // 실패 시 기본 텍스처 적용 (호환성 고려)
                    if(cardRenderer != null && cardRenderer.material != null)
                    {
                        if (cardRenderer.material.HasProperty("_BaseMap")) cardRenderer.material.SetTexture("_BaseMap", defaultTexture);
                        else if (cardRenderer.material.HasProperty("_MainTex")) cardRenderer.material.mainTexture = defaultTexture;
                    }
                }
            }
            else
            {
                // 웹 요청 실패 시 기본 텍스처 적용 (호환성 고려)
                if(cardRenderer != null && cardRenderer.material != null)
                {
                    if (cardRenderer.material.HasProperty("_BaseMap")) cardRenderer.material.SetTexture("_BaseMap", defaultTexture);
                    else if (cardRenderer.material.HasProperty("_MainTex")) cardRenderer.material.mainTexture = defaultTexture;
                }
            }
            // 리소스 해제를 위해 request Dispose 호출
            request.Dispose(); // using 문을 사용하지 않으므로 수동 Dispose
        }
        else
        {
            // 파일이 없을 경우 기본 텍스처 적용 (호환성 고려)
            if(cardRenderer != null && cardRenderer.material != null)
            {
                if (cardRenderer.material.HasProperty("_BaseMap")) cardRenderer.material.SetTexture("_BaseMap", defaultTexture);
                else if (cardRenderer.material.HasProperty("_MainTex")) cardRenderer.material.mainTexture = defaultTexture;
            }
        }
    }


    // TextMeshPro 요소 업데이트 헬퍼 메서드
    private void UpdateTextMeshPro(TextMeshPro textElement, JSONNode nodeData, string fieldName)
    {
        if (textElement != null)
        {
            if (nodeData != null)
            {
                // JSON 문자열에서 앞뒤 큰따옴표 제거
                textElement.text = nodeData.Value; // .Value 사용 시 따옴표 자동 제거
            }
            else
            {
                textElement.text = ""; // 데이터 없으면 비움
                Debug.LogWarning($"JSON 데이터에 '{fieldName}' 필드가 없습니다.");
            }
        }
        else
        {
            // 인스펙터 연결 안 됐을 때 경고 (필요하다면 주석 해제)
            // Debug.LogError($"{fieldName} TextMeshPro UI 요소가 할당되지 않았습니다!");
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
        // Debug.Log($"GetCardId 호출됨, 현재 cardId: {cardId}"); // 필요 시 주석 해제
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
    public string GetStat() => myCardData?["stat"]?.Value ?? "0";
    public string GetGrade() => myCardData?["grade"]?.Value ?? "Normal";
    public string GetCost() => myCardData?["cost"]?.Value ?? "0";
    public string GetBigClass() => myCardData?["bigClass"]?.Value ?? "unknown";
}