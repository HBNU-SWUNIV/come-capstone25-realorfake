using Unity.Netcode;
using UnityEngine;

public class KitchenCoffeepot : KitchenItem
{

    /*
     
    커피포트 (3) : (스태미나, 설치) 매 턴 50% 데미지 (3턴 지속), 2턴 후 사라짐
     
     */

    protected override void Awake()
    {
        _interactionType = InteractionType.Install;
        base.Awake();
    }
    public void InstallPassive()
    {
        --_expireCount;
        GameManager.GetInstance().Attack((int)(_stat * 0.5f * KitchenPassive.GetInstance().AdditionalDamage()), 0, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        _stamina = 3;

        GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina);
    }
}
