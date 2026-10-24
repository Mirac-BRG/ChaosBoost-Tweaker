<div align="center">

# ⚡ ChaosBoost Tweaker

**Windows ayarları, performans seçenekleri ve uygulama kurulumları tek yerde.**

C# ve WPF ile geliştirilen, Windows 10/11 odaklı Türkçe masaüstü sistem yönetim aracı.

![Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D4?logo=windows&logoColor=white)
![C%23](https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF-512BD4)
![Version](https://img.shields.io/badge/Version-2.0-00C9A7)

[Özellikler](#-özellikler) · [Kurulum](#-kurulum-ve-çalıştırma) · [Güvenlik](#-önemli-uyarılar) · [Katkıda Bulunma](#-katkıda-bulunma)

</div>

---

## 🚀 ChaosBoost Tweaker nedir?

**ChaosBoost Tweaker**, Windows'taki sık kullanılan sistem yapılandırmalarını tek bir arayüzden yönetmek ve format sonrasında ihtiyaç duyulan uygulamaların kurulumunu kolaylaştırmak için geliştirilmiştir.

Gizlilik tercihleri, servisler, güç yönetimi, ağ ayarları ve isteğe bağlı Windows bileşenleri gibi işlemleri kategorilere ayırır. Amacı, kullanıcıların farklı komutları ve kayıt defteri yollarını tek tek aramak yerine neyi değiştirdiklerini görebilecekleri pratik bir kontrol paneli sunmaktır.

> ChaosBoost Tweaker, her bilgisayarda FPS artışı veya ping düşüşü vadeden bir araç değildir. Yapılan değişikliklerin etkisi donanıma, Windows sürümüne ve kullanım senaryosuna göre değişir.

## 🧰 Özellikler

### ⚙️ Hızlı Ayarlar

- Sistem geri yükleme noktası oluşturma
- Windows tanılama/telemetri, reklam kimliği ve konum politikalarını yapılandırma
- Başlat menüsü web aramaları, Cortana ve Windows Hata Bildirimi (WER) ile ilgili ayarlar
- Windows Update hizmetini yönetme
- Klasik Not Defteri ve Microsoft Edge için isteğe bağlı sistem işlemleri
- Windows Gezgini'ni yeniden başlatma
- Desteklenen ayarlar için **Geri Al** düğmeleri

### ⚡ Performans & Ağ

- Nihai Performans (*Ultimate Performance*) güç planı
- VBS / hiper yönetici başlatma ayarları
- Xbox Game DVR, fare ivmesi, Yapışkan Tuşlar ve tam ekran optimizasyonları
- Hazırda bekletme ve SysMain yönetimi
- Network Throttling kayıt defteri ayarı
- Cloudflare DNS (`1.1.1.1` / `1.0.0.1`)
- SMBv1 özelliğini devre dışı bırakma
- Windows 11 klasik sağ tık menüsü, gizli dosyalar ve dosya uzantıları
- Seçili yerleşik UWP uygulamalarını kaldırma

### 📦 Cephanelik — Uygulama Kurulum Merkezi

**WinGet** kullanarak yaygın uygulamaları tek arayüzden kurmayı sağlar.

| Kategori | Uygulama örnekleri |
| --- | --- |
| Arşiv | 7-Zip, WinRAR |
| Günlük araçlar | Notepad++, Everything, SumatraPDF, ShareX, ImageGlass |
| Medya | VLC, Spotify |
| İletişim | Discord, Telegram |
| Geliştirme | Git, Visual Studio Code, PowerToys |
| Bileşenler | Visual C++ Redistributable, DirectX çalışma zamanı |

**Temel Paket** seçeneğiyle 7-Zip, Notepad++, Everything ve VLC art arda kurulabilir.

Cephanelik ayrıca aşağıdaki araçlara erişim sunar:

- **[SoundVibe](https://github.com/Mirac-BRG/SoundVibe):** Müzik oynatıcı kurulum dosyasını indirip çalıştırma.
- **[SplitWire-Turkey](https://github.com/cagritaskn/SplitWire-Turkey):** DPI aracı kurulum dosyasını indirip kurulumdan sonra uygulamayı açmayı deneme. **DPI yöntemi SplitWire içinde kullanıcı tarafından seçilir.**

Bu iki GitHub kurulum dosyası, kodda tanımlı sabit sürüm ve **SHA-256** değeriyle doğrulanır; eşleşmeyen dosya çalıştırılmaz. Üçüncü taraf araçlar ilgili geliştiricilerine aittir.

### 🛠️ LTSC Modülleri

Windows'un bazı LTSC kurulumlarında bulunmayabilen bileşenleri eklemeye yardımcı olur:

- Microsoft Store ve Xbox oyun hizmetleri
- Modern Hesap Makinesi / Ekran Alıntısı Aracı
- HEVC ve VP9 medya uzantıları
- Klasik Windows Fotoğraf Görüntüleyicisi
- DirectPlay ve .NET Framework 3.5

> LTSC özelliklerinin kullanılabilirliği Windows sürümüne ve Microsoft servislerinin durumuna bağlıdır; her sistemde kurulum garantisi yoktur.

## 💻 Kurulum ve çalıştırma

### Hazır sürüm

Çalıştırılabilir bir paket yayımlanmışsa [Releases](https://github.com/Mirac-BRG/ChaosBoost-Tweaker/releases) sayfasından indirebilirsiniz.

### Kaynak koddan derleme

**Gereksinimler:** Windows, Visual Studio 2022 (WPF/.NET masaüstü geliştirme bileşenleriyle) veya projede hedeflenen .NET SDK'sı. Cephanelik üzerinden uygulama kurmak için **WinGet** gereklidir.

```powershell
git clone https://github.com/Mirac-BRG/ChaosBoost-Tweaker.git
cd ChaosBoost-Tweaker
dotnet restore
dotnet build -c Release
```

Alternatif olarak `ChaosBoostTweaker.csproj` dosyasını Visual Studio'da açıp projeyi derleyebilirsiniz. Bazı sistem ayarlarını uygulamak için **yönetici izni** gerekir. Uygulama yalnızca Windows üzerinde çalışır.

## 🛡️ Önemli uyarılar

ChaosBoost Tweaker; kayıt defteri, Windows hizmetleri, önyükleme seçenekleri ve isteğe bağlı özellikler üzerinde değişiklik yapabilir. Bu nedenle:

- **Önce geri yükleme noktası oluşturun** ve önemli dosyalarınızı ayrıca yedekleyin.
- **Windows Update, WER veya güvenlik özelliklerini kapatmak** güncellemeleri ve sorun gidermeyi olumsuz etkileyebilir.
- **VBS / hiper yönetici ayarları**, Hyper-V, WSL2, sanal makineler ve bazı güvenlik işlevlerini etkileyebilir.
- Edge/Not Defteri/UWP kaldırma işlemleri uygulamaları veya Windows entegrasyonlarını bozabilir; her Windows sürümünde desteklenmeyebilir.
- Her ayarın otomatik yedeği veya **tek tıkla tamamen geri alınması** garanti edilmez. Bazı değişiklikler yeniden başlatma gerektirir.
- İşlemlerin ve kurulumların başarılı olması, Windows sürümüne, izinlere ve internet bağlantısına bağlıdır.

**Gizlilik:** Kaynak kodda uygulamanın geliştiricisine kullanım analitiği gönderen bir telemetri mekanizması bulunmaz. İşlem durumları yerel `ChaosBoost_Log.txt` dosyasına yazılmaya çalışılır. Uygulama kurulumları ve harici araçların indirilmesi internet bağlantısı kullanır.

## 🤝 Katkıda bulunma

Hata bildirmek, iyileştirme önermek veya yeni özellik istemek için [Issues](https://github.com/Mirac-BRG/ChaosBoost-Tweaker/issues) bölümünü kullanabilirsiniz. Katkı yapmak isterseniz değişikliklerinizi açıklayan bir pull request açabilirsiniz.

## 👤 Geliştirici

**[Mirac-BRG](https://github.com/Mirac-BRG)**

---

<div align="center">
<sub>ChaosBoost Tweaker v2.0 · Windows sistem yönetimi için pratik, kullanıcı kontrollü araçlar.</sub>
</div>
