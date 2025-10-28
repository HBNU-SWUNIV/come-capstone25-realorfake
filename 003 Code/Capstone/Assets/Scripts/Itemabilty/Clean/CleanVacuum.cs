using Unity.Netcode;
using UnityEngine;

public class CleanVacuum : CleanItem
{
    /*
     * 청소기(3) : (공격력, 방어) 모든 버프 효과를 제거하고 방어력 10% 증가
     */

    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 3;
        _interactionType = InteractionType.Consume;  // 소모형으로 설정
    }

    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.1f, 100, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, 0.1f, 100, NetworkManager.Singleton.IsHost);
    }
}
