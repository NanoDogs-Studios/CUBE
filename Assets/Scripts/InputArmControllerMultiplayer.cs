using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputArmControllerMP : MonoBehaviour
{
    // Reference to the root PhotonView (assign in Inspector or find at runtime)
    public PhotonView rootPhotonView;

    public InputAction leftClickAction;
    public InputAction rightClickAction;

    public void Awake()
    {
        if (rootPhotonView == null)
        {
            // Find the root PhotonView (e.g., on parent)
            rootPhotonView = GetComponentInParent<PhotonView>();
            if (rootPhotonView == null)
            {
                Debug.LogError("Root PhotonView not found!");
                enabled = false;
                return;
            }
        }
    }

    public void Start()
    {
        InitializeInputActions();
    }

    public void InitializeInputActions()
    {
        leftClickAction = new InputAction("LeftClick", InputActionType.Button, "<Mouse>/leftButton");
        leftClickAction.started += OnLeftClickStarted;
        leftClickAction.canceled += OnLeftClickCanceled;
        leftClickAction.Enable();

        rightClickAction = new InputAction("RightClick", InputActionType.Button, "<Mouse>/rightButton");
        rightClickAction.started += OnRightClickStarted;
        rightClickAction.canceled += OnRightClickCanceled;
        rightClickAction.Enable();
    }

    public void OnLeftClickStarted(InputAction.CallbackContext context)
    {
        if (!rootPhotonView.IsMine) return;
        rootPhotonView.RPC(nameof(PlayerRPCHandler.RPC_ArmHandle), RpcTarget.All, (int)Hands.Left, false);
    }

    public void OnLeftClickCanceled(InputAction.CallbackContext context)
    {
        if (!rootPhotonView.IsMine) return;
        rootPhotonView.RPC(nameof(PlayerRPCHandler.RPC_ArmHandle), RpcTarget.All, (int)Hands.Left, true);
    }

    public void OnRightClickStarted(InputAction.CallbackContext context)
    {
        if (!rootPhotonView.IsMine) return;
        rootPhotonView.RPC(nameof(PlayerRPCHandler.RPC_ArmHandle), RpcTarget.All, (int)Hands.Right, false);
    }

    public void OnRightClickCanceled(InputAction.CallbackContext context)
    {
        if (!rootPhotonView.IsMine) return;
        rootPhotonView.RPC(nameof(PlayerRPCHandler.RPC_ArmHandle), RpcTarget.All, (int)Hands.Right, true);
    }

    public void OnEnable()
    {
        leftClickAction?.Enable();
        rightClickAction?.Enable();
    }

    public void OnDisable()
    {
        leftClickAction?.Disable();
        rightClickAction?.Disable();
    }

    public void OnDestroy()
    {
        if (leftClickAction != null)
        {
            leftClickAction.started -= OnLeftClickStarted;
            leftClickAction.canceled -= OnLeftClickCanceled;
            leftClickAction.Dispose();
        }

        if (rightClickAction != null)
        {
            rightClickAction.started -= OnRightClickStarted;
            rightClickAction.canceled -= OnRightClickCanceled;
            rightClickAction.Dispose();
        }
    }
}