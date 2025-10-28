using Unity.Netcode;
using UnityEngine;

public class KitchenStrainer : KitchenItem
{
    /*
     
    체/거름망 (1) : (체력, 적군) 방어력 50% 증가, 1회
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
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
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.5f, 1, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, 0.5f, 1, NetworkManager.Singleton.IsHost);
    }
}
