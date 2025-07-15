using Unity.Netcode;
using UnityEngine;

public class WriteInstFountainpen : WriteInstItem
{
    /*
     
     만년필 (5) : (스테미나, 파괴) 모든 아이템 스탯 증가 +25%
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Consume;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 5;

        if (GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
        {
            GameManager.GetInstance().IncreaseAllItemsStats(0.25f, NetworkManager.Singleton.IsHost);
        }
    }
}
