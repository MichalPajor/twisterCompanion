using Microsoft.Extensions.Logging;
using NSubstitute;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Tests.Fakes;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy wspólnego zachowania wszystkich ViewModeli: stanu zajętości i obsługi błędów.
/// </summary>
public class ViewModelBaseTests
{
    private readonly RecordingLogger<TestViewModel> _logger = new();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly FakeLocalizationService _localization = new();

    [Fact]
    public async Task ExecuteSafeAsync_WTrakcieOperacji_UstawiaIsBusy_APoNiej_Zdejmuje()
    {
        TestViewModel viewModel = CreateViewModel();
        bool busyWewnatrzOperacji = false;

        await viewModel.RunAsync(() =>
        {
            busyWewnatrzOperacji = viewModel.IsBusy;
            return Task.CompletedTask;
        });

        Assert.True(busyWewnatrzOperacji);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.IsNotBusy);
    }

    [Fact]
    public async Task ExecuteSafeAsync_GdyOperacjaRzuca_NieWypuszczaWyjatku()
    {
        TestViewModel viewModel = CreateViewModel();

        await viewModel.RunAsync(() => throw new InvalidOperationException("awaria"));

        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExecuteSafeAsync_GdyOperacjaRzuca_LogujeBladZWyjatkiem()
    {
        TestViewModel viewModel = CreateViewModel();
        InvalidOperationException wyjatek = new("awaria");

        await viewModel.RunAsync(() => throw wyjatek);

        RecordingLogger<TestViewModel>.LogEntry wpis = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, wpis.Level);
        Assert.Same(wyjatek, wpis.Exception);
    }

    [Fact]
    public async Task ExecuteSafeAsync_GdyOperacjaRzuca_PokazujeKomunikatUzytkownikowi()
    {
        TestViewModel viewModel = CreateViewModel();

        await viewModel.RunAsync(() => throw new InvalidOperationException("awaria"));

        await _dialogService.Received(1).AlertAsync(
            Arg.Any<string>(),
            "awaria",
            Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteSafeAsync_GdyTrwaJuzOperacja_IgnorujeKolejneWywolanie()
    {
        // Chroni przed dwukrotnym kliknięciem przycisku i przed równoległym
        // uruchomieniem tej samej akcji z UI oraz z komendy głosowej (Etap 8).
        TestViewModel viewModel = CreateViewModel();
        TaskCompletionSource bramka = new();
        bool drugaOperacjaWykonana = false;

        Task pierwsza = viewModel.RunAsync(() => bramka.Task);

        await viewModel.RunAsync(() =>
        {
            drugaOperacjaWykonana = true;
            return Task.CompletedTask;
        });

        Assert.False(drugaOperacjaWykonana);

        bramka.SetResult();
        await pierwsza;

        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task InitializeAsync_WywolujeInicjalizacjeEkranu()
    {
        TestViewModel viewModel = CreateViewModel();
        bool wywolano = false;
        viewModel.InitializeBehavior = () =>
        {
            wywolano = true;
            return Task.CompletedTask;
        };

        await viewModel.InitializeAsync();

        Assert.True(wywolano);
    }

    [Fact]
    public async Task InitializeAsync_GdyInicjalizacjaRzuca_NieWypuszczaWyjatku()
    {
        // Istotne, bo ContentPageBase wywołuje tę metodę z async void —
        // wyjątek nie miałby tam gdzie zostać przechwycony.
        TestViewModel viewModel = CreateViewModel();
        viewModel.InitializeBehavior = () => throw new InvalidOperationException("awaria");

        await viewModel.InitializeAsync();

        Assert.Single(_logger.Entries);
    }

    [Fact]
    public void Konstruktor_BezLoggera_RzucaArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new TestViewModel(null!, _dialogService, _localization));

    [Fact]
    public void Konstruktor_BezSerwisuDialogow_RzucaArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new TestViewModel(_logger, null!, _localization));

    [Fact]
    public void Konstruktor_BezSerwisuTlumaczen_RzucaArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new TestViewModel(_logger, _dialogService, null!));

    private TestViewModel CreateViewModel() => new(_logger, _dialogService, _localization);
}
