using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Infrastructure.Persistence.Dto;
using TwisterCompanion.Infrastructure.Persistence.Json;
using TwisterCompanion.Infrastructure.Persistence.Mapping;

namespace TwisterCompanion.Infrastructure.Persistence;

/// <summary>
/// Ustawienia aplikacji przechowywane w pliku JSON.
/// </summary>
/// <remarks>
/// Plik, a nie <c>Preferences</c> platformy. Powody: jeden mechanizm zapisu dla wszystkich
/// danych, „usuń moje dane" (Etap 12) to skasowanie katalogu, a przede wszystkim — cała
/// klasa daje się przetestować, bo nie odwołuje się do API platformy.
/// <para>
/// Zapisy są szeregowane semaforem. Suwak głośności potrafi wygenerować serię zmian
/// szybciej, niż kończy się zapis pliku, a dwa równoległe zapisy do tej samej ścieżki
/// mogłyby się przepleść.
/// </para>
/// </remarks>
internal sealed class JsonSettingsService(
    IStoragePathProvider pathProvider,
    JsonDocumentStore documentStore,
    ILogger<JsonSettingsService> logger)
    : ISettingsService, IDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <inheritdoc />
    public AppSettings Current { get; private set; } = AppSettings.Default;

    /// <inheritdoc />
    public event EventHandler<AppSettings>? Changed;

    private string SettingsPath =>
        Path.Combine(pathProvider.AppDataDirectory, PersistenceSchema.SettingsFileName);

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        AppSettingsDto? dto = await documentStore.ReadAsync(
            SettingsPath,
            PersistenceJsonContext.Default.AppSettingsDto,
            cancellationToken);

        if (dto is null)
        {
            logger.LogInformation("Brak zapisanych ustawień — używam domyślnych.");
        }

        Current = dto is null ? AppSettings.Default : AppSettingsMapper.ToDomain(dto);

        // Zdarzenie idzie także po wczytaniu, nie tylko po zmianie z ekranu ustawień.
        // Wcześniej go tu nie było i kosztowało to błąd: motyw stosowany raz, przy starcie,
        // trafiał na wartości domyślne i zapisany wybór ciemnego wyglądu nigdy nie wchodził
        // w życie po ponownym uruchomieniu. Język działał tylko dlatego, że start aplikacji
        // ustawiał go dodatkowo ręcznie. Wczytanie ustawień jest z punktu widzenia każdego
        // subskrybenta taką samą zmianą jak każda inna i tak ma być zgłaszane.
        Changed?.Invoke(this, Current);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        Func<AppSettings, AppSettings> change,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);

        await _writeLock.WaitAsync(cancellationToken);

        AppSettings updated;
        try
        {
            updated = change(Current);

            await documentStore.WriteAsync(
                SettingsPath,
                AppSettingsMapper.ToDto(updated),
                PersistenceJsonContext.Default.AppSettingsDto,
                cancellationToken);

            Current = updated;
        }
        finally
        {
            _writeLock.Release();
        }

        // Zdarzenie poza sekcją krytyczną: subskrybent może w reakcji wywołać kolejną
        // zmianę ustawień, a wtedy czekałby na semafor, który sam trzyma.
        Changed?.Invoke(this, updated);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) =>
        UpdateAsync(_ => AppSettings.Default, cancellationToken);

    /// <inheritdoc />
    public void Dispose() => _writeLock.Dispose();
}
