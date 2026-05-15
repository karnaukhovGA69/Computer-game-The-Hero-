using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TheHero.Generated;
using System.Linq;

public class TheHeroFixOversizedMapSprites : EditorWindow
{
    [MenuItem("The Hero/Fix/Fix Oversized Map Sprites")]
    public static void FixOversizedSprites()
    {
        string mapScenePath = "Assets/Scenes/Map.unity";
        var scene = EditorSceneManager.OpenScene(mapScenePath);

        // Load map icons
        Sprite orcIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_enemy_orc_map.png");
        Sprite goblinIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_enemy_goblin_map.png");
        Sprite darkLordIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_darklord_map.png");
        Sprite goldIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_gold.png");
        Sprite castleIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_castle.png");
        Sprite mineIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_mine.png");

        var spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
        
        Debug.Log("[TheHeroScaleFix] Oversized map sprites scanned");

        foreach (var sr in spriteRenderers)
        {
            var go = sr.gameObject;
            var nameLower = go.name.ToLower();
            var mapObj = go.GetComponent<THMapObject>();
            
            // 1. Identify and replace oversized/combat sprites
            bool isBoss = mapObj != null && (mapObj.isDarkLord || mapObj.isFinalBoss || (nameLower.Contains("lord") && !nameLower.Contains("gold")));
            bool isEnemy = mapObj != null && mapObj.type == THMapObject.ObjectType.Enemy && !isBoss;
            bool isHero = go.GetComponent<THHeroMover>() != null || go.name.Contains("Hero") || go.name.Contains("Player");
            bool isCastle = mapObj != null && mapObj.type == THMapObject.ObjectType.Base;
            bool isMine = mapObj != null && mapObj.type == THMapObject.ObjectType.Mine;
            bool isResource = mapObj != null && (mapObj.type == THMapObject.ObjectType.GoldResource || mapObj.type == THMapObject.ObjectType.WoodResource || mapObj.type == THMapObject.ObjectType.StoneResource || mapObj.type == THMapObject.ObjectType.ManaResource);

            // Bounds check
            var bounds = sr.bounds;
            bool tooLarge = bounds.size.x > 2f || bounds.size.y > 2f;

            if (tooLarge || isEnemy || isBoss || isResource || isCastle || isMine)
            {
                if (isBoss)
                {
                    sr.sprite = darkLordIcon;
                    go.transform.localScale = new Vector3(1f, 1f, 1f);
                    sr.sortingOrder = 30;
                    Debug.Log($"[TheHeroScaleFix] DarkLord map icon fixed: {go.name}");
                }
                else if (isEnemy)
                {
                    if (nameLower.Contains("goblin")) sr.sprite = goblinIcon;
                    else sr.sprite = orcIcon;
                    
                    go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                    sr.sortingOrder = 25;
                    Debug.Log($"[TheHeroScaleFix] Oversized enemy sprite replaced: {go.name}");
                }
                else if (isResource && nameLower.Contains("gold"))
                {
                    sr.sprite = goldIcon;
                    go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                    sr.sortingOrder = 20;
                }
                else if (isCastle)
                {
                    sr.sprite = castleIcon;
                    go.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
                    sr.sortingOrder = 25;
                }
                else if (isMine)
                {
                    sr.sprite = mineIcon;
                    go.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
                    sr.sortingOrder = 25;
                }
            }

            // 2. Normalize Scale and Sorting for all (even if not oversized)
            if (isHero)
            {
                go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                sr.sortingOrder = 50;
            }
            else if (isResource && !isBoss && !isEnemy)
            {
                go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                sr.sortingOrder = 20;
            }

            // 3. Normalize Colliders
            NormalizeCollider(go, isHero);

            // 4. Ensure DarkLord Logic
            if (isBoss && mapObj != null)
            {
                mapObj.type = THMapObject.ObjectType.Enemy;
                mapObj.isDarkLord = true;
                mapObj.isFinalBoss = true;
                mapObj.blocksMovement = true;
                mapObj.startsCombat = true;
                mapObj.id = "Enemy_DarkLord_Final";
            }
        }

        Debug.Log("[TheHeroScaleFix] Map object visuals normalized");
        Debug.Log("[TheHeroScaleFix] Gameplay data preserved");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[TheHeroScaleFix] Ready for testing");
    }

    private static void NormalizeCollider(GameObject go, bool isHero)
    {
        // Remove PolygonCollider2D if present
        var poly = go.GetComponent<PolygonCollider2D>();
        if (poly != null) DestroyImmediate(poly);

        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();

        if (isHero)
        {
            box.size = new Vector2(0.7f, 0.7f);
        }
        else
        {
            box.size = new Vector2(0.8f, 0.8f);
        }
        box.offset = Vector2.zero;
        box.isTrigger = true;
    }
}
