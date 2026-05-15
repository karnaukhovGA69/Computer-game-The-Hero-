using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using TheHero.Domain;

namespace TheHero.Subsystems.Save
{
    public class SaveManager
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Include
        };

        private readonly string _saveDir;
        private readonly string _savePath;
        private readonly string _backupPath;

        public SaveManager()
        {
            _saveDir   = Path.Combine(Application.persistentDataPath, "saves");
            _savePath  = Path.Combine(_saveDir, "save.json");
            _backupPath = Path.Combine(_saveDir, "save_backup.json");
        }

        // Сохранить состояние игры. Перед записью создаёт резервную копию предыдущего сохранения.
        public bool Save(GameState state)
        {
            try
            {
                EnsureSaveDir();
                CreateBackup();

                var data = SaveData.From(state);
                var json = JsonConvert.SerializeObject(data, _jsonSettings);
                File.WriteAllText(_savePath, json);
                Debug.Log($"[SaveManager] Игра сохранена: {_savePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Не удалось сохранить игру: {e.Message}");
                return false;
            }
        }

        // Загрузить сохранение. При ошибке — пробует резервную копию. Возвращает null при неудаче.
        public GameState Load()
        {
            var state = TryLoadFrom(_savePath, "основного сохранения");
            if (state != null) return state;

            Debug.LogWarning("[SaveManager] Пробуем загрузить резервную копию...");
            state = TryLoadFrom(_backupPath, "резервной копии");
            if (state != null) return state;

            Debug.LogError("[SaveManager] Загрузка не удалась: ни основной файл, ни резервная копия не прошли проверку");
            return null;
        }

        // Автосохранение — вызывать при завершении каждого хода
        public void AutoSave(GameState state)
        {
            bool ok = Save(state);
            if (!ok)
                Debug.LogWarning("[SaveManager] Автосохранение не выполнено");
        }

        // Создать резервную копию текущего сохранения (если файл существует)
        public void CreateBackup()
        {
            try
            {
                if (!File.Exists(_savePath)) return;
                File.Copy(_savePath, _backupPath, overwrite: true);
                Debug.Log($"[SaveManager] Резервная копия создана: {_backupPath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Не удалось создать резервную копию: {e.Message}");
            }
        }

        // Загрузить резервную копию напрямую
        public GameState LoadBackup() =>
            TryLoadFrom(_backupPath, "резервной копии");

        public bool SaveExists() => File.Exists(_savePath);
        public bool BackupExists() => File.Exists(_backupPath);

        private GameState TryLoadFrom(string path, string label)
        {
            if (!File.Exists(path))
            {
                Debug.Log($"[SaveManager] Файл {label} не найден: {path}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<SaveData>(json, _jsonSettings);

                var validation = SaveValidator.Validate(data);
                if (!validation.IsValid)
                {
                    Debug.LogError($"[SaveManager] Файл {label} повреждён: {validation.Summary()}");
                    return null;
                }

                Debug.Log($"[SaveManager] Загружено из {label} (сохранено {data.SaveDate})");
                return data.State;
            }
            catch (JsonException e)
            {
                Debug.LogError($"[SaveManager] Ошибка разбора JSON {label}: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Ошибка чтения файла {label}: {e.Message}");
                return null;
            }
        }

        private void EnsureSaveDir()
        {
            if (!Directory.Exists(_saveDir))
            {
                Directory.CreateDirectory(_saveDir);
                Debug.Log($"[SaveManager] Создана папка сохранений: {_saveDir}");
            }
        }
    }
}
