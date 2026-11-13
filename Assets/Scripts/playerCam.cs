using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PlayerCam : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 10f;
    [SerializeField, Min(0f)] private float zoomSpeed = 20f;
    [SerializeField, Min(0.1f)] private float minZoom = 3f;
    [SerializeField, Min(0.1f)] private float maxZoom = 20f;
    [SerializeField] private Vector2 mapMin;
    [SerializeField] private Vector2 mapMax;

    private InputAction moveAction;
    private Camera controlledCamera;
    private GridPlacementSystem grid;
    private bool dragging;
    private Vector2 previousPointer;

    private void LateUpdate()
    {
        if (grid == null) grid = FindAnyObjectByType<GridPlacementSystem>();
        var mouse = Mouse.current;
        if (CityPauseMenu.BlocksGameplayInput || grid == null || mouse == null || grid.HasBuildingSelection ||
            !grid.HasMapHover || !mouse.leftButton.isPressed)
        {
            dragging = false;
            return;
        }
        Vector2 pointer = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragging = true;
            previousPointer = pointer;
        }
        if (!dragging) return;
        Vector3 previousWorld = controlledCamera.ScreenToWorldPoint(previousPointer);
        Vector3 currentWorld = controlledCamera.ScreenToWorldPoint(pointer);
        transform.position += previousWorld - currentWorld;
        previousPointer = pointer;
        ClampCameraPosition();
    }


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
        if (DisasterSystem.IsCutsceneOpen || CityPauseMenu.BlocksGameplayInput) return;
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 movement = transform.right * input.x + transform.up * input.y;
        transform.position += movement * moveSpeed * Time.unscaledDeltaTime;

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

        ClampCameraPosition();
    }

    private void ClampCameraPosition()
    {
        float halfHeight = controlledCamera.orthographicSize;
        float halfWidth = halfHeight * controlledCamera.aspect;

        float minX = mapMin.x + halfWidth;
        float maxX = mapMax.x - halfWidth;
        float minY = mapMin.y + halfHeight;
        float maxY = mapMax.y - halfHeight;

        Vector3 position = transform.position;
        position.x = minX > maxX ? (mapMin.x + mapMax.x) * 0.5f
            : Mathf.Clamp(position.x, minX, maxX);
        position.y = minY > maxY ? (mapMin.y + mapMax.y) * 0.5f
            : Mathf.Clamp(position.y, minY, maxY);
        transform.position = position;
    }
}
