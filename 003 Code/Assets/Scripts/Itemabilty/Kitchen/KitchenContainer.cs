using Unity.Netcode;
using UnityEngine;

public class KitchenContainer : KitchenItem
{
    /*
     
    컨테이너 (2) : (스태미나, 적군) 50% 보호막 생성, 3턴
     
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.SHIELD, _stat * 0.5f, 3, NetworkManager.Singleton.IsHost);
    }
}
