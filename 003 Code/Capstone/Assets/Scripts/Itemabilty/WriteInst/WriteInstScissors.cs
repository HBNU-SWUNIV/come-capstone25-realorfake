using Unity.Netcode;
using UnityEngine;

public class WriteInstScissors : WriteInstItem
{
    /*
     
    가위 (2) : (스테미나, 데미지) 90% 증가 + 1턴 동안 회복 불가
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_11_LightInFullBloom";
        _particleScale = 0.3f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Throw;
    }

    public override void Use()
    {
        base.Use();
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);

        if (NetworkManager.Singleton.IsHost)
        {
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.UNHEAL, 1, 1, !NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().Attack((int)(_stat * 0.9f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
        }
        else
        {
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.UNHEAL, 1, 1, !NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().AttackServerRpc((int)(_stat * 0.9f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina);
        }
            
    }
}
