using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] public Vector2 velocity;
    [SerializeField] public float ttl;
    
    public void SetDamage(float damage) {
        GetComponent<TMP_Text>().text = Mathf.Round(damage).ToString();
        Destroy(this.gameObject, ttl);
    }

    void FixedUpdate() {
        transform.position += (Vector3)velocity * Time.fixedDeltaTime;
    }
}
