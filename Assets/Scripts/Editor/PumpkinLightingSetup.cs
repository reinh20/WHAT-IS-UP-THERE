#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to configure PumpkinSketchFab GameObjects
/// with a Spot Light, inner glow Point Light, and PumpkinLantern component.
///
/// Fixes vs. the previous version:
///  - No more auto-run + auto-save on editor load.
///  - The generated "Pumpkin_SpotLight" / "Pumpkin_InnerGlow" children (and FBX sub-meshes)
///    are no longer treated as new pumpkins, which made lights multiply on every run.
///  - Saving a prefab from a *part* of a prefab instance no longer throws.
///  - Everything is Undo-able.
/// </summary>
public static class PumpkinLightingSetup
{
    private const string FbxAssetName = "PumpkinSketchFab";
    private const string PrefabPath = "Assets/Prefabs/PumpkinLantern.prefab";

    // ------------------------------------------------------------------ Menu items

    [MenuItem("Tools/Pumpkin Lantern/Setup All Pumpkins In Active Scene")]
    public static void ManualSetup()
    {
        ExecutePumpkinSetup(saveScene: false, silent: false);
    }

    [MenuItem("Tools/Pumpkin Lantern/Reset Light Placement (Selected)")]
    public static void ResetPlacementSelected()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            PumpkinLantern lantern = go.GetComponentInParent<PumpkinLantern>();
            if (lantern != null)
            {
                SetupSinglePumpkin(lantern.gameObject, resetPlacement: true);
            }
        }
    }

    [MenuItem("Tools/Pumpkin Lantern/Create Prefab From Selected")]
    public static void CreatePrefabFromSelected()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null || go.GetComponent<PumpkinLantern>() == null)
        {
            EditorUtility.DisplayDialog("Pumpkin Lantern", "Select a configured Pumpkin Lantern in the Hierarchy first.", "OK");
            return;
        }
        CreatePumpkinPrefab(go);
    }

    [MenuItem("GameObject/Light/Convert to Pumpkin Lantern", false, 10)]
    public static void ConvertSelection()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Setup Pumpkin", "Please select a Pumpkin GameObject in the Hierarchy first.", "OK");
            return;
        }

        // If a child (e.g. a sub-mesh) was selected, use the outermost prefab instance root instead
        GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (root == null) root = go;

        SetupSinglePumpkin(root, resetPlacement: false);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"<color=#FFA500><b>[PumpkinLightingSetup]</b></color> Configured '{root.name}' as a Pumpkin Lantern with Spot Light.", root);
    }

    // ------------------------------------------------------------------ Core

    public static void ExecutePumpkinSetup(bool saveScene = false, bool silent = false)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        List<GameObject> pumpkins = FindPumpkinRoots(activeScene);

        foreach (GameObject p in pumpkins)
        {
            SetupSinglePumpkin(p, resetPlacement: false);
        }

        if (pumpkins.Count > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (saveScene && !string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log($"<color=#FFA500><b>[PumpkinLightingSetup]</b></color> Configured {pumpkins.Count} Pumpkin Lantern(s) in scene '{activeScene.name}'.");
        }
        else if (!silent)
        {
            Debug.LogWarning("[PumpkinLightingSetup] No pumpkin found in the active scene. Drag 'Assets/asset/PumpkinSketchFab.fbx' into the scene first.");
        }
    }

    /// <summary>
    /// Returns only the top-level pumpkin objects (never their children or our generated light objects).
    /// </summary>
    public static List<GameObject> FindPumpkinRoots(Scene scene)
    {
        var result = new List<GameObject>();
        var resultSet = new HashSet<Transform>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Parents are always returned before their children
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsPumpkinCandidate(t)) continue;
                if (HasAncestorIn(t, resultSet)) continue;

                resultSet.Add(t);
                result.Add(t.gameObject);
            }
        }
        return result;
    }

    private static bool IsPumpkinCandidate(Transform t)
    {
        string n = t.name;
        if (n == PumpkinLantern.SpotLightName || n == PumpkinLantern.InnerGlowName) return false;
        if (t.GetComponent<Light>() != null) return false;

        // Already configured lantern
        if (t.GetComponent<PumpkinLantern>() != null) return true;

        // Instance of the PumpkinSketchFab FBX (outermost root only)
        if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
        {
            Object src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
            if (src != null && src.name == FbxAssetName) return true;
        }

        // Fallback: any renderer object with "pumpkin" in its name
        return n.ToLowerInvariant().Contains("pumpkin") && t.GetComponentInChildren<Renderer>(true) != null;
    }

    private static bool HasAncestorIn(Transform t, HashSet<Transform> set)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
        {
            if (set.Contains(p)) return true;
        }
        return false;
    }

    public static void SetupSinglePumpkin(GameObject pumpkin, bool resetPlacement)
    {
        Undo.SetCurrentGroupName("Setup Pumpkin Lantern");
        int undoGroup = Undo.GetCurrentGroup();

        CleanupNestedDuplicates(pumpkin);

        // 1. Ensure PumpkinLantern component
        PumpkinLantern lantern = pumpkin.GetComponent<PumpkinLantern>();
        bool isNewLantern = lantern == null;
        if (isNewLantern)
        {
            lantern = Undo.AddComponent<PumpkinLantern>(pumpkin);
        }
        Undo.RecordObject(lantern, "Setup Pumpkin Lantern");

        if (isNewLantern)
        {
            // Warm pumpkin candle flame defaults (only on first setup, keep user tweaks afterwards)
            lantern.lightColor = new Color(1.0f, 0.60f, 0.18f, 1.0f);
            lantern.spotIntensity = 5.0f;
            lantern.spotRange = 14.0f;
            lantern.spotAngle = 70.0f;
            lantern.innerSpotAngle = 35.0f;
            lantern.spotDownTilt = 20.0f;
            lantern.shadows = LightShadows.Soft;
            lantern.enableInnerGlow = true;
            lantern.innerGlowIntensity = 1.6f;
            lantern.innerGlowRange = 3.5f;
            lantern.enableFlicker = true;
            lantern.flickerSpeed = 7.0f;
            lantern.flickerAmount = 0.22f;
        }

        Bounds bounds = lantern.GetWorldBounds();
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        if (radius < 0.01f) radius = 0.5f;
        Vector3 face = lantern.GetFaceDirection();

        // 2. Spot Light child - placed just outside the carved face, tilted toward the floor
        Light spot = EnsureLightChild(pumpkin.transform, PumpkinLantern.SpotLightName, out bool spotCreated);
        if (spotCreated || resetPlacement)
        {
            Undo.RecordObject(spot.transform, "Place Pumpkin SpotLight");
            Vector3 dir = (face + Vector3.down * Mathf.Tan(lantern.spotDownTilt * Mathf.Deg2Rad)).normalized;
            spot.transform.position = center + face * (radius * 1.05f) + Vector3.up * (bounds.extents.y * 0.1f);
            spot.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
        lantern.spotLight = spot;

        // 3. Point Light child - inner glow at the pumpkin center
        Light glow = EnsureLightChild(pumpkin.transform, PumpkinLantern.InnerGlowName, out bool glowCreated);
        if (glowCreated || resetPlacement)
        {
            Undo.RecordObject(glow.transform, "Place Pumpkin InnerGlow");
            glow.transform.position = center;
        }
        lantern.pointLight = glow;

        // 4. Collider so the player can stand on / collide with it
        if (pumpkin.GetComponentInChildren<Collider>(true) == null)
        {
            SphereCollider sc = Undo.AddComponent<SphereCollider>(pumpkin);
            Vector3 s = pumpkin.transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            sc.radius = maxScale > 0.0001f ? radius / maxScale : radius;
            sc.center = pumpkin.transform.InverseTransformPoint(center);
        }

        Undo.RecordObject(spot, "Apply Pumpkin Light Settings");
        Undo.RecordObject(glow, "Apply Pumpkin Light Settings");
        lantern.ApplySettings();

        EditorUtility.SetDirty(lantern);
        PrefabUtility.RecordPrefabInstancePropertyModifications(lantern);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static Light EnsureLightChild(Transform parent, string childName, out bool created)
    {
        created = false;
        Transform t = parent.Find(childName);
        if (t == null)
        {
            GameObject go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
            // Keep world scale = 1 even though the FBX root is usually scaled x100
            Undo.SetTransformParent(go.transform, parent, "Parent " + childName);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            Vector3 ls = parent.lossyScale;
            go.transform.localScale = new Vector3(
                Mathf.Abs(ls.x) > 0.0001f ? 1f / ls.x : 1f,
                Mathf.Abs(ls.y) > 0.0001f ? 1f / ls.y : 1f,
                Mathf.Abs(ls.z) > 0.0001f ? 1f / ls.z : 1f);
            t = go.transform;
            created = true;
        }

        Light l = t.GetComponent<Light>();
        if (l == null) l = Undo.AddComponent<Light>(t.gameObject);
        return l;
    }

    /// <summary>
    /// Removes lights/lantern components that the old buggy setup created recursively
    /// (e.g. Pumpkin_SpotLight/Pumpkin_SpotLight/... or PumpkinLantern on sub-objects).
    /// </summary>
    private static void CleanupNestedDuplicates(GameObject pumpkin)
    {
        var toDestroy = new List<Object>();

        foreach (PumpkinLantern nested in pumpkin.GetComponentsInChildren<PumpkinLantern>(true))
        {
            if (nested.gameObject != pumpkin) toDestroy.Add(nested);
        }

        foreach (Transform t in pumpkin.GetComponentsInChildren<Transform>(true))
        {
            bool isGenerated = t.name == PumpkinLantern.SpotLightName || t.name == PumpkinLantern.InnerGlowName;
            if (isGenerated && t.parent != pumpkin.transform && !PrefabUtility.IsPartOfPrefabInstance(t))
            {
                toDestroy.Add(t.gameObject);
            }
        }

        foreach (Object o in toDestroy)
        {
            if (o != null) Undo.DestroyObjectImmediate(o);
        }

        if (toDestroy.Count > 0)
        {
            Debug.Log($"[PumpkinLightingSetup] Removed {toDestroy.Count} duplicated light object(s) under '{pumpkin.name}'.", pumpkin);
        }
    }

    private static void CreatePumpkinPrefab(GameObject pumpkinSource)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(pumpkinSource) && !PrefabUtility.IsOutermostPrefabInstanceRoot(pumpkinSource))
        {
            Debug.LogWarning($"[PumpkinLightingSetup] '{pumpkinSource.name}' is only a part of a prefab instance; select its root to create a prefab.", pumpkinSource);
            return;
        }

        string folder = Path.GetDirectoryName(PrefabPath).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(pumpkinSource, PrefabPath, InteractionMode.UserAction, out bool success);
        if (success)
        {
            Debug.Log($"[PumpkinLightingSetup] Saved reusable prefab at '{PrefabPath}'.", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }
    }
}
#endif
