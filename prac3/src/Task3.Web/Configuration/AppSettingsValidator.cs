using Task3.Web.Models;

namespace Task3.Web.Configuration;

public static class AppSettingsValidator
{
    public static IReadOnlyList<string> Validate(AppSettings settings)
    {
        var errors = new List<string>();

        if (settings.AllowedOrigins.Length == 0)
        {
            errors.Add("Список доверенных источников не должен быть пустым.");
        }
        else
        {
            for (var index = 0; index < settings.AllowedOrigins.Length; index++)
            {
                var origin = settings.AllowedOrigins[index]?.Trim();

                if (string.IsNullOrWhiteSpace(origin))
                {
                    errors.Add($"Доверенный источник #{index + 1} пустой.");
                    continue;
                }

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    errors.Add($"Доверенный источник '{origin}' имеет некорректный формат.");
                    continue;
                }

                if (uri.Scheme is not ("http" or "https"))
                {
                    errors.Add($"Доверенный источник '{origin}' должен использовать http или https.");
                }
            }
        }

        ValidateRateLimit(settings.RateLimits.Read, "Read", errors);
        ValidateRateLimit(settings.RateLimits.Write, "Write", errors);

        return errors;
    }

    private static void ValidateRateLimit(FixedWindowLimitSettings settings, string name, ICollection<string> errors)
    {
        if (settings.PermitLimit <= 0)
        {
            errors.Add($"RateLimit.{name}.PermitLimit должен быть больше нуля.");
        }

        if (settings.WindowSeconds <= 0)
        {
            errors.Add($"RateLimit.{name}.WindowSeconds должен быть больше нуля.");
        }

        if (settings.QueueLimit < 0)
        {
            errors.Add($"RateLimit.{name}.QueueLimit не может быть отрицательным.");
        }
    }
}
