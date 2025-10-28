using Unity.Netcode;
using UnityEngine;

public class CleanDuster : CleanItem
{

    /*
     
    먼지털이개(2) : (공격력, 방어) 70%의 데미지를 주고, 맞은 쪽의 데미지 +30% / 내 방어력 -30%
     
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
        _interactionType = InteractionType.Throw;  // 소모형으로 설정
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

        float damage = _stat * 0.7f * CleanPassive.GetInstance().AdditionalDamage();

        // 공격
        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)damage, _stamina);
        else
            GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서


        // 맞은 쪽 데미지 + 30%, 내 방어력 -30%
        if (NetworkManager.Singleton.IsHost)
        {
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCATTACK, 0.3f, 1, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, -0.3f, 1, NetworkManager.Singleton.IsHost);
        }
        else
        {
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCATTACK, 0.3f, 1, NetworkManager.Singleton.IsHost);
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, -0.3f, 1, NetworkManager.Singleton.IsHost);
        }
    }
}
