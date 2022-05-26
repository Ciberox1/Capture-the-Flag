using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
class Bullet : NetworkBehaviour
    {

    public ulong playerOwner;
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
            if (target.OwnerClientId != playerOwner)
            {
                target.playerHealth.Value--;
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}

