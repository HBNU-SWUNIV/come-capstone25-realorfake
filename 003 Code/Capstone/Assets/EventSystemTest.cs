// EventSystemTest.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemTest : MonoBehaviour, IPointerEnterHandler
{
    // Ray가 이 오브젝트의 Collider에 들어오면 즉시 호출됩니다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 이 로그가 찍히면, 이벤트 시스템의 기본 설정은 모두 정상인 것입니다.
        Debug.LogWarning(">>> 이벤트 시스템 작동 확인! Ray가 " + gameObject.name + "에 닿았습니다. <<<");
    }
}