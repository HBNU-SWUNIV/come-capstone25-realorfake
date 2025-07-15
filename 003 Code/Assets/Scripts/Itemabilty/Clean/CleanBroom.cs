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
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        // 코드 발생

        // 플레이어 앞에 카드뭉치가 있어야 함
        // 카드 사용 필요

        _stamina = 1;

        if (!GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina))
            return;

        Vector3 lookDir = transform.forward;
        Vector3 shootRotate;
        Vector3 shootDir;

        if (lookDir.z > 0)
        {
            shootRotate = new Vector3(45, 0, 0);
            shootDir = new Vector3(0, 1000, 1000);
        } else
        {
            shootRotate = new Vector3(-45, 0, 0);
            shootDir = new Vector3(0, -1000, 1000);
        }

        // 발생 방향 설정 후 발생
        transform.rotation = Quaternion.Euler(shootRotate);

        Rigidbody rb = this.GetComponent<Rigidbody>();
        rb.AddForce(shootDir);

        // 데미지 계산

        // 데미지 계산 공식
        float damage = _stat * 1.0f * CleanPassive.GetInstance().AdditionalDamage();

        // 공격
        GameManager.GetInstance().Attack((int)damage, _stamina, NetworkManager.Singleton.IsHost);
        // 또는 Update 함수에서 물체의 y 좌표가 0 이하로 내려가면 함수를 호출해서
        // 카드가 떨어진 것을 확인
        // 다시 호출하기에는 카드뭉치에서
    }
}
