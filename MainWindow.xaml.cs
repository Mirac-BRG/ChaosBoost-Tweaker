using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ChaosBoostTweaker
{
    public partial class MainWindow : Window
    {
        // Hayalet Log Dosyası Yolu
        private string logDosyaYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChaosBoost_Log.txt");

        public MainWindow()
        {
            InitializeComponent();
            LogTut("==================================================");
            LogTut("ChaosBoost Tweaker Başlatıldı. Yönetici yetkileri devrede.");
        }

        // =========================================================
        // 0. LOG VE DURUM MOTORU
        // =========================================================
        private void LogTut(string mesaj)
        {
            try { File.AppendAllText(logDosyaYolu, $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] - {mesaj}\n"); } catch { }
        }

        private void DurumGuncelle(string mesaj, bool aktifMi)
        {
            TxtDurum.Text = mesaj;
            DurumCubugu.Visibility = aktifMi ? Visibility.Visible : Visibility.Hidden;
            TxtDurum.Foreground = new System.Windows.Media.SolidColorBrush(aktifMi ? System.Windows.Media.Color.FromRgb(136, 204, 255) : System.Windows.Media.Color.FromRgb(0, 255, 204));
            LogTut(mesaj);
        }

        // =========================================================
        // 1. ARAYÜZ MEKANİKLERİ
        // =========================================================
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
        // 2. SİSTEM KOMUT MOTORU (ASENKRON)
        // =========================================================
        private async Task GizliKomutCalistirAsync(string komut)
        {
            ProcessStartInfo proc = new ProcessStartInfo("cmd.exe", "/c " + komut) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true };
            using (Process process = new Process { StartInfo = proc }) { process.Start(); await process.WaitForExitAsync(); }
        }

        private void RegAyarUygula(RegistryKey anahtar, string yol, string degerAdi, int deger)
        {
            try { using (RegistryKey key = anahtar.CreateSubKey(yol, true)) { key?.SetValue(degerAdi, deger, RegistryValueKind.DWord); } }
            catch (Exception ex) { LogTut($"HATA: Regedit sızma başarısız! {ex.Message}"); }
        }

        // =========================================================
        // 3. HIZLI AYARLAR (Optimizasyon)
        // =========================================================
        private async void BtnRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Geri Yükleme Noktası oluşturuluyor...", true);
            await GizliKomutCalistirAsync("powershell -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description 'ChaosBoost Ilk Kurulum' -RestorePointType 'MODIFY_SETTINGS'\"");
            DurumGuncelle("Geri Yükleme Noktası oluşturuldu.", false);
        }

        private async void BtnTelemetriKapat_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Telemetri kapatılıyor...", true);
            RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            await GizliKomutCalistirAsync("sc config DiagTrack start= disabled & net stop DiagTrack");
            DurumGuncelle("Telemetri tamamen kapatıldı.", false);
        }
        private async void BtnTelemetriGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Telemetri açılıyor...", true);
            RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 3);
            await GizliKomutCalistirAsync("sc config DiagTrack start= auto & net start DiagTrack");
            DurumGuncelle("Telemetri geri açıldı.", false);
        }

        private void BtnBingKapat_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0); RegAyarUygula(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1); DurumGuncelle("Bing kapatıldı.", false); }
        private void BtnBingGeriAl_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1); RegAyarUygula(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 0); DurumGuncelle("Bing açıldı.", false); }
        private void BtnCortanaKapat_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0); DurumGuncelle("Cortana kapatıldı.", false); }
        private void BtnCortanaGeriAl_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1); DurumGuncelle("Cortana açıldı.", false); }
        private void BtnAdIdKapat_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0); DurumGuncelle("Ad ID kapatıldı.", false); }
        private void BtnAdIdGeriAl_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1); DurumGuncelle("Ad ID açıldı.", false); }
        private void BtnWERKapat_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1); DurumGuncelle("WER kapatıldı.", false); }
        private void BtnWERGeriAl_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 0); DurumGuncelle("WER açıldı.", false); }
        private void BtnKonumKapat_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1); DurumGuncelle("Konum kapatıldı.", false); }
        private void BtnKonumGeriAl_Click(object sender, RoutedEventArgs e) { RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 0); DurumGuncelle("Konum açıldı.", false); }

        private async void BtnUpdateKapat_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Update kapatılıyor...", true);
            RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1);
            await GizliKomutCalistirAsync("sc config wuauserv start= disabled & net stop wuauserv");
            DurumGuncelle("Update kapatıldı.", false);
        }
        private async void BtnUpdateGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Update açılıyor...", true);
            RegAyarUygula(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 0);
            await GizliKomutCalistirAsync("sc config wuauserv start= demand & net start wuauserv");
            DurumGuncelle("Update açıldı.", false);
        }

        // YENİ EKLENEN: KLASİK NOT DEFTERİ MOTORU
        private async void BtnClassicNotepad_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Klasik Not Defteri geri getiriliyor...", true);
            await GizliKomutCalistirAsync(@"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\notepad.exe"" /ve /d ""%SystemRoot%\notepad.exe"" /f & reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\notepad.exe"" /v ""Debugger"" /t REG_SZ /d """" /f & powershell -Command ""Get-AppxPackage *Microsoft.WindowsNotepad* | Remove-AppxPackage""");
            DurumGuncelle("Klasik sekmesiz Not Defteri aktif edildi.", false);
        }
        private async void BtnClassicNotepadGeriAl_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Windows 11 Not Defteri geri yükleniyor (Sürebilir)...", true);
            await GizliKomutCalistirAsync(@"reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\notepad.exe"" /f & reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\notepad.exe"" /v ""Debugger"" /f & wsreset -i");
            DurumGuncelle("UWP Not Defteri ayarlara döndürüldü (Kurulum arka planda tamamlanacak).", false);
        }

        // NÜKLEER SEÇENEK: EDGE İMHASI
        private async void BtnEdgeSil_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Microsoft Edge sistemden kazınıyor (Nükleer Seçenek)...", true);
            string nukeKomut = @"taskkill /F /IM msedge.exe /T & for /d %i in (""C:\Program Files (x86)\Microsoft\Edge\Application\*"") do (if exist ""%i\Installer\setup.exe"" ""%i\Installer\setup.exe"" --uninstall --system-level --verbose-logging --force-uninstall) & reg add ""HKLM\SOFTWARE\Microsoft\EdgeUpdate"" /v DoNotUpdateToEdgeWithChromium /t REG_DWORD /d 1 /f";
            await GizliKomutCalistirAsync(nukeKomut);
            DurumGuncelle("Microsoft Edge başarıyla yok edildi.", false);
        }

        private async void BtnExplorerYenile_Click(object sender, RoutedEventArgs e) { await GizliKomutCalistirAsync("taskkill /f /im explorer.exe & start explorer.exe"); DurumGuncelle("Gezgin yenilendi.", false); }

        // =========================================================
        // 4. PERFORMANS VE AĞ SEÇENEKLERİ (Sleeper Ayarlar)
        // =========================================================
        private async void BtnUltimatePower_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Nihai Performans Planı aktif ediliyor...", true);
            await GizliKomutCalistirAsync("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 & powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
            DurumGuncelle("Nihai Performans Modu devrede.", false);
        }
        private async void BtnVBS_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("VBS ve Yalıtım kapatılıyor...", true);
            await GizliKomutCalistirAsync("bcdedit /set hypervisorlaunchtype off & reg add \"HKLM\\System\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f");
            DurumGuncelle("VBS kapatıldı (Yeniden başlatma gerektirir).", false);
        }
        private async void BtnXboxDVR_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Xbox DVR kapatılıyor...", true);
            await GizliKomutCalistirAsync("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 0 /f & reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v AllowGameDVR /t REG_DWORD /d 0 /f");
            DurumGuncelle("Xbox DVR kapatıldı.", false);
        }
        private async void BtnHibernate_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Uyku/Hibernate kapatılıyor...", true);
            await GizliKomutCalistirAsync("powercfg -h off");
            DurumGuncelle("Hibernate kapatıldı. Diskte yer açıldı.", false);
        }
        private async void BtnMouseAccel_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Fare ivmesi kapatılıyor...", true);
            await GizliKomutCalistirAsync("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f & reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f & reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f");
            DurumGuncelle("Fare ivmesi kapatıldı.", false);
        }

        // Yeni Oyun İyileştirmeleri
        private async void BtnStickyKeys_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Yapışkan Tuşlar kapatılıyor...", true);
            await GizliKomutCalistirAsync("reg add \"HKCU\\Control Panel\\Accessibility\\StickyKeys\" /v Flags /t REG_SZ /d 506 /f & reg add \"HKCU\\Control Panel\\Accessibility\\Keyboard Response\" /v Flags /t REG_SZ /d 122 /f & reg add \"HKCU\\Control Panel\\Accessibility\\ToggleKeys\" /v Flags /t REG_SZ /d 58 /f");
            DurumGuncelle("Yapışkan Tuşlar tamamen kapatıldı.", false);
        }
        private async void BtnFSO_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Tam Ekran İyileştirmeleri (FSO) kapatılıyor...", true);
            await GizliKomutCalistirAsync("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f & reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f");
            DurumGuncelle("Özel Tam Ekran modu aktif, FSO kapatıldı.", false);
        }
        private async void BtnSysMain_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("SysMain (Superfetch) durduruluyor...", true);
            await GizliKomutCalistirAsync("sc config SysMain start= disabled & net stop SysMain");
            DurumGuncelle("SysMain kapatıldı. RAM ve Disk rahatladı.", false);
        }

        private async void BtnNetThrottling_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Ağ gecikmesi (Throttling) kapatılıyor...", true);
            await GizliKomutCalistirAsync("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 0xffffffff /f");
            DurumGuncelle("Ağ kısıtlamaları kaldırıldı.", false);
        }
        private async void BtnDNS_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Cloudflare DNS (1.1.1.1) uygulanıyor...", true);
            await GizliKomutCalistirAsync("powershell -Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Set-DnsClientServerAddress -ServerAddresses '1.1.1.1','1.0.0.1'\"");
            DurumGuncelle("DNS başarıyla değiştirildi.", false);
        }
        private async void BtnSMBv1_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("SMBv1 (WannaCry Açığı) kapatılıyor...", true);
            await GizliKomutCalistirAsync("dism /online /Disable-Feature /FeatureName:SMB1Protocol /Quiet /NoRestart");
            DurumGuncelle("SMBv1 açığı kapatıldı.", false);
        }
        private async void BtnWin11Menu_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Klasik sağ tık menüsü açılıyor...", true);
            await GizliKomutCalistirAsync("reg add \"HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32\" /ve /d \"\" /f");
            DurumGuncelle("Klasik menü devrede (Gezgini yenileyin).", false);
        }
        private async void BtnShowHidden_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Gizli dosyalar ve uzantılar açılıyor...", true);
            await GizliKomutCalistirAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Hidden /t REG_DWORD /d 1 /f & reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v HideFileExt /t REG_DWORD /d 0 /f");
            DurumGuncelle("Sistem yöneticisi görüşü devrede.", false);
        }
        private async void BtnUwpBloat_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Gereksiz UWP Çöpleri siliniyor (Bu işlem 1-2 dk sürebilir)...", true);
            await GizliKomutCalistirAsync("powershell -Command \"Get-AppxPackage *bing* | Remove-AppxPackage; Get-AppxPackage *people* | Remove-AppxPackage; Get-AppxPackage *solitaire* | Remove-AppxPackage; Get-AppxPackage *zune* | Remove-AppxPackage\"");
            DurumGuncelle("UWP Çöpleri sistemden temizlendi.", false);
        }

        // =========================================================
        // 5. CEPHANELİK (Winget Kurulumları)
        // =========================================================
        private async Task WingetKurAsync(string id) { DurumGuncelle($"{id} kuruluyor...", true); await GizliKomutCalistirAsync($"winget install --id {id} -e --silent --accept-source-agreements --accept-package-agreements"); DurumGuncelle($"{id} kurulumu tamamlandı.", false); }
        private async void BtnVCRedistKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("VC++ Paketleri kuruluyor...", true); await GizliKomutCalistirAsync("winget install --id Microsoft.VCRedist.2015+.x64 -e --silent --accept-source-agreements --accept-package-agreements && winget install --id Microsoft.VCRedist.2015+.x86 -e --silent --accept-source-agreements --accept-package-agreements"); DurumGuncelle("VC++ paketleri tamamlandı.", false); }
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
        private async void BtnXboxKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Xbox servisleri kuruluyor...", true); await GizliKomutCalistirAsync("winget install --id 9MV0B5HZVK9Z -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9NZKPSTSNW4P -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle("Xbox servisleri tamamlandı.", false); }

        // Yeni: MS Store Kurulumu
        private async void BtnMsStore_Click(object sender, RoutedEventArgs e)
        {
            DurumGuncelle("Microsoft Store sisteme entegre ediliyor (wsreset)...", true);
            await GizliKomutCalistirAsync("wsreset -i");
            DurumGuncelle("Microsoft Store kurulum emri verildi (Arka planda inebilir).", false);
        }

        private async void BtnModernAppsKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Modern Araçlar kuruluyor...", true); await GizliKomutCalistirAsync("winget install --id 9WZDNCRFHVN5 -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9MZ95KL8MR0L -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle("Modern araçlar bitti.", false); }
        private async void BtnCodecKur_Click(object sender, RoutedEventArgs e) { DurumGuncelle("Codec paketleri kuruluyor...", true); await GizliKomutCalistirAsync("winget install --id 9N4WGH0Z6VHQ -e --silent --accept-package-agreements --accept-source-agreements && winget install --id 9N4D0MSV0403 -e --silent --accept-package-agreements --accept-source-agreements"); DurumGuncelle("Codec'ler yüklendi.", false); }
        private async void BtnClassicPhoto_Click(object sender, RoutedEventArgs e) { await GizliKomutCalistirAsync(@"reg add ""HKCR\jpegfile\shell\open\command"" /ve /t REG_EXPAND_SZ /d ""%SystemRoot%\System32\rundll32.exe \""%ProgramFiles%\Windows Photo Viewer\PhotoViewer.dll\"", ImageView_Fullscreen %1"" /f"); DurumGuncelle("Klasik fotoğraf görüntüleyici aktif.", false); }
        private async void BtnDirectPlay_Click(object sender, RoutedEventArgs e) { DurumGuncelle("DirectPlay (Eski Oyun Desteği) aktif ediliyor...", true); await GizliKomutCalistirAsync("dism /online /Enable-Feature /FeatureName:DirectPlay /All /Quiet /NoRestart"); DurumGuncelle("DirectPlay aktif edildi.", false); }
        private async void BtnNet35_Click(object sender, RoutedEventArgs e) { DurumGuncelle(".NET 3.5 indiriliyor ve kuruluyor (Sürebilir)...", true); await GizliKomutCalistirAsync("dism /online /Enable-Feature /FeatureName:NetFx3 /All /Quiet /NoRestart"); DurumGuncelle(".NET 3.5 başarıyla yüklendi.", false); }
    }
}