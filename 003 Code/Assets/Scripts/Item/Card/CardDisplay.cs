using UnityEngine;

using UnityEngine.UI;
using SimpleJSON;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public TextMeshPro smallClassText;

    private JSONNode myCardData; // 카드별 데이터 저장
    private string cardId;       // 카드 고유 ID (uid)
    private string objectId;     // 소환할 오브젝트 ID (oid)

    public void SetCardData(JSONNode cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("카드 데이터가 null입니다!");
            return;
        }

        myCardData = cardData;

        // smallClass 정보 설정
        if (cardData["smallClass"] != null)
        {
            string rawText = cardData["smallClass"].ToString();
            string cleanedText = rawText.Trim('"');
            if (smallClassText != null)
                smallClassText.text = cleanedText;
            else
                Debug.LogError("smallClassText UI 요소가 할당되지 않았습니다!");
        }
        else
        {
            Debug.LogWarning("JSON 데이터에 'smallClass' 필드가 없습니다.");
        }

        // 카드 ID 저장 (uid 필드 사용)
        if (cardData["uid"] != null)
        {
            cardId = cardData["uid"].ToString().Trim('"');
            Debug.Log($"카드 ID 설정: {cardId}");
        }
        else
        {
            Debug.LogWarning("JSON 데이터에 'uid' 필드가 없습니다.");
            cardId = "unknown_uid";
        }

        // 오브젝트 ID 저장 (oid 필드 사용)
        if (cardData["oid"] != null)
        {
            objectId = cardData["oid"].ToString().Trim('"');
            Debug.Log($"오브젝트 ID 설정: {objectId}");
        }
        else
        {
            Debug.LogWarning("JSON 데이터에 'oid' 필드가 없습니다.");
            objectId = "unknown_oid";
        }

        Debug.Log("카드 정보가 성공적으로 적용되었습니다!");
    }

    public void SetJsonData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData)) return;

        try
        {
            JSONNode cardData = JSON.Parse(jsonData);
            SetCardData(cardData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 데이터 파싱 중 오류 발생: {e.Message}");
        }
    }

    public string GetJsonData()
    {
        return myCardData != null ? myCardData.ToString() : null;
    }

    // 카드 ID 반환
    public string GetCardId()
    {
        Debug.Log($"GetCardId 호출됨, 현재 cardId: {cardId}");
        return cardId;
    }

    // 오브젝트 ID 반환
    public string GetObjectId()
    {
        return objectId;
    }

    // 필요하다면 개별 필드 접근
    public string GetOid() => myCardData?["oid"];
    public string GetSmallClass() => myCardData?["smallClass"];
}
