using Unity.Netcode;
using UnityEngine;

public class WriteInstBallpen : WriteInstItem
{
    /*
     
    효과 (1) : (스테미나, 데미지) 120% 증가
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 1;

        Shoot();

        GameManager.GetInstance().Attack((int)(_stat * 1.2f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }
}
