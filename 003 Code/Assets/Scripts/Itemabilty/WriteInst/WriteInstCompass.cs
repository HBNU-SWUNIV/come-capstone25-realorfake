using Unity.Netcode;
using UnityEngine;

public class WriteInstCompass : WriteInstItem
{
    /*
     
    나침반 (3) : (스테미나, 위치) 방어 무시 60% 증가(데미지 증가) 효과 (3턴 지속)
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Install;
        base.Awake();
    }
    public void InstallPassive()
    {
        --_expireCount;

        GameManager.GetInstance().IgnoreGuardAttack((int)(_stat * 0.6f * WriteInstPassive.GetInstance().AdditionalDamage()), _stamina, NetworkManager.Singleton.IsHost);
    }

    public override void Use()
    {
        _stamina = 3;

        GameManager.GetInstance().CheckCanUseItemAndDecreaseStamina(NetworkManager.Singleton.IsHost, _stamina);
    }
}
