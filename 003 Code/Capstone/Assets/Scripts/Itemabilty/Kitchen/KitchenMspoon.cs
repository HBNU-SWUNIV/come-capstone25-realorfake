using Unity.Netcode;
using UnityEngine;

public class KitchenMspoon : KitchenItem
{
    /*
     
    계량스푼 (1) : (체력, 적군) 현재 적용된 HEAL 버프의 회복량 증가 +30%
     
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_21_RecoveryField";
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().IncreaseBuffValue(GameManager.BUFTYPE.HEAL, 0.3f, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().IncreaseBuffValueServerRpc(GameManager.BUFTYPE.HEAL, 0.3f, NetworkManager.Singleton.IsHost);

    }
}
