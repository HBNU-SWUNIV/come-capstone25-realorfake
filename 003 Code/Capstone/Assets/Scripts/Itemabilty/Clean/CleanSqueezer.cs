using Unity.Netcode;
using UnityEngine;

public class CleanSqueezer : CleanItem
{
    /*
     * 스퀴저(2) : (액티브, 투척) 70%의 피해로 공격, 다음 턴 방어력 +30%
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceBlockCrash";
        _particleScale = 0.3f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Throw;  // 투척형으로 설정
    }

    public override void Use()
    {
        base.Use();
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);
        // 데미지 계산
        // GameManager 함수를 호출해서 적 hp 감소

        float damage = _stat * 0.7f * CleanPassive.GetInstance().AdditionalDamage(); 
        
        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)damage, _stamina);
        else
            GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 아니면 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 맞은 적의 체력 감소
        // 다시 호출하기에는 무리

        // 받는 방어력 -30%
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.3f, 1, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, 0.3f, 1, NetworkManager.Singleton.IsHost);
    }
}
