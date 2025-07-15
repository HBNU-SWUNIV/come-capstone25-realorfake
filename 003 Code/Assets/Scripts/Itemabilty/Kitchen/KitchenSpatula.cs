using Unity.Netcode;
using UnityEngine;

public class KitchenSpatula : KitchenItem
{
    /*
     
    뒤집개 (1) : (스태미나, 적군) 스태미나 소모 없이 모든 버프/디버프 효과 반전 (예: 공격력 +20% -> -20%)
    
     
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
            GameManager.GetInstance().ReverseBuff(!NetworkManager.Singleton.IsHost);
    }
}
