using Unity.Netcode;
using UnityEngine;

public class WriteInstPencil : WriteInstItem
{
    /*
     
    연필 (1) : (스테미나, 데미지) 100% 증가, 3회 사용시 스탯 절반 감소
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        _stamina = 1;

        if (_useCount >= 3)
        {
            _useCount = 0;
            _stat = (int)(_stat * 0.5f);
        }

        _useCount++;

        Shoot();

        GameManager.GetInstance().Attack((int)(_stat * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }
}
