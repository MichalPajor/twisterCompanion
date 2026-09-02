using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Rozpoznawanie mowy oparte na MAUI Community Toolkit.
/// </summary>
/// <remarks>
/// Adapter na dwie implementacje toolkitu: <see cref="SpeechToText.Default"/> (rozpoznawanie
/// systemowe, na Androidzie zwykle Google przez sieć) i <see cref="OfflineSpeechToText.Default"/>
/// (rozpoznawanie na urządzeniu). Żadna z nich nie wymaga konta, klucza ani integracji
/// z serwisem zewnętrznym — obie korzystają z usługi rozpoznawania zainstalowanej w systemie.
/// <para>
/// <b>Jedna sesja na wywołanie.</b> Toolkit po zakończeniu sesji nie wznawia nasłuchu, więc
/// ta klasa też tego nie robi. Nasłuch ciągły powstaje warstwę wyżej.
/// </para>
/// <para>
/// <b>Przełączanie trybu wymaga rozłączenia zdarzeń.</b> Obie implementacje są singletonami
/// toolkitu, więc subskrypcja trzymana na stałe zostawałaby na porzuconej implementacji po
/// zmianie trybu. Zdarzenia są podłączane przy starcie sesji i zwalniane przy jej końcu.
/// </para>
/// </remarks>
internal sealed class ToolkitSpeechRecognitionService : ISpeechRecognitionService, IDisposable, IAsyncDisposable
{
    private readonly ILogger<ToolkitSpeechRecognitionService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ISpeechToText? _active;
    private bool _disposed;

    /// <summary>Tworzy adapter rozpoznawania mowy.</summary>
    /// <param name="logger">Logger.</param>
    public ToolkitSpeechRecognitionService(ILogger<ToolkitSpeechRecognitionService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsListening => _active?.CurrentState is SpeechToTextState.Listening or SpeechToTextState.Silence;

    /// <inheritdoc />
    public event EventHandler<string>? PartialRecognized;

    /// <inheritdoc />
    public event EventHandler<SpeechRecognitionOutcome>? SessionCompleted;

    /// <inheritdoc />
    public Task<SpeechRecognitionCapabilities> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(ReadCapabilities());
    }

    /// <inheritdoc />
    public async Task<bool> RequestPermissionAsync(CancellationToken cancellationToken = default)
    {
        // Dwa osobne pytania są wymagane: uprawnienie systemowe do mikrofonu oraz zgoda
        // pilnowana przez sam toolkit. Pominięcie któregokolwiek kończy się sesją, która
        // startuje i natychmiast pada na braku uprawnień.
        PermissionStatus microphone = await Permissions.RequestAsync<Permissions.Microphone>();

        if (microphone != PermissionStatus.Granted)
        {
            _logger.LogWarning("Brak zgody na mikrofon — sterowanie głosem pozostaje wyłączone.");

            return false;
        }

        return await SpeechToText.Default.RequestPermissions(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StartAsync(
        SpeechRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Toolkit ignoruje start, jeśli poprzednia sesja nie jest zamknięta.
            // Zamykamy ją sami, żeby wywołanie nie zniknęło bez śladu.
            await DetachAsync(cancellationToken);

            ISpeechToText speech = Resolve(request.Mode);

            speech.RecognitionResultUpdated += OnRecognitionResultUpdated;
            speech.RecognitionResultCompleted += OnRecognitionResultCompleted;
            _active = speech;

            await speech.StartListenAsync(BuildOptions(request), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await DetachAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await DetachAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się zamknąć sesji rozpoznawania mowy.");
        }

        _gate.Dispose();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Kontener zależności w MAUI zwalnia usługi <b>synchronicznie</b> przy zamykaniu
    /// aplikacji, a typ z samym <see cref="IAsyncDisposable"/> zgłasza tam wyjątek.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _active = null;
        _gate.Dispose();
    }

    /// <summary>Wybiera implementację toolkitu odpowiadającą trybowi.</summary>
    private static ISpeechToText Resolve(SpeechRecognitionMode mode) => mode switch
    {
        SpeechRecognitionMode.OnDevice => OfflineSpeechToText.Default,
        _ => SpeechToText.Default,
    };

    /// <summary>
    /// Przekłada parametry sesji na opcje toolkitu.
    /// </summary>
    /// <remarks>
    /// Domyślną wartością <c>AutoStopSilenceTimeout</c> w toolkicie jest
    /// <see cref="TimeSpan.MaxValue"/>, czyli „nie zamykaj sesji z powodu ciszy". Ustawiamy
    /// ją tylko wtedy, gdy wywołujący ma własne zdanie — i budujemy oba warianty osobno,
    /// bo <c>SpeechToTextOptions</c> nie jest rekordem i nie obsługuje <c>with</c>.
    /// </remarks>
    private static SpeechToTextOptions BuildOptions(SpeechRecognitionRequest request) =>
        request.AutoStopSilenceTimeout is { } silence
            ? new SpeechToTextOptions
            {
                Culture = request.Culture,
                ShouldReportPartialResults = request.ReportPartialResults,
                AutoStopSilenceTimeout = silence,
            }
            : new SpeechToTextOptions
            {
                Culture = request.Culture,
                ShouldReportPartialResults = request.ReportPartialResults,
            };

    /// <summary>
    /// Odczytuje możliwości urządzenia.
    /// </summary>
    /// <remarks>
    /// Rozpoznawanie na urządzeniu wymaga Androida 13: samo API jest dostępne od wersji 12,
    /// ale implementacja toolkitu deklaruje wymóg 13, a niżej rzuca wyjątkiem. Warunek
    /// zapytany <b>przed</b> użyciem jest tańszy niż wyjątek złapany po fakcie.
    /// </remarks>
    private SpeechRecognitionCapabilities ReadCapabilities()
    {
#if ANDROID
        bool system = Android.Speech.SpeechRecognizer.IsRecognitionAvailable(
            Android.App.Application.Context);

        bool onDevice = OperatingSystem.IsAndroidVersionAtLeast(33)
            && Android.Speech.SpeechRecognizer.IsOnDeviceRecognitionAvailable(
                Android.App.Application.Context);

        string description = string.Join(
            ", ",
            $"Android {Android.OS.Build.VERSION.Release} (API {(int)Android.OS.Build.VERSION.SdkInt})",
            $"{Android.OS.Build.Manufacturer} {Android.OS.Build.Model}");

        return new SpeechRecognitionCapabilities(
            system,
            onDevice,
            description,
            IsMicrophoneBlockedBySystem());
#else
        // Pozostałe platformy dostaną własne odczyty razem z Etapem 16 (iOS).
        return new SpeechRecognitionCapabilities(
            IsSystemRecognitionAvailable: true,
            IsOnDeviceRecognitionAvailable: false,
            PlatformDescription: DeviceInfo.Current.Platform.ToString());
#endif
    }

#if ANDROID
    /// <summary>
    /// Czy mikrofon jest odcięty globalnym przełącznikiem prywatności Androida.
    /// </summary>
    /// <remarks>
    /// Nie da się tego odczytać wprost: <c>SensorPrivacyManager.IsSensorPrivacyEnabled</c>
    /// jest zarezerwowane dla systemu i nie ma go w wiązaniach. Pytamy więc okrężnie, przez
    /// rejestr operacji: przy wyłączonym przełączniku operacja nagrywania dźwięku jest dla
    /// tej aplikacji <b>ignorowana</b>, mimo że uprawnienie pozostaje przyznane. To właśnie
    /// ta rozbieżność — zgoda jest, a dźwięku nie ma — odróżnia przełącznik systemowy od
    /// zwykłego braku zgody.
    /// <para>
    /// Wynik jest <b>wskazówką, nie pewnikiem</b>. Producenci zmieniają zachowanie warstwy
    /// prywatności, a niepewny odczyt nie może zablokować sterowania głosem — dlatego każdy
    /// błąd oznacza „nie zablokowany" i decyzję podejmuje dalej sam nasłuch.
    /// </para>
    /// </remarks>
    private bool IsMicrophoneBlockedBySystem()
    {
        // Rejestr operacji odpowiada na to pytanie dopiero od API 29.
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            return false;
        }

        try
        {
            Android.Content.Context context = Android.App.Application.Context;

            if (context.GetSystemService(Android.Content.Context.AppOpsService)
                is not Android.App.AppOpsManager operacje)
            {
                return false;
            }

            // Wycofane w API 36, ale nadal działa i nie ma następcy dostępnego dla zwykłych
            // aplikacji: SensorPrivacyManager.IsSensorPrivacyEnabled jest zarezerwowane dla
            // systemu. Wynik i tak jest tylko wskazówką — każde niepowodzenie oznacza
            // „nie zablokowany", więc zniknięcie tej metody w przyszłej wersji Androida
            // najwyżej wyłączy podpowiedź, a nie zepsuje sterowania głosem.
#pragma warning disable CA1422
            Android.App.AppOpsManagerMode tryb = operacje.UnsafeCheckOpNoThrow(
                Android.App.AppOpsManager.OpstrRecordAudio,
                Android.OS.Process.MyUid(),
                context.PackageName!);
#pragma warning restore CA1422

            return tryb == Android.App.AppOpsManagerMode.Ignored;
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Nie udało się odczytać stanu przełącznika mikrofonu.");

            return false;
        }
    }

#endif

    private async Task DetachAsync(CancellationToken cancellationToken)
    {
        if (_active is null)
        {
            return;
        }

        ISpeechToText speech = _active;
        _active = null;

        speech.RecognitionResultUpdated -= OnRecognitionResultUpdated;
        speech.RecognitionResultCompleted -= OnRecognitionResultCompleted;

        try
        {
            await speech.StopListenAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // Zamknięcie sesji, która już się zamknęła, nie jest problemem — nasłuch
            // ciągły zatrzymuje i wznawia rozpoznawanie setki razy w trakcie partii.
            _logger.LogDebug(exception, "Zamknięcie sesji rozpoznawania zgłosiło wyjątek.");
        }
    }

    private void OnRecognitionResultUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args) =>
        PartialRecognized?.Invoke(this, args.RecognitionResult);

    private void OnRecognitionResultCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        SpeechToTextResult result = args.RecognitionResult;

        SpeechRecognitionOutcome outcome = result.IsSuccessful
            ? new SpeechRecognitionOutcome(result.Text)
            : new SpeechRecognitionOutcome(
                Text: null,
                Error: MapError(result.Exception),
                Details: result.Exception?.Message);

        SessionCompleted?.Invoke(this, outcome);
    }

    /// <summary>
    /// Przekłada awarię zgłoszoną przez toolkit na rodzaj błędu niezależny od platformy.
    /// </summary>
    /// <remarks>
    /// Toolkit nie przekazuje kodu błędu — pakuje nazwę wartości wyliczeniowej Androida
    /// w tekst komunikatu (<c>"Failure in speech engine - NoMatch"</c>). Dopasowanie po
    /// nazwie jest więc jedyną dostępną drogą; nazwy pochodzą z
    /// <c>Android.Speech.SpeechRecognizerError</c>, żeby literówka nie przeszła kompilacji.
    /// </remarks>
    private static SpeechRecognitionError MapError(Exception? exception)
    {
        string message = exception?.Message ?? string.Empty;

        if (message.Length == 0)
        {
            return SpeechRecognitionError.Other;
        }

#if ANDROID
        bool Has(string errorName) => message.Contains(errorName, StringComparison.OrdinalIgnoreCase);

        // Nazwy przez nameof, a nie przez wartości wyliczeniowe: potrzebujemy wyłącznie
        // napisu, a nameof daje sprawdzenie w czasie kompilacji — literówka w nazwie błędu
        // nie przejdzie. Analizator zgłasza część tych nazw jako dostępne od Androida 31,
        // ale nameof nie sięga po wartość w czasie działania, więc bramka wersji tu nie ma
        // zastosowania i ostrzeżenie jest wyłączone punktowo.
#pragma warning disable CA1416 // Validate platform compatibility
        if (Has(nameof(Android.Speech.SpeechRecognizerError.NoMatch)))
        {
            return SpeechRecognitionError.NoMatch;
        }

        if (Has(nameof(Android.Speech.SpeechRecognizerError.SpeechTimeout)))
        {
            return SpeechRecognitionError.SpeechTimeout;
        }

        if (Has(nameof(Android.Speech.SpeechRecognizerError.TooManyRequests)))
        {
            return SpeechRecognitionError.TooManyRequests;
        }

        if (Has(nameof(Android.Speech.SpeechRecognizerError.RecognizerBusy)))
        {
            return SpeechRecognitionError.RecognizerBusy;
        }

        if (Has(nameof(Android.Speech.SpeechRecognizerError.LanguageUnavailable))
            || Has(nameof(Android.Speech.SpeechRecognizerError.LanguageNotSupported)))
        {
            return SpeechRecognitionError.LanguageUnavailable;
        }

        if (Has(nameof(Android.Speech.SpeechRecognizerError.InsufficientPermissions)))
        {
            return SpeechRecognitionError.InsufficientPermissions;
        }

        if (Has(nameof(Android.Speech.SpeechRecognizerError.NetworkTimeout))
            || Has(nameof(Android.Speech.SpeechRecognizerError.ServerDisconnected))
            || Has(nameof(Android.Speech.SpeechRecognizerError.Network))
            || Has(nameof(Android.Speech.SpeechRecognizerError.Server)))
        {
            return SpeechRecognitionError.Network;
        }
#pragma warning restore CA1416
#endif

        return SpeechRecognitionError.Other;
    }
}
