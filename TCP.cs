using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zastitaInfoProjekat
{
    public class TCPManager
    {
        private TcpListener _server; //listener koji čeka dolazne TCP konekcije.
        private TcpClient _client; //TCP klijent koji šalje fajl na server.
        private bool _isListening = false; //prati da li server radi.
        private int _port = 8888; //default port 8888.

        // Events za logovanje
        public event Action<string, System.Drawing.Color> LogMessage; //šalje informacije UI-u ili logu.

        public bool IsListening => _isListening;

        #region Server Methods (Receiving Files)

        public async Task StartServerAsync(int port = 8888)
        {
            try
            {
                if (_isListening)
                {
                    LogMessage?.Invoke("Server već radi!", System.Drawing.Color.Orange);
                    return;
                }

                _port = port;
                _server = new TcpListener(IPAddress.Any, _port);
                _server.Start();
                _isListening = true;

                LogMessage?.Invoke($"TCP Server pokrenut na portu {_port}", System.Drawing.Color.Green);
                LogMessage?.Invoke("Čekam konekciju od kolege...", System.Drawing.Color.Yellow);

                // Asinhrono čekaj konekcije
                await Task.Run(() => AcceptConnections());
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Greška pri pokretanju servera: {ex.Message}", System.Drawing.Color.Red);
                _isListening = false;
            }
        }

        private void AcceptConnections()
        {
            while (_isListening)
            {
                try
                {
                    // Koristi asinhronu verziju sa timeout-om
                    if (_server.Pending())
                    {
                        TcpClient client = _server.AcceptTcpClient();
                        LogMessage?.Invoke($"Konekcija uspostavljena sa: {client.Client.RemoteEndPoint}", System.Drawing.Color.Green);

                        // Obradi konekciju u novom thread-u 
                        Task.Run(() => HandleClient(client));
                    }
                    else
                    {
        
                        Thread.Sleep(100);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Server je zaustavljen
                    break;
                }
                catch (SocketException ex) when (!_isListening)
                {
                   
                    break;
                }
                catch (Exception ex) when (_isListening)
                {
                    LogMessage?.Invoke($"Greška pri prihvatanju konekcije: {ex.Message}", System.Drawing.Color.Red);
                    Thread.Sleep(1000); // Pauza pre ponovnog pokušaja
                }
            }

            LogMessage?.Invoke("Server thread završen", System.Drawing.Color.Gray);
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                // Sada koristimo using ovde gde je bezbedno
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // Proveri da li je konekcija još uvek aktivna
                    if (!client.Connected)
                    {
                        LogMessage?.Invoke("Konekcija prekinuta pre čitanja", System.Drawing.Color.Orange);
                        return;
                    }

                    LogMessage?.Invoke("Početak prijema fajla...", System.Drawing.Color.Cyan);

                    // Čitaj podatke prema protokolu iz zadatka
                    string fileName = reader.ReadString();
                    long fileSize = reader.ReadInt64();
                    int hashLength = reader.ReadInt32();
                    byte[] receivedHash = reader.ReadBytes(hashLength);

                    LogMessage?.Invoke($"Prijem fajla: {fileName} ({fileSize} bajtova)", System.Drawing.Color.Yellow);
                    LogMessage?.Invoke($"Očekivani hash: {Convert.ToBase64String(receivedHash).Substring(0, 16)}...", System.Drawing.Color.Gray);

                    // Čitaj šifrovani sadržaj fajla sa dodatnim proveram
                    byte[] encryptedData = new byte[fileSize];
                    int totalRead = 0;

                    // Čitaj podatke u blokovima (može da bude potrebno više čitanja)
                    while (totalRead < fileSize && client.Connected)
                    {
                        int remainingBytes = (int)(fileSize - totalRead);
                        int bytesToRead = Math.Min(remainingBytes, 8192); // Čitaj u blokovima od 8KB

                        int bytesRead = reader.Read(encryptedData, totalRead, bytesToRead);
                        if (bytesRead == 0)
                        {
                            LogMessage?.Invoke("Konekcija prekinuta tokom čitanja", System.Drawing.Color.Red);
                            return;
                        }
                        totalRead += bytesRead;

                        // Prikaži progress
                        if (fileSize > 10000) // Samo za veće fajlove
                        {
                            double progress = (double)totalRead / fileSize * 100;
                            if (totalRead % 8192 == 0 || totalRead == fileSize) // Svaki blok ili na kraju
                            {
                                LogMessage?.Invoke($"Progress: {progress:F1}% ({totalRead}/{fileSize} bajtova)", System.Drawing.Color.Cyan);
                            }
                        }
                    }

                    LogMessage?.Invoke($"Primljeno {totalRead} bajtova", System.Drawing.Color.Green);

                    // Verifikuj hash
                    byte[] computedHash = TigerHash.ComputeHash(encryptedData);
                    bool hashValid = CompareHashes(receivedHash, computedHash);

                    if (hashValid)
                    {
                        LogMessage?.Invoke("Hash verifikacija USPEŠNA!", System.Drawing.Color.Green);

                        // Sačuvaj primljeni fajl
                        string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Received");
                        if (!Directory.Exists(outputDirectory))
                            Directory.CreateDirectory(outputDirectory);

                        string receivedFilePath = Path.Combine(outputDirectory, $"received_{fileName}");
                        File.WriteAllBytes(receivedFilePath, encryptedData);

                        LogMessage?.Invoke($"Fajl sačuvan: {receivedFilePath}", System.Drawing.Color.Green);
                        LogMessage?.Invoke("NAPOMENA: Fajl je šifrovan - potrebno ga je dešifrovati!", System.Drawing.Color.Orange);

                        // Pitaj korisnika da li želi automatsko dešifrovanje
                        ShowDecryptionDialog(receivedFilePath, fileName);
                    }
                    else
                    {
                        LogMessage?.Invoke("Hash verifikacija NEUSPEŠNA! Fajl možda oštećen.", System.Drawing.Color.Red);
                        LogMessage?.Invoke($"Očekivani: {Convert.ToBase64String(receivedHash)}", System.Drawing.Color.Red);
                        LogMessage?.Invoke($"Dobijeni:  {Convert.ToBase64String(computedHash)}", System.Drawing.Color.Red);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                LogMessage?.Invoke("Konekcija zatvorena pre završetka", System.Drawing.Color.Orange);
            }
            catch (IOException ex)
            {
                LogMessage?.Invoke($"I/O greška: {ex.Message}", System.Drawing.Color.Red);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Greška pri obradi fajla: {ex.Message}", System.Drawing.Color.Red);
            }
            finally
            {
                LogMessage?.Invoke("Konekcija zatvorena", System.Drawing.Color.Gray);
            }
        }

        private void ShowDecryptionDialog(string encryptedFilePath, string originalFileName)
        {
            // Pošto se ovo poziva iz worker thread-a, moramo koristiti Invoke
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                mainForm.Invoke(new Action(() =>
                {
                    var result = MessageBox.Show(
                        $"Fajl '{originalFileName}' je uspešno primljen i verifikovan.\n\n" +
                        "Da li želite da ga automatski dešifrujete?\n\n" +
                        "NAPOMENA: Unesite iste parametre koji su korišćeni za šifrovanje!",
                        "Dešifrovanje primljenog fajla",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Otvori dialog za dešifrovanje
                        ShowDecryptionParametersDialog(encryptedFilePath, originalFileName);
                    }
                }));
            }
        }

        private void ShowDecryptionParametersDialog(string encryptedFilePath, string originalFileName)
        {
            // Kreirati jednostavan dialog za unos parametara dešifrovanja
            Form decryptForm = new Form();
            decryptForm.Text = "Parametri za dešifrovanje";
            decryptForm.Size = new System.Drawing.Size(500, 300);
            decryptForm.StartPosition = FormStartPosition.CenterParent;

            Label lblInfo = new Label();
            lblInfo.Text = $"Dešifrovanje fajla: {originalFileName}";
            lblInfo.Location = new System.Drawing.Point(20, 20);
            lblInfo.Size = new System.Drawing.Size(400, 20);

            // Algoritam selection
            Label lblAlgorithm = new Label();
            lblAlgorithm.Text = "Algoritam:";
            lblAlgorithm.Location = new System.Drawing.Point(20, 60);
            lblAlgorithm.Size = new System.Drawing.Size(100, 20);

            ComboBox cmbAlgorithm = new ComboBox();
            cmbAlgorithm.Items.AddRange(new[] { "Railfence Cipher", "XXTEA", "AES-CBC" });
            cmbAlgorithm.Location = new System.Drawing.Point(130, 58);
            cmbAlgorithm.Size = new System.Drawing.Size(150, 20);
            cmbAlgorithm.DropDownStyle = ComboBoxStyle.DropDownList;

            // Parametar 1
            Label lblParam1 = new Label();
            lblParam1.Text = "Parametar 1:";
            lblParam1.Location = new System.Drawing.Point(20, 100);
            lblParam1.Size = new System.Drawing.Size(100, 20);

            TextBox txtParam1 = new TextBox();
            txtParam1.Location = new System.Drawing.Point(130, 98);
            txtParam1.Size = new System.Drawing.Size(200, 20);

            // Parametar 2 (za AES)
            Label lblParam2 = new Label();
            lblParam2.Text = "Parametar 2:";
            lblParam2.Location = new System.Drawing.Point(20, 140);
            lblParam2.Size = new System.Drawing.Size(100, 20);

            TextBox txtParam2 = new TextBox();
            txtParam2.Location = new System.Drawing.Point(130, 138);
            txtParam2.Size = new System.Drawing.Size(200, 20);

            // Buttons
            Button btnDecrypt = new Button();
            btnDecrypt.Text = "Dešifruj";
            btnDecrypt.Location = new System.Drawing.Point(200, 200);
            btnDecrypt.Size = new System.Drawing.Size(100, 30);
            btnDecrypt.Click += (s, e) => {
                try
                {
                    int algorithmChoice = cmbAlgorithm.SelectedIndex + 1;
                    object[] parameters = null;

                    switch (algorithmChoice)
                    {
                        case 1: // Railfence
                            parameters = new object[] { int.Parse(txtParam1.Text) };
                            break;
                        case 2: // XXTEA
                            parameters = new object[] { Encoding.UTF8.GetBytes(txtParam1.Text.PadRight(16, '0').Substring(0, 16)) };
                            break;
                        case 3: // AES-CBC
                            parameters = new object[] {
                                Encoding.UTF8.GetBytes(txtParam1.Text.PadRight(16, '0').Substring(0, 16)),
                                Encoding.UTF8.GetBytes(txtParam2.Text.PadRight(16, '0').Substring(0, 16))
                            };
                            break;
                    }

                    // Izaberi lokaciju za čuvanje
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.FileName = FileManager.GetFileNameWithoutExtension(originalFileName);
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        FileManager.DecryptFile(encryptedFilePath, sfd.FileName, algorithmChoice, parameters);
                        MessageBox.Show("Fajl uspešno dešifrovan!", "Uspeh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LogMessage?.Invoke($"Automatsko dešifrovanje završeno: {sfd.FileName}", System.Drawing.Color.Green);
                        decryptForm.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri dešifrovanju: {ex.Message}", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Button btnCancel = new Button();
            btnCancel.Text = "Otkaži";
            btnCancel.Location = new System.Drawing.Point(320, 200);
            btnCancel.Size = new System.Drawing.Size(100, 30);
            btnCancel.Click += (s, e) => decryptForm.Close();

            decryptForm.Controls.AddRange(new Control[] {
                lblInfo, lblAlgorithm, cmbAlgorithm, lblParam1, txtParam1, lblParam2, txtParam2, btnDecrypt, btnCancel
            });

            decryptForm.ShowDialog();
        }

        #endregion

        #region Client Methods (Sending Files)

        public async Task<bool> SendFileAsync(string serverIP, int port, string filePath, int algorithmChoice, object[] parameters)
        {
            try
            {
                LogMessage?.Invoke($"Povezivanje sa {serverIP}:{port}...", System.Drawing.Color.Yellow);

                using (_client = new TcpClient())
                {
                    // Dodaj timeout za konekciju
                    var connectTask = _client.ConnectAsync(serverIP, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        LogMessage?.Invoke("Timeout - server ne odgovara!", System.Drawing.Color.Red);
                        return false;
                    }

                    LogMessage?.Invoke("Konekcija uspostavljena!", System.Drawing.Color.Green);

                    // Čitaj i šifruj fajl
                    byte[] fileData = await ReadFileWithRetryAsync(filePath);
                    if (fileData == null)
                    {
                        LogMessage?.Invoke("Neuspešno čitanje fajla!", System.Drawing.Color.Red);
                        return false;
                    }

                    LogMessage?.Invoke($"Šifrovanje fajla algoritmom: {GetAlgorithmName(algorithmChoice)}", System.Drawing.Color.Cyan);
                    byte[] encryptedData = EncryptData(fileData, algorithmChoice, parameters);

                    if (encryptedData == null)
                    {
                        LogMessage?.Invoke("Greška pri šifrovanju!", System.Drawing.Color.Red);
                        return false;
                    }

                    // Izračunaj hash
                    byte[] hash = TigerHash.ComputeHash(encryptedData);
                    LogMessage?.Invoke($"Hash: {Convert.ToBase64String(hash)}", System.Drawing.Color.Gray);

                    using (NetworkStream stream = _client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        // Pošalji podatke prema protokolu
                        string fileName = Path.GetFileName(filePath);

                        writer.Write(fileName);                    // String
                        writer.Write((long)encryptedData.Length); // Long - veličina šifrovanog fajla
                        writer.Write(hash.Length);                // Int - dužina hash-a
                        writer.Write(hash);                       // Byte[] - hash
                        writer.Write(encryptedData);              // Byte[] - šifrovani sadržaj

                        writer.Flush();
                        LogMessage?.Invoke($"Fajl uspešno poslat: {fileName} ({encryptedData.Length} bajtova)", System.Drawing.Color.Green);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Greška pri slanju fajla: {ex.Message}", System.Drawing.Color.Red);
                return false;
            }
        }

        private async Task<byte[]> ReadFileWithRetryAsync(string filePath)
        {
            int maxRetries = 10;
            int retryDelayMs = 500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    LogMessage?.Invoke($"Čitanje fajla (pokušaj {attempt}/{maxRetries})...", System.Drawing.Color.Yellow);

                    // Proveri da li fajl postoji
                    if (!File.Exists(filePath))
                    {
                        LogMessage?.Invoke("Fajl ne postoji!", System.Drawing.Color.Red);
                        return null;
                    }

                    // Pokušaj da čitaš fajl
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        byte[] buffer = new byte[fileStream.Length];
                        await fileStream.ReadAsync(buffer, 0, buffer.Length);

                        LogMessage?.Invoke($"Fajl uspešno učitan ({buffer.Length} bajtova)", System.Drawing.Color.Green);
                        return buffer;
                    }
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    LogMessage?.Invoke($"Fajl je zauzet, čekam {retryDelayMs}ms...", System.Drawing.Color.Orange);
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Greška pri čitanju fajla: {ex.Message}", System.Drawing.Color.Red);
                    return null;
                }
            }

            LogMessage?.Invoke("Neuspešno čitanje fajla nakon svih pokušaja!", System.Drawing.Color.Red);
            return null;
        }

        #endregion

        #region Helper Methods

        private byte[] EncryptData(byte[] data, int algorithmChoice, object[] parameters)
        {
            try
            {
                switch (algorithmChoice)
                {
                    case 1: // Railfence
                        string text = Encoding.UTF8.GetString(data);
                        int rails = (int)parameters[0];
                        string encrypted = RailfenceCipher.Encrypt(text, rails);
                        return Encoding.UTF8.GetBytes(encrypted);

                    case 2: // XXTEA
                        byte[] key = (byte[])parameters[0];
                        return XXTEA.Encrypt(data, key);

                    case 3: // AES-CBC
                        byte[] aesKey = (byte[])parameters[0];
                        byte[] iv = (byte[])parameters[1];
                        return AESCBC.Encrypt(data, aesKey, iv);

                    default:
                        throw new ArgumentException("Nepoznat algoritam");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Greška pri šifrovanju: {ex.Message}", System.Drawing.Color.Red);
                return null;
            }
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

        private bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length)
                return false;

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                    return false;
            }
            return true;
        }

        public void StopServer()
        {
            try
            {
                if (!_isListening)
                {
                    LogMessage?.Invoke("Server nije pokrenut", System.Drawing.Color.Orange);
                    return;
                }

                LogMessage?.Invoke("Zaustavljanje TCP servera...", System.Drawing.Color.Yellow);

                _isListening = false;

                // Zaustavi server listener
                if (_server != null)
                {
                    _server.Stop();
                    _server = null;
                }

                // Zatvori client konekciju ako postoji
                if (_client != null && _client.Connected)
                {
                    _client.Close();
                    _client = null;
                }

                LogMessage?.Invoke("TCP Server zaustavljen", System.Drawing.Color.Orange);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Greška pri zaustavljanju servera: {ex.Message}", System.Drawing.Color.Red);
            }
            finally
            {
                _isListening = false;
            }
        }

        #endregion
    }
}