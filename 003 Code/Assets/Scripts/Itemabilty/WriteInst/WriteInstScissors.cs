using Unity.Netcode;
using UnityEngine;

public class WriteInstScissors : WriteInstItem
{
    /*
     
    가위 (2) : (스테미나, 데미지) 90% 증가 + 1턴 동안 회복 불가
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        if (GameManager.GetInstance().Attack((int)(_stat * 0.9f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost))
        {
            Shoot();
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.UNHEAL, 1, 1, NetworkManager.Singleton.IsHost);
        }
    }
}
