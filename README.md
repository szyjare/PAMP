<p align="center">
  <img src="PAMP/pamp_logo_min.png" alt="Logo PAMP" width="200" />

</p>

# PAMP! - Portable Apache MariaDB PHP Stack
[![en](https://img.shields.io/badge/lang-en-red.svg)](README.md)
[![pl](https://img.shields.io/badge/lang-pl-green.svg)](README.pl.md)

> 🇵🇱 **Polska wersja:** Kliknij tutaj, aby zobaczyć [dokumentację w języku polskim](README.pl.md).
---
<p align="center">
  <img width="566" height="403" alt="image" src="https://github.com/user-attachments/assets/b725e35a-a5e6-458c-926e-f388dabbb73b" />
</p>

**PAMP!** is a modern, lightweight, and portable server environment for Windows, built with C# 14 (.NET 10 WPF). It allows you to launch a local web server stack in seconds, with zero installation, full user isolation, and modular package management.

> **Current version:** `1.2.1`  
> Supported languages: **English** and **Polish** (selectable in Settings).

## ❓ Why create another stack?
While XAMPP is historically popular, it suffers from several critical architectural flaws that PAMP resolves:

- **Lack of User Isolation** - In XAMPP, all system users share the same `htdocs` directory and database files. PAMP assigns each Windows user their own isolated environment and databases.
- **Database Instability** - XAMPP users regularly face database corruption or blocked port conflicts. PAMP uses clean process lifecycle handling and volume-safe file swaps.
- **Safe Multi-Version Switching** - Upgrading or changing module versions in traditional stacks often risks wiping local databases. In PAMP, user databases in `%LocalAppData%\PAMP\mysql_data` remain 100% safe and intact when switching binaries.
- **Education & Vocational Exams** - Ideally suited for Polish vocational IT education (INF.03). Teachers and students can install the official **CKE Exam Profile** with a single click, or easily switch to bleeding-edge releases.

## ✨ Key Features
* 🚀 **Portable & Lightweight** – Runs without installation and requires no administrator privileges.
* 🎨 **Windows 11 Fluent UI** – Native Dark & Light themes, Windows 11 Mica backdrop material, system Accent Color integration, and refreshed icons.
* 🌐 **Built-in Component Manager** – Integrated package manager connecting to the official PAMP repository (`repo.sjarecki.pl`). Select, download, and switch versions of Apache, PHP, MariaDB, and phpMyAdmin on the fly.
* 🎓 **1-Click CKE Exam Profile** – Instant setup of an environment compliant with official CKE vocational exam guidelines.
* 🛠️ **Developer Tools** – Quick access to MariaDB/MySQL console, "Factory Reset" for user databases, live port conflict monitor, and parsed Apache log viewer.
* 📦 **User Data Isolation** – Binaries (`bin/`) are completely separated from user data (`LocalSites`) and database tables (`%LocalAppData%\PAMP\mysql_data`).

## 🧱 Stack Components & Repository
PAMP supports dynamic installation and swapping of component versions via the built-in **Component Manager** (`Tools -> Download & Change Versions`):

| Component | Available Versions | CKE Exam Compliant |
| :--- | :--- | :--- |
| **PHP** | 8.5.x, 8.2.x | Yes (8.2 compatible) |
| **MariaDB** | 12.3.x, 10.4.x | Yes (10.4 compatible) |
| **Apache** | 2.4.68, 2.4.58 | Yes (2.4 compatible) |
| **phpMyAdmin** | 5.2.3, 5.2.1 | Yes (5.2 compatible) |

*Versions are fetched directly from the PAMP repository and sorted semantically with the newest release at the top.*

## 📂 Directory Structure

* **Websites & HTML/PHP Projects**
    `%UserProfile%\PAMP\LocalSites`  
    *(Usually: `C:\Users\<username>\PAMP\LocalSites`)*

* **User Data & Configuration**
    Stored in `%LocalAppData%\PAMP`:
    * `conf` – Configuration files for Apache (`httpd_user.conf`) and MariaDB (`my_user.ini`).
    * `logs` – Apache and MariaDB error and access logs.
    * `mysql_data` – User database files (preserved across binary upgrades).
    * `installed_versions.json` – Active component version manifest.

## 💻 System Requirements
* Windows 10 / 11 (64-bit)
* .NET Desktop Runtime 10.0

## 🔮 Roadmap & Status

- [x] **Multi-language support:** Dynamic English & Polish interface.
- [x] **Dark & Light Mode:** Windows 11 Mica backdrop and system Accent Color.
- [x] **Component Manager:** Download and switch versions in real time from `repo.sjarecki.pl`.
- [x] **1-Click CKE Exam Profile:** One-click compliance with exam guidelines.
- [x] **First-run setup wizard:** Guided component selection on first startup.
- [ ] **System Tray Icon:** Minimize to system tray with context menu controls (Start/Stop/Restart).
- [ ] **SSL/HTTPS Manager:** One-click generation of self-signed certificates for local development.
- [ ] **Auto-Updater:** Automated updates for the PAMP launcher application.
- [ ] **Linux version:** Experimental cross-platform support.

Have an idea or feedback? [Open an issue](https://github.com/szyjare/PAMP/issues)!

## ⚠️ For Developers
To build PAMP from source:
1. Clone the repository: `git clone https://github.com/szyjare/PAMP.git`
2. Open in Visual Studio 2026 / Rider or build from terminal:
   ```bash
   dotnet build
   ```
3. Run the executable. On first launch, the Component Manager will automatically prompt you to download and configure your desired component versions.

## 📜 License
PAMP! is open-source software licensed under the **MIT License**.  
Downloaded components (Apache, PHP, MariaDB, phpMyAdmin) are subject to their respective open-source licenses (see `LICENSE.txt`).
