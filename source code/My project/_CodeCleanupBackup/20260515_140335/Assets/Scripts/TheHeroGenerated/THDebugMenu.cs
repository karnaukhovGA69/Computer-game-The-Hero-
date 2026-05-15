using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TheHero.Generated
{
    public class THDebugMenu : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnGUI()
        {
            if (THMapController.Instance == null || THMapController.Instance.State == null) return;
            
            var state = THMapController.Instance.State;

            if (GUILayout.Button("Give Resources"))
            {
                state.gold += 5000; state.wood += 500; state.stone += 500; state.mana += 500;
                THMapController.Instance.UpdateUI();
            }
            if (GUILayout.Button("Teleport to Dark Lord"))
            {
                state.heroX = 14; state.heroY = 9;
                if (THMapController.Instance.HeroMover != null)
                    THMapController.Instance.HeroMover.SetPositionImmediate(14, 9);
                THMapController.Instance.UpdateUI();
            }
            if (GUILayout.Button("Full Army"))
            {
                AddUnit(state, "barracks", "Swordsman", 50);
                AddUnit(state, "range", "Archer", 30);
                AddUnit(state, "mage", "Mage", 10);
                THMapController.Instance.UpdateUI();
            }
            if (GUILayout.Button("Reset Save"))
            {
                THSaveSystem.DeleteSave();
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void AddUnit(THGameState state, string id, string name, int count)
        {
            var u = state.army.Find(x => x.id == id);
            if (u != null) u.count += count;
            else state.army.Add(new THArmyUnit { id = id, name = name, count = count, hpPerUnit = 30, attack = 10, defense = 5, initiative = 5 });
        }
#endif
    }
}