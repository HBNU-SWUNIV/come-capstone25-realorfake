using Unity.Netcode;
using UnityEngine;

public class KitchenScale : KitchenItem
{
    /*
     
    저울 (3) : (스태미나, 적군) 모든 설치된 아이템들의 남은 횟수를 1 증가
     
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_21_RecoveryField";
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 3;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        base.Use();
        GameManager.GetInstance().IncreaseAllItemsExpireCount(1, NetworkManager.Singleton.IsHost);

        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().IncreaseAllItemsExpireCountClientRpc(1);
        else
            GameManager.GetInstance().IncreaseAllItemsExpireCountServerRpc(1);
    }
}
