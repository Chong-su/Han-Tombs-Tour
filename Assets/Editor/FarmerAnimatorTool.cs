using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 一键给场景中的农民（NPC农民*）批量挂上 Animator，并绑定动画控制器与 Avatar。
/// 菜单：Tools -> 农民 -> 给当前场景所有农民挂动画 / 给所有场景的农民挂动画
/// </summary>
public static class FarmerAnimatorTool
{
    // 农民动画控制器与农民模型的路径（请按需修改）
    private const string kControllerPath = "Assets/动作/农民动画控制.controller";
    private const string kModelPath = "Assets/mode/NPC农民.fbx";

    [MenuItem("Tools/农民/给当前场景所有农民挂动画")]
    public static void AddAnimatorToCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        int count = ProcessScene(scene);
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[农民动画] 场景「{scene.name}」处理完成，共给 {count} 个农民挂上 Animator。");
    }

    [MenuItem("Tools/农民/给所有场景的农民挂动画")]
    public static void AddAnimatorToAllScenes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Sence" });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[农民动画] 在 Assets/Sence 下没有找到任何场景。");
            return;
        }

        string lastScene = SceneManager.GetActiveScene().path;
        int total = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int count = ProcessScene(scene);
            if (count > 0)
            {
                EditorSceneManager.SaveScene(scene);
                total += count;
            }
            Debug.Log($"[农民动画] 场景「{scene.name}」：给 {count} 个农民挂上 Animator。");
        }

        // 恢复处理前的场景
        if (!string.IsNullOrEmpty(lastScene))
        {
            try { EditorSceneManager.OpenScene(lastScene, OpenSceneMode.Single); }
            catch { /* 忽略恢复失败 */ }
        }

        Debug.Log($"[农民动画] 全部完成，共处理 {total} 个农民。");
    }

    private static int ProcessScene(Scene scene)
    {
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(kControllerPath);
        if (controller == null)
        {
            Debug.LogError("[农民动画] 找不到动画控制器：" + kControllerPath);
            return 0;
        }

        // 从农民模型资产中提取 Humanoid Avatar（供 Animator 使用）
        Avatar avatar = null;
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(kModelPath))
        {
            if (obj is Avatar a)
            {
                avatar = a;
                break;
            }
        }

        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // 递归遍历所有层级，避免漏掉挂在父容器下的农民
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (!go.name.StartsWith("NPC农民"))
                {
                    continue;
                }

                Animator anim = go.GetComponent<Animator>();
                if (anim == null)
                {
                    anim = go.AddComponent<Animator>();
                }
                anim.runtimeAnimatorController = controller;
                if (avatar != null)
                {
                    anim.avatar = avatar;
                }
                else
                {
                    Debug.LogWarning($"[农民动画] 未能从 {kModelPath} 提取到 Avatar，请确认该模型 Rig 为 Humanoid 且已成功生成 Avatar。对象：{go.name}");
                }
                count++;
                Debug.Log($"[农民动画] 已处理：{go.name} (位置 {go.transform.position})");
            }
        }
        return count;
    }
}
