using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
class Bullet : NetworkBehaviour
{
    public Player playerOwner;
    // Si el servidor detecta una colisión de la bala, si choca con un jugador le quita vida.
    // En cualquier caso la bala desaparece despues de chocar con algo.
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsServer)
        {
            Player target = collision.GetComponent<Player>();
            // si choca con un muro desaparece
            if (target == null)
            {
                GetComponent<NetworkObject>().Despawn();
            }
            // hace daño si choca con un jugador que no sea el que ha disparado y desaparece
            else if (target != playerOwner)
            {
                target.playerHealth.Value--;

                if (target.playerHealth.Value == 0)
                {
                    GameManager.Singleton.AddKill(playerOwner);
                }
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}

