# ChaosBoost Tweaker

**Windows 10, Windows 11 ve LTSC Sürümleri İçin Gelişmiş Sistem Yönetim, Optimizasyon ve Dağıtım Aracı**

ChaosBoost Tweaker, sistem yöneticileri, siber güvenlik araştırmacıları ve ileri düzey bilgisayar kullanıcıları için C# (WPF) mimarisiyle geliştirilmiş bağımsız (self-contained) bir işletim sistemi yapılandırma aracıdır. 

Modern Windows sürümlerinde gömülü olarak gelen telemetri servislerini kısıtlamak, donanım kaynaklarını tüketen arka plan hizmetlerini durdurmak ve kurumsal dağıtım (deployment) süreçlerini hızlandırmak amacıyla tasarlanmıştır.

### 🛡️ Şeffaflık ve Güvenlik Beyanı
* **Açık Kaynak:** Tüm kaynak kodu herkese açıktır, bağımsız olarak incelenebilir ve denetlenebilir.
* **Sıfır Telemetri:** Araç, kullanıcı alışkanlıklarını izlemez, veri toplamaz ve arka planda hiçbir sunucuya analitik/log göndermez.
* **Çevrimdışı Çalışma:** Winget üzerinden yapılan harici paket indirmeleri (Cephanelik sekmesi) haricinde, tüm sistem yapılandırma ve optimizasyon modülleri aktif bir internet bağlantısı olmadan %100 yerel çalışır.

---

## ⚙️ Çekirdek Mimari

* **Framework:** .NET 8.0 (WPF)
* **Bağımsız Çalışma (Self-Contained):** Araç, çalıştırılacağı hedef sistemde herhangi bir .NET kütüphanesinin (Runtime) kurulu olmasını gerektirmez. Tüm çekirdek bağımlılıkları tek bir çalıştırılabilir dosya (`.exe`) içerisine gömülmüştür.
* **Asenkron İşlem Motoru:** Sistem komutları (PowerShell, CMD, DISM, Winget) asenkron (`async/await`) mimariyle arka planda yürütülür, grafiksel kullanıcı arayüzünde (GUI) donma veya kilitlenme yaşanmaz.
* **Kayıt Defteri Güvenliği:** Doğrudan yetki gerektiren operasyonlar için `RegistryKey.CreateSubKey` metotları ile güvenli (try-catch bloklu) okuma/yazma işlemleri gerçekleştirilir.

---

## 🛠️ Modüller ve Yetenekler

Yazılım, işlevselliğine göre dört ana modüle ayrılmıştır:

### 1. Sistem Optimizasyonu ve Gizlilik
İşletim sisteminin veri toplama ve arka plan izleme servislerini yöneterek kullanıcı gizliliğini artırır.
* **Telemetri Kontrolü:** Microsoft Diagnostic Tracking (DiagTrack) servislerini kayıt defteri ve SC motoru üzerinden devre dışı bırakır.
* **Arama ve Asistan İzolasyonu:** Başlat menüsü Bing web arama entegrasyonunu, Cortana'yı ve Windows kişiselleştirilmiş Reklam Kimliğini (Ad ID) kapatır. Yerel arama indekslemesini hızlandırır.
* **Windows Update Kontrolü:** İsteğe bağlı olarak Windows Update servisini (`wuauserv`) durdurur ve otomatik güncellemeleri kilitler.
* **UWP Modifikasyonları:** Windows 11'in sekmeli Not Defteri uygulamasını kaldırarak, sistemin derinliklerinde bulunan klasik (sekmesiz) `notepad.exe` sürümünü zorla varsayılan yapar.
* **Microsoft Edge Deprovisioning (Tarayıcı Bileşeni Kaldırma):** Gizli `setup.exe` parametrelerini (`--force-uninstall`) tetikleyerek Microsoft Edge tarayıcısını sistemden kaldırır ve kayıt defteri üzerinden yeniden kurulmasını mümkün olduğunca engeller (Not: Kapsamlı Windows Major güncellemeleri bu engeli aşarak tarayıcıyı geri getirebilir).

### 2. Performans ve Ağ Yönetimi
Donanım darboğazlarını ve ağ gecikmelerini gidermeye yönelik çekirdek yapılandırmaları içerir.
* **Güç ve Donanım:** Gizli "Nihai Performans" (Ultimate Performance) güç planını aktifleştirir. Hibrit uyku (Hibernate) dosyasını silerek diskte alan açar.
* **İşlemci ve Bellek Optimizasyonu:** VBS (Virtualization-Based Security), bellek bütünlüğü ve SysMain (Superfetch) hizmetlerini kapatır.
* **Oyun ve Gecikme (Input Lag) Optimizasyonları:** Tam Ekran İyileştirmelerini (FSO), Fare İvmesini (Mouse Acceleration), Yapışkan Tuşları ve Xbox Game DVR servislerini devre dışı bırakır.
* **Siber Güvenlik ve Ağ:** * Medya ağ kısıtlamasını (Network Throttling) kaldırır.
  * Ağ bağdaştırıcılarını Cloudflare DNS (1.1.1.1 / 1.0.0.1) sunucularına yönlendirir.
  * Yanal hareket ve fidye virüsü (WannaCry vb.) zafiyetleri barındıran **SMBv1 protokolünü DISM üzerinden devre dışı bırakır.**

### 3. Otomatize Cephanelik (Deployment)
Format veya yeni kurulum sonrası gerekli temel bileşenlerin Windows Package Manager (Winget) aracılığıyla sessiz (silent) kurulumunu gerçekleştirir.
* **Sistem Kütüphaneleri:** Visual C++ Redistributable Tüm Sürümler, DirectX End-User Runtime
* **Verimlilik Araçları:** Notepad++, Everything, SumatraPDF, ShareX, ImageGlass
* **İletişim ve Ağ Araçları:** Discord, Telegram Desktop, qBittorrent

### 4. LTSC Entegrasyon Modülü
Kurumsal Windows 10/11 Enterprise LTSC sürümlerinde eksik olan resmi paketlerin sisteme enjekte edilmesini sağlar.
* `wsreset` ve Winget komut zinciri ile Microsoft Store ve Xbox Oyun Hizmetleri entegrasyonu.
* Modern Windows Araçları (Hesap Makinesi, Ekran Alıntısı Aracı) ve HEVC/VP9 Medya Çözücüleri kurulumu.
* .NET Framework 3.5 ve DirectPlay aktivasyonu.

---

## 🚀 Kullanım Talimatları

1. Projenin [Releases](../../releases) sekmesinden en güncel `ChaosBoostTweaker.exe` dosyasını indirin.
2. Yazılımın çalışabilmesi ve Kayıt Defteri/DISM komutlarını yürütebilmesi için dosyaya sağ tıklayıp **Yönetici Olarak Çalıştır** seçeneğini kullanın.
3. Herhangi bir işlem yapmadan önce **Hızlı Ayarlar** sekmesindeki **Sistem Geri Yükleme Noktası Oluştur** butonunu kullanarak sisteminizin anlık yedeğini alın.
4. İlgili modüllerden ihtiyaç duyduğunuz konfigürasyonları uygulayın. İşlemlerin tam olarak yansıması için "Explorer'ı Yeniden Başlat" butonunu kullanın veya sisteminizi yeniden başlatın.

---

## ⚠️ Sorumluluk Reddi ve Sistem Değişkenleri

Bu araç, işletim sisteminin çekirdek hizmetlerine, ağ protokollerine ve kayıt defteri ayarlarına (Regedit) derin düzeyde müdahaleler gerçekleştirir. Lütfen aşağıdaki hususları dikkate alın:

* **Performans Kazanımı Değişkenliği:** SysMain, VBS kapatma, Ağ Kısıtlaması (Network Throttling) iptali, Update kapatma ve Tam Ekran İyileştirmeleri (FSO) gibi özelliklerin modifiye edilmesi her sistemde mutlak bir performans artışı sağlamayabilir. Elde edilecek kazanç; sisteminizin donanımına (SSD hızları, CPU mimarisi), sürücülerinize ve kullandığınız spesifik Windows derleme (build) sürümüne göre değişiklik gösterebilir.
* **Bileşen Bağımlılıkları:** Microsoft Edge'in kaldırılması, işletim sistemindeki "Widgets" (Araç Takımları) veya web tabanlı arama sonuçlarının çalışmasını etkileyebilir. Benzer şekilde, Windows Update veya VBS'in devre dışı bırakılması sistemin kritik güvenlik yamalarından mahrum kalmasına neden olabilir.

**Yazılımın kullanımı tamamen kullanıcının kendi inisiyatifinde ve sorumluluğundadır.** Üretim (production) veya kritik veri barındıran makinelerde kullanılmadan önce test ortamlarında denenmesi şiddetle tavsiye edilir.
