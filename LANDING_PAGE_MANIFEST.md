# PAMP! — Specyfikacja i Manifest Produktu dla Landing Page

> Dokument przygotowany dla agenta / frontend developera odpowiedzialnego za zaprojektowanie i wdrożenie nowoczesnego Landing Page dla aplikacji **PAMP! (Personal Apache MariaDB PHP)**.

---

## 1. Tożsamość Marki i Pozycjonowanie (Brand Identity)

- **Nazwa produktu:** PAMP! (akronim od *Personal Apache MariaDB PHP*)
- **Autor / Twórca:** Szymon Jarecki
- **Model dystrybucji:** 100% Darmowy, Open Source (Licencja MIT)
- **Repozytorium GitHub:** [https://github.com/szyjare/PAMP](https://github.com/szyjare/PAMP)
- **Główny slogan (Hero Headline):**  
  *Nowoczesne, lekkie i natywne środowisko LAMP dla systemu Windows.*
- **Podtytuł (Subheadline):**  
  *Pożegnaj przestarzałe narzędzia z ery Windows XP. PAMP łączy wydajność Apache, elastyczność wielu wersji PHP oraz niezawodność MariaDB w eleganckim, natywnym interfejsie Windows 11.*

---

## 2. Główny Problem Rynku i Misja PAMP

Przez ponad dwie dekady programiści PHP oraz uczniowie szkół technicznych na systemie Windows byli skazani na oprogramowanie takie jak XAMPP czy WampServer:
- Interfejsy graficzne nierozwijane od 15 lat, brak skalowania DPI, brak Dark Mode.
- Wielkie, ociężałe instalatory (instalujące na siłę Tomcat, FileZilla, Perl i masę niepotrzebnych pakietów).
- Wymuszanie uprawnień administratora przy każdej operacji i konflikt uprawnień w katalogu `Program Files`.
- Częste uszkodzenia baz danych przy nagłym wyłączaniu komputera.
- Brak oficjalnego, gotowego profilu dla szkół i egzaminów zawodowych (INF.03 / EE.09).

**PAMP rozwiązuje te problemy od podstaw.** Jest to lekka, minimalistyczna, błyskawiczna aplikacja stworzona w najnowszym stosie technologicznym **.NET 10** z dbałością o detale wizualne Fluent Design / Mica.

---

## 3. Kluczowe Przewagi Rynkowe (USPs — Unique Selling Points)

| Cecha | PAMP! | XAMPP | Laragon |
| :--- | :---: | :---: | :---: |
| **Natywny interfejs Windows 11** | ✅ Mica, Dark/Light mode, Fluent UI | ❌ Styl Windows 2000 | ⚠️ Klasyczny Win32 |
| **Rozmiar instalatora bazowego** | ✅ **~2.7 MB** (moduły na żądanie) | ❌ 150+ MB (bloatware) | ⚠️ 40–180 MB |
| **Profil egzaminacyjny CKE (INF.03 / EE.09)** | ✅ Gotowy profil 1-kliknięciem | ❌ Brak | ❌ Brak |
| **Zaawansowany czytnik logów (Log Viewer)** | ✅ Kolorowanie, filtry, WCAG AAA | ❌ Surowy Notatnik | ⚠️ Zwykły podgląd tekstowy |
| **Wsparcie dla kont bez praw Administratora** | ✅ Pełna izolacja per-user i Portable | ❌ Wymaga Admina | ⚠️ Wymaga Admina (UAC) |
| **Szybki SQL Shell** | ✅ 1-kliknięcie z auto-logowaniem | ⚠️ Wymaga wiersza poleceń | ✅ Menu podręczne |
| **Prawidłowe zatrzymywanie MariaDB (InnoDB safe)** | ✅ `mariadb-admin shutdown` | ⚠️ Częste błędy i korupcja tabel | ✅ Standardowe |
| **Nowoczesny Auto-Updater (GitHub Releases)** | ✅ Wbudowany, kanały Stable i Beta | ❌ Ręczne pobieranie nowej wersji | ❌ Ręczne |

---

## 4. Przegląd Kluczowych Funkcji (Feature Highlights)

### 🚀 1. Modułowy Menedżer Komponentów (Component Manager)
Użytkownik nie pobiera gigabajtów niepotrzebnego kodu. Sam decyduje, czego potrzebuje:
- **Wersje PHP:** Błyskawiczny wybór i przełączanie wersji PHP (8.1, 8.2, 8.3, 8.4) jednym kliknięciem.
- **Bazy danych:** MariaDB 10.x / 11.x.
- **Narzędzia bazodanowe:** phpMyAdmin instalowany na żądanie.
- **Profil CKE:** Jeden przycisk instaluje kompletny zestaw wymagany do nauki i egzaminów państwowych w technikach programistycznych i informatycznych w Polsce.

### 🎨 2. Natywny Design Windows 11 (Fluent & Mica)
- Obsługa przezroczystości systemowej **Mica Alt / DWM**.
- Pełna synchronizacja z motywem systemowym (automatyczny Dark Mode i Light Mode).
- Integracja z kolorem akcentu Windows użytkownika.
- Wyraźna typografia konsolowa Cascadia Mono / Consolas w narzędziach deweloperskich.

### 📊 3. Inteligentna Przeglądarka Logów (Log Viewer)
- Semantyczne parsowanie logów Apache (`access.log`, `error.log`) oraz MariaDB w czasie rzeczywistym.
- **Kolorowanie składni:**
  - Metody HTTP (GET na błękitno, POST na zielono, DELETE na czerwono).
  - Kody odpowiedzi (2xx zielony, 3xx cyan, 4xx bursztyn, 5xx czerwień).
  - Tagi MariaDB `[Note]`, `[Warning]`, `[Error]` z wysokim kontrastem WCAG AAA.
- **Filtry i wyszukiwarka live:** Natychmiastowe filtrowanie według poziomów błędu i wyszukiwanie fraz z buforowaniem (debounce 180 ms).
- **Przyjazność dla zasobów:** Zoptymalizowane buforowanie do 1500 najświeższych wpisów – zero przycięć interfejsu nawet przy wielomegowych plikach logów.

### 💻 4. Jedno Kliknięcie do Wszystkiego
- **Katalog stron (`LocalSites`):** Błyskawiczne przejście do folderu lokalnych witryn w profilu użytkownika (`~/PAMP/LocalSites`).
- **Przeglądarka (`localhost` / `phpMyAdmin`):** Natychmiastowe otwarcie w domyślnej przeglądarce internetowej.
- **SQL Shell:** Uruchomienie natywnego klienta `mysql.exe` z automatycznie skonfigurowanym połączeniem lokalnym bez konieczności pamiętania parametrów konsoli.

### 🔒 5. Niezawodność i Izolacja Danych
- Wersja Portable i instalacja per-user nie wymagają uprawnień administratora.
- Baza danych przechowywana bezpiecznie w katalogu użytkownika – odinstalowanie aplikacji nie niszczy Twoich baz danych ani kodu projektów.
- Prawidłowe sekwencyjne zatrzymywanie procesów: zrzut buforów MariaDB zapobiega uszkodzeniom bazy przy wyłączaniu aplikacji.

---

## 5. Dostępne Warianty Pobierania (Download Options)

Na stronie docelowej powinny znaleźć się 3 główne przyciski / karty pobierania:

1. **Instalator Standardowy (`PAMP-Setup-x.x.x.exe`, ~2.7 MB)** *(Rekomendowany)*
   - Ultra-lekki instalator Windows.
   - Jeśli w systemie nie ma Microsoft .NET 10 Desktop Runtime, instalator automatycznie go pobierze i zainstaluje w tle.
2. **Instalator Standalone (`PAMP-Setup-x.x.x-standalone.exe`, ~44 MB)**
   - Wersja w pełni autonomiczna, zawierająca zintegrowane środowisko uruchomieniowe .NET 10.
   - Idealna na komputery szkolne lub stacje robocze z ograniczonym dostępem do Internetu.
3. **Wersja Przenośna Portable (`PAMP-vX.X.X-portable.zip`)**
   - Wypakuj i pracuj bez żadnej instalacji w systemie.

---

## 6. Sugerowana Architektura Strony Landing Page

Agent tworzący stronę powinien zaimplementować następujące sekcje:

1. **Header / Navbar:**
   - Logo PAMP!, odnośniki do sekcji: *Funkcje*, *Dlaczego PAMP?*, *Profil CKE*, *Dokumentacja*, *Pobierz*, oraz przycisk *GitHub* (z licznikiem gwiazdek / linkiem do repo).
2. **Hero Section:**
   - Mocny, techniczny nagłówek.
   - Dwa przyciski CTA: „Pobierz PAMP dla Windows” (Primary) i „Zobacz na GitHubie” (Secondary).
   - Efektowna prezentacja aplikacji: Interaktywny zrzut ekranu lub makieta okna PAMP z przełącznikiem motywu Dark / Light.
   - Odznaki: `.NET 10`, `Windows 11 Ready`, `Open Source MIT`, `Zero Bloatware`.
3. **Social Proof & Target Audience:**
   - Dedykowany banner: *„Zaprojektowany z myślą o programistach, studentach i uczniach techników (egzaminy INF.03 / EE.09)”*.
4. **Interactive Feature Grid (Kluczowe możliwości):**
   - Karta 1: *Wielowersyjność PHP* – zmiana wersji w 2 sekundy.
   - Karta 2: *Nowoczesny Log Viewer* – koniec ze ślepym szukaniem błędów w Notatniku.
   - Karta 3: *SQL Shell & phpMyAdmin* – pełna kontrola nad danymi.
   - Karta 4: *Zero Uprawnień Administratora* – działa w szkole, w domu i na uczelni.
5. **Tabela Porównawcza (Comparison Matrix):**
   - Przejrzyste zestawienie PAMP vs XAMPP vs Laragon vs WAMP.
6. **Profil Szkolny CKE:**
   - Dedykowana podsekcja wyjaśniająca, jak jedno kliknięcie konfiguruje idealne środowisko pod egzaminy państwowe.
7. **Pobieranie (Download Center):**
   - 3 kafelki: Instalator Standard, Instalator Standalone, Portable ZIP.
   - Wymagania systemowe: Windows 10 (1809+) lub Windows 11 (zalecany), architektura x64.
8. **Footer:**
   - Link do repozytorium GitHub, licencji MIT, zgłaszania błędów (Issues) oraz profilu autora.

---

## 7. Wskazówki Wizualne i UI/UX dla Agenta

- **Stylistyka:** Nowoczesny, minimalistyczny Glassmorphism / Fluent UI inspirowany Windows 11 i serwisami takimi jak Raycast, Vercel, Linear czy Supabase.
- **Paleta barw:**
  - Tło ciemne: `#0F172A` (Slate 900) lub `#18181B` (Zinc 900).
  - Tło jasne (opcjonalny toggle): `#F8FAFC` (Slate 50).
  - Akcent główny: Nowoczesny lazur/błękit `#0284C7` (Sky 600) lub `#38BDF8`.
  - Akcent dodatkowy (statusy / sukces): Szmaragdowa zieleń `#10B981` (uruchomiony serwer).
- **Czcionki:** Inter, Geist Sans lub Segoe UI Variable dla tekstu; Cascadia Code lub JetBrains Mono dla kodu i logów.
