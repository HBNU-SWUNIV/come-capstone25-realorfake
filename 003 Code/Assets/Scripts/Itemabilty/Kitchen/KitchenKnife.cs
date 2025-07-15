using Unity.Netcode;
using UnityEngine;

public class KitchenKnife : KitchenItem
{

    /*
     
    칼(2) : (스태미나, 체력) 150% 데미지 공격, 20% 확률로 3턴 출혈(매턴10% 데미지)
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        // 코드 실행
        Shoot();

        // 데미지 계산
        // GameManager 함수를 호출해서 적 hp 감소

        float damage = _stat * 1.5f * KitchenPassive.GetInstance().AdditionalDamage();
        GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 막대기가 떨어진 것을 감지
        // 다시 호출하기에는 문자열이므로

        // 출혈 적용
        if (Random.Range(0, 5) == 0)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.BLEED, _stat * 0.2f, 1, !NetworkManager.Singleton.IsHost);
    }
}
