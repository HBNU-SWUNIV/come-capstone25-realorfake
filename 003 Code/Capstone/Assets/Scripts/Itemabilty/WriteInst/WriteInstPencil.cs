using Unity.Netcode;
using UnityEngine;

public class WriteInstPencil : WriteInstItem
{
    /*
     
    연필 (1) : (스테미나, 데미지) 100% 증가, 3회 사용시 스탯 절반 감소
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_26_Napalm";
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _interactionType = InteractionType.Throw;
    }
    public override void Use()
    {
        base.Use();
        if (_useCount >= 3)
        {
            _useCount = 0;
            _stat = (int)(_stat * 0.5f);
        }

        _useCount++;

        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);

        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)(_stat * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina);
        else
            GameManager.GetInstance().Attack((int)(_stat * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }
}
