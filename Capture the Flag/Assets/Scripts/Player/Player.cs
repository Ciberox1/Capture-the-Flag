using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    #region Variables

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> State;
    public NetworkVariable<int> playerHealth;

    public NetworkVariable<int> character;

    AnimationHandler animationHandler;
    Animator animator;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;

        State = new NetworkVariable<PlayerState>();
        playerHealth = new NetworkVariable<int>(6);
        character = new NetworkVariable<int>(UIManager.Singleton.characterIndex);

        animationHandler = GetComponent<AnimationHandler>();
        animator = GetComponent<Animator>();

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
        //character.OnValueChanged += OnCharacterValueChanged;
    }

    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged -= OnPlayerStateValueChanged;
        playerHealth.OnValueChanged -= OnPlayerHealthValueChanged;
        //character.OnValueChanged -= OnCharacterValueChanged;
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
        GetComponent<Animator>().runtimeAnimatorController = GetComponent<AnimationHandler>().characterAnimation[character.Value];
    }

    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        // activa el spriteRenderer de la diana del jugador local (está desactivada por defecto)
        VisualizeCrossHead();
        SetCharacterServerRpc();      
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
    public void SetCharacterServerRpc()
    {
        character.Value = UIManager.Singleton.characterIndex;
        GetComponent<Animator>().runtimeAnimatorController = GetComponent<AnimationHandler>().characterAnimation[character.Value];
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
        character.Value = newValue;

        GetComponent<Animator>().runtimeAnimatorController = GetComponent<AnimationHandler>().characterAnimation[character.Value];
    }

    #endregion
}



public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}
