using UnityEngine;

public class CameraComponent : BaseComponent
{
    private Transform lookPoint;
    private Camera camera;
    private EntityBase entity;

    // 基础参数
    private Vector3 baseOffset = new Vector3(0f, 5f, -10f);
    private float positionSmoothTime = 0.05f;

    // 缩放
    private float zoomStep = 0.5f;
    private float minZoomDistance = 5f;
    private float maxZoomDistance = 15f;

    // 视点高度
    private float lookAtHeight = 0.0f;

    // 旋转
    private float mouseSensitivity = 2.0f;
    private float maxPitch = 60f;
    private float minPitch = -30f;

    private float currentZoomDistance;
    private Vector3 positionVelocity;

    private float currentPitch = 20f;
    private float currentYaw = 0f;

    // 右键控制（原神式）
    private bool rotateWithRightMouse = true;
    private int rightMouseButton = 1;

    // 组件引用
    private InputComponent input;

    public CameraComponent(Camera camera)
    {
        this.camera = camera;
        currentZoomDistance = Mathf.Abs(baseOffset.z);
    }

    public override void Attach(EntityBase entity)
    {
        this.entity = entity;
        lookPoint = entity.transform;

        input = entity.GetEntityComponent<InputComponent>();

        // 鼠标移动事件（每帧都会触发，具体是否生效由 ShouldRotate 决定）
        if (input != null)
            input.OnMouseMoved += UpdateRotation;
    }

    public override void ClearComponent()
    {
        if (input != null)
            input.OnMouseMoved -= UpdateRotation;
    }

    public override void UpdateEntity(float dt)
    {
        HandleZoomInput();

        // 原神式：按住右键锁定鼠标并旋转；松开右键释放
        if (rotateWithRightMouse)
        {
            bool rotating = Input.GetMouseButton(rightMouseButton);
            Cursor.lockState = rotating ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !rotating;
        }
        else
        {
            // 如果你想一直旋转（类似一些MMO），可以把 rotateWithRightMouse 设为 false
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public override void LateUpdateEntity(float dt)
    {
        if (lookPoint == null || camera == null) return;

        Vector3 idealPosition = CalculateIdealPosition();
        Vector3 finalPosition = idealPosition; // 这里你以后可以加碰撞修正

        camera.transform.position = Vector3.SmoothDamp(
            camera.transform.position,
            finalPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        Vector3 lookAtPoint = lookPoint.position + Vector3.up * lookAtHeight;
        camera.transform.LookAt(lookAtPoint);
    }

    private void HandleZoomInput()
    {
        if (input == null) return;

        if (Mathf.Abs(input.ScrollDelta) > 1e-6f)
        {
            currentZoomDistance = Mathf.Clamp(
                currentZoomDistance - input.ScrollDelta * zoomStep * 10f,
                minZoomDistance,
                maxZoomDistance
            );
        }
    }

    /// <summary>
    /// 输入驱动相机旋转
    /// </summary>
    public void UpdateRotation(Vector2 mouseDelta)
    {
        if (entity == null) return;
        if (mouseDelta.sqrMagnitude <= 0.0001f) return;
        
        if (entity.FSM.Ctx.LockTurn)
            return;

        if (rotateWithRightMouse && !Input.GetMouseButton(rightMouseButton))
            return;

        currentYaw += mouseDelta.x * mouseSensitivity;
        currentPitch = Mathf.Clamp(currentPitch - mouseDelta.y * mouseSensitivity, minPitch, maxPitch);
    }

    private Vector3 CalculateIdealPosition()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 offset = new Vector3(baseOffset.x, baseOffset.y, -currentZoomDistance);
        return lookPoint.position + rotation * offset;
    }
    
    public Vector3 GetCameraForwardProjected()
    {
        Vector3 f = camera.transform.forward;
        f.y = 0;
        return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
    }

    public Vector3 GetCameraRightProjected()
    {
        Vector3 r = camera.transform.right;
        r.y = 0;
        return r.sqrMagnitude > 1e-6f ? r.normalized : Vector3.right;
    }

    public float GetCurrentYaw() => currentYaw;
    
    public void SetRotateWithRightMouse(bool enabled) => rotateWithRightMouse = enabled;
}
