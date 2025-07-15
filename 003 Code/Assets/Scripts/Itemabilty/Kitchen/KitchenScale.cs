using Unity.Netcode;
using UnityEngine;

public class KitchenScale : KitchenItem
{
    /*
     
    저울 (3) : (스태미나, 적군) 모든 설치된 아이템들의 남은 횟수를 1 증가
     
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 3;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().IncreaseAllItemsExpireCount(1, NetworkManager.Singleton.IsHost);
    }
}
