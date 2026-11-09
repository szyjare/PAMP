<p align="center">
  <img src="PAMP/pamp_logo_min.png" alt="Logo PAMP" width="220" />
</p>

# PAMP! - Portable Apache MariaDB PHP Stack

[![version](https://img.shields.io/badge/wersja-1.3.0-blue.svg)](https://github.com/szyjare/PAMP/releases)
[![dotnet](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![platform](https://img.shields.io/badge/platforma-Windows%2010%20%7C%2011%20(x64)-lightgrey.svg)]()
[![license](https://img.shields.io/badge/licencja-MIT-green.svg)](LICENSE.txt)
[![en](https://img.shields.io/badge/lang-en-green.svg)](README.md)
[![pl](https://img.shields.io/badge/lang-pl-red.svg)](README.pl.md)

> 🇬🇧 **English version:** Click here to see the [English documentation](README.md).

---

<p align="center">
   <img width="566" height="403" alt="Okno główne PAMP" src="https://github.com/user-attachments/assets/0a2cfb60-cf3f-49fc-8bd4-339706d9b6ed" />
</p>

**PAMP!** to nowoczesne, lekkie i w pełni przenośne środowisko serwerowe dla systemu Windows, napisane w języku C# 14 (.NET 10 WPF). Umożliwia błyskawiczne uruchomienie lokalnego serwera WWW bez zbędnej konfiguracji, z pełną izolacją danych użytkowników, modułowym menedżerem pobierania pakietów oraz wbudowanym mechanizmem automatycznych aktualizacji.

> **Aktualna stabilna wersja:** `1.3.0`  
> Obsługiwane języki interfejsu: **polski**, **angielski** oraz **ukraiński** (wybór w Ustawieniach lub automatyczne dopasowanie do języka systemu).

---

## ❓ Dlaczego kolejny stack serwerowy?

Chociaż XAMPP jest historycznie najpopularniejszym rozwiązaniem, posiada kilka istotnych wad architektonicznych, które PAMP skutecznie rozwiązuje:

- **Brak izolacji użytkowników** – W tradycyjnych stackach wszyscy użytkownicy systemu Windows dzielą ten sam folder `htdocs` i pliki baz danych. PAMP w trybie instalacyjnym gwarantuje każdemu użytkownikowi Windows własne, odizolowane środowisko i bazy, a w trybie portable przechowuje wszystko lokalnie w jednym katalogu.
- **Niestabilność baz i blokowanie portów** – Użytkownicy XAMPP regularnie napotykają błędy uszkodzenia tabel MariaDB, zablokowane porty 80/3306 lub procesy-widma. PAMP dba o bezpieczne zamykanie procesów, czyste zwalnianie zasobów i automatyczną diagnostykę konfliktów portów.
- **Bezpieczna zmiana wersji silników** – Zmiana wersji modułów w innych stackach często wiąże się z ryzykiem utraty baz danych. W PAMP bazy danych użytkownika są bezpiecznie odseparowane i zachowywane podczas zmiany binarek.
- **Prawdziwy tryb Portable Single-Folder** – Możliwość przeniesienia całego środowiska, silników, baz danych i projektów stron na pendrive lub dysk przenośny USB, bez zapisywania jakichkolwiek plików w profilu Windows.
- **Idealny dla edukacji i egzaminów zawodowych (INF.03)** – Dedykowany dla techników i szkół branżowych. Uczniowie i nauczyciele mogą jednym kliknięciem zainstalować oficjalny **Profil Egzaminacyjny CKE** lub swobodnie korzystać z najnowszych wersji deweloperskich.

---

## ✨ Główne funkcje

* 🚀 **Przenośny i lekki** – Dostępny w wersji lekkiej (wymagającej .NET Desktop Runtime) oraz wersji Standalone (z wbudowanym środowiskiem .NET 10 – działa od razu na każdym komputerze bez instalacji czegokolwiek).
* 🔄 **Wbudowany automatyczny aktualizator** – Sprawdzanie aktualizacji z GitHub Releases API, dedykowane okno z listą zmian (changelog) oraz bezpieczna aktualizacja aplikacji w locie (zarówno dla instalatorów, jak i wersji Portable ZIP).
* 💾 **Prawdziwy tryb przenośny (Single-Folder)** – W wersji portable wszystkie pliki stron (`LocalSites/`), bazy danych (`data/mysql_data/`) oraz konfiguracja znajdują się wewnątrz folderu PAMP – idealne do pracy z pendrive'a.
* 🎨 **Nowoczesny interfejs Windows 11 Fluent UI** – Pełne wsparcie dla ciemnego i jasnego motywu, materiał Mica, integracja z kolorem wiodącym Windows (Accent Color) oraz czytelne ikony.
* 🌐 **Menedżer Komponentów** – Wbudowany instalator łączący się z oficjalnym repozytorium PAMP (`repo.sjarecki.pl`). Wybieraj, pobieraj i przełączaj wersje Apache, PHP, MariaDB oraz phpMyAdmin jednym kliknięciem.
* 🎓 **1-klikowy Profil Egzaminacyjny CKE i Profil Najnowszych wersji** – Błyskawiczna instalacja zestawu w 100% zgodnego z wytycznymi egzaminacyjnymi CKE lub najnowszego stabilnego stosu technologicznego.
* 🔍 **Zaawansowana przeglądarka logów** – Podgląd logów w czasie rzeczywistym z dwurzędowym paskiem narzędzi, filtrami poziomów (Wszystkie / Ostrzeżenia i błędy / Tylko błędy), wyszukiwarką fraz, autoscrollem oraz kontrastową paletą dla trybu jasnego i ciemnego.
* 🧹 **Narzędzia katalogu LocalSites i nowy szablon index.php** – Szybkie czyszczenie folderu stron oraz nowoczesny, w 100% anglojęzyczny, responsywny szablon `index.php` z wbudowanym podglądem konfiguracji `phpinfo()` i skrótem do phpMyAdmin.
* 🛠️ **Zestaw narzędzi deweloperskich** – Bezpośredni dostęp do SQL Shell, phpMyAdmin, podglądu localhost w przeglądarce oraz opcji czyszczenia bazy danych (Factory Reset).
* 🔒 **Instalacja bez uprawnień administratora** – Możliwość instalacji i zarządzania komponentami bez uprawnień administratora, gdy katalog aplikacji jest zapisywalny dla bieżącego użytkownika.

---

## 🧱 Komponenty i Repozytorium Pakietów

PAMP umożliwia dynamiczną instalację i zmianę wersji komponentów poprzez wbudowany **Menedżer Komponentów** (`Narzędzia -> Pobierz i zmień wersje...`):

| Komponent | Wersje w repozytorium | Zgodność z CKE |
| :--- | :--- | :--- |
| **PHP** | 8.5.x, 8.2.x | Tak (zgodne z 8.2) |
| **MariaDB** | 12.3.x, 10.4.x | Tak (zgodne z 10.4) |
| **Apache** | 2.4.68, 2.4.58 | Tak (zgodne z 2.4) |
| **phpMyAdmin** | 5.2.3, 5.2.1 | Tak (zgodne z 5.2) |

*Wersje pobierane są bezpośrednio z repozytorium PAMP i sortowane semantycznie – najnowsze wydanie zawsze znajduje się na samej górze.*

---

## 📂 Struktura katalogów

### Tryb standardowy (zainstalowany)
* **Pliki stron WWW i projektów:**  
  `%UserProfile%\PAMP\LocalSites` *(np. `C:\Users\<nazwa_użytkownika>\PAMP\LocalSites`)*
* **Dane i konfiguracja użytkownika:**  
  Przechowywane w `%LocalAppData%\PAMP`:
  * `conf/` – Generowane pliki konfiguracyjne Apache (`httpd_user.conf`) i MariaDB (`my_user.ini`).
  * `logs/` – Logi błędów i dostępu Apache oraz MariaDB.
  * `mysql_data/` – Pliki bazy danych użytkownika (nienaruszane podczas aktualizacji silników).
  * `installed_versions.json` – Manifest z aktualnie zainstalowanymi wersjami komponentów.

### Tryb przenośny (Portable Single-Folder na USB)
Gdy aplikacja znajduje się poza Program Files lub w katalogu znajduje się plik `portable.lock`:
* `<KatalogPAMP>\LocalSites\` – Projekty i strony WWW użytkownika.
* `<KatalogPAMP>\data\mysql_data\` – Bazy danych MariaDB.
* `<KatalogPAMP>\conf\` – Pliki konfiguracyjne.
* `<KatalogPAMP>\logs\` – Logi serwerów.

---

## 💻 Wymagania systemowe

* **System operacyjny:** Windows 10 (od wersji 1809) lub Windows 11 (64-bit x64)
* **Środowisko uruchomieniowe:**
  * *Wydanie standardowe:* .NET Desktop Runtime 10.0 (x64)
  * *Wydanie Standalone:* Brak wymagań (środowisko .NET 10 jest wbudowane w program)
* **Biblioteki systemowe:** Microsoft Visual C++ 2015–2022 Redistributable (x64) — PAMP wykrywa brak bibliotek i oferuje ich pobranie.

---

## 🔮 Plany i status rozwoju

- [x] **Wielojęzyczność:** Dynamiczny interfejs po polsku, angielsku i ukraińsku z autowykrywaniem języka systemu.
- [x] **Tryb ciemny i jasny:** Efekt Mica z Windows 11 oraz integracja z kolorem wiodącym systemu.
- [x] **Menedżer Komponentów:** Pobieranie i przełączanie wersji w locie z repozytorium `repo.sjarecki.pl`.
- [x] **1-klikowy Profil Egzaminacyjny CKE i Profil Najnowszych:** Szybkie przygotowanie środowiska do egzaminów zawodowych i nowoczesnego developmentu.
- [x] **Wbudowany Auto-Updater:** Automatyczne pobieranie aktualizacji z GitHub Releases.
- [x] **Prawdziwy tryb Portable Single-Folder:** Zapisywanie baz i stron bezpośrednio w folderze PAMP na USB.
- [x] **Zaawansowana przeglądarka logów:** Wyszukiwanie fraz w czasie rzeczywistym, filtry poziomów i dwurzędowy układ paska narzędzi.
- [x] **Narzędzia katalogu LocalSites:** Nowoczesny, anglojęzyczny szablon startowy z podglądem `phpinfo()` i czyszczeniem folderu stron.
- [ ] **Minimalizacja do zasobnika systemowego (Tray):** Zwijanie do traya z menu kontekstowym (Start / Stop / Restart).
- [ ] **Menedżer lokalnego SSL/HTTPS:** Generowanie zaufanych certyfikatów self-signed dla localhost jednym kliknięciem.
- [ ] **Wersja na system Linux:** Eksperymentalna kompilacja wieloplatformowa.

Masz pomysł lub sugestię? [Otwórz zgłoszenie (Issue)](https://github.com/szyjare/PAMP/issues)!

---

## ⚠️ Kompilacja ze źródeł

1. Sklonuj repozytorium:
   ```bash
   git clone https://github.com/szyjare/PAMP.git
   cd PAMP/PAMP
   ```
2. Skompiluj za pomocą .NET SDK:
   ```bash
   dotnet build -c Release
   ```
3. Aby wygenerować wszystkie oficjalne paczki wydania (Portable ZIP + Instalatory Inno Setup):
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File ..\build_release.ps1 -All
   ```

---

## 📜 Licencja

PAMP! jest udostępniany na licencji **MIT**.  
Pobierane komponenty (Apache, PHP, MariaDB, phpMyAdmin) podlegają swoim własnym licencjom open source (szczegóły w pliku `LICENSE.txt`).
