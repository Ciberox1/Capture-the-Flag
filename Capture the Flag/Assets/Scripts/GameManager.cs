using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance; // Variable para el singleton

    private const int MAX_PLAYERS = 4;
    private const int MIN_PLAYERS = 2;
    public int timer = 5;

    private Dictionary<int, Player> players = new Dictionary<int, Player>();

    public NetworkVariable<State> state = new NetworkVariable<State>(State.Lobby);
    public NetworkVariable<int> playersReady = new NetworkVariable<int>(0);

    // Usamos esto para acceder a elementos del GameManager en otras clases que los necesiten
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

    public void EnableApprovalCallback()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    } 

    public void AddPlayer(int playerId, Player player)
    {
        //Añade jugador al diccionario de jugadores
        players.Add(playerId, player);
        
        if (IsServer) 
        {
            //Comrpobar que se inicia la partida
            if (players.Keys.Count >= MIN_PLAYERS) 
            {
                //Empezar la partida
                state.Value = State.Waiting;
                
                StartCoroutine(EmpezarPartida());

                //Hacer que impriman el esperar a jugadores
                UIManager.Singleton.WaitingForPlayers(playersReady.Value, players.Keys.Count);
            }
        }
    }

    public Dictionary<int, Player> GetPlayers()
    {
        return players;
    }

    public void SetPlayerNames()
    {
        //Dictionary<int, Player> currentPlayers = GetPlayers();
        foreach (int playerId in players.Keys)
        {
            players[playerId].playerName.text = players[playerId].givenName.Value.ToString();
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
            //Llamar a UImanager y actualizar el texto (timer)
            UIManager.Singleton.UpdateTimer(timer);
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

        //Respawn de los jugadores
        foreach (var player in players.Values) 
        {
            DieAndRespawn(player);
        }
    }

    //Metodo para comprobar si pueden entrar, no deja si ya hay un maximo de jugadores
    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback) 
    {
        bool approve = players.Keys.Count < MAX_PLAYERS;

        Vector3 spawnPosition = SetPlayerSpawnPosition();

        callback(true, null, approve, spawnPosition, null);
    }

    // El servidor busca un punto de spawn y coloca ahí al jugador
    public Vector3 SetPlayerSpawnPosition()
    {
        GameObject spawnPositions = GameObject.FindWithTag("Respawn");
        int pos = Random.Range(0, spawnPositions.transform.childCount);
        Vector3 spawnPosition = spawnPositions.transform.GetChild(pos).position;
        //transform.position = spawnPosition;
        return spawnPosition;
    }

    public void DieAndRespawn(Player player)
    {
        player.playerHealth.Value = 6;
        // se envia al jugador a una nueva posicion de respawn y se le cura
        player.transform.position = SetPlayerSpawnPosition();
    }

    public enum State 
    {
        Lobby, 
        Waiting,
        Game,
        Finish
    }
}
