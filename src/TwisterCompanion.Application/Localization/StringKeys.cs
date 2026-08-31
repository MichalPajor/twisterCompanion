namespace TwisterCompanion.Application.Localization;

/// <summary>
/// Klucze zasobów używane z kodu C#.
/// </summary>
/// <remarks>
/// Klucze wpisywane w XAML zostają tam jako tekst — nie da się ich zastąpić stałą wewnątrz
/// rozszerzenia znaczników. Klucze używane z kodu trafiają tutaj, żeby literówka była
/// błędem kompilacji.
/// <para>
/// Test <c>StringKeysTests</c> sprawdza, że każda stała z tej klasy ma odpowiednik
/// w plikach zasobów — dodanie stałej bez tłumaczenia nie przejdzie.
/// </para>
/// </remarks>
public static class StringKeys
{
    /// <summary>Teksty wspólne dla wielu ekranów.</summary>
    public static class Common
    {
        /// <summary>Tytuł komunikatu o nieoczekiwanym błędzie.</summary>
        public const string ErrorTitle = "Common_Error_Title";

        /// <summary>Etykieta przycisku zamykającego komunikat.</summary>
        public const string ButtonOk = "Common_Button_Ok";

        /// <summary>Tytuł komunikatu informacyjnego — nie błędu.</summary>
        public const string InfoTitle = "Common_Info_Title";

        /// <summary>Etykieta przycisku anulowania.</summary>
        public const string ButtonCancel = "Common_Button_Cancel";

        /// <summary>Etykieta przycisku potwierdzenia w pytaniu zamkniętym.</summary>
        public const string ButtonYes = "Common_Button_Yes";

        /// <summary>Etykieta przycisku odmowy w pytaniu zamkniętym.</summary>
        public const string ButtonNo = "Common_Button_No";

        /// <summary>Wartość „włączone" w podsumowaniach ustawień.</summary>
        public const string LabelOn = "Common_Label_On";

        /// <summary>Wartość „wyłączone" w podsumowaniach ustawień.</summary>
        public const string LabelOff = "Common_Label_Off";
    }

    /// <summary>Teksty ekranu rozgrywki używane z kodu.</summary>
    public static class Game
    {
        /// <summary>Wzorzec informacji o numerze tury.</summary>
        public const string LabelTurn = "Game_Label_Turn";

        /// <summary>Informacja o wstrzymaniu rozgrywki.</summary>
        public const string LabelPaused = "Game_Label_Paused";

        /// <summary>Podpowiedź, że trzeba najpierw dodać graczy.</summary>
        public const string LabelAddPlayersFirst = "Game_Label_AddPlayersFirst";

        /// <summary>Wzorzec podsumowania partii ze zwycięzcą.</summary>
        public const string SummaryWinner = "Game_Summary_Winner";

        /// <summary>Wzorzec podsumowania partii bez zwycięzcy.</summary>
        public const string SummaryNoWinner = "Game_Summary_NoWinner";

        /// <summary>Stan mikrofonu: nasłuchuje.</summary>
        public const string VoiceListening = "Game_Label_VoiceListening";

        /// <summary>Stan mikrofonu: przerwa między sesjami nasłuchu.</summary>
        public const string VoiceWaiting = "Game_Label_VoiceWaiting";

        /// <summary>Stan mikrofonu: nie nasłuchuje, bo trwa odczyt albo czas na ruch.</summary>
        public const string VoiceIdle = "Game_Label_VoiceIdle";

        /// <summary>Stan mikrofonu: sterowanie głosem wyłączone.</summary>
        public const string VoiceDisabled = "Game_Label_VoiceDisabled";

        /// <summary>Stan mikrofonu: rozpoznawanie niedostępne.</summary>
        public const string VoiceUnavailable = "Game_Label_VoiceUnavailable";

        /// <summary>Wzorzec potwierdzenia rozpoznanej komendy.</summary>
        public const string VoiceCommandHeard = "Game_Label_VoiceCommandHeard";

        /// <summary>Podpis odliczania czasu na wykonanie zadania z wydarzenia.</summary>
        public const string CountdownTask = "Game_Label_CountdownTask";

        /// <summary>Podpis odliczania czasu na wykonanie ruchu.</summary>
        public const string CountdownMove = "Game_Label_CountdownMove";

        /// <summary>Nazwa ekranu przed partią, pokazywana w pasku górnym.</summary>
        public const string SetupTitle = "Game_Setup_Title";

        /// <summary>Podpis wiersza z trybem gry w podsumowaniu przed partią.</summary>
        public const string SetupMode = "Game_Setup_Mode";

        /// <summary>Podpis wiersza z wydarzeniami w podsumowaniu przed partią.</summary>
        public const string SetupEvents = "Game_Setup_Events";

        /// <summary>Informacja, że partia toczy się bez wydarzeń.</summary>
        public const string SetupNoEvents = "Game_Setup_NoEvents";

        /// <summary>Wzorzec opisu paczki wydarzeń: nazwa i liczba wydarzeń.</summary>
        public const string SetupEventPackFormat = "Game_Setup_EventPackFormat";

        /// <summary>Wzorzec wartości w sekundach.</summary>
        public const string SetupSecondsFormat = "Game_Setup_SecondsFormat";

        /// <summary>Podpis wiersza ze sposobem przechodzenia tur.</summary>
        public const string SetupTurnAdvance = "Game_Setup_TurnAdvance";

        /// <summary>Wartość: tury zmienia gracz.</summary>
        public const string SetupTurnManual = "Game_Setup_TurnManual";

        /// <summary>Wartość: tury zmieniają się same.</summary>
        public const string SetupTurnAutomatic = "Game_Setup_TurnAutomatic";

        /// <summary>Podpis wiersza z zasadą odpadania.</summary>
        public const string SetupElimination = "Game_Setup_Elimination";

        /// <summary>Wartość: gracz, który upadnie, odpada z gry.</summary>
        public const string SetupEliminationManual = "Game_Setup_EliminationManual";

        /// <summary>Wartość: nikt nie odpada.</summary>
        public const string SetupEliminationNone = "Game_Setup_EliminationNone";

        /// <summary>Podpis wiersza z liczbą rozegranych tur w podsumowaniu partii.</summary>
        public const string SummaryTurns = "Game_Summary_Turns";

        /// <summary>Podpis wiersza z czasem trwania partii.</summary>
        public const string SummaryDuration = "Game_Summary_Duration";

        /// <summary>Wzorzec czasu trwania partii: minuty i sekundy.</summary>
        public const string SummaryDurationFormat = "Game_Summary_DurationFormat";

        /// <summary>Podpis wiersza z kolejnością odpadania.</summary>
        public const string SummaryEliminated = "Game_Summary_Eliminated";

        /// <summary>Tytuł pytania o zakończenie partii.</summary>
        public const string EndConfirmTitle = "Game_Confirm_EndTitle";

        /// <summary>Treść pytania o zakończenie partii.</summary>
        public const string EndConfirmMessage = "Game_Confirm_EndMessage";

        /// <summary>Etykieta przycisku kończącego partię.</summary>
        public const string ButtonEnd = "Game_Button_End";
    }

    /// <summary>Teksty ekranu graczy używane z kodu.</summary>
    /// <summary>Klucze ekranu startowego.</summary>
    public static class Home
    {
        /// <summary>Tytuł pytania zadawanego, gdy skład graczy jest pusty.</summary>
        public const string NoPlayersTitle = "Home_Confirm_NoPlayersTitle";

        /// <summary>Treść pytania zadawanego, gdy skład graczy jest pusty.</summary>
        public const string NoPlayersMessage = "Home_Confirm_NoPlayersMessage";
    }

    public static class Players
    {
        /// <summary>Tytuł ekranu, używany też jako podpis w podsumowaniu partii.</summary>
        public const string Title = "Players_Title";

        /// <summary>Informacja, że gracz o takim imieniu już jest na liście.</summary>
        public const string DuplicateName = "Players_Label_DuplicateName";

        /// <summary>Tytuł pytania o usunięcie gracza.</summary>
        public const string DeleteConfirmTitle = "Players_Confirm_DeleteTitle";

        /// <summary>Wzorzec pytania o usunięcie gracza.</summary>
        public const string DeleteConfirmMessage = "Players_Confirm_DeleteMessage";

        /// <summary>Etykieta przycisku potwierdzającego usunięcie.</summary>
        public const string ButtonRemove = "Players_Button_Remove";
    }

    /// <summary>Teksty wprowadzenia „Jak grać".</summary>
    /// <remarks>
    /// Treść kroków jest w zasobach, a nie w XAML: kroki powstają w ViewModelu, bo ich liczba
    /// i kolejność są danymi, a nie układem ekranu.
    /// </remarks>
    public static class Onboarding
    {
        /// <summary>Tytuł kroku o przebiegu partii.</summary>
        public const string Step1Title = "Onboarding_Step1_Title";

        /// <summary>Treść kroku o przebiegu partii.</summary>
        public const string Step1Body = "Onboarding_Step1_Body";

        /// <summary>Tytuł kroku o sterowaniu głosem.</summary>
        public const string Step2Title = "Onboarding_Step2_Title";

        /// <summary>Treść kroku o sterowaniu głosem.</summary>
        public const string Step2Body = "Onboarding_Step2_Body";

        /// <summary>Tytuł kroku o wydarzeniach i trybach.</summary>
        public const string Step3Title = "Onboarding_Step3_Title";

        /// <summary>Treść kroku o wydarzeniach i trybach.</summary>
        public const string Step3Body = "Onboarding_Step3_Body";

        /// <summary>Wzorzec informacji „krok z ilu".</summary>
        public const string ProgressFormat = "Onboarding_Label_ProgressFormat";
    }

    /// <summary>Teksty ekranu zasad używane z kodu.</summary>
    public static class Rules
    {
        /// <summary>Informacja o braku opisu zasad dla trybu.</summary>
        public const string LabelMissing = "Rules_Label_Missing";
    }

    /// <summary>Teksty ekranu ustawień używane z kodu.</summary>
    public static class Settings
    {
        /// <summary>Nazwa pozycji „głos domyślny systemu" na liście głosów.</summary>
        public const string LabelSystemVoice = "Settings_Label_SystemVoice";

        /// <summary>
        /// Prefiks kluczy z nazwami motywów.
        /// </summary>
        /// <remarks>
        /// Klucz powstaje z nazwy wartości <c>AppThemePreference</c>, na przykład
        /// <c>Settings_Theme_Dark</c>.
        /// </remarks>
        public const string ThemePrefix = "Settings_Theme_";

        /// <summary>Nazwa ustawienia sterowania głosem.</summary>
        public const string LabelVoiceControl = "Settings_Label_VoiceControl";

        /// <summary>Informacja o braku zgody na mikrofon.</summary>
        public const string MicrophoneDenied = "Settings_Label_MicrophoneDenied";

        /// <summary>Informacja o braku rozpoznawania mowy na urządzeniu.</summary>
        public const string VoiceControlUnavailable = "Settings_Label_VoiceControlUnavailable";

        /// <summary>Informacja o wyłączeniu tur automatycznych przez sterowanie głosem.</summary>
        public const string AutomaticTurnsDisabledByVoice = "Settings_Info_AutomaticTurnsOff";

        /// <summary>Informacja o wyłączeniu sterowania głosem przez tury automatyczne.</summary>
        public const string VoiceControlDisabledByAutomaticTurns = "Settings_Info_VoiceControlOff";

        /// <summary>Tytuł pytania o przywrócenie ustawień domyślnych.</summary>
        public const string ResetConfirmTitle = "Settings_Confirm_ResetTitle";

        /// <summary>Treść pytania o przywrócenie ustawień domyślnych.</summary>
        public const string ResetConfirmMessage = "Settings_Confirm_ResetMessage";

        /// <summary>Etykieta przycisku przywracającego ustawienia domyślne.</summary>
        public const string ButtonReset = "Settings_Button_Reset";

        /// <summary>Tytuł pytania o usunięcie danych.</summary>
        public const string EraseConfirmTitle = "Settings_Confirm_EraseTitle";

        /// <summary>Treść pytania o usunięcie danych.</summary>
        public const string EraseConfirmMessage = "Settings_Confirm_EraseMessage";

        /// <summary>Etykieta przycisku usuwającego dane.</summary>
        public const string ButtonErase = "Settings_Button_Erase";

        /// <summary>Potwierdzenie, że dane zostały usunięte.</summary>
        public const string EraseDone = "Settings_Info_EraseDone";
    }

    /// <summary>Nazwy komend głosowych pokazywane graczom.</summary>
    /// <remarks>
    /// Osobne od fraz rozpoznawania: fraz jest kilka na komendę i są zapisane bez ogonków,
    /// a na ekranie pokazujemy jedną, poprawnie napisaną nazwę.
    /// </remarks>
    public static class VoiceCommands
    {
        /// <summary>Prefiks kluczy z nazwami komend.</summary>
        public const string NamePrefix = "Game_VoiceCommand_";
    }

    /// <summary>Teksty ekranu paczek wydarzeń używane z kodu.</summary>
    public static class EventPacks
    {
        /// <summary>Wzorzec nazwy kopii paczki.</summary>
        public const string CopyNameFormat = "EventPacks_Label_CopyNameFormat";

        /// <summary>Informacja, że paczki wbudowanej nie można zmieniać.</summary>
        public const string BuiltInReadOnly = "EventPacks_Label_BuiltInReadOnly";

        /// <summary>Tytuł pytania o usunięcie paczki.</summary>
        public const string DeleteConfirmTitle = "EventPacks_Confirm_DeleteTitle";

        /// <summary>Wzorzec pytania o usunięcie paczki.</summary>
        public const string DeleteConfirmMessage = "EventPacks_Confirm_DeleteMessage";

        /// <summary>Etykieta przycisku potwierdzającego usunięcie.</summary>
        public const string ButtonDelete = "EventPacks_Button_Delete";

        /// <summary>Wzorzec podsumowania sumy szans.</summary>
        public const string TotalChanceFormat = "EventPacks_Label_TotalChanceFormat";
    }

    /// <summary>
    /// Teksty przeznaczone do odczytu na głos — zbiór <c>StringCatalog.Voice</c>.
    /// </summary>
    /// <remarks>
    /// Nazwy części ciała i kolorów nie są tu wymienione, bo klucze buduje się z nazwy
    /// wartości wyliczeniowej (<c>Voice_BodyPart_</c> + <c>RightHand</c>). Kompletności
    /// tego zbioru pilnuje osobny test
    /// <c>KatalogGlosowy_ZawieraWszystkieCzesciCialaIKolory</c>.
    /// </remarks>
    public static class Voice
    {
        /// <summary>Wzorzec wywołania gracza, którego jest tura.</summary>
        public const string AnnouncePlayerTurn = "Voice_Announce_PlayerTurn";

        /// <summary>
        /// Wzorzec komunikatu o ruchu: część ciała, kolor.
        /// </summary>
        /// <remarks>
        /// Bez imienia gracza — pada ono osobno, wcześniej, żeby gracz wiedział, że to jego
        /// kolej, zanim usłyszy polecenie.
        /// </remarks>
        public const string AnnounceMove = "Voice_Announce_Move";

        /// <summary>
        /// Wzorzec komunikatu o wydarzeniu.
        /// </summary>
        /// <remarks>
        /// Brzmi „Wydarzenie: …", a nie „Następne wydarzenie: …". Wydarzenie dotyczy tury,
        /// która właśnie się rozgrywa, więc słowo „następne" wskazywałoby na kolejną turę
        /// i wprowadzało w błąd.
        /// </remarks>
        public const string AnnounceEvent = "Voice_Announce_Event";

        /// <summary>Informacja o rozpoczęciu gry.</summary>
        public const string AnnounceGameStart = "Voice_Announce_GameStart";

        /// <summary>Informacja o zakończeniu gry.</summary>
        public const string AnnounceGameEnd = "Voice_Announce_GameEnd";

        /// <summary>Wzorzec informacji o odpadnięciu gracza.</summary>
        public const string AnnouncePlayerEliminated = "Voice_Announce_PlayerEliminated";

        /// <summary>Wzorzec informacji o zwycięzcy.</summary>
        public const string AnnounceWinner = "Voice_Announce_Winner";

        /// <summary>Informacja o wstrzymaniu gry.</summary>
        public const string AnnouncePaused = "Voice_Announce_Paused";

        /// <summary>Informacja o wznowieniu gry.</summary>
        public const string AnnounceResumed = "Voice_Announce_Resumed";

        /// <summary>Zdanie odczytywane przy sprawdzaniu głosu w ustawieniach.</summary>
        /// <remarks>
        /// Ma formę polecenia ruchu, a nie neutralnego „test głosu": użytkownik ocenia głos
        /// pod kątem tego, jak brzmi w czasie gry, i musi usłyszeć dokładnie taki tekst.
        /// </remarks>
        public const string Sample = "Voice_Sample";

        /// <summary>
        /// Prefiks kluczy z frazami komend głosowych.
        /// </summary>
        /// <remarks>
        /// Klucz powstaje z nazwy wartości <c>VoiceCommandType</c>, na przykład
        /// <c>Voice_Command_Next</c>. Frazy w jednym wpisie rozdziela znak <c>|</c>,
        /// więc dołożenie synonimu jest zmianą w zasobach, nie w kodzie.
        /// </remarks>
        public const string CommandPrefix = "Voice_Command_";

        /// <summary>Prefiks kluczy z nazwami części ciała.</summary>
        public const string BodyPartPrefix = "Voice_BodyPart_";

        /// <summary>Prefiks kluczy z nazwami kolorów.</summary>
        public const string ColorPrefix = "Voice_Color_";
    }
}
