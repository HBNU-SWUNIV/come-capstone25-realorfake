using Unity.Netcode;
using UnityEngine;

public class KitchenCookpot : KitchenItem
{
    /*
     
    냄비 (3) : (스태미나, 설치) 3턴 동안 적의 데미지 30% 감소 + 매턴 20% 데미지
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Install;
        base.Awake();
    }
    public void InstallPassive()
    {
        --_expireCount;
        GameManager.GetInstance().Attack((int)(_stat * 0.2f * KitchenPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        _stamina = 3;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.3f, 3, NetworkManager.Singleton.IsHost);
    }

}
