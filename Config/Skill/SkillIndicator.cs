using System;
using UnityEngine;
using Object = UnityEngine.Object;


public class SkillIndicator
{
    private readonly Camera mainCamera;
    private readonly LayerMask groundMask = LayerMask.GetMask("Ground");
    private readonly LayerMask monsterMask = LayerMask.GetMask("Monster");
    private readonly Transform characterRoot;          
    
    private readonly CircleIndicator circleIndicator;
    private readonly SectorIndicator sectorIndicator;
    private readonly CasterRangeIndicator casterRangeIndicator;
    
    private SkillConfig skillConfig;
    private enum Mode { None, MeleeSector, AoeCircle, UnitTarget }
    private Mode currentMode = Mode.None;

    public event Action<int, int> OnUnitTargetSelected;
    public event Action<int, Vector3> OnAoeCircleSelected;
    public event Action<int, Vector3> OnMeleeSectorSelected;
    
    private RaycastHit hit; 

    public SkillIndicator(Camera mainCamera, Transform characterRoot)
    {
        this.mainCamera = mainCamera;
        this.characterRoot = characterRoot;
        
            
        casterRangeIndicator = Object.Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/SkillIndicators/CasterRangeIndicator")).GetComponent<CasterRangeIndicator>();
        sectorIndicator =  Object.Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/SkillIndicators/SectorIndicator")).GetComponent<SectorIndicator>();
        circleIndicator =  Object.Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/SkillIndicators/CircleIndicator")).GetComponent<CircleIndicator>();
        
        sectorIndicator.gameObject.SetActive(false);
        circleIndicator.gameObject.SetActive(false);
        casterRangeIndicator.gameObject.SetActive(false);
    }

    public void Update()
    {
        HandleModeSwitch();
        if (currentMode == Mode.None) return;
        if (currentMode == Mode.AoeCircle)
        {
            casterRangeIndicator.transform.position = characterRoot.transform.position + Vector3.up * 0.05f;
            
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hit, 1000f, groundMask))
            {
                Vector3 hitPoint = hit.point;

                // 角色脚下（忽略高度）
                Vector3 origin = characterRoot.position;
                origin.y = 0;

                Vector3 target = hitPoint;
                target.y = 0;

                float maxDist = casterRangeIndicator.radius;      // 你 ShowIndicator 里用 CastMaxDistance 设过
                float dist = Vector3.Distance(origin, target);

                bool inRange = dist <= maxDist;

                // 不再 clamp，落点永远跟随鼠标，颜色提示是否超出
                Vector3 finalPos = new Vector3(target.x, hitPoint.y, target.z);
                circleIndicator.transform.position = finalPos + Vector3.up * 0.02f;
                
                casterRangeIndicator.SetInRange(inRange);
                circleIndicator.SetInRange(inRange);
            }

            if (Input.GetMouseButtonDown(0))
            {
                OnAoeCircleSelected?.Invoke(skillConfig.Id, circleIndicator.transform.position);
                HideIndicator();
            }
            return;
        }
        
        if (currentMode == Mode.UnitTarget)
        {
            casterRangeIndicator.transform.position = characterRoot.position + Vector3.up * 0.05f;

            bool inRange = true;

            // 射线检测当前鼠标指向的怪
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hit, 1000f, monsterMask))
            {
                var entity = hit.collider.GetComponent<EntityBase>();
                if (entity != null && skillConfig.Cast is UnitTargetCastConfig unitCfg)
                {
                    Vector3 origin = characterRoot.position;
                    origin.y = 0;

                    Vector3 targetPos = entity.transform.position;
                    targetPos.y = 0;

                    float dist = Vector3.Distance(origin, targetPos);
                    inRange = dist <= unitCfg.CastMaxDistance;
                }
            }

            // ✅ 根据距离切颜色
            casterRangeIndicator.SetInRange(inRange);

            // 左键点击部分（你原来的逻辑）：
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hit, 1000f, monsterMask))
                {
                    var entity = hit.collider.GetComponent<EntityBase>();
                    if (entity == null) return;

                    var entityId = entity.EntityId;
                    
                    if (!inRange) 
                    {
                        Debug.Log("超出范围了");
                        return;
                    }

                    Debug.Log("我选中了 " + entityId);
                    OnUnitTargetSelected?.Invoke(skillConfig.Id, entityId);
                    HideIndicator();
                }
            }

            return;
        }

        if (currentMode == Mode.MeleeSector)
        {
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hit, 1000f, groundMask))
            {
                var hitPoint = hit.point;
                Vector3 origin = characterRoot.position;
                origin.y = 0;

                Vector3 target = hitPoint;
                target.y = 0;

                Vector3 dir = target - origin;
                if (dir.sqrMagnitude < 0.01f) return;

                sectorIndicator.transform.position = characterRoot.position + Vector3.up * 0.02f;
                sectorIndicator.transform.rotation = Quaternion.LookRotation(dir);
                if (Input.GetMouseButtonDown(0))
                {
                    OnMeleeSectorSelected?.Invoke(skillConfig.Id, hitPoint);
                }
            }

        }



    }


    public void HideIndicator()
    {
        InputBindService.Instance.AttackBan = false;
        SetMode(Mode.None);
    }
    
    public void ShowIndicator(SkillConfig config)
    {
        skillConfig = config;
        InputBindService.Instance.AttackBan = true;
        switch (skillConfig.Cast.InputType)
        {
            case SkillCastInputType.Direction:
                if (skillConfig.Cast.AreaShape == SkillAreaShape.Sector)
                {
                    var cfg = (MeleeSectorCastConfig)skillConfig.Cast;
                    sectorIndicator.SetParams(cfg.Radius, cfg.Angle);
                    SetMode(Mode.MeleeSector);
                }
                else if (skillConfig.Cast.AreaShape == SkillAreaShape.Line)
                {
                    
                    // TODO: 切换到“直线箭头”指示器
                }
                break;
            case SkillCastInputType.GroundPosition:
                if (skillConfig.Cast.AreaShape == SkillAreaShape.Circle)
                {
                    var cfg = (GroundCircleCastConfig)skillConfig.Cast;
                    circleIndicator.SetRadius(cfg.Radius);
                    casterRangeIndicator.SetRadius(cfg.CastMaxDistance);
                    SetMode(Mode.AoeCircle);
                }
                break;
            case SkillCastInputType.UnitTarget:
                if (skillConfig.Cast.AreaShape == SkillAreaShape.None)
                {
                    var cfg = (UnitTargetCastConfig)skillConfig.Cast;
                    casterRangeIndicator.SetRadius(cfg.CastMaxDistance);
                    SetMode(Mode.UnitTarget);
                }
                break;

            case SkillCastInputType.None:

                break;
        }
    }

    private void HandleModeSwitch()
    {
        if (Input.GetMouseButtonDown(1)) // 右键取消
        {
            InputBindService.Instance.AttackBan = false;
            SetMode(Mode.None);
        }
    }

    private void SetMode(Mode mode)
    {
        currentMode = mode;

        if (sectorIndicator != null)
            sectorIndicator.gameObject.SetActive(mode == Mode.MeleeSector);

        if (circleIndicator != null)
            circleIndicator.gameObject.SetActive(mode == Mode.AoeCircle);
        
        if (casterRangeIndicator != null)
            casterRangeIndicator.gameObject.SetActive(mode == Mode.AoeCircle || mode == Mode.UnitTarget);
    }
    
}
