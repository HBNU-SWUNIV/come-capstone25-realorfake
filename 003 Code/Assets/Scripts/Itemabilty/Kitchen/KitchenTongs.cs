using UnityEngine;
using Unity.Netcode;

public class KitchenTongs : KitchenItem
{
    /*
     
    집게 (2) : (스태미나, 적군) 스태미나 소모 없이 적 스태미나 2 감소, 내 스태미나 2 감소
     
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
        {
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCSTAMINA, 2, 1, !NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCSTAMINA, -2, 1, NetworkManager.Singleton.IsHost);
        }
    }
}
