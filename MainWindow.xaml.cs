using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChaosBoostTweaker
{
    // =========================================================
    // 1. ÇEKİRDEK MOTOR (SİSTEM RADARI VE İNFAZ)
    // =========================================================
    public static class SistemKomutlari
    {
        // GERÇEK HATA YAKALAYICI: İşlem başarılıysa true, hata verirse false döner.
        public static async Task<bool> CMDCalistirAsync(string komut)
        {
            try
            {
                ProcessStartInfo proc = new ProcessStartInfo("cmd.exe", "/c " + komut)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = proc })
                {
                    process.Start();
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0; // 0 = Hata Yok
                }
            }
            catch { return false; }
        }

        // GÜVENLİ REGEDIT YAZICISI
        public static bool RegAyarUygula(RegistryKey anahtar, string yol, string degerAdi, int deger)
        {
            try
            {
                using (RegistryKey key = anahtar.CreateSubKey(yol, true))
                {
                    key?.SetValue(degerAdi, deger, RegistryValueKind.DWord);
                    return true;
                }
            }
            catch { return false; }
        }

        // SİSTEM DURUMU OKUYUCU (Programın "Körlüğünü" Bitiren Radar)
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
    // 2. ANA ARAYÜZ (VİTRİN) KONTROLLERİ
    // =========================================================
    public partial class MainWindow : Window
    {
        private string logDosyaYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChaosBoost_Log.txt");

        public MainWindow()
        {
            InitializeComponent();
            LogTut("==================================================");
            LogTut("ChaosBoost Tweaker v2.0 Başlatıldı. Sistem radarı ve hata yakalayıcı aktif.");
        }

        private void LogTut(string mesaj)
        {
            try { File.AppendAllText(logDosyaYolu, $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] - {mesaj}\n"); } catch { }
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
        // 3. AKILLI HIZLI AYARLAR (Durum Okumalı)
        // =========================================================
        private async void BtnRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Geri Yükleme Noktası oluşturuluyor...", true);
            bool basari = await SistemKomutlari.CMDCalistirAsync("powershell -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description 'ChaosBoost Ilk Kurulum' -RestorePointType 'MODIFY_SETTINGS'\"");
            DurumGuncelle(basari ? "Geri Yükleme Noktası oluşturuldu." : "HATA: Sistem Koruması devre dışı olabilir!", false, !basari);
        }

        private async void BtnTelemetriKapat_Click(object sender, RoutedEventArgs e)
        {
            if (SistemKomutlari.RegAyarOkunuyorMu(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0))
            {
                DurumGuncelle("Telemetri zaten kapalı durumda. Es geçildi.", false); return;
            }
            DurumGuncelle("Telemetri kapatılıyor...", true);
            SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            bool basari = await SistemKomutlari.CMDCalistirAsync("sc config DiagTrack start= disabled & net stop DiagTrack");
            DurumGuncelle(basari ? "Telemetri tamamen kapatıldı." : "HATA: Telemetri servisi durdurulamadı!", false, !basari);
        }

        private async void BtnTelemetriGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Telemetri açılıyor...", true);
            SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 3);
            await SistemKomutlari.CMDCalistirAsync("sc config DiagTrack start= auto & net start DiagTrack");
            DurumGuncelle("Telemetri geri açıldı.", false);
        }

        private void BtnBingKapat_Click(object sender, RoutedEventArgs e)
        {
            if (SistemKomutlari.RegAyarOkunuyorMu(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0)) { DurumGuncelle("Bing zaten kapalı.", false); return; }
            SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
            SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
            DurumGuncelle("Bing kapatıldı.", false);
        }
        private void BtnBingGeriAl_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1); SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 0); DurumGuncelle("Bing açıldı.", false); }

        private void BtnCortanaKapat_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0); DurumGuncelle("Cortana kapatıldı.", false); }
        private void BtnCortanaGeriAl_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1); DurumGuncelle("Cortana açıldı.", false); }
        private void BtnAdIdKapat_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0); DurumGuncelle("Ad ID kapatıldı.", false); }
        private void BtnAdIdGeriAl_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1); DurumGuncelle("Ad ID açıldı.", false); }
        private void BtnWERKapat_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1); DurumGuncelle("WER kapatıldı.", false); }
        private void BtnWERGeriAl_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 0); DurumGuncelle("WER açıldı.", false); }
        private void BtnKonumKapat_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1); DurumGuncelle("Konum kapatıldı.", false); }
        private void BtnKonumGeriAl_Click(object sender, RoutedEventArgs e) { SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 0); DurumGuncelle("Konum açıldı.", false); }

        private async void BtnUpdateKapat_Click(object sender, RoutedEventArgs e)
        {
            if (SistemKomutlari.RegAyarOkunuyorMu(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1)) { DurumGuncelle("Update zaten kapalı.", false); return; }
            DurumGuncelle("Update kapatılıyor...", true);
            SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1);
            bool basari = await SistemKomutlari.CMDCalistirAsync("sc config wuauserv start= disabled & net stop wuauserv");
            DurumGuncelle(basari ? "Update kapatıldı." : "HATA: Windows Update durdurulamadı!", false, !basari);
        }
        private async void BtnUpdateGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Update açılıyor...", true);
            SistemKomutlari.RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 0);
            await SistemKomutlari.CMDCalistirAsync("sc config wuauserv start= demand & net start wuauserv");
            DurumGuncelle("Update açıldı.", false);
        }

        private async void BtnClassicNotepad_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Klasik Not Defteri geri getiriliyor...", true);
            bool basari = await SistemKomutlari.CMDCalistirAsync(@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\notepad.exe"" /ve /d ""%SystemRoot%\notepad.exe"" /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\notepad.exe"" /v ""Debugger"" /t REG_SZ /d """" /f & powershell -Command ""Get-AppxPackage *Microsoft.WindowsNotepad* | Remove-AppxPackage""");
            DurumGuncelle(basari ? "Klasik Not Defteri aktif edildi." : "HATA: Not Defteri yaması başarısız.", false, !basari);
        }
        private async void BtnClassicNotepadGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Windows 11 Not Defteri geri yükleniyor...", true);
            await SistemKomutlari.CMDCalistirAsync(@"reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\notepad.exe"" /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\notepad.exe"" /v ""Debugger"" /f & wsreset -i");
            DurumGuncelle("UWP Not Defteri ayarlara döndürüldü (Mağazadan güncellenecek).", false);
        }

        private async void BtnEdgeSil_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Microsoft Edge sistemden kazınıyor (Nükleer Seçenek)...", true);
            string nukeKomut = @"taskkill /F /IM msedge.exe /T & for /d %i in (""C:\Program Files (x86)\Microsoft\Edge\Application\*"") do (if exist ""%i\Installer\setup.exe"" ""%i\Installer\setup.exe"" --uninstall --system-level --verbose-logging --force-uninstall) & reg add ""HKLM\SOFTWARE\Microsoft\EdgeUpdate"" /v DoNotUpdateToEdgeWithChromium /t REG_DWORD /d 1 /f";
            bool basari = await SistemKomutlari.CMDCalistirAsync(nukeKomut);
            DurumGuncelle(basari ? "Microsoft Edge başarıyla yok edildi." : "UYARI: Edge tam olarak silinemedi (Bazı dosyalar kilitli olabilir).", false, !basari);
        }

        private async void BtnExplorerYenile_Click(object sender, RoutedEventArgs e) { await SistemKomutlari.CMDCalistirAsync("taskkill /f /im explorer.exe & start explorer.exe"); DurumGuncelle("Gezgin yenilendi.", false); }

        // =========================================================
        // 4. PERFORMANS VE AĞ SEÇENEKLERİ
        // =========================================================
        private async void BtnUltimatePower_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Nihai Performans Planı aktif ediliyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 & powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61"); DurumGuncelle(basari ? "Performans Modu devrede." : "HATA: Güç planı bulunamadı.", false, !basari); }
        private async void BtnVBS_Click(object sender, RoutedEventArgs e) { DurumGuncelle("VBS kapatılıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("bcdedit /set hypervisorlaunchtype off & reg add \"HKLM\\System\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f"); DurumGuncelle(basari ? "VBS kapatıldı (Yeniden başlatma gerektirir)." : "HATA: BIOS üzerinden engelleniyor olabilir.", false, !basari); }
        private async void BtnXboxDVR_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Xbox DVR kapatılıyor...", true); await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 0 /f & reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v AllowGameDVR /t REG_DWORD /d 0 /f"); DurumGuncelle("Xbox DVR kapatıldı.", false); }
        private async void BtnHibernate_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Hibernate kapatılıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("powercfg -h off"); DurumGuncelle(basari ? "Hibernate kapatıldı. Diskte yer açıldı." : "HATA: Sistem buna izin vermedi.", false, !basari); }
        private async void BtnMouseAccel_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Fare ivmesi kapatılıyor...", true); await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f & reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f & reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f"); DurumGuncelle("Fare ivmesi kapatıldı.", false); }
        private async void BtnStickyKeys_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Yapışkan Tuşlar kapatılıyor...", true); await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Control Panel\\Accessibility\\StickyKeys\" /v Flags /t REG_SZ /d 506 /f & reg add \"HKCU\\Control Panel\\Accessibility\\Keyboard Response\" /v Flags /t REG_SZ /d 122 /f & reg add \"HKCU\\Control Panel\\Accessibility\\ToggleKeys\" /v Flags /t REG_SZ /d 58 /f"); DurumGuncelle("Yapışkan Tuşlar kapatıldı.", false); }
        private async void BtnFSO_Click(object sender, RoutedEventArgs e) { DurumGuncelle("FSO kapatılıyor...", true); await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f & reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f"); DurumGuncelle("Özel Tam Ekran modu aktif.", false); }
        private async void BtnSysMain_Click(object sender, RoutedEventArgs e) { DurumGuncelle("SysMain durduruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("sc config SysMain start= disabled & net stop SysMain"); DurumGuncelle(basari ? "SysMain kapatıldı. RAM rahatladı." : "HATA: SysMain servisi durdurulamadı.", false, !basari); }
        private async void BtnNetThrottling_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Ağ gecikmesi kapatılıyor...", true); await SistemKomutlari.CMDCalistirAsync("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 0xffffffff /f"); DurumGuncelle("Ağ kısıtlamaları kaldırıldı.", false); }
        private async void BtnDNS_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Cloudflare DNS uygulanıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("powershell -Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Set-DnsClientServerAddress -ServerAddresses '1.1.1.1','1.0.0.1'\""); DurumGuncelle(basari ? "DNS başarıyla değiştirildi." : "HATA: Ağ bağdaştırıcısı ayarı başarısız.", false, !basari); }
        private async void BtnSMBv1_Click(object sender, RoutedEventArgs e) { DurumGuncelle("SMBv1 kapatılıyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("dism /online /Disable-Feature /FeatureName:SMB1Protocol /Quiet /NoRestart"); DurumGuncelle(basari ? "SMBv1 açığı kapatıldı." : "HATA: DISM modülü işlem yapamadı.", false, !basari); }
        private async void BtnWin11Menu_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Klasik sağ tık menüsü açılıyor...", true); await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /ve /d \"\" /f"); DurumGuncelle("Klasik menü devrede (Gezgini yenileyin).", false); }
        private async void BtnShowHidden_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Gizli dosyalar açılıyor...", true); await SistemKomutlari.CMDCalistirAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 1 /f & reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 0 /f"); DurumGuncelle("Sistem yöneticisi görüşü devrede.", false); }
        private async void BtnUwpBloat_Click(object sender, RoutedEventArgs e) { DurumGuncelle("UWP Çöpleri siliniyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("powershell -Command \"Get-AppxPackage *bing* | Remove-AppxPackage; Get-AppxPackage *people* | Remove-AppxPackage; Get-AppxPackage *solitaire* | Remove-AppxPackage; Get-AppxPackage *zune* | Remove-AppxPackage\""); DurumGuncelle(basari ? "UWP Çöpleri sistemden temizlendi." : "HATA: Paket işlemi sırasında hata oluştu.", false, !basari); }

        // =========================================================
        // 5. CEPHANELİK (Güvenli Winget Motoru)
        // =========================================================
        private async Task WingetKurAsync(string id)
        {
            DurumGuncelle($"{id} kuruluyor...", true);
            bool basari = await SistemKomutlari.CMDCalistirAsync($"winget install --id {id} -e --silent --accept-source-agreements --accept-package-agreements");
            DurumGuncelle(basari ? $"{id} kurulumu tamamlandı." : $"HATA: {id} indirilemedi! (İnternet bağlantınızı kontrol edin)", false, !basari);
        }

        private async void BtnVCRedistKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("VC++ Paketleri kuruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("winget install --id Microsoft.VCRedist.2015+.x64 -e --silent --accept-source-agreements --accept-package-agreements && winget install --id Microsoft.VCRedist.2015+.x86 -e --silent --accept-source-agreements --accept-package-agreements"); DurumGuncelle(basari ? "VC++ paketleri tamamlandı." : "HATA: İndirme başarısız.", false, !basari); }
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
        private async void BtnMsStore_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Microsoft Store sisteme entegre ediliyor...", true); await SistemKomutlari.CMDCalistirAsync("wsreset -i"); DurumGuncelle("Microsoft Store kurulum emri verildi.", false); }
        private async void BtnModernAppsKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Modern Araçlar kuruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("winget install --id 9WZDNCRFHVN5 -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9MZ95KL8MR0L -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle(basari ? "Modern araçlar bitti." : "HATA: Sistem uygun değil.", false, !basari); }
        private async void BtnCodecKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Codec paketleri kuruluyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("winget install --id 9N4WGH0Z6VHQ -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9N4D0MSV0403 -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle(basari ? "Codec'ler yüklendi." : "HATA: Kurulum başarısız.", false, !basari); }
        private async void BtnClassicPhoto_Click(object sender, RoutedEventArgs e) { await SistemKomutlari.CMDCalistirAsync(@"reg add ""HKCR\jpegfile\shell\open\command"" /ve /t REG_EXPAND_SZ /d ""%SystemRoot%\System32\rundll32.exe \""%ProgramFiles%\Windows Photo Viewer\PhotoViewer.dll\"", ImageView_Fullscreen %1"" /f"); DurumGuncelle("Klasik fotoğraf görüntüleyici aktif.", false); }
        private async void BtnDirectPlay_Click(object sender, RoutedEventArgs e) { DurumGuncelle("DirectPlay aktif ediliyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("dism /online /Enable-Feature /FeatureName:DirectPlay /All /Quiet /NoRestart"); DurumGuncelle(basari ? "DirectPlay aktif edildi." : "HATA: DISM modülü hata verdi.", false, !basari); }
        private async void BtnNet35_Click(object sender, RoutedEventArgs e) { DurumGuncelle(".NET 3.5 indiriliyor...", true); bool basari = await SistemKomutlari.CMDCalistirAsync("dism /online /Enable-Feature /FeatureName:NetFx3 /All /Quiet /NoRestart"); DurumGuncelle(basari ? ".NET 3.5 başarıyla yüklendi." : "HATA: İndirme başarısız (İnternetinizi kontrol edin).", false, !basari); }
    }
}