using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    #region Variables

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> State;
    public NetworkVariable<int> playerHealth;
    public NetworkVariable<int> character;
    public NetworkVariable<FixedString64Bytes> givenName; // nombre del jugador dado a UIManager
    public NetworkVariable<int> kills;

    AnimationHandler animationHandler;
    Animator animator;

    [SerializeField] public Text playerName; // objeto que muestra el nombre del jugador en la partida

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;

        State = new NetworkVariable<PlayerState>();
        playerHealth = new NetworkVariable<int>(6);
        givenName = new NetworkVariable<FixedString64Bytes>("");
        kills = new NetworkVariable<int>(0);

        animationHandler = GetComponent<AnimationHandler>();
        animator = GetComponent<Animator>();

        int id = GetComponent<NetworkObject>().GetInstanceID();
        GameManager.Singleton.AddPlayer(id, this);
    }

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
        playerHealth.OnValueChanged += OnPlayerHealthValueChanged;
        character.OnValueChanged += OnCharacterValueChanged;
    }

    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged -= OnPlayerStateValueChanged;
        playerHealth.OnValueChanged -= OnPlayerHealthValueChanged;
        character.OnValueChanged -= OnCharacterValueChanged;
    }

    private void OnDestroy()
    {
        GameManager.Singleton.DeletePlayer(GetComponent<NetworkObject>().GetInstanceID());
        GameManager.Singleton.CancelGame();
    }
    #endregion

    #region Config Methods

    void ConfigurePlayer(ulong clientID)
    {
        if (IsLocalPlayer)
        {
            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();  
        }
        // Se pone fuera de la comprobaci?n a fin de que la asignaci?n ocurra en las copias de otros clientes
        SetCharacter(character.Value);
    }

    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);       
        VisualizeCrossHead(); // activa el spriteRenderer de la diana del jugador local (est? desactivada por defecto)
        SetCharacterServerRpc(UIManager.Singleton.characterIndex); // Coge la id del personaje elegido desde UIManager
        SetPlayerNameServerRpc(UIManager.Singleton.inputFieldName.text); // Coge el nombre del jugador desde UIManager
    }

    void ConfigureCamera()
    {
        // https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBrainProperties.html
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }

    void ConfigureControls()
    {
        GetComponent<InputHandler>().enabled = true;
    }

    void VisualizeCrossHead()
    {
        // El hijo 0 de player es el crosshead
        this.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {       
        State.Value = state;
    }

    // Poner la vida al m?ximo cuando el jugador reaparece
    [ServerRpc]
    public void DieAndRespawnServerRpc()
    {
        GameManager.Singleton.DieAndRespawn(this);
    }

    [ServerRpc]
    public void SetCharacterServerRpc(int chara)
    {
        character.Value = chara;
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(string name)
    {
        this.givenName.Value = new FixedString64Bytes(name);
        playerName.text = name;
    }

    #endregion

    #endregion

    #region Netcode Related Methods

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        State.Value = current;
    }

    // Actualizar la vida y morir si es necesario
    private void OnPlayerHealthValueChanged(int previousValue, int newValue)
    {
        if (IsLocalPlayer)
        {
            playerHealth.Value = newValue;
            UIManager.Singleton.UpdateLifeUI(newValue);

            if (newValue == 0)
            {
                DieAndRespawnServerRpc();
            }
        }
    }

    private void OnCharacterValueChanged(int previousValue, int newValue)
    {
        SetCharacter(newValue);
    }

    // asigna la animaci?n correspondiente
    private void SetCharacter(int character) 
    {
        if (animator != null)
            animator.runtimeAnimatorController = animationHandler.characterAnimation[character];
    }

    #endregion
}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
