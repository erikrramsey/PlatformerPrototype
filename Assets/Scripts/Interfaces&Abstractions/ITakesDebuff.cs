using UnityEngine;

public interface ITakesDebuff {
    public virtual void TakeDebuffServerRpc(Debuff debuff, float duration, float value, bool isMult = false) {
        Debug.LogError("Calling base take debuff func, don't do that.");
    }
}