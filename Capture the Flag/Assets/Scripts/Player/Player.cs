using System.Collections;
using System.Collections.Generic;
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
    public NetworkVariable<FixedString64Bytes> givenName;

    AnimationHandler animationHandler;
    Animator animator;

    [SerializeField] public Text playerName;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;

        State = new NetworkVariable<PlayerState>();
        playerHealth = new NetworkVariable<int>(6);
        givenName = new NetworkVariable<FixedString64Bytes>("");
        //character = new NetworkVariable<int>(UIManager.Singleton.characterIndex);

        animationHandler = GetComponent<AnimationHandler>();
        animator = GetComponent<Animator>();

        int id = GetComponent<NetworkObject>().GetInstanceID();
        GameManager.Singleton.AddPlayer(id, this);
    }

    private void Start()
    {
        SetSpawnPosition();
    }


    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
        playerHealth.OnValueChanged += OnPlayerHealthValueChanged;
        character.OnValueChanged += OnCharacterValueChanged;
        givenName.OnValueChanged += OnPlayerNameChanged;
    }

    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged -= OnPlayerStateValueChanged;
        playerHealth.OnValueChanged -= OnPlayerHealthValueChanged;
        character.OnValueChanged -= OnCharacterValueChanged;
        givenName.OnValueChanged -= OnPlayerNameChanged;

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
        //GetComponent<Animator>().runtimeAnimatorController = GetComponent<AnimationHandler>().characterAnimation[character.Value];
        SetCharacter(character.Value);
        SetName(playerName.text);

        //int id = GetComponent<NetworkObject>().GetInstanceID();
        //GameManager.Singleton.AddPlayer(id, this);
    }

    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        // activa el spriteRenderer de la diana del jugador local (está desactivada por defecto)
        VisualizeCrossHead();
        SetCharacterServerRpc(UIManager.Singleton.characterIndex);
        SetPlayerNameServerRpc(UIManager.Singleton.inputFieldName.text);
        //SetPlayerNameClientRpc(UIManager.Singleton.inputFieldName.text);
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

    void SetSpawnPosition()
    {
        if (IsLocalPlayer)
            // pide al servidor que busque uno de los puntos de aparición que hay por el mapa y coloque al jugador en él
            SetPlayerSpawnPositionServerRpc();
    }

    void VisualizeCrossHead()
    {
        // El hijo 0 de player es el crosshead
        this.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
    }

    void DieAndRespawn()
    {
        if (IsLocalPlayer) {
            // se envia al jugador a una nueva posicion de respawn y se le cura
            SetPlayerSpawnPositionServerRpc();
            RestorePlayerServerRpc();
        }
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

    [ServerRpc]
    public void SetPlayerSpawnPositionServerRpc()
    {
        GameObject spawnPositions = GameObject.FindWithTag("Respawn");
        int pos = Random.Range(0, spawnPositions.transform.childCount);
        Vector3 spawnPosition = spawnPositions.transform.GetChild(pos).position;
        transform.position = spawnPosition;
    }

    [ServerRpc]
    public void RestorePlayerServerRpc()
    {
        playerHealth.Value = 6;
    }

    [ServerRpc]
    public void SetCharacterServerRpc(int chara)
    {
        character.Value = chara;
        //GetComponent<Animator>().runtimeAnimatorController = GetComponent<AnimationHandler>().characterAnimation[character.Value];
        //SetCharacterClientRpc(chara);
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(string name)
    {
        this.givenName.Value = new FixedString64Bytes(name);
        playerName.text = name;

        //GameManager.Singleton.SetPlayerNames();
        //playerName.text = givenName.Value.ToString();
        //SetPlayerNameClientRpc(givenName.Value.ToString());
    }

    #endregion

    #region ClientRPC
    [ClientRpc]
    public void SetPlayerNameClientRpc(string name)
    {
        GameManager.Singleton.SetPlayerNames();
    }

    #endregion

    #endregion

    #region Netcode Related Methods

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        State.Value = current;
    }

    private void OnPlayerHealthValueChanged(int previousValue, int newValue)
    {
        if (IsLocalPlayer)
        {
            playerHealth.Value = newValue;
            UIManager.Singleton.UpdateLifeUI(newValue);

            if (newValue == 0)
            {
                DieAndRespawn();
            }
        }
    }

    private void OnCharacterValueChanged(int previousValue, int newValue)
    {
        //character.Value = newValue;

        //GetComponent<Animator>().runtimeAnimatorController = GetComponent<AnimationHandler>().characterAnimation[character.Value];
        SetCharacter(newValue);
    }

    private void OnPlayerNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetName(newValue.ToString());
    }

    private void SetCharacter(int character) 
    {
        animator.runtimeAnimatorController = animationHandler.characterAnimation[character];
    }

    private void SetName(string name)
    {
        playerName.text = name;
    }

    #endregion
}



public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
