using UnityEngine;
using UnityEngine.UI;

public class LocalPlayer : MonoBehaviour
{
    protected float CameraRotSpeedX = 100.0f;
    protected float CameraRotSpeedY = 100.0f;

    private Player owningPlayer = null;
    private Camera playerCamera = null;

    [SerializeField]
    private UI_Bar staminaBar = null;

    [SerializeField]
    private UI_GameState gameState = null;

    [SerializeField]
    private Slider sensitivitySlider = null;

    private ServerManager manager = null;

    private bool hasPlayer = false;

    private bool hasGameStateUI = false;
    private bool hasStaminaSliderUI = false;
    
    protected bool mouseLocked = false;

    private void Start()
    {
        this.playerCamera = GetComponentInChildren<Camera>();
        this.hasStaminaSliderUI = staminaBar != null;
        this.hasGameStateUI = gameState != null;
    }

    private void OnDestroy()
    {
        if (this.owningPlayer)
        {
            this.owningPlayer.UnbindOnDestroy(OnPlayerDestroyed);
            this.owningPlayer.UnbindOnUpdate(OnPlayerUpdated);
            this.owningPlayer.UnbindOnChangeLivingState(OnPlayerLivingStateChanged);
        }

        if(this.sensitivitySlider)
        {
            this.sensitivitySlider.onValueChanged.RemoveAllListeners();
        }
    }

    private void Update()
    {
        if (!this.hasPlayer)
            return;
        

        this.OnUpdateClientCamera();
        this.OnUpdateClientControls();
    }

    private void FixedUpdate()
    {
        if (!this.hasPlayer && !manager)
            return;

        OnUpdatePlayersUI();
    }

    protected virtual void OnInitialized()
    {

    }

    protected virtual void OnDestroyed()
    {

    }

    private void OnPlayerDestroyed(Player player, bool ByScene)
    {
        SetShowUi(false);

        this.owningPlayer = null;
        this.hasPlayer = false;

        Cursor.lockState = CursorLockMode.None;

        mouseLocked = false;

        if (!ByScene)
        {

        }
    }

    private void OnPlayerUpdated(Player player)
    {
        if (!this.hasPlayer) 
            return;

        if (this.hasStaminaSliderUI)
        {
            float perc = player.GetStamina() / player.GetMaxStamina();

            this.staminaBar.SetPercentage(perc);
        }

        OnServerCalledPlayerUpdate(player);
    }

    private void OnPlayerLivingStateChanged(Player player, bool alive)
    {
        if(gameState)
        {
            if(!alive)
            {
                CreateGameState("You Died, Lmao!", 1.0f, 0.5f, 1.0f, true);
            }
        }
    }

    private void CreateGameState(string text, float showTime, float holdTime, float hideTime, bool force = false)
    {
        if(this.hasGameStateUI)
        {
            if(force)
                this.gameState.ForceEvent(text, showTime, holdTime, hideTime);
            else
                this.gameState.ExecuteEvent(text, showTime, holdTime, hideTime);
        }
    }


    public void BindNetworkPlayer(Player player)
    {
        this.owningPlayer = player;
        this.hasPlayer = true;

        owningPlayer.isLocalPlayer = true;

        owningPlayer.BindOnDestroy(OnPlayerDestroyed);
        owningPlayer.BindOnUpdate(OnPlayerUpdated);
        owningPlayer.BindOnChangeLivingState(OnPlayerLivingStateChanged);

        Cursor.lockState = CursorLockMode.Locked;

        mouseLocked = true;
    }

    public void OnInitializedLocalPlayer()
    {
        if (this.owningPlayer.serverManager)
        {
            this.manager = owningPlayer.serverManager.GetComponent<ServerManager>();

            this.manager.GetServerPlayerDataRpc();

            this.sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        OnInitialized();
    }

    private void OnSensitivityChanged(float value)
    {
        this.CameraRotSpeedX = value;
        this.CameraRotSpeedY = value;
    }

    protected void SetShowUi(bool value)
    {
        if (this.staminaBar != null)
        {
            this.hasStaminaSliderUI = value;
            this.staminaBar.enabled = value;
        }
        else
            this.hasStaminaSliderUI = false;

        if(this.gameState != null)
        {
            this.hasGameStateUI = value;
            this.gameState.enabled = value;
        }
        else
            this.hasGameStateUI = false;
    }

    //Called on Client & Server, but called by Server when the Player has been Updated
    virtual protected void OnServerCalledPlayerUpdate(Player player)
    {

    }

    //for Updating the UI of other Players
    virtual protected void OnUpdatePlayersUI()
    {
        var playerList = manager.GetPlayerList();

        for (int i = 0; i < playerList.Count; i++)
        {
            if (!playerList[i].isLocalPlayer)
                playerList[i].UpdatePlayerNameLabel(playerCamera);
        }
    }

    // Called only when localplayer is bound to Player
    virtual protected void OnUpdateClientControls() 
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            this.mouseLocked = !this.mouseLocked;

            if (this.mouseLocked)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;
        }

        Player.PlayerMovement movement = Player.PlayerMovement.None;

        if (Input.GetKey(KeyCode.LeftShift))
            movement |= Player.PlayerMovement.FastForward;

        if (Input.GetKey(KeyCode.W))
            movement |= Player.PlayerMovement.Forward;


        if (Input.GetKey(KeyCode.S))
            movement |= Player.PlayerMovement.Backward;

        if (Input.GetKey(KeyCode.A))
            movement |= Player.PlayerMovement.Left;

        if (Input.GetKey(KeyCode.D))
            movement |= Player.PlayerMovement.Right;

        if (Input.GetKey(KeyCode.Space) && this.owningPlayer.IsGrounded())
            movement |= Player.PlayerMovement.Up;

        this.owningPlayer.OnClientMoveRpc(movement);

        return;
    }

    virtual protected void OnUpdateClientCamera()
    {
        if (this.mouseLocked == false)
            return;

        float translation = Input.GetAxis("Mouse X") * this.CameraRotSpeedX * Time.deltaTime;
        float rotation = -Input.GetAxis("Mouse Y") * this.CameraRotSpeedY * Time.deltaTime;

        if (rotation != 0.0f)
        {
            this.playerCamera.transform.Rotate(rotation, 0.0f, 0.0f);
        }

        if (translation != 0.0f)
        {
            Vector3 addEuler = new Vector3(0.0f, translation, 0.0f);

            this.owningPlayer.OnClientRotateRpc(addEuler);
        }
    }

    protected T GetOwningPlayer<T>() where T : Player
    {
        return (T)this.owningPlayer;
    }

    protected T GetServerManager<T>() where T : ServerManager
    {
        return (T)this.manager;
    }
    protected UI_GameState GetGameState()
    {
        return this.gameState;
    }

    protected Camera GetPlayerCamera()
    {
        return this.playerCamera;
    }

    protected bool HasPlayer()
    {
        return this.hasPlayer;
    }
}
