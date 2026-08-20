using System.Globalization;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Voice;

/// <summary>
/// Składa komunikaty z zasobów językowych.
/// </summary>
/// <remarks>
/// Nazwy części ciała i kolorów pochodzą z kluczy budowanych na podstawie nazw wartości
/// wyliczeniowych, na przykład <c>Voice_BodyPart_RightHand</c>. Dzięki temu dodanie koloru
/// albo części ciała nie wymaga zmiany tej klasy — wystarczy nowa wartość w wyliczeniu
/// i wpis w zasobach.
/// <para>
/// Cały komunikat o ruchu jest wzorcem formatowania z zasobów, a nie konkatenacją w kodzie.
/// To jedyny sposób, żeby język mógł zmienić kolejność członów albo interpunkcję.
/// </para>
/// </remarks>
internal sealed class AnnouncementBuilder(ILocalizationService localization) : IAnnouncementBuilder
{
    private readonly ILocalizationService _localization =
        localization ?? throw new ArgumentNullException(nameof(localization));

    /// <inheritdoc />
    public Announcement BuildPlayerTurn(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        string text = _localization.GetFormattedString(
            StringKeys.Voice.AnnouncePlayerTurn,
            StringCatalog.Voice,
            player.Name);

        return new Announcement(text, AnnouncementKind.PlayerTurn);
    }

    /// <inheritdoc />
    public Announcement BuildMove(Turn turn)
    {
        ArgumentNullException.ThrowIfNull(turn);

        string text = _localization.GetFormattedString(
            StringKeys.Voice.AnnounceMove,
            StringCatalog.Voice,
            GetBodyPartName(turn.Move.Part),
            GetColorName(turn.Move.Color));

        return new Announcement(text, AnnouncementKind.Move);
    }

    /// <inheritdoc />
    public Announcement BuildEvent(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        string text = _localization.GetFormattedString(
            StringKeys.Voice.AnnounceEvent,
            StringCatalog.Voice,
            GetEventName(gameEvent));

        return new Announcement(text, AnnouncementKind.Event);
    }

    /// <inheritdoc />
    public Announcement BuildVoiceSample() => new(
        _localization.GetString(StringKeys.Voice.Sample, StringCatalog.Voice),
        AnnouncementKind.VoiceSample);

    /// <inheritdoc />
    public Announcement BuildGameStart() => new(
        _localization.GetString(StringKeys.Voice.AnnounceGameStart, StringCatalog.Voice),
        AnnouncementKind.GameStart);

    /// <inheritdoc />
    public Announcement BuildGameEnd(Player? winner)
    {
        // Bez zwycięzcy — na przykład w trybie treningowym z jednym graczem — mówimy
        // tylko o zakończeniu, bez ogłaszania wygranego.
        string text = winner is null
            ? _localization.GetString(StringKeys.Voice.AnnounceGameEnd, StringCatalog.Voice)
            : _localization.GetFormattedString(
                StringKeys.Voice.AnnounceWinner,
                StringCatalog.Voice,
                winner.Name);

        return new Announcement(text, AnnouncementKind.GameEnd);
    }

    /// <inheritdoc />
    public Announcement BuildPlayerEliminated(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        string text = _localization.GetFormattedString(
            StringKeys.Voice.AnnouncePlayerEliminated,
            StringCatalog.Voice,
            player.Name);

        return new Announcement(text, AnnouncementKind.PlayerEliminated);
    }

    /// <inheritdoc />
    public Announcement BuildPaused() => new(
        _localization.GetString(StringKeys.Voice.AnnouncePaused, StringCatalog.Voice),
        AnnouncementKind.Paused);

    /// <inheritdoc />
    public Announcement BuildResumed() => new(
        _localization.GetString(StringKeys.Voice.AnnounceResumed, StringCatalog.Voice),
        AnnouncementKind.Resumed);

    /// <inheritdoc />
    public string GetEventName(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        if (gameEvent.CustomName is not null)
        {
            return gameEvent.CustomName;
        }

        return gameEvent.NameKey is null
            ? string.Empty
            : _localization[gameEvent.NameKey];
    }

    private string GetBodyPartName(BodyPart part) => _localization.GetString(
        StringKeys.Voice.BodyPartPrefix + part.ToString(),
        StringCatalog.Voice);

    private string GetColorName(SpinColor color) => _localization.GetString(
        string.Create(CultureInfo.InvariantCulture, $"{StringKeys.Voice.ColorPrefix}{color}"),
        StringCatalog.Voice);
}
