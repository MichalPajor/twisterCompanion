// Alias dla typu Application z MAUI.
//
// Problem: pliki tego projektu leżą w namespace TwisterCompanion.App.*, a wyszukiwanie
// nazw przechodzi przez namespace nadrzędne. W TwisterCompanion istnieje namespace
// TwisterCompanion.Application (warstwa aplikacji), więc bez kwalifikacji nazwa
// "Application" wiąże się z NAMESPACE, a nie z typem Microsoft.Maui.Controls.Application.
// Efekt to komunikaty w rodzaju:
//     error CS0118: 'Application' is a namespace but is used like a type
//     error CS0234: The type or namespace name 'Current' does not exist
//                   in the namespace 'TwisterCompanion.Application'
//
// Kolizja pojawiła się w Etapie 3 — dopóki warstwa aplikacji nie zawierała żadnego typu,
// jej namespace nie istniał w metadanych i nic nie zasłaniał.
//
// Nazwa aliasu NIE może brzmieć MauiApplication: tak nazywa się androidowa klasa bazowa
// Microsoft.Maui.MauiApplication, po której dziedziczy Platforms/Android/MainApplication.
//
// W tym projekcie używamy MauiControlsApplication zamiast Application.
global using MauiControlsApplication = Microsoft.Maui.Controls.Application;
