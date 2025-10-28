using Unity.Netcode;
using UnityEngine;

public class KitchenContainer : KitchenItem
{
    /*
     
    컨테이너 (2) : (스태미나, 적군) 50% 보호막 생성, 3턴
     
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_09_HolyShield";
        _particleScale = 1.5f;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Consume;
    }

    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.SHIELD, _stat * 0.5f, 3, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.SHIELD, _stat * 0.5f, 3, NetworkManager.Singleton.IsHost);   

    }
}
