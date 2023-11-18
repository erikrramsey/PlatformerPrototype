using UnityEngine;

public interface ITakesDebuff {
    public virtual void TakeDebuffServerRpc(Debuff debuff, float duration, float value, bool isMult = false) {
        Debug.LogError("Calling base take debuff func, don't do that. Debuff: " + debuff);
    }

    public virtual void TakeDebuffServerRpc(Debuff debuff, float duration, Vector3 value, bool isMult = false) {
        Debug.LogError("Calling base take debuff func, don't do that. Debuff: " + debuff);
    }
}