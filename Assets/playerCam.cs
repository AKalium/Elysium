using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class playerCam : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 10f;
    [SerializeField, Min(0f)] private float zoomSpeed = 2f;
    [SerializeField, Min(0.1f)] private float minZoom = 3f;
    [SerializeField, Min(0.1f)] private float maxZoom = 20f;

    private InputAction moveAction;
    private Camera controlledCamera;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        controlledCamera.orthographic = true;

        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 movement = transform.right * input.x + transform.up * input.y;
        transform.position += movement * moveSpeed * Time.deltaTime;

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                controlledCamera.orthographicSize = Mathf.Clamp(
                    controlledCamera.orthographicSize - scroll * zoomSpeed * 0.01f,
                    minZoom,
                    maxZoom);
            }
        }
    }
}
