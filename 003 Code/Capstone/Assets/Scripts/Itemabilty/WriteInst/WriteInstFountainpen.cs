using Unity.Netcode;
using UnityEngine;

public class WriteInstFountainpen : WriteInstItem
{
    /*
     
     만년필 (5) : (스테미나, 파괴) 모든 아이템 스탯 증가 +25%
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 5;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        base.Use();
        GameManager.GetInstance().IncreaseAllItemsStats(0.25f, NetworkManager.Singleton.IsHost);

        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().IncreaseAllItemsStatsClientRpc(0.25f);
        else
            GameManager.GetInstance().IncreaseAllItemsStatsServerRpc(0.25f);
    }
}
