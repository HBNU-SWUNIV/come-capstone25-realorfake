using Unity.Netcode;
using UnityEngine;

public class CleanTapeCleaner : CleanItem
{
    /*
     * 테이프 클리너(1) : (공격력, 설치) 5%의 확률로 모든 데미지를 막음
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_09_GuardianShield";
        _particleScale = 0.2f;
        _installParticlePath = "Effect/Effect_25_IceField";
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _expireCount = 100;
        _interactionType = InteractionType.Install;  // 설치형으로 설정
    }

    public override void InstallPassive()
    {
        if (Random.Range(0, 20) < 1)
        {
            if (NetworkManager.Singleton.IsHost)
                GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.GUARD, 1, 1, NetworkManager.Singleton.IsHost);
            else
                GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.GUARD, 1, 1, NetworkManager.Singleton.IsHost);
        }
            
    }

    public override void Use()
    {
        base.Use();
    }
}
