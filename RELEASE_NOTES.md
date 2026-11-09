# PAMP v1.3.0 — Official Release Notes

> **Release Date:** November 9, 2026  
> **Tag:** `v1.3.0`  
> **Previous Stable Version:** `v1.2.2`

PAMP v1.3.0 is a major milestone release introducing an in-app automatic updater, remote component manager with 1-click CKE and latest stack profiles, true single-folder portable mode (ideal for USB drives), an advanced multi-filter log viewer, refreshed English index.php template, full Ukrainian language support, and dozens of UI, stability, and permission enhancements.

---

## 🌟 Highlights & Key New Features

### 🔄 In-App Auto-Updater
* **Automated Update Checking:** Periodically checks GitHub Releases API on startup, with configurable options to enable/disable or opt into pre-release (beta) versions.
* **Interactive Update Dialog:** Dedicated release notes modal with Markdown changelog formatting, real-time download progress bar, and megabyte counter.
* **In-Place Self-Updating:**
  * For **Installers:** Automatically downloads setup, stops Apache/MariaDB services, and launches the installer.
  * For **Portable ZIPs:** Automatically extracts and applies updates cleanly in place with automatic restart.
* **SemVer 2.0 Version Comparison:** Robust semantic version parser handling pre-release tags (`beta.1`, `beta.2`, `beta.3` vs stable).
* **CLI Testing Mode:** Added `--mock-update` flag to simulate updates during testing without publishing public releases.

### 🌐 Built-in Component Manager & Repository
* **Remote Catalog Integration:** Connects live to `repo.sjarecki.pl` with automatic local offline caching.
* **🎓 1-Click CKE Exam Profile:** Instant installation of the exact stack compliant with Polish vocational IT exam guidelines (INF.03: Apache 2.4.58, PHP 8.2.12, MariaDB 10.4.32, phpMyAdmin 5.2.1).
* **🚀 1-Click Latest Profile:** Instant installation of the latest stable stack releases (Apache 2.4.68, PHP 8.5.x, MariaDB 12.3.x, phpMyAdmin 5.2.3).
* **Version Mixing & MariaDB Downgrade Protection:** Warnings alerting users when combining incompatible versions or downgrading MariaDB engines (protecting InnoDB tables).
* **Smart Non-Admin Support:** Allows component installation and swapping without administrator privileges whenever the PAMP directory is user-writable (e.g. per-user setup or portable folder).
* **Elevated Restart Integration:** Automatically and gracefully stops running Apache/MariaDB services before restarting as administrator to prevent zombie processes.
* **Full Multi-Language Progress Reporting:** Download percentages, megabytes, SHA-256 hash checks, archive extraction, and bin installation are fully localized in English, Polish, and Ukrainian.

### 💾 True Single-Folder Portable Mode (USB-Ready)
* **Centralized Path Architecture (`AppPaths`):** Automatically detects portable environments (running outside Program Files or presence of `portable.lock`).
* **Zero Host Pollution:** All user files, databases (`data/mysql_data/`), web projects (`LocalSites/`), configs (`conf/`), and logs (`logs/`) stay strictly inside the application folder on your USB flash drive or portable SSD without touching `%LocalAppData%` or `%UserProfile%`.
* **Standardized Portable Naming:**
  * `PAMP-v1.3.0-win-x64-portable.zip` (lightweight, framework-dependent)
  * `PAMP-v1.3.0-win-x64-portable-standalone.zip` (self-contained, bundles .NET 10 runtime)

### 🔍 Advanced Log Viewer
* **Dual-Row Toolbar:** Separates filters (Log file, Level, Live Search) from actions (Autoscroll, Clear view, Open in editor, Refresh now) preventing window over-stretching.
* **Live Search & Level Filtering:** Real-time search query filtering with debounce; view All levels, Warnings & Errors, or Errors only.
* **Optimized High-Contrast Syntax Themes:** Completely overhauled semantic palettes for both Dark and Light modes. Light mode features dark slate text on white background with clear, high-contrast badges for errors, notices, dates, and HTTP verbs.
* **Performance & Stability:** Fixed startup layout issues and optimized rendering of large log files.

### 🧹 LocalSites Tools & Modern index.php Template
* **Clear Web Directory Tool:** Added `Tools -> Clear web directory (LocalSites)` with confirmation dialog to safely wipe old project files and regenerate a clean `index.php`.
* **100% English Default Template:** Removed legacy Polish text and bilingual labels for a clean, professional international appearance.
* **Always-Visible PHP Info:** Embedded live `phpinfo()` directly into the second card without requiring click-to-expand or page reloads.
* **Widened Layout (1100px):** Expanded cards from 680px to 1100px with custom responsive CSS table styling, preventing cramped directive columns in light and dark themes.
* **Dynamic DocumentRoot:** Uses PHP `__DIR__` so that projects moved across USB drives or paths always display the accurate current path.

### 🎨 UI, Localization & Fixes
* **Ukrainian Language Support:** Added full Ukrainian interface translations (`Lang/uk.json`) with automatic Windows system language detection.
* **Instant Window Hide on Close:** Clicking the "X" button now hides the window instantly, while stopping server background processes gracefully without UI freeze.
* **Prevent Dialog Minimize Drops:** Fixed an issue where the main window dropped behind other windows upon closing dialogs.
* **Visual Polish:** Centered the "OK" button in the About window and restored native Windows 11 Fluent disabled button styling.
* **Process Tree Termination:** Enhanced process killing to terminate child Apache and PHP worker processes cleanly.
* **Visual C++ Redistributable Helper:** Detects missing VC++ 2015-2022 runtimes on startup and prompts with direct download link.

---

## 📦 Release Artifacts

| File Name | Distribution Type | Target Audience |
| :--- | :--- | :--- |
| `PAMP-Setup-1.3.0.exe` | Inno Setup Installer | Lightweight installer (requires .NET Desktop Runtime 10) |
| `PAMP-Setup-1.3.0-standalone.exe` | Inno Setup Installer | Self-contained installer (embedded .NET 10 runtime) |
| `PAMP-v1.3.0-win-x64-portable.zip` | Portable ZIP Archive | Lightweight portable archive (.NET Desktop Runtime 10 required) |
| `PAMP-v1.3.0-win-x64-portable-standalone.zip` | Portable ZIP Archive | USB-ready standalone portable archive (zero dependencies) |

---

## 🇵🇱 Polskie podsumowanie wydania (dla użytkowników z Polski)

Wersja **PAMP v1.3.0** to największa dotychczasowa aktualizacja środowiska:
1. **Wbudowany automatyczny aktualizator:** Samodzielna aktualizacja aplikacji w locie z poziomu okna programu (dla instalatorów i wydań portable).
2. **Menedżer Komponentów:** 1-klikowa instalacja oficjalnego **Profilu Egzaminacyjnego CKE** (INF.03) oraz profilu najnowszych wersji, z zabezpieczeniem baz MariaDB i ostrzeżeniami o uprawnieniach.
3. **Prawdziwy tryb Portable (Single-Folder):** Trzymaj bazy danych, strony i pliki w jednym folderze na pendrive – bez śmiecenia w folderach użytkownika Windows.
4. **Zaawansowana przeglądarka logów:** Dwurzędowy pasek narzędzi, wyszukiwarka fraz na żywo, filtry błędów/ostrzeżeń i wysoki kontrast w trybie jasnym oraz ciemnym.
5. **Narzędzia LocalSites:** Opcja czyszczenia katalogu stron oraz nowoczesny, poszerzony do 1100px szablon `index.php` w języku angielskim ze stałym podglądem `phpinfo()`.
6. **Wsparcie dla języka ukraińskiego:** Kompletne tłumaczenie aplikacji na język ukraiński z autodetekcją języka systemu.
7. **Szybkie zamykanie:** Natychmiastowe ukrywanie okna przyciskiem "X" i asynchroniczne zatrzymywanie usług w tle.
