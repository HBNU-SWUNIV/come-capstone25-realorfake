using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 에어건 아이템
/// 청소 도구 중 하나로, 공기를 이용해 먼지를 제거하는 도구입니다.
/// </summary>
public class CleanAirgun : CleanItem
{
    /*
     * 에어건 효과 (3턴)
     * - 사용 시 스태미나 4 소모
     * - 모든 공격을 100% 방어
     * - 2턴 동안 지속
     */

    /// <summary>
    /// 패시브 효과 설치
    /// 스태미나를 소모하여 방어 버프를 적용합니다.
    /// </summary>
    /// 
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void InstallPassive()
    {
        if (!_isShooted)
        {
            _stamina = 4;

            if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            {
                _expireCount--;
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.GUARD, 1, 100, NetworkManager.Singleton.IsHost);
                _isShooted = true;
            }
        }
    }

    /// <summary>
    /// 아이템 사용
    /// 스태미나를 소모하여 아이템을 설치합니다.
    /// </summary>
    public override void Use()
    {
        _stamina = 3;
        _expireCount = 2;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
        {
            Install();
        }
    }
}
