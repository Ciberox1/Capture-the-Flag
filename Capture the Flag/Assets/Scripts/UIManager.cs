using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : NetworkBehaviour
{

    #region Variables

    private static UIManager _instance; // Variable para el singleton

    [SerializeField] NetworkManager networkManager;
    UnityTransport transport;
    readonly ushort port = 7777;

    [SerializeField] Sprite[] hearts = new Sprite[3];

    private bool hosting = false;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;

    [Header("Lobby")]
    [SerializeField] Sprite[] characters = new Sprite[5];
    [SerializeField] private GameObject lobby;
    [SerializeField] private Button buttonRight;
    [SerializeField] private Button buttonLeft;
    [SerializeField] private Button buttonReady;
    [SerializeField] private Image characterImage;
    [SerializeField] public InputField inputFieldName;
    public int characterIndex = 0;
    
    [Header("Waiting")]
    [SerializeField] private GameObject waiting;
    [SerializeField] private Text waitingText;

    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] RawImage[] heartsUI = new RawImage[3];
    
    #endregion

    // Usamos esto para acceder a elementos del UIManager en otras clases que los necesiten
    public static UIManager Singleton
    {
        get
        {
            if (_instance == null)
            {
                UIManager[] objs = FindObjectsOfType<UIManager>();
                if (objs.Length > 0)
                    _instance = objs[0];
                if (_instance == null)
                {
                    UIManager obj = new UIManager();
                }
            }
            return _instance;
        }
    }

    #region Unity Event Functions

    private void Awake()
    {
        transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
    }

    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        buttonRight.onClick.AddListener(() => ChangeCharacter(0));
        buttonLeft.onClick.AddListener(() => ChangeCharacter(1));
        buttonReady.onClick.AddListener(() => PlayerReady());
        ActivateMainMenu();
    }

    // A fin de actualizar los nombres de todos los jugadores para todos los clientes se llama a SetPlayerNames en OnGUI
    private void OnGUI()
    {
        GameManager.Singleton.SetPlayerNames();
        UpdateTimer(GameManager.Singleton.timer); 
    }

    #endregion

    #region UI Related Methods

    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
    }

    private void ActivateLobby()
    {
        characterImage.sprite = characters[characterIndex];
        mainMenu.SetActive(false);
        lobby.SetActive(true);
    }

    private void ActivateWaiting() 
    {
        lobby.SetActive(false);
        mainMenu.SetActive(false);
        waiting.SetActive(true);
        inGameHUD.SetActive(true);
    }

    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        lobby.SetActive(false);
        waiting.SetActive(false);
        inGameHUD.SetActive(true);
    }

    public void UpdateLifeUI(int hitpoints)
    {
        // Hemos invertido el orden de los casos porque pasamos directamente la vida restante del jugador
        switch (hitpoints)
        {
            case 0:
                heartsUI[0].texture = hearts[2].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 1:
                heartsUI[0].texture = hearts[1].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 2:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 3:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[1].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 4:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 5:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[1].texture;
                break;
            case 6:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[0].texture;
                break;
        }
    }

    // Método llamado por los botones de cambiar personaje (< y >). En función de cual se pulse mueve el
    // índice en una dirección u otra y actualiza el personaje que se muestra en el menú
    private void ChangeCharacter(int v)
    {
        if (v == 0)
        {
            if (characterIndex == characters.Length - 1) {
                characterImage.sprite = characters[0];
                characterIndex = 0;
            }
            else
            {
                characterIndex++;
                characterImage.sprite = characters[characterIndex];               
            }
        }
        else
        {
            if (characterIndex == 0)
            {
                characterImage.sprite = characters[characters.Length - 1];
                characterIndex = characters.Length - 1;
            }
            else
            {
                characterIndex--;
                characterImage.sprite = characters[characterIndex];
            }
        }
    }

    // Cuando el jugador pulse el botón de preparado se unirá a la partida
    private void PlayerReady()
    {
        ActivateWaiting();

        // Si el jugador no ha dado un nombre, se le asignará uno.
        if (inputFieldName.text == "")
        {
            inputFieldName.text = "Player_" + Random.Range(1, 1000).ToString("000");
        }

        if (hosting)
        {
            GameManager.Singleton.EnableApprovalCallback();
            NetworkManager.Singleton.StartHost();
            //ActivateInGameHUD();
        }
        else 
        {
            NetworkManager.Singleton.StartClient();
            //ActivateInGameHUD();
        }
    }

    public void WaitingForPlayers(int playerReady, int playeTotal) 
    {
        WaitingForPlayersClientRpc(playerReady, playeTotal);
    } 

    public void UpdateTimer(int timer) 
    {
        //UpdateTimerClientRpc(timer);
       // if (GameManager.Singleton.state.Value == GameManager.State.Waiting) 
       // {
       //     waitingText.text = "La partida empieza en " + timer;
       // }
       //
       // if (GameManager.Singleton.state.Value == GameManager.State.Game)
       // {
       //     //ActivateInGameHUD();
       //     UpdateTimerServerRpc(timer);
       // }
    }

    [ClientRpc]
    private void WaitingForPlayersClientRpc(int playerReady, int playeTotal)
    {
        waitingText.text = "Hay " + playerReady + "/" + playeTotal + " jugadores listos";
    }

    [ServerRpc]
    private void UpdateTimerServerRpc(int timer)
    {
        
        //UpdateTimerClientRpc(timer);

       //waitingText.text = "La partida empieza en " + timer;
       //if (timer == 0)
       //{
       //    ActivateInGameHUD();
       //}
    }

    [ClientRpc]
    public void UpdateTimerClientRpc() 
    {
        //waitingText.text = "La partida empieza en " + timer;
        //if (timer == 0)
        //{
        //    ActivateInGameHUD();
        //}
        if (GameManager.Singleton.state.Value == GameManager.State.Waiting)
        {
            //waitingText.text = "La partida empieza en " + timer;

            waitingText.text = "La partida comenzar en breve";
        }

        if (GameManager.Singleton.state.Value == GameManager.State.Game)
        {
            ActivateInGameHUD();
        }
    }

    [ClientRpc]
    public void RestartWaitClientRpc()
    {
        ActivateWaiting();
        waitingText.text = "Esperando jugadores...";
    }

    [ClientRpc]
    public void ActivateGameHUDClientRpc()
    {
        ActivateInGameHUD();
    }

    #endregion

    #region Netcode Related Methods

    private void StartHost()
    {
        if (!SetIPAndPort()) { return; }
        hosting = true;
        ActivateLobby();
    }

    private void StartClient()
    {       
        if (!SetIPAndPort()) { return; }
        hosting = false;
        ActivateLobby();
    }

    private void StartServer()
    {
        if (!SetIPAndPort()) { return; }
        GameManager.Singleton.EnableApprovalCallback();
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
    }

    private bool SetIPAndPort()
    {
        bool success = false;
        var ip = inputFieldIP.text;

        if (!string.IsNullOrEmpty(ip))
        {
            transport.SetConnectionData(ip, port);
            success = true;
        }

        return success;
    }

    #endregion

    
}
