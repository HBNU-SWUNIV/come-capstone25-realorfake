using Unity.Netcode;
using UnityEngine;

public class KitchenMbowl : KitchenItem
{
    /*
     
    믹싱볼 (3) : (스태미나, 적군) 모든 적에게 60% 데미지 + 적의 다음 공격 50% 확률 빗나감
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 3;

        // 코드 실행
        Shoot();

        // 데미지 계산
        // GameManager 함수를 호출해서 적 hp 감소

        float damage = _stat * 0.6f * KitchenPassive.GetInstance().AdditionalDamage();
        GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 막대기가 떨어진 것을 감지
        // 다시 호출하기에는 문자열이므로

        // 50% 확률로 공격 빗나감
        if (Random.Range(0, 2) == 0)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.ATTACKMISS, 1, 1, !NetworkManager.Singleton.IsHost);
    }
}
