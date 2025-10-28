using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 추가

/// <summary>
/// 개별 버프 아이콘이 자신의 이름, 설명, 지속시간 Text 데이터를 가지도록 하는 컴포넌트입니다.
/// </summary>
public class BuffIconDisplay : MonoBehaviour
{
    [Header("버프 정보")]
    [Tooltip("툴팁에 표시될 버프 이름")]
    public string buffName;

    [TextArea(3, 5)]
    [Tooltip("툴팁에 표시될 버프의 기본 설명")]
    public string buffDescription;

    [Header("UI 연결")]
    [Tooltip("지속시간을 표시하는 자식 Text (TMP) 오브젝트")]
    public TextMeshProUGUI durationText; // 지속시간 Text를 연결할 변수 추가
}