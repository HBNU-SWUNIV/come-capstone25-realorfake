using Unity.Netcode;
using UnityEngine;

public class KitchenMicrowave : KitchenItem
{
    /*
     
    전자레인지 (3) : (스태미나, 적군) 데미지를 주는 모든 아이템들이 2배로 증가
     
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
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.X2, 1, 1, NetworkManager.Singleton.IsHost);
    }
}
