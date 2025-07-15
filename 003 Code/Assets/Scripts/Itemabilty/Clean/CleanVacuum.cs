using Unity.Netcode;
using UnityEngine;

public class CleanVacuum : CleanItem
{
    /*
     * 청소기(3) : (공격력, 방어) 모든 버프 효과를 제거하고 방어력 10% 증가
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
        base.Awake();
    }

    public override void Use()
    {
        _stamina = 3;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.1f, 100, NetworkManager.Singleton.IsHost);
    }
}
