using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 退出游戏。
// 用法:
//   1)UI 按钮:在 Canvas 上建一个 Button,把挂了本脚本的物体拖到 Button 的 OnClick,选 QuitGame.Quit()。
//   2)快捷键:默认按 Esc 也能退出(把 quitKey 设为 None 可禁用)。
// 编辑器里点退出会停止播放;打包成 exe 后会真正关闭程序。
public class QuitGame : MonoBehaviour
{
    [Tooltip("按此键退出游戏;设为 None 可禁用快捷键,只用按钮")]
    public KeyCode quitKey = KeyCode.Escape;

    void Update()
    {
        if (quitKey != KeyCode.None && Input.GetKeyDown(quitKey))
        {
            Quit();
        }
    }

    // 供 UI Button 的 OnClick() 调用,也可被快捷键触发
    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;   // 编辑器:停止播放
#else
        Application.Quit();                     // 打包后:退出程序
#endif
    }
}
