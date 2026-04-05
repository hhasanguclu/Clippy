<p align="center">
  <img src="clippy_hero_image_1772738956761.png" alt="Clippy" width="720" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/UI-Avalonia_11-8B5CF6?logo=data:image/svg+xml;base64," alt="Avalonia" />
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue" alt="Platforms" />
  <img src="https://img.shields.io/badge/license-MIT-green" alt="License" />
</p>

<h1 align="center">📋 Clippy</h1>
<p align="center">Cross-platform clipboard manager — fast, keyboard-first, privacy-focused.</p>

---

🇬🇧 [English](#english) · 🇹🇷 [Türkçe](#türkçe)

---

## English

Clippy is a lightweight clipboard history manager that runs in the system tray. Press `Ctrl+Shift+V` anywhere to search and paste from your clipboard history. Built with .NET 8 and Avalonia UI, it runs natively on Windows, Linux and macOS.

> All data is stored locally. Nothing leaves your machine.

### Features

- Clipboard history with text, HTML and image support
- Global hotkey (`Ctrl+Shift+V`) — works from any application
- Instant fuzzy search with keyboard navigation
- Pin important items to the top
- Image thumbnails in the history list
- Plain-text paste with `Shift+Enter`
- Pause capture / ignore next copy
- Auto-start with the operating system
- English and Turkish UI
- Acrylic blur popup with dark theme
- SQLite-backed persistent storage

### Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+Shift+V` | Toggle popup |
| `Enter` | Paste selected item |
| `Shift+Enter` | Paste as plain text |
| `Ctrl+P` | Pin / unpin |
| `Delete` | Delete item |
| `Esc` | Close popup |
| `↑` `↓` | Navigate |

### Requirements

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (build only)
- Windows 10+, Ubuntu 20.04+ / Fedora 38+, or macOS 12+

### Build & Run

```bash
git clone https://github.com/user/Clippy.git
cd Clippy
dotnet run --project Clippy/Clippy.csproj
```

### Publish

```bash
# Windows
publish-win.bat

# Linux
chmod +x publish-linux.sh && ./publish-linux.sh

# macOS (arm64 default, pass x64 for Intel)
chmod +x publish-macos.sh && ./publish-macos.sh
```

### Create Installers

```bash
# Windows — requires Inno Setup 6+ (https://jrsoftware.org/isdl.php)
# Open setup.iss in Inno Setup and compile.

# Linux — creates .tar.gz + .deb
chmod +x setup-linux.sh && ./setup-linux.sh

# macOS — creates .app bundle + .dmg
chmod +x setup-macos.sh && ./setup-macos.sh
```

### Project Structure

```
Clippy/
├── Program.cs                  # Entry point, Avalonia AppBuilder
├── App.axaml / App.axaml.cs    # Application lifecycle, tray icon, services
├── Assets/
│   └── TrayIconGenerator.cs    # Programmatic tray icon
├── Forms/
│   ├── PopupWindow.axaml(.cs)  # Clipboard history popup (acrylic blur)
│   └── SettingsWindow.axaml(.cs)
├── Models/
│   └── ClipboardEntry.cs       # Data model
├── Services/
│   ├── ClipboardWatcher.cs     # Clipboard polling (cross-platform)
│   ├── DatabaseService.cs      # SQLite persistence
│   ├── HistoryManager.cs       # History logic
│   ├── HotkeyManager.cs        # Global hotkey via SharpHook
│   ├── PasteService.cs         # Paste simulation (Ctrl+V / Cmd+V)
│   └── StartupService.cs       # Auto-start (Registry / XDG / LaunchAgent)
└── Localization/
    ├── L.cs                    # Resource manager
    ├── Strings.resx            # English
    └── Strings.tr.resx         # Turkish
```

### Tech Stack

- .NET 8.0
- Avalonia UI 11 (cross-platform UI framework)
- SharpHook (global keyboard hook & input simulation)
- Microsoft.Data.Sqlite (local database)

---

## Türkçe

Clippy, system tray'de çalışan hafif bir pano geçmişi yöneticisidir. Herhangi bir uygulamadayken `Ctrl+Shift+V` ile pano geçmişinizi arayıp yapıştırabilirsiniz. .NET 8 ve Avalonia UI ile geliştirilmiştir; Windows, Linux ve macOS'ta çalışır.

> Tüm veriler yerel olarak saklanır. Hiçbir şey dışarıya gönderilmez.

### Özellikler

- Metin, HTML ve görsel desteğiyle pano geçmişi
- Global kısayol (`Ctrl+Shift+V`) — her uygulamadan erişim
- Anlık arama ve klavye navigasyonu
- Önemli öğeleri sabitleme
- Görsel küçük resimleri (thumbnail)
- `Shift+Enter` ile düz metin yapıştırma
- Yakalamayı duraklat / sonraki kopyayı yoksay
- İşletim sistemiyle otomatik başlatma
- Türkçe ve İngilizce arayüz
- Acrylic blur efektli koyu tema popup
- SQLite ile kalıcı depolama

### Kısayollar

| Kısayol | İşlev |
|---|---|
| `Ctrl+Shift+V` | Popup aç/kapat |
| `Enter` | Seçili öğeyi yapıştır |
| `Shift+Enter` | Düz metin olarak yapıştır |
| `Ctrl+P` | Sabitle / kaldır |
| `Delete` | Öğeyi sil |
| `Esc` | Kapat |
| `↑` `↓` | Listede gezin |

### Gereksinimler

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (sadece derleme için)
- Windows 10+, Ubuntu 20.04+ / Fedora 38+ veya macOS 12+

### Derleme ve Çalıştırma

```bash
git clone https://github.com/user/Clippy.git
cd Clippy
dotnet run --project Clippy/Clippy.csproj
```

### Yayınlama

```bash
# Windows
publish-win.bat

# Linux
chmod +x publish-linux.sh && ./publish-linux.sh

# macOS (varsayılan arm64, Intel için x64 verin)
chmod +x publish-macos.sh && ./publish-macos.sh
```

### Kurulum Dosyaları Oluşturma

```bash
# Windows — Inno Setup 6+ gerekli (https://jrsoftware.org/isdl.php)
# setup.iss dosyasını Inno Setup ile açıp derleyin.

# Linux — .tar.gz + .deb oluşturur
chmod +x setup-linux.sh && ./setup-linux.sh

# macOS — .app bundle + .dmg oluşturur
chmod +x setup-macos.sh && ./setup-macos.sh
```

---

## License / Lisans

[MIT](LICENSE)
