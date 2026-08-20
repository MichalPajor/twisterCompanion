using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ustawienie czasu w sekundach: pole do wpisania z przyciskami zmiany co pięć sekund.
/// </summary>
/// <remarks>
/// Zastępuje suwak, bo suwak nie pokazywał <b>ile właściwie ustawia</b> — a przy wartości
/// mierzonej w sekundach to jedyna informacja, która się liczy. Pole pozwala wpisać dokładną
/// liczbę, przyciski dają zmianę bez klawiatury.
/// <para>
/// Wartość jest wystawiona jako tekst, żeby dało się ją wpisywać. Tekst niebędący liczbą jest
/// ignorowany, a liczba poza zakresem przycinana i poprawiana w polu — użytkownik widzi wtedy,
/// co faktycznie zostało ustawione, zamiast wpisywać wartość, która cicho nie zadziała.
/// </para>
/// </remarks>
public partial class SecondsSetting : ObservableObject
{
    /// <summary>O ile sekund zmieniają wartość przyciski.</summary>
    public const int Step = 5;

    private readonly Action<TimeSpan> _onChanged;

    private bool _suppressNotifications;

    /// <summary>Tworzy ustawienie czasu.</summary>
    /// <param name="minimum">Najmniejsza dopuszczalna wartość.</param>
    /// <param name="maximum">Największa dopuszczalna wartość.</param>
    /// <param name="onChanged">Wywoływane po zmianie wartości przez użytkownika.</param>
    public SecondsSetting(TimeSpan minimum, TimeSpan maximum, Action<TimeSpan> onChanged)
    {
        ArgumentNullException.ThrowIfNull(onChanged);

        Minimum = (int)minimum.TotalSeconds;
        Maximum = (int)maximum.TotalSeconds;
        _onChanged = onChanged;
        _seconds = Minimum;
        _text = Minimum.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>Najmniejsza dopuszczalna liczba sekund.</summary>
    public int Minimum { get; }

    /// <summary>Największa dopuszczalna liczba sekund.</summary>
    public int Maximum { get; }

    /// <summary>Aktualna wartość w sekundach.</summary>
    public int Seconds
    {
        get => _seconds;
        private set => SetProperty(ref _seconds, value);
    }

    /// <summary>Wartość jako tekst — do wpisywania z klawiatury.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value) || _suppressNotifications)
            {
                return;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out int parsed))
            {
                // Pusty albo niepoprawny tekst zostawiamy w polu bez zmiany wartości:
                // użytkownik jest w trakcie wpisywania i kasowanie mu znaku pod palcem
                // byłoby walką z klawiaturą.
                return;
            }

            Apply(parsed);
        }
    }

    private int _seconds;
    private string _text;

    /// <summary>Ustawia wartość bez zgłaszania zmiany — przy wczytywaniu z ustawień.</summary>
    /// <param name="value">Wartość do pokazania.</param>
    public void Load(TimeSpan value)
    {
        _suppressNotifications = true;
        try
        {
            Seconds = Math.Clamp((int)value.TotalSeconds, Minimum, Maximum);
            Text = Seconds.ToString(CultureInfo.CurrentCulture);
        }
        finally
        {
            _suppressNotifications = false;
        }
    }

    /// <summary>Zwiększa wartość o krok.</summary>
    [RelayCommand]
    private void Increase() => Apply(Seconds + Step);

    /// <summary>Zmniejsza wartość o krok.</summary>
    [RelayCommand]
    private void Decrease() => Apply(Seconds - Step);

    private void Apply(int seconds)
    {
        int clamped = Math.Clamp(seconds, Minimum, Maximum);

        Seconds = clamped;

        // Tekst poprawiamy tylko wtedy, gdy różni się od wartości — inaczej wpisywanie
        // „1" w drodze do „15" cofałoby kursor.
        if (!string.Equals(_text, clamped.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal))
        {
            _suppressNotifications = true;
            try
            {
                Text = clamped.ToString(CultureInfo.CurrentCulture);
            }
            finally
            {
                _suppressNotifications = false;
            }
        }

        _onChanged(TimeSpan.FromSeconds(clamped));
    }
}
