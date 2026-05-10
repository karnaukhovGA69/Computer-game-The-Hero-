using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TheHero.Generated
{
    public class THArtifactManager : MonoBehaviour
    {
        private static THArtifactManager _instance;
        public static THArtifactManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THArtifactManager");
                    _instance = go.AddComponent<THArtifactManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private List<THArtifactData> _artifacts = new List<THArtifactData>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDatabase();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeDatabase()
        {
            _artifacts.Add(new THArtifactData { 
                id = "sword_dawn", 
                name = "Sword of Dawn", 
                description = "+2 Army Attack", 
                iconPath = "Sprites/UI/art_sword_dawn", 
                attackBonus = 2 
            });
            _artifacts.Add(new THArtifactData { 
                id = "shield_stone", 
                name = "Shield of Stone", 
                description = "+2 Army Defense", 
                iconPath = "Sprites/UI/art_shield_stone", 
                defenseBonus = 2 
            });
            _artifacts.Add(new THArtifactData { 
                id = "boots_travel", 
                name = "Boots of Travel", 
                description = "+4 Max Movement", 
                iconPath = "Sprites/UI/art_boots_travel", 
                moveBonus = 4 
            });
            _artifacts.Add(new THArtifactData { 
                id = "crown_wisdom", 
                name = "Crown of Wisdom", 
                description = "+20% Experience", 
                iconPath = "Sprites/UI/art_crown_wisdom", 
                expMultiplier = 1.2f 
            });
            _artifacts.Add(new THArtifactData { 
                id = "crystal_orb", 
                name = "Crystal Orb", 
                description = "+5 Mana Income", 
                iconPath = "Sprites/UI/art_crystal_orb", 
                manaIncome = 5 
            });
        }

        public THArtifactData GetArtifact(string id) => _artifacts.FirstOrDefault(a => a.id == id);
        
        public List<THArtifactData> GetAllArtifacts() => _artifacts;

        public THArtifactData GetRandomArtifact(THGameState state)
        {
            var available = _artifacts.Where(a => !state.heroArtifactIds.Contains(a.id)).ToList();
            if (available.Count == 0) return null;
            return available[Random.Range(0, available.Count)];
        }

        public int GetTotalAttackBonus(List<string> artifactIds) => artifactIds.Sum(id => GetArtifact(id)?.attackBonus ?? 0);
        public int GetTotalDefenseBonus(List<string> artifactIds) => artifactIds.Sum(id => GetArtifact(id)?.defenseBonus ?? 0);
        public int GetTotalMoveBonus(List<string> artifactIds) => artifactIds.Sum(id => GetArtifact(id)?.moveBonus ?? 0);
        public float GetTotalExpMultiplier(List<string> artifactIds) => artifactIds.Aggregate(1.0f, (acc, id) => acc * (GetArtifact(id)?.expMultiplier ?? 1.0f));
        public int GetTotalManaIncome(List<string> artifactIds) => artifactIds.Sum(id => GetArtifact(id)?.manaIncome ?? 0);
    }
}
