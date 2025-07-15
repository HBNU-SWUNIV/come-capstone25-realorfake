using Unity.Netcode;
using UnityEngine;

public class KitchenToaster : KitchenItem
{
    /*
     
     토스터기 (2) : (스태미나, 설치) 매 턴 40% 데미지 (2회 사용 가능), 3턴 후 사라짐
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Install;
        base.Awake();
    }
    public void InstallPassive()
    {
        --_expireCount;
        GameManager.GetInstance().Attack((int)(_stat * 0.4f * KitchenPassive.GetInstance().AdditionalDamage()), 0, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        _stamina = 2;

        GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina);
    }
}
