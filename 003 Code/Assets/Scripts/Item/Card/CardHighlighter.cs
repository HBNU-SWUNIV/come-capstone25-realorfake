using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CardHighlighter : MonoBehaviour
{
    public XRRayInteractor rayInteractor; // XR Ray Interactor 참조

    private GameObject lastHighlightedCard; // 마지막으로 강조된 카드

    void Update()
    {
        // XR Ray Interactor에서 현재 레이캐스트 결과 가져오기
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            //Debug.Log($"Raycast hit: {hitObject.name}"); // 감지된 오브젝트 이름 출력

            // 부모에서 CardDisplay 컴포넌트 가져오기
            CardDisplay cardDisplay = hitObject.GetComponentInParent<CardDisplay>();
            if (cardDisplay != null)
            {
                string oid = cardDisplay.GetObjectId(); // oid 가져오기
                
            }
            

            if (lastHighlightedCard != hitObject)
            {
                ClearHighlight(); // 이전 강조 제거

                // 새로운 카드 강조
                Outline outline = hitObject.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = true; // 외곽선 활성화
                    lastHighlightedCard = hitObject;
                }
            }
        }
        else
        {
            ClearHighlight(); // 강조 제거
        }
    }

    void ClearHighlight()
    {
        if (lastHighlightedCard != null)
        {
            Outline outline = lastHighlightedCard.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false; // 외곽선 비활성화
            }

            lastHighlightedCard = null;
        }
    }
}
