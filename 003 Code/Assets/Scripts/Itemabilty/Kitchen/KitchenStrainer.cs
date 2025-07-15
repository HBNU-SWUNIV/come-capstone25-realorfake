using Unity.Netcode;
using UnityEngine;

public class KitchenStrainer : KitchenItem
{
    /*
     
    체/거름망 (1) : (체력, 적군) 방어력 50% 증가, 1회
     
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
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.5f, 1, NetworkManager.Singleton.IsHost);
    }
}
