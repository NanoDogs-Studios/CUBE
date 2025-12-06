using Photon.Pun;
using UnityEngine;

public class BasePlayerStats : MonoBehaviourPunCallbacks
{
    // health: duh
    public int health = 100;
    // stamina: used for sprinting, dodging, etc.
    public int stamina = 100;
    // speed: affects movement speed
    public int speed = 5;
    // malice: your chance of being killer, higher is more chance, lower is less chance
    public int malice = 1;
    // cc: cube coins: money! duh!
    public int money = 100;

    [PunRPC]
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health < 0) health = 0;
    }
    [PunRPC]
    public void Heal(int amount)
    {
        health += amount;
        if (health > 100) health = 100;
    }
    [PunRPC]
    public void UseStamina(int amount)
    {
        stamina -= amount;
        if (stamina < 0) stamina = 0;
    }
    [PunRPC]
    public void RecoverStamina(int amount)
    {
        stamina += amount;
        if (stamina > 100) stamina = 100;
    }
    [PunRPC]
    public void GrantMalice(int amount)
    {
        malice += amount;
    }
    [PunRPC]
    public void RemoveMalice(int amount)
    {
        malice -= amount;
    }
    [PunRPC]
    public void GrantMoney(int amount)
    {
        money += amount;
    }
    [PunRPC]
    public void RemoveMoney(int amount)
    {
        money -= amount;
    }

    [PunRPC]
    public void AdjustMalice(int amount)
    {
        malice += amount;
        if (malice < 0) malice = 0;
        Debug.Log($"{photonView.Owner.NickName} malice now {malice}");
    }
}
