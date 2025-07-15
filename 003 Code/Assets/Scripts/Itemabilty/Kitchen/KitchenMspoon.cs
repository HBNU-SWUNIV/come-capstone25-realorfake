using Unity.Netcode;
using UnityEngine;

public class KitchenMspoon : KitchenItem
{
    /*
     
    계량스푼 (1) : (체력, 적군) 현재 적용된 HEAL 버프의 회복량 증가 +30%
     
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 1;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().IncreaseBuffValue(GameManager.BUFTYPE.HEAL, 0.3f, NetworkManager.Singleton.IsHost);
    }
}
