using UnityEngine;

public class WriteInstGlue : WriteInstItem
{
    /*
     
     풀 (1) : (스테미나, 설치) 설치...
     
     */
    protected override void Awake()
    {
        
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 1;
        _expireCount = 1;
        _interactionType = InteractionType.Throw;
    }

    public override void Use()
    {
        base.Use();
    }
}
