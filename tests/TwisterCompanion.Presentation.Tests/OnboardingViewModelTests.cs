using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy wprowadzenia „Jak grać".
/// </summary>
public class OnboardingViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly FakeSettingsService _settings = new();

    [Fact]
    public void Wprowadzenie_MaTrzyKroki()
    {
        // Trzy, bo tyle wymaga plan — i tyle mieści się w cierpliwości kogoś, kto chce zagrać.
        OnboardingViewModel viewModel = CreateViewModel();

        Assert.Equal(3, viewModel.Steps.Count);
        Assert.All(viewModel.Steps, step =>
        {
            Assert.NotEmpty(step.Title);
            Assert.NotEmpty(step.Body);
        });
    }

    [Fact]
    public void PierwszyKrok_NieMaDoCzegoWracac()
    {
        OnboardingViewModel viewModel = CreateViewModel();

        Assert.True(viewModel.IsFirstStep);
        Assert.False(viewModel.IsLastStep);
        Assert.Same(viewModel.Steps[0], viewModel.Current);
    }

    [Fact]
    public async Task Dalej_PrzechodziKrokiDoOstatniego()
    {
        OnboardingViewModel viewModel = CreateViewModel();

        await viewModel.NextCommand.ExecuteAsync(parameter: null);

        Assert.Same(viewModel.Steps[1], viewModel.Current);
        Assert.False(viewModel.IsFirstStep);

        await viewModel.NextCommand.ExecuteAsync(parameter: null);

        Assert.Same(viewModel.Steps[2], viewModel.Current);
        Assert.True(viewModel.IsLastStep);
    }

    [Fact]
    public async Task Wstecz_WracaDoPoprzedniegoKroku()
    {
        OnboardingViewModel viewModel = CreateViewModel();

        await viewModel.NextCommand.ExecuteAsync(parameter: null);
        viewModel.BackCommand.Execute(parameter: null);

        Assert.Same(viewModel.Steps[0], viewModel.Current);
    }

    [Fact]
    public void WsteczNaPierwszymKroku_NicNieRobi()
    {
        // Gest przesunięcia palcem trafia w tę komendę także na pierwszym kroku.
        OnboardingViewModel viewModel = CreateViewModel();

        viewModel.BackCommand.Execute(parameter: null);

        Assert.Same(viewModel.Steps[0], viewModel.Current);
    }

    [Fact]
    public async Task DalejNaOstatnimKroku_KonczyWprowadzenie()
    {
        OnboardingViewModel viewModel = CreateViewModel();

        await viewModel.NextCommand.ExecuteAsync(parameter: null);
        await viewModel.NextCommand.ExecuteAsync(parameter: null);
        await viewModel.NextCommand.ExecuteAsync(parameter: null);

        Assert.True(_settings.Current.HasSeenOnboarding);
        await _navigation.Received(1).GoBackAsync();
    }

    [Fact]
    public async Task Pominiecie_TezZapisujeZeWprowadzenieWidziano()
    {
        // Inaczej wprowadzenie wracałoby przy każdym uruchomieniu do kogoś, kto właśnie
        // powiedział, że go nie chce.
        OnboardingViewModel viewModel = CreateViewModel();

        await viewModel.FinishCommand.ExecuteAsync(parameter: null);

        Assert.True(_settings.Current.HasSeenOnboarding);
        await _navigation.Received(1).GoBackAsync();
    }

    [Fact]
    public void PostepPokazujeNumerKroku()
    {
        OnboardingViewModel viewModel = CreateViewModel();

        Assert.NotEmpty(viewModel.ProgressText);
    }

    private OnboardingViewModel CreateViewModel() => new(
        _navigation,
        _settings,
        NullLogger<OnboardingViewModel>.Instance,
        Substitute.For<IDialogService>(),
        new FakeLocalizationService());
}
