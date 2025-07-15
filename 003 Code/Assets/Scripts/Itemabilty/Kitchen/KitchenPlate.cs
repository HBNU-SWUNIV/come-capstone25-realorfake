using Unity.Netcode;
using UnityEngine;

public class KitchenPlate : KitchenItem
{
    /*
     
    접시 (2) : (스태미나, 설치) 데미지를 막아주는 보호막 생성 (최대 100% 데미지까지)
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Install;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().SetShield((int)(_stat * KitchenPassive.GetInstance().AdditionalDamage()), NetworkManager.Singleton.IsHost);
    }
}
