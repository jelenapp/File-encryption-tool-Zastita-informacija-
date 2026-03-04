using System;
using System.IO;
using System.Threading.Tasks;

namespace zastitaInfoProjekat
{
    public class ImprovedFileSystemWatcherManager
    {
        private FileSystemWatcher _watcher; //objekat koji prati fajlove
        private string _targetDirectory; //direktorijum koji se prati.
        private string _outputDirectory; //gde se čuvaju šifrovani fajlovi.
        private int _selectedAlgorithm; //indeks izabranog algoritma.
        private object[] _algorithmParameters; //parametri algoritma (npr. ključ, IV, broj redova).
        private bool _isWatching = false; //flag koji pokazuje da li watcher radi.

        public bool IsWatching => _isWatching;

        // Event za logovanje
        public event Action<string, System.Drawing.Color> LogMessage;

        public void StartWatching(string targetDirectory, string outputDirectory, int algorithmChoice, object[] parameters)
        {
            try
            {
                if (_isWatching)
                {
                    LogMessage?.Invoke("File System Watcher već radi!", System.Drawing.Color.Orange);
                    return;
                }

                // Validacija parametara
                if (string.IsNullOrEmpty(targetDirectory) || string.IsNullOrEmpty(outputDirectory))
                {
                    LogMessage?.Invoke("Greška: Direktorijumi nisu podešeni!", System.Drawing.Color.Red);
                    return;
                }

                if (algorithmChoice < 1 || algorithmChoice > 3 || parameters == null)
                {
                    LogMessage?.Invoke("Greška: Algoritam nije ispravno konfigurisan!", System.Drawing.Color.Red);
                    return;
                }

                _targetDirectory = targetDirectory;
                _outputDirectory = outputDirectory;
                _selectedAlgorithm = algorithmChoice;
                _algorithmParameters = parameters;

                // Kreiraj direktorijume ako ne postoje
                if (!Directory.Exists(_targetDirectory))
                {
                    Directory.CreateDirectory(_targetDirectory);
                    LogMessage?.Invoke($"Kreiran Target direktorijum: {_targetDirectory}", System.Drawing.Color.Cyan);
                }
                if (!Directory.Exists(_outputDirectory))
                {
                    Directory.CreateDirectory(_outputDirectory);
                    LogMessage?.Invoke($"Kreiran Output direktorijum: {_outputDirectory}", System.Drawing.Color.Cyan);
                }

                _watcher = new FileSystemWatcher();
                _watcher.Path = _targetDirectory;
                _watcher.Filter = "*.*"; // Prati sve fajlove
                _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName; //prate se samo created eventi
                _watcher.IncludeSubdirectories = false; // Ne prati poddirektorijume

                // Koristi async event handler
                _watcher.Created += OnFileCreatedAsync;
                _watcher.Error += OnWatcherError;

                _watcher.EnableRaisingEvents = true;
                _isWatching = true;

                LogMessage?.Invoke($"✓ FSW uspešno pokrenut!", System.Drawing.Color.Green);
                LogMessage?.Invoke($"  Target: {_targetDirectory}", System.Drawing.Color.Gray);
                LogMessage?.Invoke($"  Output: {_outputDirectory}", System.Drawing.Color.Gray);
                LogMessage?.Invoke($"  Algoritam: {GetAlgorithmName(_selectedAlgorithm)}", System.Drawing.Color.Gray);
                LogMessage?.Invoke("  FSW čeka nove fajlove...", System.Drawing.Color.Yellow);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"✗ Greška pri pokretanju FSW: {ex.Message}", System.Drawing.Color.Red);
                _isWatching = false;
            }
        }

        public void StopWatching()
        {
            if (_watcher != null && _isWatching)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileCreatedAsync;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
                _isWatching = false;
                LogMessage?.Invoke("✓ FSW zaustavljen", System.Drawing.Color.Orange);
            }
        }

        private async void OnFileCreatedAsync(object sender, FileSystemEventArgs e)
        {
            // Koristi Task.Run da ne blokira UI thread
            await Task.Run(() => ProcessNewFile(e));
        }

        private void ProcessNewFile(FileSystemEventArgs e)
        {
            try
            {
                LogMessage?.Invoke($"🔍 Detektovan novi fajl: {e.Name}", System.Drawing.Color.Yellow);

                // Proverava da li je fajl spreman za čitanje sa retry logikom jer drugi procesi mogu privremeno zakljucati fajl 
                if (!WaitForFileReady(e.FullPath, TimeSpan.FromSeconds(5)))
                {
                    LogMessage?.Invoke($"✗ Fajl nije spreman za čitanje: {e.Name}", System.Drawing.Color.Red);
                    return;
                }

                // Proveri da li fajl još uvek postoji
                if (!File.Exists(e.FullPath))
                {
                    LogMessage?.Invoke($"✗ Fajl ne postoji više: {e.Name}", System.Drawing.Color.Red);
                    return;
                }

                LogMessage?.Invoke($"📁 Šifrovanje počinje za: {e.Name}", System.Drawing.Color.Cyan);
                LogMessage?.Invoke($"   Algoritam: {GetAlgorithmName(_selectedAlgorithm)}", System.Drawing.Color.Gray);

                string encryptedFilePath = FileManager.EncryptFile(
                    e.FullPath,
                    _outputDirectory,
                    _selectedAlgorithm,
                    _algorithmParameters);

                if (encryptedFilePath != null)
                {
                    // Izračunaj hash za verifikaciju
                    byte[] fileData = FileManager.ReadFileBytes(encryptedFilePath);
                    if (fileData != null)
                    {
                        byte[] hash = TigerHash.ComputeHash(fileData);

                        LogMessage?.Invoke($"✅ Fajl uspešno šifrovan!", System.Drawing.Color.Green);
                        LogMessage?.Invoke($"   Originalni: {e.Name}", System.Drawing.Color.Gray);
                        LogMessage?.Invoke($"   Šifrovani: {Path.GetFileName(encryptedFilePath)}", System.Drawing.Color.Gray);
                        LogMessage?.Invoke($"   Veličina: {fileData.Length} bajtova", System.Drawing.Color.Gray);
                        LogMessage?.Invoke($"   Hash: {Convert.ToBase64String(hash).Substring(0, 16)}...", System.Drawing.Color.Cyan);
                        LogMessage?.Invoke("", System.Drawing.Color.White); // Prazna linija
                    }
                }
                else
                {
                    LogMessage?.Invoke($"✗ Neuspešno šifrovanje: {e.Name}", System.Drawing.Color.Red);
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"✗ Greška pri obradi fajla {e.Name}: {ex.Message}", System.Drawing.Color.Red);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            LogMessage?.Invoke($"✗ FSW greška: {e.GetException().Message}", System.Drawing.Color.Red);
            _isWatching = false;
        }

        private bool WaitForFileReady(string filePath, TimeSpan timeout)
        {
            var endTime = DateTime.Now.Add(timeout);

            while (DateTime.Now < endTime)
            {
                try
                {
                    // Pokušaj da otvoriš fajl ekskluzivno
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        // Ako možemo da otvorimo, fajl je spreman
                        return true;
                    }
                }
                catch (IOException)
                {
                    // Fajl je još uvek zauzet, sačekaj malo
                    System.Threading.Thread.Sleep(100);
                }
                catch (Exception)
                {
                    // Druga greška, vrati false
                    return false;
                }
            }

            return false;
        }

        private string GetAlgorithmName(int algorithmChoice)
        {
            switch (algorithmChoice)
            {
                case 1: return "Railfence Cipher";
                case 2: return "XXTEA";
                case 3: return "AES-CBC";
                default: return "Nepoznat algoritam";
            }
        }

        public void SetAlgorithmParameters(int algorithmChoice, object[] parameters)
        {
            _selectedAlgorithm = algorithmChoice;
            _algorithmParameters = parameters;
            if (_isWatching)
            {
                LogMessage?.Invoke($"⚙️ Algoritam promenjen na: {GetAlgorithmName(algorithmChoice)}", System.Drawing.Color.Cyan);
            }
        }
    }
}