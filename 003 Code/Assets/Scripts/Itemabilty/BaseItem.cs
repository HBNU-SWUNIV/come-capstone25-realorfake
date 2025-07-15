using UnityEngine;

/// <summary>
/// 아이템의 기본 클래스
/// 모든 아이템이 상속받아야 하는 기본 기능을 제공합니다.
/// </summary>
public class BaseItem : MonoBehaviour
{
    protected int _stamina;        // 아이템 사용에 필요한 스태미나
    protected bool _isShooted;     // 아이템 사용 여부
    protected int _uid;           // 아이템 고유 ID
    protected int _stat;          // 아이템 효과 수치
    protected int _expireCount;    // 아이템 효과 지속 턴 수
    protected bool _isInstalled;   // 아이템 설치 여부

    /// <summary>
    /// 아이템 초기화
    /// </summary>
    public virtual void Init(int uid, int stat, int expireCount)
    {
        _isShooted = false;
        _isInstalled = false;
        _uid = uid;
        _stat = stat;
        _expireCount = expireCount;
    }

    /// <summary>
    /// 아이템 설치
    /// </summary>
    public virtual void Install()
    {
        if (!_isInstalled)
        {
            _isInstalled = true;
            InstallPassive();
        }
    }

    /// <summary>
    /// 아이템 효과 만료 여부 확인
    /// </summary>
    public bool IsExpired()
    {
        if (_expireCount != 0)
            return false;
        return true;
    }

    /// <summary>
    /// 아이템 효과 지속 턴 수 증가
    /// </summary>
    public void IncreaseExpireCount()
    {
        if (_expireCount != -1)
            _expireCount++;
    }

    /// <summary>
    /// 아이템 효과 수치 증가
    /// </summary>
    public void IncreaseStats(float value)
    {
        _stat = (int)(_stat * (1 + value));
    }

    /// <summary>
    /// 설치된 아이템의 효과 적용
    /// </summary>
    public virtual void InstallPassive()
    {
        // 기본 구현은 비어있음. 자식 클래스에서 오버라이드하여 구현
    }

    /// <summary>
    /// 아이템 효과 지속 턴 수 감소
    /// </summary>
    public void DecreaseExpireCount()
    {
        if (_expireCount != -1)
            _expireCount--;
    }

    /// <summary>
    /// 현재 남은 턴 수 반환
    /// </summary>
    public int GetExpireCount()
    {
        return _expireCount;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public virtual void Uninstall()
    {
        _isInstalled = false;
    }

    /// <summary>
    /// 설치 여부 확인
    /// </summary>
    public bool IsInstalled()
    {
        return _isInstalled;
    }

    /// <summary>
    /// 아이템 사용 여부 확인
    /// </summary>
    public bool IsShooted()
    {
        return _isShooted;
    }

    /// <summary>
    /// 아이템 사용
    /// </summary>
    public virtual void Use()
    {
        _isShooted = true;
    }

    /// <summary>
    /// 아이템 사용에 필요한 스태미나 반환
    /// </summary>
    public int GetStamina()
    {
        return _stamina;
    }
}
