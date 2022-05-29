using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    private const int MAX_PLAYERS = 4;
    private const int MIN_PLAYERS = 2;

    private Dictionary<int, Player> playerNames = new Dictionary<int, Player>();

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

    public void AddPlayer(int playerId, Player player)
    {
        playerNames.Add(playerId, player);
    }

    public Dictionary<int, Player> GetPlayers()
    {
        return playerNames;
    }

    public void SetPlayerNames()
    {
        Dictionary<int, Player> currentPlayers = GetPlayers();
        foreach (int playerId in currentPlayers.Keys)
        {
            currentPlayers[playerId].playerName.text = currentPlayers[playerId].givenName.Value.ToString(); 
        }
    }
}
