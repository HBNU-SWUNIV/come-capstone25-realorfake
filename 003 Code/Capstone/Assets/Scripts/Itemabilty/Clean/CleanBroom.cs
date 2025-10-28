using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 빗자루 아이템
/// 청소 도구 중 하나로, 먼지를 쓸어내는 도구입니다.
/// </summary>
public class CleanBroom : CleanItem
{
    /*
     * 빗자루 효과 (1턴)
     * - 스태미나 1 소모
     * - 100%의 데미지를 줌
     * 
     * 구현 방식
     * 1. 전체 플레이어 앞에 카드(이미 사용 비활성화), 카드뭉치(덱)를 모두 활성화, 카드 사용 / 카드 거리 이상이면 비활성화
     * 2. 코드 사용: 카드와 카드뭉치를 찾아서 비활성화 / 확정적으로 사용할 수 있음
     * 
     * -> 코드 발생 방식 사용
     */

    /// <summary>
    /// 아이템 사용
    /// 빗자루를 던져 데미지를 줍니다.
    /// </summary>
    /// 
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
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);

        // 데미지 계산

        // 데미지 계산 공식
        float damage = _stat * 1.0f * CleanPassive.GetInstance().AdditionalDamage();

        // 공격
        if (!NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().AttackServerRpc((int)damage, _stamina);
        else
            GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서
    }
}
