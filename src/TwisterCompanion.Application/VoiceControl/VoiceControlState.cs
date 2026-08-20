namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Stan sterowania głosem, pokazywany graczom na ekranie rozgrywki.
/// </summary>
public enum VoiceControlState
{
    /// <summary>Wyłączone w ustawieniach albo bez zgody na mikrofon.</summary>
    Disabled,

    /// <summary>Włączone, ale w tej chwili nie nasłuchuje — trwa odczyt albo przerwa na ruch.</summary>
    Idle,

    /// <summary>Mikrofon słucha, komenda zostanie usłyszana.</summary>
    Listening,

    /// <summary>Przerwa między sesjami nasłuchu.</summary>
    Waiting,

    /// <summary>Urządzenie nie potrafi rozpoznawać mowy albo usługa odmawia obsługi.</summary>
    Unavailable,
}
