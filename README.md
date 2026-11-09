<p align="center">
  <img src="PAMP/pamp_logo_min.png" alt="Logo PAMP" width="220" />
</p>

# PAMP! - Portable Apache MariaDB PHP Stack

[![version](https://img.shields.io/badge/version-1.3.0-blue.svg)](https://github.com/szyjare/PAMP/releases)
[![dotnet](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011%20(x64)-lightgrey.svg)]()
[![license](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.txt)
[![en](https://img.shields.io/badge/lang-en-red.svg)](README.md)
[![pl](https://img.shields.io/badge/lang-pl-green.svg)](README.pl.md)

 🌐 **Website:** [pamp.sjarecki.pl](https://pamp.sjarecki.pl)

> 🇵🇱 **Polska wersja:** Kliknij tutaj, aby zobaczyć [dokumentację w języku polskim](README.pl.md).

---

<p align="center">
  <img width="576" height="458" alt="PAMP Main Window" src="https://github.com/user-attachments/assets/7ac1eee9-f0a0-4a62-90da-e705e6fe3437" />
</p>

**PAMP!** is a modern, lightweight, and truly portable server stack environment for Windows, built with C# 14 (.NET 10 WPF). It allows you to launch a local web server stack in seconds, with zero configuration headaches, full user isolation, modular package management, and an integrated in-app updater.

> **Current stable version:** `1.3.0`  
> Supported interface languages: **English**, **Polish**, and **Ukrainian** (selectable in Settings or auto-detected).

---

## ❓ Why create another stack?
While XAMPP has historically been a popular solution, it suffers from several critical architectural flaws that PAMP solves:

- **Lack of User Isolation** – In standard stacks, all Windows users share the exact same `htdocs` directory and database files. PAMP provides full isolation: standard installations assign each Windows user their own private databases and projects, while portable installations keep everything self-contained in a single folder.
- **Database Corruption & Port Lockups** – Users regularly face MariaDB/MySQL table corruptions, blocked port conflicts, or lingering background orphan processes. PAMP features clean process lifecycle management, graceful shutdown sequences, and automated port conflict diagnostics.
- **Safe Multi-Version Switching** – Upgrading or swapping component versions in traditional stacks risks wiping local databases. In PAMP, user databases are preserved intact when switching binaries.
- **True Portable Single-Folder Mode** – Carry your entire development environment, databases, and website files on a USB flash drive or portable SSD without leaving traces in Windows profile directories.
- **Education & Vocational Exams (INF.03)** – Tailor-made for vocational IT education in Poland. Teachers and students can install the official **CKE Exam Profile** with a single click, or switch to bleeding-edge releases at will.

---

## ✨ Key Features

* 🚀 **Portable & Lightweight** – Runs without installation. Available as a lightweight package (.NET runtime dependent) or standalone package (bundles .NET 10 runtime, zero dependencies).
* 🔄 **In-App Auto Updater** – Automated update checking via GitHub Releases API, interactive release notes dialog, and seamless self-updating for both Portable ZIP and Inno Setup installer distributions.
* 💾 **True Single-Folder Portable Mode** – When running portably, all binaries (`bin/`), web files (`LocalSites/`), and database files (`data/`) stay inside the application folder, making it 100% portable on external USB drives.
* 🎨 **Windows 11 Fluent UI** – Native Dark & Light themes, Windows 11 Mica backdrop material, system Accent Color integration, and high-contrast typography.
* 🌐 **Built-in Component Manager** – Integrated package manager connecting to the official PAMP repository (`repo.sjarecki.pl`). Select, download, and switch versions of Apache, PHP, MariaDB, and phpMyAdmin on the fly.
* 🎓 **1-Click CKE Exam Profile & Latest Profile** – Instant setup of an environment compliant with official CKE vocational exam guidelines, or 1-click update to latest stable stack.
* 🔍 **Advanced Log Viewer** – Real-time log monitoring with dual-row toolbar, log level filters (All / Warnings & Errors / Errors only), live search queries, autoscroll toggle, and dark/light optimized syntax themes.
* 🧹 **LocalSites Tools & Modern index.php** – Quick tools to wipe and refresh the web project folder with a modern, responsive, English-only default `index.php` featuring live PHP configuration (`phpinfo()`) and phpMyAdmin shortcuts.
* 🛠️ **Developer Toolset** – Quick access to SQL Shell, phpMyAdmin, localhost browser shortcuts, and database reset tools.
* 🔒 **Smart Non-Admin Support** – Install and change components without administrator privileges when the application directory is user-writable.

---

## 🧱 Stack Components & Repository

PAMP supports dynamic installation and swapping of component versions via the built-in **Component Manager** (`Tools -> Download & Change Versions...`):

| Component | Repository Versions | CKE Exam Compliant |
| :--- | :--- | :--- |
| **PHP** | 8.5.x, 8.2.x | Yes (8.2 compatible) |
| **MariaDB** | 12.3.x, 10.4.x | Yes (10.4 compatible) |
| **Apache** | 2.4.68, 2.4.58 | Yes (2.4 compatible) |
| **phpMyAdmin** | 5.2.3, 5.2.1 | Yes (5.2 compatible) |

*Component versions are fetched live from `repo.sjarecki.pl` and sorted semantically.*

---

## 📂 Directory Structure

### Standard Installed Mode
* **Websites & PHP/HTML Projects:**  
  `%UserProfile%\PAMP\LocalSites` *(e.g. `C:\Users\<username>\PAMP\LocalSites`)*
* **User Data & Configuration:**  
  Stored in `%LocalAppData%\PAMP`:
  * `conf/` – Generated configuration files for Apache (`httpd_user.conf`) and MariaDB (`my_user.ini`).
  * `logs/` – Apache and MariaDB error and access logs.
  * `mysql_data/` – User database tables (preserved across binary upgrades).
  * `installed_versions.json` – Version manifest of installed components.

### Portable Single-Folder Mode (USB-Ready)
When placed in a non-ProgramFiles directory or marked with `portable.lock`:
* `<AppDir>\LocalSites\` – Website files and projects.
* `<AppDir>\data\mysql_data\` – Portable MariaDB databases.
* `<AppDir>\conf\` – Configuration files.
* `<AppDir>\logs\` – Server log files.

---

## 💻 System Requirements

* **Operating System:** Windows 10 (version 1809+) or Windows 11 (64-bit x64)
* **Runtime:**
  * *Standard / Lightweight edition:* .NET Desktop Runtime 10.0 (x64)
  * *Standalone edition:* None required (embedded .NET 10 runtime)
* **Dependencies:** Microsoft Visual C++ 2015–2022 Redistributable (x64) — PAMP will prompt and guide you if missing.

---

## 🔮 Roadmap & Status

- [x] **Multi-language support:** Dynamic English, Polish, and Ukrainian interface with auto-detection.
- [x] **Dark & Light Mode:** Windows 11 Mica backdrop and system Accent Color.
- [x] **Component Manager:** Download and switch versions in real time from `repo.sjarecki.pl`.
- [x] **1-Click CKE Exam Profile & Latest Profile:** Quick environment setup for vocational exams and modern development.
- [x] **In-App Auto-Updater:** Automated updates via GitHub Releases API for both installers and portable archives.
- [x] **True Portable Single-Folder Mode:** Self-contained mode storing databases and websites in the app folder for USB drives.
- [x] **Advanced Log Viewer:** Real-time log monitoring with live search queries, level filtering, and dual-row layout.
- [x] **LocalSites Management:** Fresh English start template with live `phpinfo()` and directory wipe utility.
- [ ] **System Tray Minimization:** Minimize to system tray with context menu controls (Start / Stop / Restart).
- [ ] **Local SSL/HTTPS Manager:** One-click generation of trusted self-signed certificates for localhost development.
- [ ] **Cross-Platform Linux version:** Experimental Linux build.

Have an idea or feedback? [Open an issue](https://github.com/szyjare/PAMP/issues)!

---

## ⚠️ Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/szyjare/PAMP.git
   cd PAMP/PAMP
   ```
2. Build with .NET SDK:
   ```bash
   dotnet build -c Release
   ```
3. To package all release artifacts (Portable ZIPs + Inno Setup Installers):
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File ..\build_release.ps1 -All
   ```

---

## 📜 License

PAMP! is open-source software licensed under the **MIT License**.  
Downloaded components (Apache, PHP, MariaDB, phpMyAdmin) are subject to their respective open-source licenses (see `LICENSE.txt`).
