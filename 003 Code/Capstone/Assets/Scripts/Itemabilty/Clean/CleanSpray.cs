using Unity.Netcode;
using UnityEngine;

public class CleanSpray : CleanItem
{
    /*
     * 스프레이(2) : (공격력, 방어) 맞는 대상의 효과를 무작위로 랜덤 효과 (일반 공격 - 적 공격력 10% 감소, 행주로 공격 - 적 방어력 10% 감소)
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
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
        if (Random.Range(0, 2) == 0)
        {
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, -0.1f, 100, !NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCATTACK, -0.1f, 100, !NetworkManager.Singleton.IsHost);
        }
        else
        {
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, -0.1f, 100, !NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, -0.1f, 100, !NetworkManager.Singleton.IsHost);
        }
    }
}
