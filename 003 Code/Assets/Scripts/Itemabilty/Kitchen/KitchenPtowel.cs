using Unity.Netcode;
using UnityEngine;

public class KitchenPtowel : KitchenItem
{
    /*
     
    키친타월 (2) : (스태미나, 적군) 출혈 제거 + 매 턴 체력 10% 회복 (2턴)
     
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
        {
            GameManager.GetInstance().RemoveBuff(GameManager.BUFTYPE.BLEED, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.HEAL, 0.1f, 2, NetworkManager.Singleton.IsHost);
        }
    }
}
