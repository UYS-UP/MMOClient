using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputComponent : BaseComponent
{
    private enum MoveAxisSource { RawKeys, UnityAxisRaw, UnityAxisSmooth }
    
    private EntityBase entity;
    private InputBindService keys => InputBindService.Instance;

    private const MoveAxisSource AxisSource = MoveAxisSource.RawKeys;
    private const string HorizontalAxisName = "Horizontal";
    private const string VerticalAxisName = "Vertical";

    private const string MouseXName = "Mouse X";
    private const string MouseYName = "Mouse Y";
    private const string MouseScrollName = "Mouse ScrollWheel";

    private const float MouseSensitivity = 1.0f;
    private const bool InvertMouseY = false;

    private const float AxisSmoothingSpeed = 0f;

    public Vector2 MoveAxis { get; private set; }
    public Vector2 MouseDelta { get; private set; } // x=水平移动, y=垂直移动（已乘以敏感度并考虑反转）
    public float ScrollDelta { get; private set; } // 鼠标滚轮

    private Vector2 axisCurrent;

    public event Action<PlayerAction> ActionStarted;
    public event Action<float> OnScrolled;
    public event Action<Vector2> OnMouseMoved;
    
    public override void Attach(EntityBase entity)
    {
        this.entity = entity;

    }

    public override void UpdateEntity(float dt)
    {
        if(entity == null || !entity.IsLocal || keys.UIIsOpen) return;
        
        // 移动
        Vector2 targetAxis;
        switch (AxisSource)
        {
            case MoveAxisSource.UnityAxisRaw:
                targetAxis = new Vector2(Input.GetAxisRaw(HorizontalAxisName),
                    Input.GetAxisRaw(VerticalAxisName));
                break;
            case MoveAxisSource.UnityAxisSmooth:
                targetAxis = new Vector2(Input.GetAxis(HorizontalAxisName), 
                    Input.GetAxis(VerticalAxisName));
                break;
            default:
                float h = 0f, v = 0f;
                if (keys.IsPressed(PlayerAction.MoveLeft))  h -= 1f;
                if (keys.IsPressed(PlayerAction.MoveRight)) h += 1f;
                if (keys.IsPressed(PlayerAction.MoveBackward)) v -= 1f;
                if (keys.IsPressed(PlayerAction.MoveForward))  v += 1f;
                targetAxis = new Vector2(h, v);
                break;
        }
        if (targetAxis.sqrMagnitude > 1f) targetAxis = targetAxis.normalized;

        if (AxisSmoothingSpeed > 0f)
        {
            float a = 1f - Mathf.Exp(-AxisSmoothingSpeed * Mathf.Max(0f, dt));
            axisCurrent = Vector2.Lerp(axisCurrent, targetAxis, a);
            MoveAxis = axisCurrent;
        }
        else
        {
            axisCurrent = targetAxis;
            MoveAxis = targetAxis;
        }

        // 鼠标
        float mx = Input.GetAxis(MouseXName) * MouseSensitivity;
        float my = Input.GetAxis(MouseYName) * MouseSensitivity * (InvertMouseY ? -1 : 1);
        MouseDelta = new Vector2(mx, my);
        
        OnMouseMoved?.Invoke(MouseDelta);
        
        float scroll = Input.GetAxis(MouseScrollName);
        if (Mathf.Abs(scroll) > 1e-6f)
        {
            ScrollDelta = scroll;
            OnScrolled?.Invoke(scroll);
        }
        else
        {
            ScrollDelta = 0f;
        }
        
        // 事件
        if(keys.IsPressed(PlayerAction.MouseRight)) ActionStarted?.Invoke(PlayerAction.MouseRight);
        if(keys.IsDown(PlayerAction.Skill1)) ActionStarted?.Invoke(PlayerAction.Skill1);
        if(keys.IsDown(PlayerAction.Roll)) ActionStarted?.Invoke(PlayerAction.Roll);
        if (!InputBindService.Instance.AttackBan)
        {
            if(keys.IsDown(PlayerAction.Attack)) ActionStarted?.Invoke(PlayerAction.Attack);
        }
        
    }

    public override void LateUpdateEntity(float dt)
    {
        
    }
}
