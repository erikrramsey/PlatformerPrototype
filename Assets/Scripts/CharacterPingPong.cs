using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPingPong : PlayerCharacter {
    public override void teamColor_OnValueChanged(TeamColor previous, TeamColor current) {
        base.teamColor_OnValueChanged(previous, current);

        if (current == TeamColor.red) {
            transform.Find("Parts/Racket").gameObject.layer = LayerMask.NameToLayer("RedHitbox");
        } else if (current == TeamColor.blue) {
            transform.Find("Parts/Racket").gameObject.layer = LayerMask.NameToLayer("BlueHitbox");
        }
    }
}
