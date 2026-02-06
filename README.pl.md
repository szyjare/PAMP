<p align="center">
  <img src="PAMP/pamp_logo_min.png" alt="Logo PAMP" width="200" />
</p>

# PAMP! - Portable Apache MariaDB PHP Stack
[![en](https://img.shields.io/badge/lang-en-green.svg)](README.md)
[![pl](https://img.shields.io/badge/lang-pl-red.svg)](README.pl.md)

> 🇬🇧 **English version:** Click here to see the [English documentation](README.md).
---
**PAMP!** to lekkie, przenośne środowisko serwerowe dla systemu Windows, napisane w C# (WPF / .NET 8). Umożliwia uruchomienie lokalnego serwera WWW w kilka sekund, bez konieczności instalacji czy skomplikowanej konfiguracji.

> **Obecna wersja:** `1.0.0`
> Obecna wersja wspiera tylko polską wersję językową która jest wpisana na sztywno w kod aplikacji. Planuję nad zmianątego, aby ułątwić tłumaczenie na inne języki.

## ❓ Dlaczego kolejny stack?
Chociaż XAMPP jest obecnie najpopularniejszym rozwiązaniem, posiada kilka wad architektonicznych, które PAMP stara się rozwiązać:

- **Brak izolacji użytkowników** - W XAMPP wszyscy użytkownicy systemu dzielą ten sam katalog `htdocs` i bazę danych. PAMP zapewnia każdemu użytkownikowi własne, odizolowane środowisko.
- **Niestabilność bazy danych** - Użytkownicy XAMPP często napotykają błędy uszkodzenia bazy danych, które uniemożliwiają start serwera. PAMP stawia na bezpieczne procedury zamykania procesów, aby chronić integralność danych.
- **Problemy z zarządzaniem procesami** - Znany problem w istniejących rozwiązaniach to niemożność poprawnego zamknięcia aplikacji, co często wymaga wymuszania zamknięcia przez Menedżer Zadań. PAMP niezawodnie zarządza procesami w tle.
- **Przyjazny dla edukacji** - Idealne rozwiązanie do nauki zawodu informatyka i pracowni komputerowych, gdzie nauczyciele zmagają się z konserwacją XAMPP-a. PAMP oferuje stabilną alternatywę, która jest znacznie łatwiejsza do wdrażania, zarządzania i resetowania między lekcjami.

## ✨ Główne funkcje
* 🚀 **Przenośny i lekki** – Działa bez instalacji i nie wymaga uprawnień administratora.
* 🎨 **Minimalistyczny UI** – Czysty, czytelny interfejs skoncentrowany na funkcjonalności.
* 🛠️ **Wbudowane narzędzia** – Szybki dostęp do konsoli MySQL, "Factory Reset" (przywracanie ustawień fabrycznych) dla baz danych, podgląd aktywnych portów oraz czytelny podgląd logów Apache.
* 📦 **Izolacja użytkownika** – Pliki baz danych i konfiguracji są oddzielone od plików binarnych silnika. Każdy użytkownik Windows otrzymuje własne katalogi `LocalSites` (htdocs) i bazy danych, co eliminuje konflikty uprawnień.

## 🧱 Komponenty Stacku
Skompilowane wydania (Releases) zawierają:

| Komponent | Wersja |
| :--- | :--- |
| **PHP** | 8.2.12 |
| **MariaDB** | 10.4.32 |
| **phpMyAdmin** | 5.2.1 |
| **Apache** | 2.4.58 |

*Uwaga: Wersje komponentów dotyczą tylko wersji Portable oraz Instalatora.*

**Dlaczego starsze wersje modułów?**
Domyślny zestaw jest zgodny z **XAMPP 8.2.12** (PHP 8.2), co jest obecnie wymagane przez **CKE** (Centralną Komisję Egzaminacyjną) na egzaminach zawodowych **INF.03**. Zapewnia to pełną kompatybilność uczniom przygotowującym się do egzaminów. Wydania z najnowszymi wersjami modułów są planowane w przyszłości.

## 📂 Struktura katalogów

* **Strony WWW**
    Umieść swoje projekty w:
    `%UserProfile%\PAMP\LocalSites`
    *(Zazwyczaj: `C:\Użytkownicy\<nazwa_użytkownika>\PAMP\LocalSites`)*

* **Dane i Konfiguracja**
    Dane specyficzne dla użytkownika są przechowywane w `%LocalAppData%\PAMP`:
    * `conf` – Pliki konfiguracyjne dla Apache i MariaDB.
    * `logs` – Logi błędów i dostępu Apache oraz MariaDB.
    * `mysql_data` – Pliki bazy danych użytkownika.

## 💻 Wymagania systemowe
* Windows 10 / 11 (64-bit)
* .NET Desktop Runtime 8.0

## 🔮 Plany na przyszłość

Stale pracuję nad ulepszaniem PAMP. Oto funkcje, które planuję w przyszłych wydaniach:

- [ ] **Ikona w zasobniku systemowym (Tray):** Minimalizacja aplikacji do traya z szybkimi akcjami w menu kontekstowym (Start/Stop/Restart).
- [ ] **Instalator sieciowy:** Lekki instalator pobierający najnowsze wersje modułów podczas instalacji (zmniejszający początkowy rozmiar pliku).
- [ ] **Tryb ciemny:** Pełne wsparcie dla systemowego motywu ciemnego.
- [ ] **Menedżer SSL/HTTPS:** Generowanie certyfikatów self-signed dla localhost jednym kliknięciem.
- [ ] **Automatyczne aktualizacje:** Automatyczne sprawdzanie aktualizacji launchera PAMP.
- [ ] **Wersja na Linuxa:** Linux jest super, kochamy Linuxa!

Masz pomysł? [Otwórz zgłoszenie (Issue)](https://github.com/szyjare/PAMP/issues) i opowiedz mi o nim!

## ⚠️ Dla Programistów
**Ważne:** To repozytorium zawiera tylko kod źródłowy launchera PAMP!. **Nie zawiera** ono plików binarnych dla Apache, MariaDB, PHP ani phpMyAdmin, aby utrzymać rozsądny rozmiar repozytorium. Musisz pobrać je i umieścić ręcznie w katalogu `ServerFiles`, aby zbudować projekt.

## 📜 Licencja
PAMP! jest udostępniany na licencji **MIT**.
Dołączone moduły (Apache, PHP, MariaDB, phpMyAdmin) podlegają swoim własnym licencjom. Szczegóły znajdziesz w pliku `LICENSE.txt`.