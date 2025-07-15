using Unity.Netcode;
using UnityEngine;

public class KitchenPan : KitchenItem
{
    /*
     
    프라이팬 (2) : (스태미나, 설치) 적의 공격을 반사, 2회 사용 후 사라짐
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Install;
        base.Awake();
    }
    public void InstallPassive()
    {
        --_expireCount;

        // 반사 버프적용..
        GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.REFLECT, 1, 1, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        _stamina = 2;

        if (!GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            return;

        // 설치 완료
    }
}
