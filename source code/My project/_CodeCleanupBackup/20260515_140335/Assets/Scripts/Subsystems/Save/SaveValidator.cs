using System.Collections.Generic;

namespace TheHero.Subsystems.Save
{
    public class ValidationResult
    {
        public bool IsValid;
        public List<string> Errors = new List<string>();

        public static ValidationResult Ok() => new ValidationResult { IsValid = true };

        public static ValidationResult Fail(string reason)
        {
            var r = new ValidationResult { IsValid = false };
            r.Errors.Add(reason);
            return r;
        }

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public string Summary() => string.Join("; ", Errors);
    }

    public static class SaveValidator
    {
        public static ValidationResult Validate(SaveData data)
        {
            var result = ValidationResult.Ok();

            if (data == null)
                return ValidationResult.Fail("Файл сохранения пустой или повреждён");

            if (data.SaveVersion < 1)
                result.AddError($"Неверная версия сохранения: {data.SaveVersion}");

            if (string.IsNullOrEmpty(data.SaveDate))
                result.AddError("Отсутствует дата сохранения");

            var state = data.State;
            if (state == null)
            {
                result.AddError("Состояние игры (GameState) отсутствует");
                return result;
            }

            if (state.Hero == null)
                result.AddError("Данные героя отсутствуют");
            else
            {
                if (string.IsNullOrEmpty(state.Hero.Name))
                    result.AddError("Имя героя не задано");

                if (state.Hero.Level < 1)
                    result.AddError($"Недопустимый уровень героя: {state.Hero.Level}");

                if (state.Hero.Army == null)
                    result.AddError("Армия героя отсутствует");
            }

            if (state.Wallet == null)
                result.AddError("Кошелёк ресурсов отсутствует");

            if (state.Day < 1 || state.Day > 7)
                result.AddError($"Недопустимый день: {state.Day} (ожидается 1–7)");

            if (state.Week < 1)
                result.AddError($"Недопустимая неделя: {state.Week}");

            if (state.TurnNumber < 1)
                result.AddError($"Недопустимый номер хода: {state.TurnNumber}");

            return result;
        }
    }
}
