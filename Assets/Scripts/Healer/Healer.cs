using UnityEngine;
using Unity.Netcode;

public class Healer : PlayerCharacter {

    [Header("Skill 1 Stuff")]
    [SerializeField] private GameObject Skill1Projectile;
    [SerializeField] private float Skill1Velocity;
    /*
    [Header("Skill 2 Stuff")]

    [Header("Skill 3 Stuff")]
    */


    protected override void Skill1Pressed() {
    }

    protected override void Skill1Held() {
        if (!skill1Ready) return;
        CooldownSkill(1);

        SpawnSkill1ProjectileServerRpc(GetNormalizedAim() * Skill1Velocity / Time.fixedDeltaTime);
    }

    protected override void Skill1Released() {
    }

    [ServerRpc]
    void SpawnSkill1ProjectileServerRpc(Vector2 direction) {
        var p = GameObject.Instantiate(Skill1Projectile, transform.position, Quaternion.identity).GetComponent<HealerSkill1Projectile>();
        p.GetComponent<NetworkObject>().Spawn();
        p.GetComponent<HealerSkill1Projectile>().Setup(teamColor.Value == TeamColor.red ? TeamColor.blue : TeamColor.red, null, transform);
        p.SetInitialForce(direction);
    }


    protected override void Skill2Released() {
    }

    protected override void Skill3Pressed() {
    }

}
