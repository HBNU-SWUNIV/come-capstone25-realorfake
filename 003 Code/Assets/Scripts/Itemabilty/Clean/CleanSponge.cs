using Unity.Netcode;
using UnityEngine;

public class CleanSponge : CleanItem
{
    /*
     * 스펀지(1) : (방어, 회피) 자신의 방어 버프 지속시간을 2% 증가
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
        base.Awake();
    }

    public override void Use()
    {
        _stamina = 1;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.02f, 100, NetworkManager.Singleton.IsHost);
    }
}
