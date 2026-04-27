// Локализация строк интерфейса — загружает словарь по языку
using System.Collections.Generic;

namespace TheHero.Infrastructure
{
    public class LocalizationManager
    {
        private Dictionary<string, string> _strings = new();

        public void LoadLanguage(string lang) { }

        public string Get(string key) =>
            _strings.TryGetValue(key, out var val) ? val : key;
    }
}
