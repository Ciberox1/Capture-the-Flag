using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance;

    private const int MAX_PLAYERS = 4;
    private const int MIN_PLAYERS = 2;
    private int timer = 5;

    public HashSet<Player> players = new HashSet<Player>();

    public NetworkVariable<State> state = new NetworkVariable<State>(State.Lobby);
    public NetworkVariable<int> playersReady = new NetworkVariable<int>(0);

    public void EnableApprovalCallback()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    public static GameManager Singleton
    {
        get
        {
            if (_instance == null)
            {
                GameManager[] objs = FindObjectsOfType<GameManager>();
                if (objs.Length > 0)
                    _instance = objs[0];
                if (_instance == null)
                {
                    GameManager obj = new GameManager();
                }
            }
            return _instance;
        }
    }

    public void AddPlayer(Player player) 
    {
        players.Add(player);
        if (IsServer) 
        {
            //Comrpobar que se inicia la partida
            if (players.Count >= MIN_PLAYERS) 
            {
                //Empezar la partida
                state.Value = State.Waiting;
                if (players.Count == playersReady.Value)
                {
                    StartCoroutine(EmpezarPartida());
                }
                else 
                {
                    //Hacer que impriman el esperar a jugadores
                    UIManager.Singleton.WaitingForPlayers(playersReady.Value, players.Count);
                }
            }
        }
    }

    private IEnumerator EmpezarPartida() 
    {
        //Hacer cuenta atras y empezar la partida
       // for (int i = 0; i < timer; i++) 
       // {
       //     yield return new WaitForSeconds(1);
       //     //Mostrar por pantalla la espera en segundos
       //     UIManager.Singleton.UpdateTimer(timer);
       // }
       //
       // state.Value = State.Game;

        while(timer > 0) 
        {
            yield return new WaitForSeconds(1);
            if (state.Value != State.Waiting) 
            {
                yield break;
            }

            timer--;
            print(timer);
        }

        //Empezar partida
        state.Value = State.Game;
    }

    //Metodo para comprobar si pueden entrar, no deja si ya hay un maximo de jugadores
    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback) 
    {
        bool approve = players.Count < MAX_PLAYERS;

        GameObject spawnPositions = GameObject.FindWithTag("Respawn");
        int pos = Random.Range(0, spawnPositions.transform.childCount);
        Vector3 spawnPosition = spawnPositions.transform.GetChild(pos).position;

        callback(true, null, approve, spawnPosition, null);
    }

    public enum State 
    {
        Lobby, 
        Waiting,
        Game,
        Finish
    }
}
