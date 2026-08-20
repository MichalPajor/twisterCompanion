using Microsoft.Extensions.Logging;

namespace TwisterCompanion.App.Diagnostics;

/// <summary>
/// Ostatnia linia obrony: loguje wyjątki, których nikt nie przechwycił.
/// </summary>
/// <remarks>
/// Nie próbuje ratować aplikacji — przy nieobsłużonym wyjątku proces i tak zostanie
/// zamknięty przez system. Celem jest zostawienie śladu w logu, żeby awarię dało się
/// zdiagnozować, zamiast patrzeć na ciche zniknięcie aplikacji.
/// Rejestrowane raz, przy starcie, z <c>MauiProgram</c>.
/// </remarks>
internal static class GlobalExceptionHandler
{
    private static bool _registered;

    /// <summary>Podłącza globalne przechwytywanie wyjątków.</summary>
    /// <param name="logger">Logger, do którego trafią zgłoszenia.</param>
    public static void Register(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (_registered)
        {
            return;
        }

        _registered = true;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            logger.LogCritical(
                args.ExceptionObject as Exception,
                "Nieobsłużony wyjątek. Aplikacja zostanie zamknięta: {IsTerminating}.",
                args.IsTerminating);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            logger.LogError(args.Exception, "Wyjątek zadania, którego nikt nie zaobserwował.");

            // Bez tego proces zostałby zabity przy finalizacji zadania.
            args.SetObserved();
        };
    }
}
