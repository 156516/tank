using System.Collections;
using UnityEngine;

// 主菜单开场动画:场景开始时,菜单所有 UI 从屏幕下方整体上移到原本位置(仿原版 Battle City)。
//
// 用法:挂在主菜单的 Canvas 上。
//   - 它会把 Canvas 的所有「直接子物体」一起先下移到屏幕外,再平滑滑回它们在编辑器里摆好的位置。
//     (所以请先在编辑器把菜单摆成最终居中的样子,脚本负责让它从底部滑入。)
//   - 动画期间禁用 enableAfterIntro 里的组件(通常是 Option 选项控制器),结束后再启用,
//     避免玩家在动画没播完时就按键。
[RequireComponent(typeof(RectTransform))]
public class MenuIntro : MonoBehaviour
{
    [Tooltip("滑入时长(秒)")]
    public float slideDuration = 1.2f;

    [Tooltip("起始下移距离(UI 单位);<= 0 时自动取 Canvas 高度 = 整屏从底部滑入")]
    public float slideDistance = 0f;

    [Tooltip("动画期间禁用、结束后启用的组件(通常拖入 Option 选项控制器)")]
    public MonoBehaviour[] enableAfterIntro;

    private RectTransform[] items;   // Canvas 的直接子物体
    private Vector2[] targetPos;     // 各自的最终(设计)位置
    private float startOffset;       // 起始下移距离

    void Awake()
    {
        // 动画开始前先禁用交互
        SetInteractablesEnabled(false);

        // 收集 Canvas 的所有直接子 RectTransform(只移顶层,子孙会跟随,避免重复位移)
        int childCount = transform.childCount;
        var list = new System.Collections.Generic.List<RectTransform>(childCount);
        for (int i = 0; i < childCount; i++)
        {
            RectTransform rt = transform.GetChild(i) as RectTransform;
            if (rt != null) list.Add(rt);
        }
        items = list.ToArray();
        targetPos = new Vector2[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            targetPos[i] = items[i].anchoredPosition;
        }

        // 计算起始偏移:默认取 Canvas 高度,保证从整屏底部之外滑入
        startOffset = slideDistance;
        if (startOffset <= 0f)
        {
            RectTransform canvasRt = GetComponent<RectTransform>();
            startOffset = canvasRt.rect.height > 1f ? canvasRt.rect.height : 1000f;
        }

        // 立刻把所有 UI 下移到屏幕外(避免第一帧闪现在中间)
        ApplyOffset(startOffset);
    }

    void Start()
    {
        StartCoroutine(SlideIn());
    }

    private IEnumerator SlideIn()
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / slideDuration)); // 平滑缓动到位
            ApplyOffset(Mathf.Lerp(startOffset, 0f, k));
            yield return null;
        }
        ApplyOffset(0f);                    // 精确归位
        SetInteractablesEnabled(true);      // 允许选择
    }

    // 把所有子物体相对最终位置下移 offset
    private void ApplyOffset(float offset)
    {
        if (items == null) return;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].anchoredPosition = targetPos[i] + Vector2.down * offset;
        }
    }

    private void SetInteractablesEnabled(bool on)
    {
        if (enableAfterIntro == null) return;
        for (int i = 0; i < enableAfterIntro.Length; i++)
        {
            if (enableAfterIntro[i] != null) enableAfterIntro[i].enabled = on;
        }
    }
}
