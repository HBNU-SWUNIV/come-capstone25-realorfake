using UnityEngine;

/// <summary>
/// 청소 아이템의 패시브 효과를 관리하는 클래스
/// </summary>
public class CleanPassive : MonoBehaviour
{
    /*
     * 청소 아이템 효과
     * - 청소 아이템을 사용하지 않으면 공격력 +20%
     * - 청소 아이템을 사용하면 공격력 -50%
     * 
     * 구현 방식
     * - Clean 아이템 사용 시 UnUseCleanPassive 호출
     */

    /*
     * 효과 적용 방식
     * 1. 청소 아이템을 사용하지 않을 때
     *    - 공격력 +20%
     * 2. 청소 아이템을 사용할 때
     *    - 공격력 -50%
     */

    private static CleanPassive _instance;
    private static float _additionalStat = 0;

    public static CleanPassive GetInstance()
    {
        if (_instance == null)
        {
            GameObject obj = new GameObject("CleanPassive");
            _instance = obj.AddComponent<CleanPassive>();
            DontDestroyOnLoad(obj);
        }
        return _instance;
    }

    /// <summary>
    /// 청소 아이템을 사용하지 않을 때 호출
    /// 공격력을 20% 증가시킵니다.
    /// </summary>
    public void UnUseCleanItem()
    {
        _additionalStat += 0.2f;
    }

    /// <summary>
    /// 청소 아이템을 사용할 때 호출
    /// 공격력을 50% 감소시킵니다.
    /// </summary>
    public void UseCleanItem()
    {
        _additionalStat /= 2;
        // 소수점 제거
        if (_additionalStat < 0.001f)
            _additionalStat = 0;
    }

    /// <summary>
    /// 추가 데미지 계산
    /// </summary>
    /// <returns>기본 데미지에 적용될 배율</returns>
    public float AdditionalDamage()
    {
        return _additionalStat + 1.0f;
    }
}
