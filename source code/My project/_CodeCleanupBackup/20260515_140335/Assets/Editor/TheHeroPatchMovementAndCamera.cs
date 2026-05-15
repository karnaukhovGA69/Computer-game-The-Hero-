// TheHeroPatchMovementAndCamera.cs
// MenuItem: "The Hero/Fix/Patch Movement And Camera"
// Adds THStrictGridHeroMovement to the Hero and THCameraFollow to Camera.main with Target = Hero.
// Used to fix CHECK 7 / CHECK 8 failures from TheHeroFinalGameValidation without touching Tilemap.

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TheHero.Generated;

public static class TheHeroPatchMovementAndCamera
{
    [MenuItem("The Hero/Fix/Patch Movement And Camera")]
    public static void Patch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

        var heroGO = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
            .FirstOrDefault(g => g.name == "Hero");
        if (heroGO == null)
        {
            Debug.LogError("[TheHeroPatch] Hero GameObject not found in Map.unity");
            return;
        }

        // Ensure exactly one movement component on Hero
        foreach (var t in new System.Type[] { typeof(THReliableHeroMovement), typeof(THGuaranteedHeroMovement), typeof(THHeroMover) })
        {
            var c = heroGO.GetComponent(t);
            if (c != null) Object.DestroyImmediate(c);
        }

        var mover = heroGO.GetComponent<THStrictGridHeroMovement>();
        if (mover == null)
        {
            mover = heroGO.AddComponent<THStrictGridHeroMovement>();
            Debug.Log("[TheHeroPatch] Added THStrictGridHeroMovement to Hero");
        }
        else
        {
            Debug.Log("[TheHeroPatch] Hero already has THStrictGridHeroMovement");
        }

        EditorUtility.SetDirty(heroGO);

        // Camera
        var cam = Camera.main;
        if (cam == null)
        {
            cam = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).FirstOrDefault();
        }
        if (cam == null)
        {
            Debug.LogError("[TheHeroPatch] No Camera found in scene");
            return;
        }

        var follow = cam.GetComponent<THCameraFollow>();
        if (follow == null)
        {
            follow = cam.gameObject.AddComponent<THCameraFollow>();
            Debug.Log("[TheHeroPatch] Added THCameraFollow to Camera");
        }
        follow.Target = heroGO.transform;
        if (follow.MinBounds == Vector2.zero && follow.MaxBounds == Vector2.zero)
        {
            follow.MinBounds = new Vector2(-50f, -50f);
            follow.MaxBounds = new Vector2(50f, 50f);
        }
        EditorUtility.SetDirty(cam);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[TheHeroPatch] Saved Map.unity");
    }
}
