<p align="center">
  <img src="PAMP/pamp_logo_min.png" alt="Logo PAMP" width="200" />
</p>

# PAMP! - Portable Apache MariaDB PHP Stack
[![en](https://img.shields.io/badge/lang-en-green.svg)](README.md)
[![pl](https://img.shields.io/badge/lang-pl-red.svg)](README.pl.md)

> 🇬🇧 **English version:** Click here to see the [English documentation](README.md).
---
**PAMP!** to nowoczesne, lekkie i w pełni przenośne środowisko serwerowe dla systemu Windows, napisane w C# 14 (.NET 10 WPF). Umożliwia uruchomienie lokalnego serwera WWW w kilka sekund, bez konieczności instalacji, z pełną izolacją użytkowników oraz modułowym zarządzaniem pakietami.

> **Obecna wersja:** `1.2.1`  
> Obsługiwane języki: **Polski** oraz **Angielski** (wybór w Ustawieniach programu).

## ❓ Dlaczego kolejny stack?
Chociaż XAMPP jest historycznie najpopularniejszym rozwiązaniem, posiada kilka poważnych wad architektonicznych, które PAMP skutecznie rozwiązuje:

- **Brak izolacji użytkowników** - W XAMPP wszyscy użytkownicy systemu Windows dzielą ten sam katalog `htdocs` i bazę danych. PAMP zapewnia każdemu użytkownikowi własne, odizolowane środowisko i bazy.
- **Niestabilność bazy danych** - Użytkownicy XAMPP często napotykają błędy uszkodzenia bazy danych lub zablokowane porty. PAMP dba o bezpieczne zamykanie procesów i bezkolizyjne procedury.
- **Bezpieczna zmiana wersji silników** - Zmiana lub aktualizacja wersji modułów w tradycyjnych stackach grozi utratą tabel. W PAMP bazy danych użytkownika w `%LocalAppData%\PAMP\mysql_data` pozostają w 100% nienaruszone i bezpieczne podczas podmiany binarek.
- **Przyjazny dla edukacji i egzaminów zawodowych** - Idealne rozwiązanie do nauki w technikach i szkołach branżowych (kwalifikacja **INF.03**). Uczniowie i nauczyciele mogą jednym kliknięciem zainstalować oficjalny **Profil Egzaminacyjny CKE** lub swobodnie przejść na najnowsze wersje deweloperskie.

## ✨ Główne funkcje
* 🚀 **Przenośny i lekki** – Działa bez instalacji i nie wymaga uprawnień administratora.
* 🎨 **Nowoczesny interfejs Windows 11** – Pełne wsparcie dla ciemnego i jasnego motywu, materiał Mica, integracja z kolorem wiodącym Windows (Accent Color) oraz odświeżone ikony.
* 🌐 **Wbudowany Menedżer Komponentów** – Wbudowany menedżer pobierania łączący się z oficjalnym repozytorium PAMP (`repo.sjarecki.pl`). Wybieraj, pobieraj i przełączaj wersje Apache, PHP, MariaDB oraz phpMyAdmin w locie.
* 🎓 **1-klikowy Profil Egzaminacyjny CKE** – Błyskawiczna konfiguracja środowiska w 100% zgodnego z aktualnymi wytycznymi egzaminacyjnymi CKE.
* 🛠️ **Narzędzia deweloperskie** – Szybki dostęp do konsoli MariaDB/MySQL, "Factory Reset" dla baz danych, monitor zajętych portów oraz czytelny podgląd logów Apache.
* 📦 **Izolacja danych** – Pliki silników (`bin/`) są odseparowane od plików stron (`LocalSites`) oraz danych baz (`%LocalAppData%\PAMP\mysql_data`).

## 🧱 Komponenty i Repozytorium Pakietów
PAMP umożliwia dynamiczną instalację i zmianę wersji komponentów poprzez wbudowany **Menedżer Komponentów** (`Narzędzia -> Pobierz i zmień wersje...`):

| Komponent | Dostępne wersje w repozytorium | Zgodność z CKE |
| :--- | :--- | :--- |
| **PHP** | 8.5.x, 8.2.x | Tak (zgodne z 8.2) |
| **MariaDB** | 12.3.x, 10.4.x | Tak (zgodne z 10.4) |
| **Apache** | 2.4.68, 2.4.58 | Tak (zgodne z 2.4) |
| **phpMyAdmin** | 5.2.3, 5.2.1 | Tak (zgodne z 5.2) |

*Wersje pobierane są bezpośrednio z repozytorium PAMP i sortowane semantycznie – najnowsze wydanie zawsze znajduje się na samej górze.*

## 📂 Struktura katalogów

* **Strony WWW (projekty PHP i HTML)**
    `%UserProfile%\PAMP\LocalSites`  
    *(Zazwyczaj: `C:\Użytkownicy\<nazwa_użytkownika>\PAMP\LocalSites`)*

* **Dane i Konfiguracja użytkownika**
    Przechowywane w `%LocalAppData%\PAMP`:
    * `conf` – Pliki konfiguracyjne dla Apache (`httpd_user.conf`) i MariaDB (`my_user.ini`).
    * `logs` – Logi błędów i dostępu Apache oraz MariaDB.
    * `mysql_data` – Pliki bazy danych użytkownika (nienaruszane podczas aktualizacji silnika).
    * `installed_versions.json` – Plik manifestu z aktualnie zainstalowanymi wersjami.

## 💻 Wymagania systemowe
* Windows 10 / 11 (64-bit)
* .NET Desktop Runtime 10.0

## 🔮 Plany i status rozwoju

- [x] **Wielojęzyczność:** Dynamiczny interfejs po polsku i po angielsku.
- [x] **Tryb ciemny i jasny:** Efekt Mica z Windows 11 oraz kolor wiodący systemu.
- [x] **Menedżer Komponentów:** Pobieranie i przełączanie wersji w locie z `repo.sjarecki.pl`.
- [x] **1-klikowy Profil Egzaminacyjny CKE:** Zgodność z wytycznymi egzaminacyjnymi za jednym kliknięciem.
- [x] **Kreator pierwszego uruchomienia:** Okno wyboru wersji przy pierwszym włączeniu programu.
- [ ] **Ikona w zasobniku systemowym (Tray):** Minimalizacja do traya z menu kontekstowym (Start/Stop/Restart).
- [ ] **Menedżer SSL/HTTPS:** Generowanie certyfikatów self-signed dla localhost jednym kliknięciem.
- [ ] **Automatyczne aktualizacje:** Automatyczne sprawdzanie nowych wydań launchera PAMP.
- [ ] **Wersja na Linuxa:** Eksperymentalne wsparcie wieloplatformowe.

Masz pomysł? [Otwórz zgłoszenie (Issue)](https://github.com/szyjare/PAMP/issues)!

## ⚠️ Dla Programistów
Aby skompilować PAMP ze źródeł:
1. Sklonuj repozytorium: `git clone https://github.com/szyjare/PAMP.git`
2. Otwórz w Visual Studio 2026 / Rider lub skompiluj z wiersza poleceń:
   ```bash
   dotnet build
   ```
3. Uruchom aplikację. Przy pierwszym uruchomieniu Menedżer Komponentów automatycznie zaproponuje pobranie i skonfigurowanie pożądanych wersji komponentów.

## 📜 Licencja
PAMP! jest udostępniany na licencji **MIT**.  
Pobierane komponenty (Apache, PHP, MariaDB, phpMyAdmin) podlegają swoim własnym licencjom open source (szczegóły w pliku `LICENSE.txt`).