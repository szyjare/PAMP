<p align="center">
  <img src="PAMP/pamp_logo_min.png" alt="Logo PAMP" width="200" />
</p>

# PAMP! - Portable Apache MariaDB PHP Stack
[![en](https://img.shields.io/badge/lang-en-red.svg)](README.md)
[![pl](https://img.shields.io/badge/lang-pl-green.svg)](README.pl.md)

> 🇵🇱 **Polska wersja:** Kliknij tutaj, aby zobaczyć [dokumentację w języku polskim](README.pl.md).
---
**PAMP!** is a lightweight, portable server environment for Windows, built with C# (WPF / .NET 8). It allows you to launch a local web server stack in seconds, with zero installation or complex configuration required.

> **Current version:** `1.0.0`
> Current version only supports polish language which is hardcoded. I'm planning on changing it for easy translation.

## ❓ Why create another stack?
While XAMPP is currently the most popular solution, it has several architectural flaws that PAMP aims to resolve:

- **Lack of User Isolation** - In XAMPP, all system users share the same `htdocs` directory and database. PAMP gives each user their own isolated environment.
- **Database Instability** - XAMPP users often encounter database corruption errors that prevent the server from starting. PAMP focuses on safe shutdown procedures to protect data integrity.
- **Process Management Issues** - A well-known issue in existing solutions is the inability to exit the application cleanly, often requiring forced termination via Task Manager. PAMP handles background processes reliably.
- **Education-Friendly** - Ideally suited for IT vocational training and computer labs where teachers struggle with XAMPP maintenance. PAMP offers a stable alternative that is significantly easier to deploy, manage, and reset between classes.

## ✨ Key Features
* 🚀 **Portable & Lightweight** – Runs without installation and requires no administrator privileges.
* 🎨 **Minimalist UI** – Clean, readable interface focused on functionality.
* 🛠️ **Built-in Tools** – Includes quick access to the MySQL console, "Factory Reset" for databases, active ports viewer, and a parsed Apache log viewer.
* 📦 **User Isolation** – Database and configuration files are separated from the engine binaries. Each Windows user gets their own `LocalSites` (htdocs) and database directories, eliminating multi-user permission conflicts.

## 🧱 Stack Components
The pre-compiled releases include:

| Component | Version |
| :--- | :--- |
| **PHP** | 8.2.12 |
| **MariaDB** | 10.4.32 |
| **phpMyAdmin** | 5.2.1 |
| **Apache** | 2.4.58 |

*Note: Component versions apply only to the portable and installer releases found in the [Releases] section.*

**Why older module versions?**
The default stack is aligned with **XAMPP 8.2.12** (PHP 8.2), which is currently required by **CKE** (Central Examination Board in Poland) for IT exams. This ensures full compatibility for students preparing for their vocational exams. Releases featuring the latest module versions are planned for the future.

## 📂 Directory Structure

* **Websites**
    Place your projects in:
    `%UserProfile%\PAMP\LocalSites`
    *(Usually: `C:\Users\<username>\PAMP\LocalSites`)*

* **Data & Config**
    User-specific data is stored in `%LocalAppData%\PAMP`:
    * `conf` – Configuration files for Apache and MariaDB.
    * `logs` – Apache and MariaDB error/access logs.
    * `mysql_data` – User's database files.

## 💻 System Requirements
* Windows 10 / 11 (64-bit)
* .NET Desktop Runtime 8.0

## 🔮 Roadmap & Future Plans

I'm constantly working to improve PAMP. Here are some features I'm planning for future releases:

- [ ] **System Tray Icon:** Minimize the app to the system tray with quick context menu actions (Start/Stop/Restart).
- [ ] **Web Installer:** A lightweight installer that downloads the latest module versions during setup (reducing the initial file size).
- [ ] **Dark Mode:** Full support for system dark theme.
- [ ] **SSL/HTTPS Manager:** One-click generation of self-signed certificates for local development.
- [ ] **Auto-Updater:** Automatic checks for PAMP launcher updates.
- [ ] **Linux version:** Linux good, we love Linux!

Have an idea? [Open an issue](https://github.com/szyjare/PAMP/issues) and tell me about it!

## ⚠️ For Developers
**Important:** This repository contains the source code for the PAMP! launcher only. It **does not** include the binaries for Apache, MariaDB, PHP, or phpMyAdmin to keep the repo size manageable. You must download and place them in the `ServerFiles` directory manually to build the project.


## 📜 License
PAMP! is licensed under the **MIT License**.
Bundled modules (Apache, PHP, MariaDB, phpMyAdmin) are subject to their respective licenses. See `LICENSE.txt` for details.