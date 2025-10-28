using UnityEngine;
using Unity.Netcode;

public class KitchenTongs : KitchenItem
{
    /*
     
    집게 (2) : (스태미나, 적군) 스태미나 소모 없이 적 스태미나 2 감소, 내 스태미나 2 감소
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_48_BondageChain";
        _particleScale = 0.5f;
        _particleTarget = ParticleTarget.Enemy;
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
        {
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCSTAMINA, 2, 1, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCSTAMINA, -2, 1, !NetworkManager.Singleton.IsHost);
        }
        else
        {
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCSTAMINA, 2, 1, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCSTAMINA, -2, 1, !NetworkManager.Singleton.IsHost);
        }
    }
}
