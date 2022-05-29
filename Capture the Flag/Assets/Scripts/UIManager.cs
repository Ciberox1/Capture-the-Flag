using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : MonoBehaviour
{

    #region Variables

    private static UIManager _instance;

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
    public int characterIndex = 0;
    [SerializeField] public InputField inputFieldName;

    [Header("Waiting")]
    [SerializeField] private GameObject waiting;
    [SerializeField] private Text waitingText;

    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] RawImage[] heartsUI = new RawImage[3];
    
    #endregion

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

    private void OnGUI()
    {
        GameManager.Singleton.SetPlayerNames();
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

    private void PlayerReady()
    {
        lobby.SetActive(false);
        inGameHUD.SetActive(true);

        if (inputFieldName.text == "")
        {
            inputFieldName.text = "Player_" + Random.Range(1, 1000).ToString("000");
        }

        if (hosting)
        {
            GameManager.Singleton.EnableApprovalCallback();
            NetworkManager.Singleton.StartHost();
            ActivateInGameHUD();
        }
        else 
        {
            NetworkManager.Singleton.StartClient();
            ActivateInGameHUD();
        }
    }

    public void WaitingForPlayers(int playerReady, int playeTotal) 
    {
        WaitingForPlayersServerRpc(playerReady, playeTotal);
    } 

    public void UpdateTimer(int timer) 
    {
        UpdateTimerServerRpc(timer);
    }

    [ServerRpc]
    private void WaitingForPlayersServerRpc(int playerReady, int playeTotal)
    {
        waitingText.text = "Hay " + playerReady + "/" + playeTotal + " jugadores listos";
    }

    [ServerRpc]
    private void UpdateTimerServerRpc(int timer)
    {
        waitingText.text = "La partida empieza en " + timer;
    }

    #endregion

    #region Netcode Related Methods

    private void StartHost()
    {
        hosting = true;
        //NetworkManager.Singleton.StartHost();
        ActivateLobby();
    }

    private void StartClient()
    {
        var ip = inputFieldIP.text;
        hosting = false;

        if (!string.IsNullOrEmpty(ip))
        {
            transport.SetConnectionData(ip, port);
        }
        //NetworkManager.Singleton.StartClient();

        ActivateLobby();
    }

    private void StartServer()
    {
        GameManager.Singleton.EnableApprovalCallback();
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
    }

    #endregion

    
}
