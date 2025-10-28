using Unity.Netcode;
using UnityEngine;

public class KitchenPtowel : KitchenItem
{
    /*
     
    키친타월 (2) : (스태미나, 적군) 출혈 제거 + 매 턴 체력 10% 회복 (2턴)
     
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
        if (NetworkManager.Singleton.IsHost)
        {
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.HEAL, 0.1f, 2, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().RemoveBuff(GameManager.BUFTYPE.BLEED, NetworkManager.Singleton.IsHost);
        }
        else
        {
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.HEAL, 0.1f, 2, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().RemoveBuffServerRpc(GameManager.BUFTYPE.BLEED, NetworkManager.Singleton.IsHost);
        }
    }
}
