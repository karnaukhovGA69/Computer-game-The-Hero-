// Точка входа: инициализирует все модули и запускает главное меню
using UnityEngine;

namespace TheHero.Infrastructure
{
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Порядок инициализации важен: конфиги → модули → UI
        }
    }
}
