using UnityEngine;

public abstract class LurkerBaseState
{
    public abstract void EnterState(LurkerMonsterScript lurker);
    public abstract void UpdateState(LurkerMonsterScript lurker);
    
}
