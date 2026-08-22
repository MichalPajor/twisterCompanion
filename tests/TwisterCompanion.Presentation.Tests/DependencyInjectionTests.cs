using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.DependencyInjection;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy rejestracji warstwy prezentacji w kontenerze — realizacja kryterium
/// „DI rozwiązuje wszystkie zarejestrowane typy" z Etapu 1.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddPresentation_RozwiazujeKazdyZarejestrowanyViewModel()
    {
        ServiceProvider provider = BuildProvider();

        foreach (Type viewModelType in PresentationServiceCollectionExtensions.ViewModelTypes)
        {
            object viewModel = provider.GetRequiredService(viewModelType);

            Assert.IsAssignableFrom<ViewModelBase>(viewModel);
        }
    }

    [Fact]
    public void ViewModelTypes_ZawieraKazdyViewModelZWarstwyPrezentacji()
    {
        // Test pilnuje, żeby dodanie nowego ekranu bez rejestracji w kontenerze
        // wyszło tutaj, a nie dopiero przy wejściu na ekran w działającej aplikacji.
        IEnumerable<Type> viewModeleWAssembly = typeof(ViewModelBase).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsPublic: true }
                           && typeof(ViewModelBase).IsAssignableFrom(type));

        Type[] niezarejestrowane = viewModeleWAssembly
            .Except(PresentationServiceCollectionExtensions.ViewModelTypes)
            .ToArray();

        Assert.Empty(niezarejestrowane);
    }

    [Fact]
    public void AddPresentation_RejestrujeViewModeleJakoTransient()
    {
        // ViewModel trzyma stan ekranu, więc każde wejście na ekran musi dostać
        // świeżą instancję.
        ServiceProvider provider = BuildProvider();

        HomeViewModel pierwszy = provider.GetRequiredService<HomeViewModel>();
        HomeViewModel drugi = provider.GetRequiredService<HomeViewModel>();

        Assert.NotSame(pierwszy, drugi);
    }

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();

        services.AddLogging();
        services.AddSingleton(Substitute.For<INavigationService>());
        services.AddSingleton(Substitute.For<IDialogService>());
        services.AddSingleton(Substitute.For<IExternalBrowser>());
        services.AddSingleton<ILocalizationService>(new FakeLocalizationService());
        services.AddSingleton<ISettingsService>(new FakeSettingsService());
        services.AddSingleton(Substitute.For<IPlayerRosterRepository>());
        services.AddSingleton(Substitute.For<IGameEngine>());
        services.AddSingleton(Substitute.For<IEventPackService>());
        services.AddSingleton(Substitute.For<IAnnouncementBuilder>());
        services.AddSingleton<ITextToSpeechService>(new FakeTextToSpeechService());
        services.AddSingleton(Substitute.For<IAnnouncementSpeaker>());
        services.AddSingleton<ISpeechRecognitionService>(new FakeSpeechRecognitionService());
        services.AddSingleton<IAudioCueService>(new FakeAudioCueService());
        services.AddSingleton<IGameFeedback>(new FakeGameFeedback());
        services.AddSingleton(Substitute.For<IUserDataService>());
        services.AddSingleton<IVoiceControlService>(new FakeVoiceControlService());
        services.AddSingleton(Substitute.For<IVoiceControlCoordinator>());
        services.AddSingleton(Substitute.For<IAdCoordinator>());
        services.AddSingleton(Substitute.For<IGameModeService>());
        services.AddSingleton<IUiDispatcher>(new ImmediateUiDispatcher());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(TimeProvider.System);
        services.AddPresentation();

        return services.BuildServiceProvider(validateScopes: true);
    }
}
