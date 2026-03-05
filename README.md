<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/platform-Windows-0078D6?logo=windows" alt="Windows" />
  <img src="https://img.shields.io/badge/license-MIT-green" alt="License" />
</p>

# 📋 Clippy — Windows Clipboard Manager

🇬🇧 [English](#-english) · 🇹🇷 [Türkçe](#-türkçe)

---

## 🇬🇧 English

**Clippy** is a fast, lightweight and keyboard-first clipboard manager for Windows. It combines the power of [Ditto](https://ditto-cp.sourceforge.io/) with the simplicity of [Maccy](https://maccy.app/).

> 🔒 **Privacy-first** — All data is stored locally. Nothing is sent to the cloud.

### ✨ Features

| Feature | Description |
|---------|-------------|
| 📋 **Clipboard History** | Automatically saves text, HTML, and image content |
| ⌨️ **Global Hotkey** | Press `Ctrl+Shift+V` for instant popup |
| 🔍 **Instant Search** | Fast filtering with keyboard navigation |
| 📌 **Pin / Favorites** | Keep important items at the top |
| 🖼️ **Image Support** | Stores copied images with thumbnail preview |
| 📝 **Plain Text Paste** | Paste without formatting via `Shift+Enter` |
| 🔐 **Privacy Controls** | Pause capture, ignore next copy |
| 🌍 **Multi-Language** | English and Turkish UI |
| 🚀 **Auto-Start** | Starts automatically with Windows |
| 💾 **SQLite Database** | Persistent history across restarts |

### 🛠️ Requirements

- **Windows 10/11** (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 🚀 Getting Started

```bash
# Clone the repository
git clone https://github.com/USERNAME/Clippy.git
cd Clippy

# Build and run
dotnet build Clippy\Clippy.csproj -c Release
dotnet run --project Clippy\Clippy.csproj
```

### ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+V` | Open / close popup |
| `Enter` | Copy selected item |
| `Shift+Enter` | Paste as plain text |
| `Delete` | Delete selected item |
| `Ctrl+P` | Pin / Unpin |
| `Esc` | Close popup |
| `↑` `↓` | Navigate the list |

### 📁 Project Structure

```
Clippy/
├── Clippy.sln
└── Clippy/
    ├── Program.cs              # Application entry point
    ├── TrayContext.cs           # System tray management
    ├── NativeMethods.cs         # Windows API calls
    ├── Forms/
    │   ├── PopupForm.cs         # Clipboard history popup
    │   └── SettingsForm.cs      # Settings window
    ├── Models/
    │   └── ClipboardEntry.cs    # Clipboard data model
    ├── Services/
    │   ├── ClipboardWatcher.cs  # Clipboard listener
    │   ├── DatabaseService.cs   # SQLite database
    │   ├── HistoryManager.cs    # History management
    │   ├── HotkeyManager.cs     # Global hotkey
    │   ├── PasteService.cs      # Paste service
    │   └── StartupService.cs    # Windows startup service
    └── Localization/
        ├── L.cs                 # Language manager
        ├── Strings.resx         # English strings
        └── Strings.tr.resx     # Turkish strings
```

### 🧰 Technologies

- **.NET 8.0** — Modern runtime
- **Windows Forms** — Native Windows UI
- **SQLite** (Microsoft.Data.Sqlite) — Local database

---

## 🇹🇷 Türkçe

**Clippy**, Windows için hızlı, hafif ve klavye odaklı bir clipboard manager uygulamasıdır. [Ditto](https://ditto-cp.sourceforge.io/)'nun gücünü ve [Maccy](https://maccy.app/)'nin sadeliğini bir araya getirir.

> 🔒 **Gizlilik öncelikli** — Tüm veriler yerel olarak saklanır, hiçbir veri dışarıya gönderilmez.

### ✨ Özellikler

| Özellik | Açıklama |
|---------|----------|
| 📋 **Clipboard Geçmişi** | Text, HTML ve image içerikleri otomatik kaydeder |
| ⌨️ **Global Hotkey** | `Ctrl+Shift+V` ile anında popup pencere |
| 🔍 **Anlık Arama** | Geçmişte hızlı filtreleme ve klavye navigasyonu |
| 📌 **Sabitleme** | Önemli öğeleri üstte tutun |
| 🖼️ **Resim Desteği** | Kopyalanan görselleri thumbnail ile saklar |
| 📝 **Düz Metin Yapıştırma** | `Shift+Enter` ile formatsız yapıştırma |
| 🔐 **Gizlilik** | Yakalamayı duraklat, sonraki kopyayı yoksay |
| 🌍 **Çoklu Dil** | Türkçe ve İngilizce arayüz desteği |
| 🚀 **Otomatik Başlatma** | Windows ile birlikte otomatik başlar |
| 💾 **SQLite Veritabanı** | Kalıcı geçmiş, yeniden başlatmada kaybolmaz |

### 🛠️ Gereksinimler

- **Windows 10/11** (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 🚀 Başlangıç

```bash
# Projeyi klonla
git clone https://github.com/KULLANICI_ADI/Clippy.git
cd Clippy

# Derle ve çalıştır
dotnet build Clippy\Clippy.csproj -c Release
dotnet run --project Clippy\Clippy.csproj
```

### ⌨️ Kısayollar

| Kısayol | İşlev |
|---------|-------|
| `Ctrl+Shift+V` | Popup pencereyi aç/kapat |
| `Enter` | Seçili öğeyi kopyala |
| `Shift+Enter` | Düz metin olarak yapıştır |
| `Delete` | Seçili öğeyi sil |
| `Ctrl+P` | Sabitle / Sabitlemeden çıkar |
| `Esc` | Pencereyi kapat |
| `↑` `↓` | Listede gezin |

### 📁 Proje Yapısı

```
Clippy/
├── Clippy.sln
└── Clippy/
    ├── Program.cs              # Uygulama giriş noktası
    ├── TrayContext.cs           # System tray yönetimi
    ├── NativeMethods.cs         # Windows API çağrıları
    ├── Forms/
    │   ├── PopupForm.cs         # Clipboard geçmişi popup'ı
    │   └── SettingsForm.cs      # Ayarlar penceresi
    ├── Models/
    │   └── ClipboardEntry.cs    # Clipboard veri modeli
    ├── Services/
    │   ├── ClipboardWatcher.cs  # Clipboard dinleyici
    │   ├── DatabaseService.cs   # SQLite veritabanı
    │   ├── HistoryManager.cs    # Geçmiş yönetimi
    │   ├── HotkeyManager.cs     # Global hotkey
    │   ├── PasteService.cs      # Yapıştırma servisi
    │   └── StartupService.cs    # Windows başlangıç servisi
    └── Localization/
        ├── L.cs                 # Dil yöneticisi
        ├── Strings.resx         # İngilizce metinler
        └── Strings.tr.resx     # Türkçe metinler
```

### 🧰 Teknolojiler

- **.NET 8.0** — Modern runtime
- **Windows Forms** — Native Windows UI
- **SQLite** (Microsoft.Data.Sqlite) — Yerel veritabanı

---

## 📄 License / Lisans

This project is licensed under the [MIT](LICENSE) license.

Bu proje [MIT](LICENSE) lisansı ile lisanslanmıştır.

---

<p align="center">
  <b>Clippy</b> — Your clipboard history, always at your fingertips! 📋✨
</p>
