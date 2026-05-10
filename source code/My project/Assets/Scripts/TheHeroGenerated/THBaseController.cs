using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TheHero.Generated
{
    public class THBaseController : MonoBehaviour
    {
        public THGameState State;
        
        [Header("UI Sections")]
        public Text ResourcesText;
        public Text DayWeekText;
        public Text InfoText;
        public Text ArmyListText;

        private void Start()
        {
            State = THSaveSystem.LoadGame();
            if (State == null) State = THSaveSystem.NewGame();
            UpdateUI();
            Log("Добро пожаловать в замок!");

            if (THAudioManager.Instance != null) THAudioManager.Instance.PlayMusic("Base");

            if (THStoryManager.Instance != null)
                THStoryManager.Instance.ShowDialog("first_base", "Замок", "В замке можно нанимать юнитов и улучшать здания.", "Sprites/Units/unit_swordsman_portrait");
        }

        public void UpdateUI()
        {
            if (ResourcesText) ResourcesText.text = $"Gold: {State.gold} | Wood: {State.wood} | Stone: {State.stone} | Mana: {State.mana}";
            if (DayWeekText) DayWeekText.text = $"День: {State.day} / Неделя: {State.week}";
            
            if (ArmyListText)
            {
                string armyStr = "Ваша армия:\n";
                foreach (var unit in State.army)
                {
                    if (unit.count > 0) armyStr += $"- {unit.name}: {unit.count}\n";
                }
                ArmyListText.text = armyStr;
            }
        }

        public void Recruit(string id)
        {
            var b = State.buildings.Find(x => x.id == id);
            if (b != null && b.recruitsAvailable > 0)
            {
                if (State.gold >= b.goldCost && State.wood >= b.woodCost && State.stone >= b.stoneCost && State.mana >= b.manaCost)
                {
                    State.gold -= b.goldCost;
                    State.wood -= b.woodCost;
                    State.stone -= b.stoneCost;
                    State.mana -= b.manaCost;
                    b.recruitsAvailable--;

                    var unit = State.army.Find(u => u.id == id);
                    if (unit != null)
                    {
                        unit.count++;
                    }
                    else
                    {
                        if (State.army.Count >= 5)
                        {
                            Log("Армия заполнена!");
                            return;
                        }
                        AddUnitToArmy(id, b);
                        }
                        Log($"+1 {b.name}");
                        if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("recruit");
                        UpdateUI();
                        THSaveSystem.SaveGame(State);
}
                else
                {
                    Log("Недостаточно ресурсов!");
                }
            }
            else
            {
                Log("Нет доступных рекрутов!");
            }
        }

        private void AddUnitToArmy(string id, THBuildingData b)
        {
            THArmyUnit u = new THArmyUnit { id = id, name = b.name, count = 1 };
            if (id == "unit_swordsman") { u.name = "Swordsman"; u.hpPerUnit = 30; u.attack = 5; u.defense = 2; u.initiative = 5; }
            if (id == "unit_archer") { u.name = "Archer"; u.hpPerUnit = 20; u.attack = 7; u.defense = 1; u.initiative = 7; }
            if (id == "unit_mage") { u.name = "Mage"; u.hpPerUnit = 25; u.attack = 10; u.defense = 2; u.initiative = 8; }
            
            if (b.level >= 2)
            {
                if (id == "unit_swordsman") { u.hpPerUnit += 10; u.attack += 2; }
                if (id == "unit_archer") { u.hpPerUnit += 5; u.attack += 3; }
                if (id == "unit_mage") { u.hpPerUnit += 8; u.attack += 4; }
            }
            
            State.army.Add(u);
        }

        public void Upgrade(string id)
        {
            var b = State.buildings.Find(x => x.id == id);
            if (b == null) return;
            if (b.level >= 2) { Log("Максимальный уровень!"); return; }

            int goldCost = 0; int woodCost = 0; int stoneCost = 0; int manaCost = 0;
            if (id == "unit_swordsman") { goldCost = 300; woodCost = 10; }
            if (id == "unit_archer") { goldCost = 350; woodCost = 15; }
            if (id == "unit_mage") { goldCost = 500; stoneCost = 20; manaCost = 10; }

            if (State.gold >= goldCost && State.wood >= woodCost && State.stone >= stoneCost && State.mana >= manaCost)
            {
                State.gold -= goldCost;
                State.wood -= woodCost;
                State.stone -= stoneCost;
                State.mana -= manaCost;
                b.level++;
                UpdateUI();
                Log("Здание улучшено!");
                if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("upgrade");
                THSaveSystem.SaveGame(State);
                }
else Log("Недостаточно ресурсов!");
        }

        public void RecruitAll(string id)
        {
            var b = State.buildings.Find(x => x.id == id);
            if (b == null) return;
            int count = 0;
            while (b.recruitsAvailable > 0 && State.gold >= b.goldCost)
            {
                Recruit(id);
                count++;
            }
        }

        public void Log(string msg) { if (InfoText) InfoText.text = msg; }
        public void BackToMap() => THSceneLoader.Instance.LoadMap();
        }
        }
