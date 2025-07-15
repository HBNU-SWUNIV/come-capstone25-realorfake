using Unity.Netcode;
using UnityEngine;

public class WriteInstRuler : WriteInstItem
{
    /*
     
    자 (2) : (스테미나, 데미지) 80% 증가 및 2회 공격
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 2;

        if (GameManager.GetInstance().Attack((int)(_stat * 0.8f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost))
            GameManager.GetInstance().Attack((int)(_stat * 0.8f * WriteInstPassive.GetInstance().AdditionalDamage()), 0, NetworkManager.Singleton.IsHost);
    }
}
