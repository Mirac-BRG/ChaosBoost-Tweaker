using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChaosBoostTweaker
{
    // =========================================================
    // 1. ÇEKİRDEK İŞLEMLER (KOMUT VE KAYIT DEFTERİ YÖNETİMİ)
    // =========================================================
    public static class SistemKomutlari
    {
        // Hata kodunu ve komut çıktısını birlikte okuyarak zincir hatalarını saklamaz.
        public static async Task<(bool Basarili, string Cikti)> CMDCalistirCiktiAsync(string komut)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo("cmd.exe", "/d /c " + komut)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                if (!process.Start()) return (false, "Komut işlemi başlatılamadı.");
                Task<string> ciktiGorevi = process.StandardOutput.ReadToEndAsync();
                Task<string> hataGorevi = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                return (process.ExitCode == 0,
                    ((await ciktiGorevi) + Environment.NewLine + (await hataGorevi)).Trim());
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static async Task<bool> CMDCalistirAsync(string komut) =>
            (await CMDCalistirCiktiAsync(komut)).Basarili;

        // Kayıt defteri DWORD değerini oluşturur veya günceller.
        public static bool RegAyarUygula(RegistryKey anahtar, string yol, string degerAdi, int deger)
        {
            try
            {
                using (RegistryKey key = anahtar.CreateSubKey(yol, true))
                {
                    if (key == null) return false;
                    key.SetValue(degerAdi, deger, RegistryValueKind.DWord);
                    return true;
                }
            }
            catch { return false; }
        }

        // Geri alma işlemleri sabit ilkeyi zorla başka bir değere çekmek yerine kaldırır.
        public static bool RegAyarSil(RegistryKey anahtar, string yol, string degerAdi)
        {
            try
            {
                using (RegistryKey? key = anahtar.OpenSubKey(yol, true))
                {
                    if (key == null) return true;
                    key.DeleteValue(degerAdi, throwOnMissingValue: false);
                    return true;
                }
            }
            catch { return false; }
        }

        // Kayıt defterindeki değerin beklenen değerle eşleşmesini kontrol eder.
        public static bool RegAyarOkunuyorMu(RegistryKey anahtar, string yol, string degerAdi, int beklenenDeger)
        {
            try
            {
                using (RegistryKey key = anahtar.OpenSubKey(yol, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(degerAdi);
                        if (val != null && (int)val == beklenenDeger) return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }

    // =========================================================
    // 2. ARAYÜZ VE PENCERE OLAYLARI
    // =========================================================
    public partial class MainWindow : Window
    {
        private readonly string logDosyaYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChaosBoostTweaker", "ChaosBoost_Log.txt");

        public MainWindow()
        {
            InitializeComponent();
            LogTut("==================================================");
            LogTut("ChaosBoost Tweaker v2.0.1 başlatıldı. Komut yürütme ve durum kaydı hazır.");
        }

        private void LogTut(string mesaj)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logDosyaYolu)!);
                File.AppendAllText(logDosyaYolu, $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] - {mesaj}{Environment.NewLine}");
            }
            catch { /* Günlük yazma hatası arayüzü durdurmamalı. */ }
        }

        private void DurumGuncelle(string mesaj, bool aktifMi, bool hataMi = false)
        {
            TxtDurum.Text = mesaj;
            DurumCubugu.Visibility = aktifMi ? Visibility.Visible : Visibility.Hidden;

            if (hataMi)
                TxtDurum.Foreground = new SolidColorBrush(Color.FromRgb(255, 68, 68)); // Kırmızı (Hata)
            else
                TxtDurum.Foreground = new SolidColorBrush(aktifMi ? Color.FromRgb(136, 204, 255) : Color.FromRgb(0, 255, 204)); // Mavi/Yeşil (Normal)

            LogTut(hataMi ? "[HATA] " + mesaj : mesaj);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void KapatButonu_Click(object sender, RoutedEventArgs e) { LogTut("ChaosBoost Tweaker Kapatıldı."); Application.Current.Shutdown(); }

        private void MenuAktifEt(Visibility h, Visibility p, Visibility c, Visibility l)
        {
            PanelHizliAyarlar.Visibility = h; PanelPerformans.Visibility = p; PanelCephanelik.Visibility = c; PanelLTSC.Visibility = l;
        }
        private void MenuHizliAyarlar_Click(object sender, RoutedEventArgs e) => MenuAktifEt(Visibility.Visible, Visibility.Hidden, Visibility.Hidden, Visibility.Hidden);
        private void MenuPerformans_Click(object sender, RoutedEventArgs e) => MenuAktifEt(Visibility.Hidden, Visibility.Visible, Visibility.Hidden, Visibility.Hidden);
        private void MenuCephanelik_Click(object sender, RoutedEventArgs e) => MenuAktifEt(Visibility.Hidden, Visibility.Hidden, Visibility.Visible, Visibility.Hidden);
        private void MenuLTSC_Click(object sender, RoutedEventArgs e) => MenuAktifEt(Visibility.Hidden, Visibility.Hidden, Visibility.Hidden, Visibility.Visible);

        // =========================================================
        // 3. HIZLI AYARLAR VE DURUM KONTROLLERİ
        // =========================================================
        private async void BtnRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Geri Yükleme Noktası oluşturuluyor...", true);
            bool basari = await SistemKomutlari.CMDCalistirAsync("powershell -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description 'ChaosBoost Ilk Kurulum' -RestorePointType 'MODIFY_SETTINGS'\"");
            DurumGuncelle(basari ? "Geri Yükleme Noktası oluşturuldu." : "HATA: Sistem Koruması devre dışı olabilir!", false, !basari);
        }

        private async void BtnTelemetriKapat_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Telemetri ilkesi ve DiagTrack hizmeti ayarlanıyor...", true);
            bool reg = SistemKomutlari.RegAyarUygula(Registry.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            bool servis = await SistemKomutlari.CMDCalistirAsync("sc config DiagTrack start= disabled");
            // Zaten durdurulmuş hizmette net stop hata döndürebilir: belirleyici olan başlangıç ayarıdır.
            if (servis) await SistemKomutlari.CMDCalistirAsync("net stop DiagTrack >nul 2>&1");
            DurumGuncelle(reg && servis
                ? "Tanılama ilkesi ayarlandı ve DiagTrack başlangıcı devre dışı bırakıldı (Windows sürümüne bağlıdır)."
                : "Telemetri ilkesi veya DiagTrack hizmeti ayarlanamadı.", false, !reg || !servis);
        }

        private async void BtnTelemetriGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Telemetri için Windows varsayılan ilkesi geri yükleniyor...", true);
            bool reg = SistemKomutlari.RegAyarSil(Registry.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
            bool svc = await SistemKomutlari.CMDCalistirAsync("sc config DiagTrack start= auto");
            DurumGuncelle(reg && svc
                ? "Telemetri kısıtlaması kaldırıldı, DiagTrack otomatik başlatmaya ayarlandı."
                : "Telemetri geri alma işleminin bir kısmı başarısız.", false, !reg || !svc);
        }

        private void BtnBingKapat_Click(object sender, RoutedEventArgs e)
        {
            bool ara = SistemKomutlari.RegAyarUygula(Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
            bool ilke = SistemKomutlari.RegAyarUygula(Registry.CurrentUser,
                @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
            DurumGuncelle(ara && ilke ? "Bing arama ayarları güncellendi." : "Bing ayarlarından biri yazılamadı.",
                false, !ara || !ilke);
        }
        private void BtnBingGeriAl_Click(object sender, RoutedEventArgs e)
        {
            bool ara = SistemKomutlari.RegAyarSil(Registry.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled");
            bool ilke = SistemKomutlari.RegAyarSil(Registry.CurrentUser,
                @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions");
            DurumGuncelle(ara && ilke ? "Bing ilke kısıtlamaları kaldırıldı." : "Bing ayarlarından biri geri alınamadı.",
                false, !ara || !ilke);
        }

        private void BtnCortanaKapat_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0); DurumGuncelle(ok ? "Cortana kapatıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }
        private void BtnCortanaGeriAl_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1); DurumGuncelle(ok ? "Cortana açıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }
        private void BtnAdIdKapat_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0); DurumGuncelle(ok ? "Ad ID kapatıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }
        private void BtnAdIdGeriAl_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1); DurumGuncelle(ok ? "Ad ID açıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }
        private void BtnWERKapat_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1); DurumGuncelle(ok ? "WER kapatıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }
        private void BtnWERGeriAl_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 0); DurumGuncelle(ok ? "WER açıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }
        private void BtnKonumKapat_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1); DurumGuncelle(ok ? "Konum kapatıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }
        private void BtnKonumGeriAl_Click(object sender, RoutedEventArgs e) { bool ok = SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 0); DurumGuncelle(ok ? "Konum açıldı." : "İşlem başarısız: Kayıt defteri yazılamadı.", false, !ok); }

        private async void BtnUpdateKapat_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Windows Update'i kapatmak güvenlik yamalarını, sürücüleri, Store ve bazı yazılım kurulumlarını etkiler. " +
                    "Sadece geçici bir süre için kullanın. Yine de kapatılsın mı?",
                    "Güvenlik uyarısı - Windows Update", MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            DurumGuncelle("Windows Update kapatılıyor...", true);
            bool reg = SistemKomutlari.RegAyarUygula(Registry.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1);
            bool svc = await SistemKomutlari.CMDCalistirAsync("sc config wuauserv start= disabled");
            if (svc) await SistemKomutlari.CMDCalistirAsync("net stop wuauserv >nul 2>&1");
            DurumGuncelle(reg && svc
                ? "Windows Update başlangıcı kapatıldı. Güvenlik güncellemeleri engellenebilir; işiniz bitince geri alın."
                : "Windows Update politikası veya hizmeti değiştirilemedi.", false, !reg || !svc);
        }
        private async void BtnUpdateGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Windows Update hizmeti geri alınıyor...", true);
            bool reg = SistemKomutlari.RegAyarSil(Registry.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate");
            bool svc = await SistemKomutlari.CMDCalistirAsync("sc config wuauserv start= demand");
            // net start burada bir tanılama adımıdır, servis yapılandırmasıyla aynı şey değildir.
            if (svc) await SistemKomutlari.CMDCalistirAsync("net start wuauserv >nul 2>&1");
            DurumGuncelle(reg && svc
                ? "Windows Update politika engeli kaldırıldı; hizmet elle başlatmaya ayarlandı."
                : "Update geri alma işleminin bir kısmı başarısız.", false, !reg || !svc);
        }

        private void BtnClassicNotepad_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Windows 11'de Not Defteri'nin klasik sürümünü sistemden zorla geri getirmenin güvenilir, her sürümde çalışan bir yolu yok. " +
                "Önceki yöntem modern Not Defteri'ni kaldırıp IFEO Debugger değerini bozabiliyordu; bu riskli işlem kaldırıldı. " +
                "Klasik bir metin düzenleyici için Cephanelik'ten Notepad++ kurabilirsiniz.",
                "Klasik Not Defteri hakkında", MessageBoxButton.OK, MessageBoxImage.Information);
            DurumGuncelle("Klasik Not Defteri için riskli sistem değişiklikleri uygulanmadı.", false);
        }
        private void BtnClassicNotepadGeriAl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?productid=9MSMLRH6LZF3")
                { UseShellExecute = true });
                DurumGuncelle("Modern Not Defteri'nin Microsoft Store sayfası açıldı; buradan yükleyebilirsiniz.", false);
            }
            catch
            {
                DurumGuncelle("Microsoft Store açılamadı. Not Defteri'ni Mağaza'dan elle yükleyin.", false, true);
            }
        }

        private async void BtnEdgeSil_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Microsoft Edge kaldırma işlemi Windows WebView2, oturum açma ve bazı sistem özelliklerini bozabilir. " +
                    "Microsoft tarafından desteklenen kaldırma yöntemleri cihaz/bölgeye göre değişir. " +
                    "Bu işlem tüm sistemlerde çalışmayabilir. Yine de denensin mi?",
                    "Microsoft Edge kaldırma uyarısı", MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            DurumGuncelle("Edge kaldırma komutu deneniyor...", true);
            // Güvenli başarım raporu: son reg add komutunun başarılı olması kaldırıldı demek değildir.
            const string nukeKomut = @"for /d %i in (""C:\Program Files (x86)\Microsoft\Edge\Application\*"") do (if exist ""%i\Installer\setup.exe"" ""%i\Installer\setup.exe"" --uninstall --system-level --verbose-logging --force-uninstall)";
            bool bitti = await SistemKomutlari.CMDCalistirAsync(nukeKomut);
            DurumGuncelle(bitti
                ? "Edge kaldırma komutu denendi. Ayarlar > Uygulamalar'dan sonucu kontrol edin."
                : "Edge kaldırma komutu hata verdi. Windows bölgesel/politika kısıtı olabilir.", false, !bitti);
        }

        private async void BtnExplorerYenile_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Windows Gezgini yeniden başlatılıyor...", true);
            bool bitti = await SistemKomutlari.CMDCalistirAsync("taskkill /f /im explorer.exe && start explorer.exe");
            DurumGuncelle(bitti ? "Windows Gezgini yeniden başlatma komutu uygulandı."
                : "Gezgin yeniden başlatılamadı; görev yöneticisinden explorer.exe'yi çalıştırabilirsiniz.", false, !bitti);
        }

        // =========================================================
        // 4. PERFORMANS VE AĞ SEÇENEKLERİ
        // =========================================================
        private async void BtnUltimatePower_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Nihai Performans güç planı kontrol ediliyor...", true);
            const string kaynak = "e9a42b02-d5df-448d-aa00-03f14749eb61";
            const string hedef = "a760cf1b-247a-4793-bc2f-c6c0cff210b1";
            var planlar = await SistemKomutlari.CMDCalistirCiktiAsync("powercfg /list");
            bool mevcut = planlar.Basarili && planlar.Cikti.Contains(hedef, StringComparison.OrdinalIgnoreCase);
            bool olusturuldu = mevcut ||
                await SistemKomutlari.CMDCalistirAsync($"powercfg /duplicatescheme {kaynak} {hedef}");
            bool etkin = olusturuldu && await SistemKomutlari.CMDCalistirAsync($"powercfg /setactive {hedef}");
            DurumGuncelle(etkin
                ? "Nihai Performans planı etkinleştirildi. Dizüstünde pil tüketimi artabilir."
                : "Nihai Performans planı oluşturulamadı veya etkinleştirilemedi.", false, !etkin);
        }
        private async void BtnVBS_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "VBS/Hyper-V ayarlarını değiştirmek WSL2, Hyper-V, Sandbox, sanal makineler ve bellek bütünlüğünü etkileyebilir. " +
                    "Güvenlik özellikleri zayıflayabilir. Yeniden başlatma gerekir. Devam edilsin mi?",
                    "Sanal makine ve güvenlik uyarısı", MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            DurumGuncelle("Hypervisor önyükleme ve Device Guard ayarları değiştiriliyor...", true);
            bool bcd = await SistemKomutlari.CMDCalistirAsync("bcdedit /set hypervisorlaunchtype off");
            bool reg = SistemKomutlari.RegAyarUygula(Registry.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0);
            DurumGuncelle(bcd && reg
                ? "Hypervisor ve VBS yapılandırıldı; yeniden başlatıp Windows Güvenliği'nden doğrulayın."
                : "VBS ayarlarından biri değiştirilemedi.", false, !bcd || !reg);
        }
        private async void BtnXboxDVR_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Xbox DVR kapatılıyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 0 /f && reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v AllowGameDVR /t REG_DWORD /d 0 /f"); DurumGuncelle(ok ? "Game DVR kayıt defteri ayarları uygulandı." : "İşlem başarısız: Game DVR kayıt defteri ayarları uygulandı.", false, !ok); }
        private async void BtnHibernate_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Hibernate kapatılıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("powercfg -h off"); DurumGuncelle(basari ? "Hibernate kapatıldı. Diskte yer açıldı." : "HATA: Sistem buna izin vermedi.", false, !basari); }
        private async void BtnMouseAccel_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Fare ivmesi kapatılıyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f && reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f && reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f"); DurumGuncelle(ok ? "Fare ivmesi kayıt defteri ayarları uygulandı." : "İşlem başarısız: Fare ivmesi kayıt defteri ayarları uygulandı.", false, !ok); }
        private async void BtnStickyKeys_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Yapışkan Tuşlar kapatılıyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Control Panel\\Accessibility\\StickyKeys\" /v Flags /t REG_SZ /d 506 /f && reg add \"HKCU\\Control Panel\\Accessibility\\Keyboard Response\" /v Flags /t REG_SZ /d 122 /f && reg add \"HKCU\\Control Panel\\Accessibility\\ToggleKeys\" /v Flags /t REG_SZ /d 58 /f"); DurumGuncelle(ok ? "Erişilebilirlik kısayolu ayarları uygulandı." : "İşlem başarısız: Erişilebilirlik kısayolu ayarları uygulandı.", false, !ok); }
        private async void BtnFSO_Click(object sender, RoutedEventArgs e) { DurumGuncelle("FSO kapatılıyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f && reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f"); DurumGuncelle(ok ? "Tam ekran optimizasyonu ayarları uygulandı." : "İşlem başarısız: Tam ekran optimizasyonu ayarları uygulandı.", false, !ok); }
        private async void BtnSysMain_Click(object sender, RoutedEventArgs e) { DurumGuncelle("SysMain durduruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("sc config SysMain start= disabled"); if (basari) await SistemKomutlari.CMDCalistirAsync("net stop SysMain >nul 2>&1"); DurumGuncelle(basari ? "SysMain başlangıcı kapatıldı (yeniden başlatma sonrası da uygulanır)." : "HATA: SysMain yapılandırılamadı.", false, !basari); }
        private async void BtnNetThrottling_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Ağ gecikmesi kapatılıyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 0xffffffff /f"); DurumGuncelle(ok ? "NetworkThrottlingIndex kayıt defteri değeri ayarlandı." : "İşlem başarısız: NetworkThrottlingIndex kayıt defteri değeri ayarlandı.", false, !ok); }
        private async void BtnDNS_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Cloudflare DNS uygulanıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("powershell -Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Set-DnsClientServerAddress -ServerAddresses '1.1.1.1','1.0.0.1'\""); DurumGuncelle(basari ? "DNS başarıyla değiştirildi." : "HATA: Ağ bağdaştırıcısı ayarı başarısız.", false, !basari); }
        private async void BtnSMBv1_Click(object sender, RoutedEventArgs e) { DurumGuncelle("SMBv1 kapatılıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("dism /online /Disable-Feature /FeatureName:SMB1Protocol /Quiet /NoRestart"); DurumGuncelle(basari ? "SMBv1 özelliğini kapatma komutu tamamlandı." : "HATA: DISM modülü işlem yapamadı.", false, !basari); }
        private async void BtnWin11Menu_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Klasik sağ tık menüsü açılıyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /ve /d \"\" /f"); DurumGuncelle(ok ? "Klasik menü devrede (Gezgini yenileyin)." : "İşlem başarısız: Klasik menü devrede (Gezgini yenileyin).", false, !ok); }
        private async void BtnShowHidden_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Gizli dosyalar açılıyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 1 /f && reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 0 /f"); DurumGuncelle(ok ? "Gizli dosya ve dosya uzantısı ayarları uygulandı." : "İşlem başarısız: Gizli dosya ve dosya uzantısı ayarları uygulandı.", false, !ok); }
        private async void BtnUwpBloat_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Seçili UWP uygulamaları kaldırılıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("powershell -Command \"Get-AppxPackage *bing* | Remove-AppxPackage; Get-AppxPackage *people* | Remove-AppxPackage; Get-AppxPackage *solitaire* | Remove-AppxPackage; Get-AppxPackage *zune* | Remove-AppxPackage\""); DurumGuncelle(basari ? "Seçili UWP paketleri için kaldırma komutları tamamlandı." : "HATA: Paket işlemi sırasında hata oluştu.", false, !basari); }

        private bool cephanelikKurulumCalisiyor = false;

        // =========================================================
        // 5. UYGULAMA DAĞITIMI (WINGET)
        // =========================================================
        // Kimlikleri yalnızca sabit, doğrulanmış uygulama paketlerinden gönderiyoruz.
        // Kullanıcı girdisini komut satırına eklemiyoruz.
        private static string WingetKomutu(string id) =>
            $"winget install --id {id} --exact --silent --accept-source-agreements --accept-package-agreements";

        private async Task WingetKurAsync(string id)
        {
            if (cephanelikKurulumCalisiyor)
            {
                DurumGuncelle("Başka bir Cephanelik kurulumu devam ediyor. Lütfen bekleyin.", true);
                return;
            }

            cephanelikKurulumCalisiyor = true;
            try
            {
                DurumGuncelle($"{id} kuruluyor...", true);
                bool basari = await SistemKomutlari.CMDCalistirAsync(WingetKomutu(id));
                DurumGuncelle(basari
                    ? $"{id}: WinGet işlemi tamamlandı. Uygulamayı kontrol edin."
                    : $"{id}: WinGet işlemi tamamlanamadı. Paket zaten kurulu olabilir; WinGet, ağ ve izinleri kontrol edin.",
                    false, !basari);
            }
            finally
            {
                cephanelikKurulumCalisiyor = false;
            }
        }

        // Kullanıcının seçtiği toplu işlem; önceden onay almadan kuruluma geçmez.
        private async void BtnTemelPaketKur_Click(object sender, RoutedEventArgs e)
        {
            if (cephanelikKurulumCalisiyor)
            {
                DurumGuncelle("Başka bir Cephanelik kurulumu devam ediyor.", true);
                return;
            }

            const string soru = "7-Zip, Notepad++, Everything ve VLC sırayla kurulacak. Devam edilsin mi?";
            if (MessageBox.Show(soru, "ChaosBoost Tweaker - Temel Paket", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            string[] paketler = { "7zip.7zip", "Notepad++.Notepad++", "voidtools.Everything", "VideoLAN.VLC" };
            cephanelikKurulumCalisiyor = true;
            int tamamlanan = 0;
            try
            {
                for (int i = 0; i < paketler.Length; i++)
                {
                    DurumGuncelle($"Temel paket {i + 1}/{paketler.Length}: {paketler[i]} kuruluyor...", true);
                    bool basarili = await SistemKomutlari.CMDCalistirAsync(WingetKomutu(paketler[i]));
                    if (basarili) tamamlanan++;
                    else LogTut($"[UYARI] Temel paket başarısız veya zaten yüklü: {paketler[i]}");
                }
                DurumGuncelle(
                    $"Temel paket bitti: {tamamlanan}/{paketler.Length} WinGet işlemi başarılı. Eksikler için günlük ve yüklenen programları kontrol edin.",
                    false, tamamlanan != paketler.Length);
            }
            finally
            {
                cephanelikKurulumCalisiyor = false;
            }
        }

        // 20.10.2026 itibarıyla yayımlanmış resmi sürümler. Yeni sürüme geçerken
        // URL ile SHA-256 hash'i BİRLİKTE güncellenmeli; indirme kaynağı sabittir.
        private const string SplitWireSetupUrl =
            "https://github.com/cagritaskn/SplitWire-Turkey/releases/download/1.5.7/SplitWire-Turkey-Setup-Windows-1.5.7.exe";
        private const string SplitWireSetupSha256 =
            "b1d2505b16164548048792e2da2ea2f9c22ce6ab7391354c125d4b3f98eb127b";
        private const string SoundVibeSetupUrl =
            "https://github.com/Mirac-BRG/SoundVibe/releases/download/1.0/SoundVibe_Setup.exe";
        private const string SoundVibeSetupSha256 =
            "c2f842911d2e4f63f95bf1e0d677e6ac44e3aaea57803ffe9ab8047ac071012a";

        private static readonly HttpClient cephanelikHttp = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };

        private static string DosyaSha256(string dosya)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(dosya))
            {
                return Convert.ToHexString(sha.ComputeHash(stream));
            }
        }

        // Son sürümü tahmin etmek yerine sabit release + sabit SHA-256 kullanılır.
        // İndirilen EXE hash eşleşmezse çalıştırılmaz; kısmi indirme kalıntısı silinir.
        private async Task<string> DogrulanmisSetupIndirAsync(
            string ad, string url, string dosyaAdi, string beklenenSha)
        {
            var adres = new Uri(url, UriKind.Absolute);
            if (adres.Scheme != Uri.UriSchemeHttps || adres.Host != "github.com" ||
                !adres.AbsolutePath.StartsWith("/", StringComparison.Ordinal))
                throw new InvalidOperationException("Beklenmeyen indirme kaynağı.");

            string klasor = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ChaosBoostTweaker", "Downloads");
            Directory.CreateDirectory(klasor);
            string hedef = Path.Combine(klasor, dosyaAdi);
            string kismi = hedef + ".partial";

            // Önceden indirilen, doğrulanmış kurulum dosyası yeniden kullanılabilir.
            if (File.Exists(hedef))
            {
                if (DosyaSha256(hedef).Equals(beklenenSha, StringComparison.OrdinalIgnoreCase))
                {
                    DurumGuncelle(ad + ": doğrulanmış kurulum dosyası hazır.", true);
                    return hedef;
                }
                File.Delete(hedef);
            }

            try
            {
                DurumGuncelle(ad + ": GitHub kurulum dosyası indiriliyor...", true);
                using (var yanit = await cephanelikHttp.GetAsync(
                    adres, HttpCompletionOption.ResponseHeadersRead))
                {
                    yanit.EnsureSuccessStatusCode();
                    long? boyut = yanit.Content.Headers.ContentLength;
                    const long ustSinir = 400L * 1024 * 1024;
                    if (boyut.HasValue && boyut.Value > ustSinir)
                        throw new InvalidDataException("Kurulum dosyası beklenenden büyük.");

                    using (var kaynak = await yanit.Content.ReadAsStreamAsync())
                    using (var hedefStream = new FileStream(
                        kismi, FileMode.Create, FileAccess.Write, FileShare.None,
                        81920, useAsync: true))
                    {
                        byte[] buffer = new byte[81920];
                        long toplam = 0;
                        int sonYuzde = -5;
                        int okunan;
                        while ((okunan = await kaynak.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            toplam += okunan;
                            if (toplam > ustSinir)
                                throw new InvalidDataException("İndirme sınırı aşıldı.");
                            await hedefStream.WriteAsync(buffer, 0, okunan);
                            if (boyut.HasValue && boyut.Value > 0)
                            {
                                int yuzde = (int)(100 * toplam / boyut.Value);
                                if (yuzde >= sonYuzde + 5)
                                {
                                    sonYuzde = yuzde;
                                    // Yalnızca durum metni güncellenir; sahte kurulum ilerlemesi gösterilmez.
                                    DurumGuncelle($"{ad}: indiriliyor %{yuzde}", true);
                                }
                            }
                        }
                    }
                }

                DurumGuncelle(ad + ": SHA-256 bütünlük kontrolü yapılıyor...", true);
                if (!DosyaSha256(kismi).Equals(beklenenSha, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 uyuşmazlığı! Güvenlik nedeniyle dosya çalıştırılmadı.");

                File.Move(kismi, hedef);
                return hedef;
            }
            finally
            {
                try { if (File.Exists(kismi)) File.Delete(kismi); }
                catch { /* İndirme hata bilgisini gizleme. */ }
            }
        }

        // SplitWire: kullanıcı ön onayı -> otomatik indirme ve doğrulama ->
        // Inno Setup sessiz kurulumu -> varsa kurulan uygulamayı açma.
        // DPI modu (Zapret/ByeDPI/WireSock) kendi arayüzünde seçilir.
        private async void BtnSplitWireKur_Click(object sender, RoutedEventArgs e)
        {
            if (cephanelikKurulumCalisiyor)
            {
                DurumGuncelle("Başka bir Cephanelik kurulumu devam ediyor.", true);
                return;
            }

            const string aciklama =
                "SplitWire-Turkey 1.5.7 (~173 MB) GitHub'dan indirilecek, " +
                "SHA-256 ile doğrulanacak ve sessizce kurulacak. " +
                "Windows yönetici izni isteyebilir.\n\n" +
                "Bu adım DPI yöntemini etkinleştirmez; kurulumu takiben " +
                "SplitWire arayüzünden yöntem seçmelisiniz. Devam edilsin mi?";
            if (MessageBox.Show(aciklama, "ChaosBoost - DPI aracı kurulumu",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            cephanelikKurulumCalisiyor = true;
            try
            {
                string yol = await DogrulanmisSetupIndirAsync(
                    "SplitWire-Turkey", SplitWireSetupUrl,
                    "SplitWire-Turkey-Setup-Windows-1.5.7.exe", SplitWireSetupSha256);
                DurumGuncelle("SplitWire-Turkey kurulumu başlatılıyor...", true);
                var psi = new ProcessStartInfo
                {
                    FileName = yol,
                    // Kaynak proje Inno Setup 6 kullanıyor. /NORESTART otomatik reboot'u önler.
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                using (var kurulum = Process.Start(psi))
                {
                    if (kurulum == null) throw new InvalidOperationException("Kurulum açılamadı.");
                    await kurulum.WaitForExitAsync();
                    if (kurulum.ExitCode != 0)
                        throw new InvalidOperationException("Kurulum çıkış kodu: " + kurulum.ExitCode);
                }

                string? uygulama = SplitWireExeBul();
                if (uygulama == null)
                {
                    DurumGuncelle("SplitWire kurulumu tamamlandı; uygulama konumu belirlenemedi. Başlat menüsünden açıp DPI yöntemini seçin.", false);
                    return;
                }
                Process.Start(new ProcessStartInfo(uygulama) { UseShellExecute = true });
                DurumGuncelle("SplitWire-Turkey açıldı. DPI yönteminizi kendi arayüzünden seçip etkinleştirin.", false);
            }
            catch (Exception ex)
            {
                DurumGuncelle("SplitWire indirme/kurulum hatası: " + ex.Message, false, true);
            }
            finally
            {
                cephanelikKurulumCalisiyor = false;
            }
        }

        private static string? SplitWireExeBul()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\SplitWire-Turkey"))
                {
                    var konum = key?.GetValue("InstallPath") as string;
                    if (!string.IsNullOrWhiteSpace(konum))
                    {
                        string exe = Path.Combine(konum, "SplitWire-Turkey.exe");
                        if (File.Exists(exe)) return exe;
                    }
                }
            }
            catch { /* Kayıt defteri yoksa varsayılan klasörleri kontrol et. */ }
            foreach (var kok in new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                string exe = Path.Combine(kok, "SplitWire-Turkey", "SplitWire-Turkey.exe");
                if (File.Exists(exe)) return exe;
            }
            return null;
        }

        // SoundVibe: pembe düğme -> resmi, sabit sürüm kurulumu indirilir,
        // SHA-256 ile kontrol edilir ve kullanıcıya görünür kurulum sihirbazı başlatılır.
        private async void BtnSoundVibeIndir_Click(object sender, RoutedEventArgs e)
        {
            if (cephanelikKurulumCalisiyor)
            {
                DurumGuncelle("Başka bir Cephanelik kurulumu devam ediyor.", true);
                return;
            }
            if (MessageBox.Show(
                    "SoundVibe v1.0 (~48 MB) resmi GitHub sürümünden indirilecek, " +
                    "SHA-256 ile doğrulanacak ve kurulum sihirbazı açılacak. Devam edilsin mi?",
                    "ChaosBoost - SoundVibe", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    != MessageBoxResult.Yes) return;

            cephanelikKurulumCalisiyor = true;
            try
            {
                string yol = await DogrulanmisSetupIndirAsync(
                    "SoundVibe", SoundVibeSetupUrl, "SoundVibe_Setup.exe", SoundVibeSetupSha256);
                Process.Start(new ProcessStartInfo(yol) { UseShellExecute = true });
                DurumGuncelle("SoundVibe kurulum sihirbazı açıldı. Kurulum ekranındaki adımları tamamlayın.", false);
            }
            catch (Exception ex)
            {
                DurumGuncelle("SoundVibe indirme/kurulum hatası: " + ex.Message, false, true);
            }
            finally
            {
                cephanelikKurulumCalisiyor = false;
            }
        }

        private async void BtnWinRarKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("RARLab.WinRAR");
        private async void Btn7ZipKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("7zip.7zip");
        private async void BtnSpotifyKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Spotify.Spotify");
        private async void BtnVLCKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("VideoLAN.VLC");
        private async void BtnGitKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Git.Git");
        private async void BtnVSCodeKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Microsoft.VisualStudioCode");
        private async void BtnPowerToysKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Microsoft.PowerToys");

        private async void BtnVCRedistKur_Click(object sender, RoutedEventArgs e)
        {
            if (cephanelikKurulumCalisiyor)
            {
                DurumGuncelle("Başka bir Cephanelik kurulumu devam ediyor.", true);
                return;
            }
            cephanelikKurulumCalisiyor = true;
            try
            {
                DurumGuncelle("VC++ x64 ve x86 paketleri kuruluyor...", true);
                bool x64 = await SistemKomutlari.CMDCalistirAsync(WingetKomutu("Microsoft.VCRedist.2015+.x64"));
                bool x86 = await SistemKomutlari.CMDCalistirAsync(WingetKomutu("Microsoft.VCRedist.2015+.x86"));
                DurumGuncelle(x64 && x86 ? "VC++ WinGet işlemleri tamamlandı." : "VC++ paketlerinden biri tamamlanamadı; kurulu uygulamaları kontrol edin.", false, !x64 || !x86);
            }
            finally
            {
                cephanelikKurulumCalisiyor = false;
            }
        }
        private async void BtnDirectXKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Microsoft.DirectX");
        private async void BtnNotepadKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Notepad++.Notepad++");
        private async void BtnEverythingKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("voidtools.Everything");
        private async void BtnSumatraKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("SumatraPDF.SumatraPDF");
        private async void BtnShareXKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("ShareX.ShareX");
        private async void BtnImageGlassKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("ImageGlass.ImageGlass");
        private async void BtnDiscordKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Discord.Discord");
        private async void BtnTelegramKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("Telegram.TelegramDesktop");
        private async void BtnQbitKur_Click(object sender, RoutedEventArgs e) => await WingetKurAsync("qBittorrent.qBittorrent");

        // =========================================================
        // 6. LTSC MODÜLLERİ
        // =========================================================
        private async void BtnXboxKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Xbox servisleri kuruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("winget install --id 9MV0B5HZVK9Z -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9NZKPSTSNW4P -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle(basari ? "Xbox servisleri tamamlandı." : "HATA: İndirme başarısız.", false, !basari); }
        private async void BtnMsStore_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Microsoft Store sisteme entegre ediliyor...", true); bool ok = await SistemKomutlari.CMDCalistirAsync("wsreset -i"); DurumGuncelle(ok ? "Microsoft Store kurulum emri verildi." : "Store kurulum komutu çalıştırılamadı.", false, !ok); }
        private async void BtnModernAppsKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Modern Araçlar kuruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("winget install --id 9WZDNCRFHVN5 -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9MZ95KL8MR0L -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle(basari ? "Modern araçlar bitti." : "HATA: Sistem uygun değil.", false, !basari); }
        private async void BtnCodecKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Codec paketleri kuruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("winget install --id 9N4WGH0Z6VHQ -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9N4D0MSV0403 -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle(basari ? "Codec'ler yüklendi." : "HATA: Kurulum başarısız.", false, !basari); }
        private async void BtnClassicPhoto_Click(object sender, RoutedEventArgs e) { bool ok = await SistemKomutlari.CMDCalistirAsync(@"reg add ""HKCR\jpegfile\shell\open\command"" /ve /t REG_EXPAND_SZ /d ""%SystemRoot%\System32\rundll32.exe \""%ProgramFiles%\Windows Photo Viewer\PhotoViewer.dll\"", ImageView_Fullscreen %1"" /f"); DurumGuncelle(ok ? "Klasik fotoğraf görüntüleyici ilişkilendirme komutu çalıştırıldı; varsayılan uygulamaları kontrol edin." : "İşlem başarısız: Klasik fotoğraf görüntüleyici aktif.", false, !ok); }
        private async void BtnDirectPlay_Click(object sender, RoutedEventArgs e) { DurumGuncelle("DirectPlay aktif ediliyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("dism /online /Enable-Feature /FeatureName:DirectPlay /All /Quiet /NoRestart"); DurumGuncelle(basari ? "DirectPlay aktif edildi." : "HATA: DISM modülü hata verdi.", false, !basari); }
        private async void BtnNet35_Click(object sender, RoutedEventArgs e) { DurumGuncelle(".NET 3.5 indiriliyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("dism /online /Enable-Feature /FeatureName:NetFx3 /All /Quiet /NoRestart"); DurumGuncelle(basari ? ".NET 3.5 başarıyla yüklendi." : "HATA: İndirme başarısız (İnternetinizi kontrol edin).", false, !basari); }
    }
}