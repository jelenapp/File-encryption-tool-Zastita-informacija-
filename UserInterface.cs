using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace zastitaInfoProjekat
{
    public partial class MainForm : Form
    {
        private ImprovedFileSystemWatcherManager _fileWatcher;
        private Settings _settings;

        // Controls
        private TabControl tabControl;
        private GroupBox grpAlgorithm;
        private RadioButton rbRailfence, rbXXTEA, rbAESCBC;
        private Panel pnlRailfence, pnlXXTEA, pnlAESCBC;
        private NumericUpDown nudRails;
        private TextBox txtXXTEAKey, txtAESKey, txtAESIV;
        private Button btnEncryptFile, btnDecryptFile;
        private Button btnStartFSW, btnStopFSW, btnSettings;
        private TextBox txtTargetDir, txtOutputDir;
        private RichTextBox rtbLog;
        private Button btnTestAlgorithms, btnTestHash;
        private Label lblStatus, lblFSWStatus;
        private ProgressBar progressBar;
        private TCPManager _tcpManager;

        public MainForm()
        {
            _settings = new Settings();
            _fileWatcher = new ImprovedFileSystemWatcherManager();
            _fileWatcher.LogMessage += LogMessage;
            _tcpManager = new TCPManager();
            _tcpManager.LogMessage += LogMessage;


            InitializeComponent();
            UpdateUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Aplikacija za Šifrovanje Fajlova - Zaštita Informacija 2024/2025";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(900, 700);
            this.Icon = SystemIcons.Shield;

            // Tab Control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 9F);

            // Create all tabs
            CreateEncryptionTab();
            CreateFSWTab();
            CreateSettingsTab();
            CreateTestsTab();

            // Status bar
            lblStatus = new Label();
            lblStatus.Text = "Spreman";
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.BackColor = Color.LightGray;
            lblStatus.Padding = new Padding(5);
            lblStatus.Font = new Font("Segoe UI", 9F);

            progressBar = new ProgressBar();
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Visible = false;
            progressBar.Style = ProgressBarStyle.Continuous;

            this.Controls.Add(tabControl);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblStatus);

            this.ResumeLayout(false);
        }

        private void CreateEncryptionTab()
        {
            var tabEncrypt = new TabPage("Šifrovanje/Dešifrovanje");

            // Algorithm selection group
            grpAlgorithm = new GroupBox();
            grpAlgorithm.Text = "Izaberi Algoritam";
            grpAlgorithm.Location = new Point(10, 10);
            grpAlgorithm.Size = new Size(850, 180);
            grpAlgorithm.AutoSize = false;
            grpAlgorithm.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            // Radio buttons
            rbRailfence = new RadioButton();
            rbRailfence.Text = "Railfence Cipher";
            rbRailfence.Location = new Point(20, 25);
            rbRailfence.Size = new Size(150, 20);
            rbRailfence.Checked = true;
            rbRailfence.CheckedChanged += AlgorithmChanged;
            rbRailfence.Font = new Font("Segoe UI", 9F);

            rbXXTEA = new RadioButton();
            rbXXTEA.Text = "XXTEA";
            rbXXTEA.Location = new Point(200, 25);
            rbXXTEA.Size = new Size(80, 20);
            rbXXTEA.CheckedChanged += AlgorithmChanged;
            rbXXTEA.Font = new Font("Segoe UI", 9F);

            rbAESCBC = new RadioButton();
            rbAESCBC.Text = "AES-CBC";
            rbAESCBC.Location = new Point(320, 25);
            rbAESCBC.Size = new Size(100, 20);
            rbAESCBC.CheckedChanged += AlgorithmChanged;
            rbAESCBC.Font = new Font("Segoe UI", 9F);

            // Parameter panels
            CreateParameterPanels();

            grpAlgorithm.Controls.AddRange(new Control[] {
                rbRailfence, rbXXTEA, rbAESCBC,
                pnlRailfence, pnlXXTEA, pnlAESCBC
            });

            // File operation buttons
            btnEncryptFile = new Button();
            btnEncryptFile.Text = "Šifruj Fajl";
            btnEncryptFile.Location = new Point(50, 250);
            btnEncryptFile.Size = new Size(150, 45);
            btnEncryptFile.BackColor = Color.LightYellow;
            btnEncryptFile.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnEncryptFile.FlatStyle = FlatStyle.Flat;
            btnEncryptFile.Click += BtnEncryptFile_Click;

            btnDecryptFile = new Button();
            btnDecryptFile.Text = "Dešifruj Fajl";
            btnDecryptFile.Location = new Point(220, 250);
            btnDecryptFile.Size = new Size(150, 45);
            btnDecryptFile.BackColor = Color.LightSalmon;
            btnDecryptFile.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDecryptFile.FlatStyle = FlatStyle.Flat;
            btnDecryptFile.Click += BtnDecryptFile_Click;

            // Log
            var lblLog = new Label();
            lblLog.Text = "Log aktivnosti:";
            lblLog.Location = new Point(10, 320);
            lblLog.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLog.Size = new Size(200, 20);

            rtbLog = new RichTextBox();
            rtbLog.Location = new Point(10, 345);
            rtbLog.Size = new Size(850, 280);
            rtbLog.ReadOnly = true;
            rtbLog.BackColor = Color.Black;
            rtbLog.ForeColor = Color.LightGreen;
            rtbLog.Font = new Font("Consolas", 9);
            rtbLog.BorderStyle = BorderStyle.FixedSingle;

            tabEncrypt.Controls.AddRange(new Control[] {
                grpAlgorithm, btnEncryptFile, btnDecryptFile, lblLog, rtbLog
            });

            tabControl.TabPages.Add(tabEncrypt);
            CreateTCPTab();
        }

        private void CreateParameterPanels()
        {
            // Railfence panel
            pnlRailfence = new Panel();
            pnlRailfence.Location = new Point(20, 55);
            pnlRailfence.Size = new Size(800, 50);
            pnlRailfence.BorderStyle = BorderStyle.FixedSingle;
            pnlRailfence.BackColor = Color.WhiteSmoke;

            var lblRails = new Label();
            lblRails.Text = "Broj redova (rails):";
            lblRails.Location = new Point(10, 15);
            lblRails.Size = new Size(120, 20);
            lblRails.Font = new Font("Segoe UI", 9F);

            nudRails = new NumericUpDown();
            nudRails.Location = new Point(140, 13);
            nudRails.Size = new Size(60, 20);
            nudRails.Minimum = 2;
            nudRails.Maximum = 20;
            nudRails.Value = 3;

            var lblRailsInfo = new Label();
            lblRailsInfo.Text = "Više redova = kompleksnija šifra";
            lblRailsInfo.Location = new Point(220, 15);
            lblRailsInfo.Size = new Size(200, 20);
            lblRailsInfo.ForeColor = Color.Gray;
            lblRailsInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);

            pnlRailfence.Controls.AddRange(new Control[] { lblRails, nudRails, lblRailsInfo });

            // XXTEA panel
            pnlXXTEA = new Panel();
            pnlXXTEA.Location = new Point(20, 55);
            pnlXXTEA.Size = new Size(800, 50);
            pnlXXTEA.Visible = false;
            pnlXXTEA.BorderStyle = BorderStyle.FixedSingle;
            pnlXXTEA.BackColor = Color.WhiteSmoke;

            var lblXXTEAKey = new Label();
            lblXXTEAKey.Text = "Ključ (16 karaktera):";
            lblXXTEAKey.Location = new Point(10, 15);
            lblXXTEAKey.Size = new Size(120, 20);
            lblXXTEAKey.Font = new Font("Segoe UI", 9F);

            txtXXTEAKey = new TextBox();
            txtXXTEAKey.Location = new Point(140, 13);
            txtXXTEAKey.Size = new Size(200, 20);
            txtXXTEAKey.Text = "defaultkey123456";
            txtXXTEAKey.MaxLength = 16;

            var lblXXTEAInfo = new Label();
            lblXXTEAInfo.Text = "XXTEA - brz i siguran block cipher";
            lblXXTEAInfo.Location = new Point(360, 15);
            lblXXTEAInfo.Size = new Size(200, 20);
            lblXXTEAInfo.ForeColor = Color.Gray;
            lblXXTEAInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);

            pnlXXTEA.Controls.AddRange(new Control[] { lblXXTEAKey, txtXXTEAKey, lblXXTEAInfo });

            // AES-CBC panel
            pnlAESCBC = new Panel();
            pnlAESCBC.Location = new Point(20, 55);
            pnlAESCBC.Size = new Size(800, 90);
            pnlAESCBC.Visible = false;
            pnlAESCBC.BorderStyle = BorderStyle.FixedSingle;
            pnlAESCBC.BackColor = Color.WhiteSmoke;

            var lblAESKey = new Label();
            lblAESKey.Text = "Ključ (16 karaktera):";
            lblAESKey.Location = new Point(10, 15);
            lblAESKey.Size = new Size(120, 20);
            lblAESKey.Font = new Font("Segoe UI", 9F);

            txtAESKey = new TextBox();
            txtAESKey.Location = new Point(140, 13);
            txtAESKey.Size = new Size(200, 20);
            txtAESKey.Text = "defaultkey123456";
            txtAESKey.MaxLength = 16;

            var lblAESIV = new Label();
            lblAESIV.Text = "IV (16 karaktera):";
            lblAESIV.Location = new Point(10, 50);
            lblAESIV.Size = new Size(120, 20);
            lblAESIV.Font = new Font("Segoe UI", 9F);

            txtAESIV = new TextBox();
            txtAESIV.Location = new Point(140, 48);
            txtAESIV.Size = new Size(200, 20);
            txtAESIV.Text = "defaultiv1234567";
            txtAESIV.MaxLength = 16;

            var lblAESInfo = new Label();
            lblAESInfo.Text = "AES-CBC - military-grade šifrovanje";
            lblAESInfo.Location = new Point(360, 15);
            lblAESInfo.Size = new Size(200, 20);
            lblAESInfo.ForeColor = Color.Gray;
            lblAESInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);

            var btnGenerateRandom = new Button();
            btnGenerateRandom.Text = "Generiši random";
            btnGenerateRandom.Location = new Point(360, 47);
            btnGenerateRandom.Size = new Size(120, 23);
            btnGenerateRandom.Click += (s, e) => GenerateRandomKeys();

            pnlAESCBC.Controls.AddRange(new Control[] {
                lblAESKey, txtAESKey, lblAESIV, txtAESIV, lblAESInfo, btnGenerateRandom
            });
        }

        private void CreateFSWTab()
        {
            var tabFSW = new TabPage("File System Watcher");

            var grpFSW = new GroupBox();
            grpFSW.Text = "File System Watcher Kontrole";
            grpFSW.Location = new Point(10, 10);
            grpFSW.Size = new Size(850, 180);
            grpFSW.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            btnStartFSW = new Button();
            btnStartFSW.Text = "🚀 Pokreni FSW";
            btnStartFSW.Location = new Point(20, 30);
            btnStartFSW.Size = new Size(150, 50);
            btnStartFSW.BackColor = Color.LightYellow;
            btnStartFSW.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnStartFSW.FlatStyle = FlatStyle.Flat;
            btnStartFSW.Click += BtnStartFSW_Click;

            btnStopFSW = new Button();
            btnStopFSW.Text = "🛑 Zaustavi FSW";
            btnStopFSW.Location = new Point(190, 30);
            btnStopFSW.Size = new Size(150, 50);
            btnStopFSW.BackColor = Color.LightSalmon;
            btnStopFSW.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnStopFSW.FlatStyle = FlatStyle.Flat;
            btnStopFSW.Enabled = false;
            btnStopFSW.Click += BtnStopFSW_Click;

            lblFSWStatus = new Label();
            lblFSWStatus.Text = "Status: Neaktivan";
            lblFSWStatus.Location = new Point(360, 40);
            lblFSWStatus.Size = new Size(200, 30);
            lblFSWStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblFSWStatus.ForeColor = Color.Red;

            var lblFSWInfo = new Label();
            lblFSWInfo.Text = "FSW automatski prati Target direktorijum i šifruje sve nove fajlove odabranim algoritmom.\n" +
                             "Kada se doda novi fajl u Target folder, automatski će biti šifrovan i prebačen u Output folder.";
            lblFSWInfo.Location = new Point(20, 100);
            lblFSWInfo.Size = new Size(800, 60);
            lblFSWInfo.Font = new Font("Segoe UI", 9F);

            var btnCreateTestFile = new Button();
            btnCreateTestFile.Text = "🧪 Kreiraj Test Fajl";
            btnCreateTestFile.Location = new Point(620, 30);
            btnCreateTestFile.Size = new Size(150, 50);
            btnCreateTestFile.BackColor = Color.LightYellow;
            btnCreateTestFile.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCreateTestFile.FlatStyle = FlatStyle.Flat;
            btnCreateTestFile.Click += BtnCreateTestFile_Click;

            grpFSW.Controls.AddRange(new Control[] { btnStartFSW, btnStopFSW, lblFSWStatus, lblFSWInfo, btnCreateTestFile });

            var grpDirectories = new GroupBox();
            grpDirectories.Text = "Direktorijumi";
            grpDirectories.Location = new Point(10, 210);
            grpDirectories.Size = new Size(850, 120);
            grpDirectories.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var lblTarget = new Label();
            lblTarget.Text = "Target direktorijum (prati se):";
            lblTarget.Location = new Point(20, 30);
            lblTarget.Font = new Font("Segoe UI", 9F);
            lblTarget.Size = new Size(170, 20);

            txtTargetDir = new TextBox();
            txtTargetDir.Location = new Point(200, 28);
            txtTargetDir.Size = new Size(500, 20);
            txtTargetDir.ReadOnly = true;
            txtTargetDir.BackColor = Color.LightYellow;

            var lblOutput = new Label();
            lblOutput.Text = "Output direktorijum (šifrovani fajlovi):";
            lblOutput.Location = new Point(20, 65);
            lblOutput.Font = new Font("Segoe UI", 9F);
            lblOutput.Size = new Size(170, 20);

            txtOutputDir = new TextBox();
            txtOutputDir.Location = new Point(200, 63);
            txtOutputDir.Size = new Size(500, 20);
            txtOutputDir.ReadOnly = true;
            txtOutputDir.BackColor = Color.LightYellow;

            var btnOpenTarget = new Button();
            btnOpenTarget.Text = "Otvori";
            btnOpenTarget.Location = new Point(720, 26);
            btnOpenTarget.Size = new Size(60, 25);
            btnOpenTarget.Click += (s, e) => OpenDirectory(_settings.TargetDirectory);

            var btnOpenOutput = new Button();
            btnOpenOutput.Text = "Otvori";
            btnOpenOutput.Location = new Point(720, 61);
            btnOpenOutput.Size = new Size(60, 25);
            btnOpenOutput.Click += (s, e) => OpenDirectory(_settings.OutputDirectory);

            grpDirectories.Controls.AddRange(new Control[] {
                lblTarget, txtTargetDir, lblOutput, txtOutputDir, btnOpenTarget, btnOpenOutput
            });

            tabFSW.Controls.AddRange(new Control[] { grpFSW, grpDirectories });
            tabControl.TabPages.Add(tabFSW);
        }

        private void CreateSettingsTab()
        {
            var tabSettings = new TabPage("Podešavanja");

            var grpDirs = new GroupBox();
            grpDirs.Text = "Direktorijumi";
            grpDirs.Location = new Point(10, 10);
            grpDirs.Size = new Size(850, 200);
            grpDirs.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var lblTargetSetting = new Label();
            lblTargetSetting.Text = "Target direktorijum:";
            lblTargetSetting.Location = new Point(20, 30);
            lblTargetSetting.Font = new Font("Segoe UI", 9F);
            lblTargetSetting.Size = new Size(150, 20);

            var txtTargetSetting = new TextBox();
            txtTargetSetting.Location = new Point(20, 55);
            txtTargetSetting.Size = new Size(600, 20);
            txtTargetSetting.Text = _settings.TargetDirectory;
            txtTargetSetting.Name = "txtTargetSetting";

            var btnBrowseTarget = new Button();
            btnBrowseTarget.Text = "Pretraži";
            btnBrowseTarget.Location = new Point(630, 53);
            btnBrowseTarget.Size = new Size(100, 25);
            btnBrowseTarget.BackColor = Color.LightBlue;
            btnBrowseTarget.FlatStyle = FlatStyle.Flat;
            btnBrowseTarget.Click += (s, e) => BrowseFolder(txtTargetSetting);

            var lblOutputSetting = new Label();
            lblOutputSetting.Text = "Output direktorijum:";
            lblOutputSetting.Location = new Point(20, 95);
            lblOutputSetting.Font = new Font("Segoe UI", 9F);
            lblOutputSetting.Size = new Size(150, 20);

            var txtOutputSetting = new TextBox();
            txtOutputSetting.Location = new Point(20, 120);
            txtOutputSetting.Size = new Size(600, 20);
            txtOutputSetting.Text = _settings.OutputDirectory;
            txtOutputSetting.Name = "txtOutputSetting";

            var btnBrowseOutput = new Button();
            btnBrowseOutput.Text = "Pretraži";
            btnBrowseOutput.Location = new Point(630, 118);
            btnBrowseOutput.Size = new Size(100, 25);
            btnBrowseOutput.BackColor = Color.LightBlue;
            btnBrowseOutput.FlatStyle = FlatStyle.Flat;
            btnBrowseOutput.Click += (s, e) => BrowseFolder(txtOutputSetting);

            var btnSaveSettings = new Button();
            btnSaveSettings.Text = "💾 Sačuvaj Podešavanja";
            btnSaveSettings.Location = new Point(20, 155);
            btnSaveSettings.Size = new Size(200, 35);
            btnSaveSettings.BackColor = Color.LightSteelBlue;
            btnSaveSettings.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSaveSettings.FlatStyle = FlatStyle.Flat;
            btnSaveSettings.Click += (s, e) => SaveSettings(txtTargetSetting.Text, txtOutputSetting.Text);

            grpDirs.Controls.AddRange(new Control[] {
                lblTargetSetting, txtTargetSetting, btnBrowseTarget,
                lblOutputSetting, txtOutputSetting, btnBrowseOutput,
                btnSaveSettings
            });

            var grpInfo = new GroupBox();
            grpInfo.Text = "Informacije o Algoritmu";
            grpInfo.Location = new Point(10, 230);
            grpInfo.Size = new Size(850, 150);
            grpInfo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var rtbAlgInfo = new RichTextBox();
            rtbAlgInfo.Location = new Point(20, 30);
            rtbAlgInfo.Size = new Size(810, 100);
            rtbAlgInfo.ReadOnly = true;
            rtbAlgInfo.Font = new Font("Segoe UI", 9F);
            rtbAlgInfo.Text = "ALGORITMI KOJI SU IMPLEMENTIRANI:\n\n" +
                             "1. RAILFENCE CIPHER - Klasična transpoziciona šifra koja piše tekst u zigzag pattern\n" +
                             "2. XXTEA - Brz block cipher algoritam, poboljšana verzija TEA algoritma\n" +
                             "3. AES-CBC - Advanced Encryption Standard u CBC modu, military-grade šifrovanje\n" +
                             "4. TIGER HASH - Kriptografski hash algoritam za verifikaciju intergiteta fajlova";

            grpInfo.Controls.Add(rtbAlgInfo);

            tabSettings.Controls.AddRange(new Control[] { grpDirs, grpInfo });
            tabControl.TabPages.Add(tabSettings);
        }

        private void CreateTestsTab()
        {
            var tabTests = new TabPage("Testovi");

            var grpTests = new GroupBox();
            grpTests.Text = "Pokretanje Testova";
            grpTests.Location = new Point(10, 10);
            grpTests.Size = new Size(850, 100);
            grpTests.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            btnTestAlgorithms = new Button();
            btnTestAlgorithms.Text = "Testiraj Algoritme";
            btnTestAlgorithms.Location = new Point(20, 30);
            btnTestAlgorithms.Size = new Size(180, 45);
            btnTestAlgorithms.BackColor = Color.LightBlue;
            btnTestAlgorithms.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnTestAlgorithms.FlatStyle = FlatStyle.Flat;
            btnTestAlgorithms.Click += BtnTestAlgorithms_Click;

            btnTestHash = new Button();
            btnTestHash.Text = "Testiraj Tiger Hash";
            btnTestHash.Location = new Point(220, 30);
            btnTestHash.Size = new Size(180, 45);
            btnTestHash.BackColor = Color.LightSalmon;
            btnTestHash.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnTestHash.FlatStyle = FlatStyle.Flat;
            btnTestHash.Click += BtnTestHash_Click;

            var btnRunAllTests = new Button();
            btnRunAllTests.Text = "Pokreni Sve Testove";
            btnRunAllTests.Location = new Point(420, 30);
            btnRunAllTests.Size = new Size(180, 45);
            btnRunAllTests.BackColor = Color.LightGoldenrodYellow;
            btnRunAllTests.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnRunAllTests.FlatStyle = FlatStyle.Flat;
            btnRunAllTests.Click += (s, e) => RunAllTests();

            grpTests.Controls.AddRange(new Control[] { btnTestAlgorithms, btnTestHash, btnRunAllTests });

            var rtbTestResults = new RichTextBox();
            rtbTestResults.Location = new Point(20, 130);
            rtbTestResults.Size = new Size(840, 480);
            rtbTestResults.ReadOnly = true;
            rtbTestResults.Font = new Font("Consolas", 9);
            rtbTestResults.Name = "rtbTestResults";
            rtbTestResults.BorderStyle = BorderStyle.FixedSingle;
            rtbTestResults.BackColor = Color.Black;
            rtbTestResults.ForeColor = Color.White;

            tabTests.Controls.AddRange(new Control[] { grpTests, rtbTestResults });
            tabControl.TabPages.Add(tabTests);
        }
        private void CreateTCPTab()
        {
            var tabTCP = new TabPage("TCP Razmena");

            // Server sekcija
            var grpServer = new GroupBox();
            grpServer.Text = "TCP Server (Prijem fajlova)";
            grpServer.Location = new Point(10, 10);
            grpServer.Size = new Size(850, 120);
            grpServer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var lblServerPort = new Label();
            lblServerPort.Text = "Port:";
            lblServerPort.Location = new Point(20, 30);
            lblServerPort.Size = new Size(40, 20);
            lblServerPort.Font = new Font("Segoe UI", 9F);

            var txtServerPort = new TextBox();
            txtServerPort.Location = new Point(70, 28);
            txtServerPort.Size = new Size(80, 20);
            txtServerPort.Text = "8888";
            txtServerPort.Name = "txtServerPort";

            var btnStartServer = new Button();
            btnStartServer.Text = "🌐 Pokreni Server";
            btnStartServer.Location = new Point(170, 25);
            btnStartServer.Size = new Size(150, 30);
            btnStartServer.BackColor = Color.LightYellow;
            btnStartServer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStartServer.FlatStyle = FlatStyle.Flat;
            btnStartServer.Click += async (s, e) =>
            {
                try
                {
                    int port = int.Parse(txtServerPort.Text);
                    btnStartServer.Enabled = false;
                    await _tcpManager.StartServerAsync(port);
                }
                catch (Exception ex)
                {
                    LogMessageThreadSafe($"Greška pri pokretanju servera: {ex.Message}", Color.Red);
                    btnStartServer.Enabled = true;
                }
            };

            var btnStopServer = new Button();
            btnStopServer.Text = "🛑 Zaustavi Server";
            btnStopServer.Location = new Point(340, 25);
            btnStopServer.Size = new Size(150, 30);
            btnStopServer.BackColor = Color.LightSalmon;
            btnStopServer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnStopServer.FlatStyle = FlatStyle.Flat;
            btnStopServer.Click += (s, e) =>
            {
                _tcpManager.StopServer();
                btnStartServer.Enabled = true;
            };

            var lblServerInfo = new Label();
            lblServerInfo.Text = "Server automatski prima fajlove i verifikuje hash.";
            lblServerInfo.Location = new Point(20, 65);
            lblServerInfo.Size = new Size(400, 40);
            lblServerInfo.Font = new Font("Segoe UI", 8F);

            grpServer.Controls.AddRange(new Control[] {
        lblServerPort, txtServerPort, btnStartServer, btnStopServer, lblServerInfo
    });

            // Client sekcija
            var grpClient = new GroupBox();
            grpClient.Text = "TCP Client (Slanje fajlova)";
            grpClient.Location = new Point(10, 150);
            grpClient.Size = new Size(850, 200);
            grpClient.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var lblServerIP = new Label();
            lblServerIP.Text = "Server IP:";
            lblServerIP.Location = new Point(20, 30);
            lblServerIP.Size = new Size(70, 20);
            lblServerIP.Font = new Font("Segoe UI", 9F);

            var txtServerIP = new TextBox();
            txtServerIP.Location = new Point(100, 28);
            txtServerIP.Size = new Size(120, 20);
            txtServerIP.Text = "127.0.0.1";

            var lblClientPort = new Label();
            lblClientPort.Text = "Port:";
            lblClientPort.Location = new Point(240, 30);
            lblClientPort.Size = new Size(40, 20);
            lblClientPort.Font = new Font("Segoe UI", 9F);

            var txtClientPort = new TextBox();
            txtClientPort.Location = new Point(290, 28);
            txtClientPort.Size = new Size(80, 20);
            txtClientPort.Text = "8888";

            var btnSelectFile = new Button();
            btnSelectFile.Text = "📁 Izaberi Fajl";
            btnSelectFile.Location = new Point(20, 70);
            btnSelectFile.Size = new Size(120, 30);
            btnSelectFile.BackColor = Color.LightBlue;
            btnSelectFile.Font = new Font("Segoe UI", 9F);
            btnSelectFile.FlatStyle = FlatStyle.Flat;

            var txtSelectedFile = new TextBox();
            txtSelectedFile.Location = new Point(150, 75);
            txtSelectedFile.Size = new Size(500, 20);
            txtSelectedFile.ReadOnly = true;
            txtSelectedFile.BackColor = Color.LightYellow;

            btnSelectFile.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Izaberite fajl za slanje";
                    ofd.Filter = "Svi fajlovi (*.*)|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtSelectedFile.Text = ofd.FileName;
                    }
                }
            };

            var btnSendFile = new Button();
            btnSendFile.Text = "📤 Pošalji Fajl";
            btnSendFile.Location = new Point(680, 70);
            btnSendFile.Size = new Size(120, 30);
            btnSendFile.BackColor = Color.Orange;
            btnSendFile.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSendFile.FlatStyle = FlatStyle.Flat;
            btnSendFile.Click += async (s, e) =>
            {
                await SendSelectedFile(txtServerIP.Text, int.Parse(txtClientPort.Text), txtSelectedFile.Text);
            };

            var lblClientInfo = new Label();
            lblClientInfo.Text = "Algoritam koji će se koristiti:\nKoristi se isti algoritam koji je trenutno izabran u prvom tabu.\nKomunicirajte sa kolegom o ključevima!";
            lblClientInfo.Location = new Point(20, 120);
            lblClientInfo.Size = new Size(500, 60);
            lblClientInfo.Font = new Font("Segoe UI", 8F);
            lblClientInfo.ForeColor = Color.DarkBlue;

            var lblCurrentAlgo = new Label();
            lblCurrentAlgo.Text = "Trenutni algoritam: ";
            lblCurrentAlgo.Location = new Point(540, 120);
            lblCurrentAlgo.Size = new Size(280, 60);
            lblCurrentAlgo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblCurrentAlgo.ForeColor = Color.DarkGreen;
            lblCurrentAlgo.Name = "lblCurrentAlgo";

            grpClient.Controls.AddRange(new Control[] {
        lblServerIP, txtServerIP, lblClientPort, txtClientPort,
        btnSelectFile, txtSelectedFile, btnSendFile,
        lblClientInfo, lblCurrentAlgo
    });

            // Instrukcije
            var grpInstructions = new GroupBox();
            grpInstructions.Text = "Instrukcije za korišćenje";
            grpInstructions.Location = new Point(10, 370);
            grpInstructions.Size = new Size(850, 150);
            grpInstructions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            var rtbInstructions = new RichTextBox();
            rtbInstructions.Location = new Point(20, 30);
            rtbInstructions.Size = new Size(810, 100);
            rtbInstructions.ReadOnly = true;
            rtbInstructions.Font = new Font("Segoe UI", 9F);
            rtbInstructions.Text =
                "KAKO KORISTITI TCP RAZMENU:\n\n" +
                "1. PRIJEM: Pokrenite server na određenom portu\n" +
                "2. SLANJE: Unesite IP adresu i port kolege, izaberite fajl\n" +
                "3. ALGORITMI: Koristite iste parametre kao kolega (ključ, broj redova, itd.)\n" +
                "4. KOMUNIKACIJA: Dogovorite se sa kolegom o ključevima pre slanja!\n" +
                "5. VERIFIKACIJA: Aplikacija automatski proverava Tiger Hash\n" +
                "6. DEŠIFROVANJE: Nakon prijema, možete automatski dešifrovati fajl";

            grpInstructions.Controls.Add(rtbInstructions);

            tabTCP.Controls.AddRange(new Control[] { grpServer, grpClient, grpInstructions });
            tabControl.TabPages.Add(tabTCP);

            // Update trenutni algoritam label periodično - samo kada je potrebno
            Timer algorithmTimer = new Timer();
            algorithmTimer.Interval = 2000; // Svake 2 sekunde (ređe)
            algorithmTimer.Tick += (s, e) =>
            {
                // Proverava da li je tab aktivan pre ažuriranja
                if (tabControl.SelectedTab != tabTCP) return;

                var currentAlgoLabel = tabTCP.Controls.Find("lblCurrentAlgo", true);
                if (currentAlgoLabel.Length > 0)
                {
                    string algoText = GetCurrentAlgorithmName();
                    var (choice, parameters) = GetSelectedAlgorithm();

                    string paramText = "";
                    if (choice != -1 && parameters != null)
                    {
                        switch (choice)
                        {
                            case 1:
                                paramText = $"({(int)parameters[0]} redova)";
                                break;
                            case 2:
                                paramText = "(XXTEA ključ)";
                                break;
                            case 3:
                                paramText = "(AES ključ + IV)";
                                break;
                            default:
                                paramText = "(nije izabran)";
                                break;
                        }
                    }
                    else
                    {
                        paramText = "(nije izabran)";
                    }

                    string serverStatus = _tcpManager.IsListening ? " [SERVER AKTIVAN]" : "";
                    ((Label)currentAlgoLabel[0]).Text = $"Trenutni algoritam:\n{algoText} {paramText}{serverStatus}";
                }
            };
            algorithmTimer.Start();

            // Zaustavi timer kada se tab zatvara
            tabTCP.Disposed += (s, e) => algorithmTimer?.Stop();
        }
            #region Event Handlers

            private void AlgorithmChanged(object sender, EventArgs e)
        {
            pnlRailfence.Visible = rbRailfence.Checked;
            pnlXXTEA.Visible = rbXXTEA.Checked;
            pnlAESCBC.Visible = rbAESCBC.Checked;
            
            // Dodaj logovanje - THREAD-SAFE
            if (rbRailfence.Checked)
                LogMessageThreadSafe($"Algoritam promenjen na: Railfence ({(int)nudRails.Value} redova)", Color.Cyan);
            else if (rbXXTEA.Checked)
                LogMessageThreadSafe("Algoritam promenjen na: XXTEA", Color.Cyan);
            else if (rbAESCBC.Checked)
                LogMessageThreadSafe("Algoritam promenjen na: AES-CBC", Color.Cyan);

            // Adjust group box height
            if (rbAESCBC.Checked)
                grpAlgorithm.Height = 180;
            else
                grpAlgorithm.Height = 200;
        }

        private void GenerateRandomKeys()
        {
            Random rand = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            // Generate random AES key
            StringBuilder keyBuilder = new StringBuilder();
            for (int i = 0; i < 16; i++)
                keyBuilder.Append(chars[rand.Next(chars.Length)]);
            txtAESKey.Text = keyBuilder.ToString();

            // Generate random IV
            StringBuilder ivBuilder = new StringBuilder();
            for (int i = 0; i < 16; i++)
                ivBuilder.Append(chars[rand.Next(chars.Length)]);
            txtAESIV.Text = ivBuilder.ToString();

            LogMessageThreadSafe("Random AES ključ i IV generisani!", Color.Cyan);
        }

        private (int algorithmChoice, object[] parameters) GetSelectedAlgorithm()
        {
            try
            {
                if (rbRailfence.Checked)
                {
                    int rails = (int)nudRails.Value;
                    LogMessageThreadSafe($"Izabran Railfence sa {rails} redova", Color.Cyan);
                    return (1, new object[] { rails });
                }
                else if (rbXXTEA.Checked)
                {
                    string key = txtXXTEAKey.Text;
                    if (string.IsNullOrEmpty(key))
                    {
                        LogMessageThreadSafe("XXTEA ključ je prazan, koristim default", Color.Orange);
                        key = "defaultkey123456";
                    }
                    key = key.PadRight(16, '0').Substring(0, 16);
                    LogMessageThreadSafe($"Izabran XXTEA sa ključem: {key.Substring(0, 4)}****", Color.Cyan);
                    return (2, new object[] { System.Text.Encoding.UTF8.GetBytes(key) });
                }
                else if (rbAESCBC.Checked)
                {
                    string key = txtAESKey.Text;
                    string iv = txtAESIV.Text;

                    if (string.IsNullOrEmpty(key))
                    {
                        LogMessageThreadSafe("AES ključ je prazan, koristim default", Color.Orange);
                        key = "defaultkey123456";
                    }
                    if (string.IsNullOrEmpty(iv))
                    {
                        LogMessageThreadSafe("AES IV je prazan, koristim default", Color.Orange);
                        iv = "defaultiv1234567";
                    }

                    key = key.PadRight(16, '0').Substring(0, 16);
                    iv = iv.PadRight(16, '0').Substring(0, 16);

                    LogMessageThreadSafe($"Izabran AES-CBC sa ključem: {key.Substring(0, 4)}****", Color.Cyan);
                    return (3, new object[] { System.Text.Encoding.UTF8.GetBytes(key), System.Text.Encoding.UTF8.GetBytes(iv) });
                }
            }
            catch (Exception ex)
            {
                LogMessageThreadSafe($"Greška pri selekciji algoritma: {ex.Message}", Color.Red);
            }

            LogMessageThreadSafe("Nijedan algoritam nije izabran!", Color.Red);
            return (-1, null);
        }

        private void BtnEncryptFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Izaberite fajl za šifrovanje";
                ofd.Filter = "Svi fajlovi (*.*)|*.*|Tekstualni fajlovi (*.txt)|*.txt|Slike (*.jpg;*.png)|*.jpg;*.png";
                ofd.FilterIndex = 1;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var (algorithmChoice, parameters) = GetSelectedAlgorithm();
                    if (algorithmChoice == -1)
                    {
                        LogMessageThreadSafe("Greška: Neispravan algoritam!", Color.Red);
                        MessageBox.Show("Molim izaberite algoritam!", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    progressBar.Visible = true;
                    progressBar.Value = 0;
                    lblStatus.Text = "Šifrovanje u toku...";
                    this.Enabled = false;

                    try
                    {
                        LogMessageThreadSafe($"Pokretanje šifrovanja fajla: {Path.GetFileName(ofd.FileName)}", Color.Yellow);

                        string encryptedFilePath = FileManager.EncryptFile(ofd.FileName, _settings.OutputDirectory, algorithmChoice, parameters);

                        if (encryptedFilePath != null)
                        {
                            byte[] fileData = FileManager.ReadFileBytes(encryptedFilePath);
                            byte[] hash = TigerHash.ComputeHash(fileData);

                            LogMessageThreadSafe($"✓ Fajl uspešno šifrovan: {Path.GetFileName(encryptedFilePath)}", Color.Green);
                            LogMessageThreadSafe($"  Algoritam: {GetAlgorithmName(algorithmChoice)}", Color.Cyan);
                            LogMessageThreadSafe($"  Tiger Hash: {Convert.ToBase64String(hash)}", Color.Cyan);
                            LogMessageThreadSafe($"  Originalna veličina: {new FileInfo(ofd.FileName).Length} bajtova", Color.Gray);
                            LogMessageThreadSafe($"  Šifrovana veličina: {fileData.Length} bajtova", Color.Gray);
                            LogMessageThreadSafe("", Color.White);

                            MessageBox.Show($"Fajl je uspešno šifrovan!\nLokacija: {encryptedFilePath}",
                                "Uspeh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            LogMessageThreadSafe("✗ Greška pri šifrovanju fajla!", Color.Red);
                            MessageBox.Show("Dogodila se greška pri šifrovanju fajla.",
                                "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessageThreadSafe($"✗ Greška: {ex.Message}", Color.Red);
                        MessageBox.Show($"Greška pri šifrovanju: {ex.Message}",
                            "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        progressBar.Visible = false;
                        lblStatus.Text = "Spreman";
                        this.Enabled = true;
                    }
                }
            }
        }

        private void BtnDecryptFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Izaberite šifrovani fajl";
                ofd.InitialDirectory = _settings.OutputDirectory;
                ofd.Filter = "Šifrovani fajlovi (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Title = "Sačuvaj dešifrovani fajl";
                        sfd.FileName = FileManager.GetFileNameWithoutExtension(Path.GetFileName(ofd.FileName));

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            var (algorithmChoice, parameters) = GetSelectedAlgorithm();
                            if (algorithmChoice == -1)
                            {
                                LogMessageThreadSafe("Greška: Neispravan algoritam!", Color.Red);
                                MessageBox.Show("Molim izaberite algoritam!", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            progressBar.Visible = true;
                            lblStatus.Text = "Dešifrovanje u toku...";
                            this.Enabled = false;

                            try
                            {
                                LogMessageThreadSafe($"Pokretanje dešifrovanja fajla: {Path.GetFileName(ofd.FileName)}", Color.Yellow);

                                FileManager.DecryptFile(ofd.FileName, sfd.FileName, algorithmChoice, parameters);

                                LogMessageThreadSafe($"✓ Fajl uspešno dešifrovan: {sfd.FileName}", Color.Green);
                                LogMessageThreadSafe($"  Algoritam: {GetAlgorithmName(algorithmChoice)}", Color.Cyan);
                                LogMessageThreadSafe("", Color.White);

                                MessageBox.Show($"Fajl je uspešno dešifrovan!\nLokacija: {sfd.FileName}",
                                    "Uspeh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                LogMessageThreadSafe($"✗ Greška pri dešifrovanju: {ex.Message}", Color.Red);
                                MessageBox.Show($"Greška pri dešifrovanju: {ex.Message}",
                                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            finally
                            {
                                progressBar.Visible = false;
                                lblStatus.Text = "Spreman";
                                this.Enabled = true;
                            }
                        }
                    }
                }
            }
        }

        private void BtnStartFSW_Click(object sender, EventArgs e)
        {
            var (algorithmChoice, parameters) = GetSelectedAlgorithm();
            if (algorithmChoice == -1)
            {
                MessageBox.Show("Molim prvo izaberite algoritam pre pokretanja FSW!", "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_settings.TargetDirectory) || string.IsNullOrEmpty(_settings.OutputDirectory))
            {
                MessageBox.Show("Molim prvo podesite Target i Output direktorijume u Podešavanja tabu!", "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedIndex = 2;
                return;
            }

            LogMessageThreadSafe($"🚀 Pokretanje FSW sa algoritmom: {GetAlgorithmName(algorithmChoice)}", Color.Cyan);
            LogMessageThreadSafe($"Target dir: {_settings.TargetDirectory}", Color.Gray);
            LogMessageThreadSafe($"Output dir: {_settings.OutputDirectory}", Color.Gray);
            LogMessageThreadSafe("", Color.White);

            _fileWatcher.StartWatching(_settings.TargetDirectory, _settings.OutputDirectory, algorithmChoice, parameters);
            UpdateFSWUI();
        }

        private void BtnStopFSW_Click(object sender, EventArgs e)
        {
            _fileWatcher.StopWatching();
            UpdateFSWUI();
            LogMessageThreadSafe("✓ File System Watcher zaustavljen", Color.Orange);
            LogMessageThreadSafe("", Color.White);
        }

        private void BtnCreateTestFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.TargetDirectory))
                {
                    MessageBox.Show("Molim prvo podesite Target direktorijum u Podešavanja tabu!", "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tabControl.SelectedIndex = 2;
                    return;
                }

                string testFileName = $"test_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string testFilePath = Path.Combine(_settings.TargetDirectory, testFileName);
                string testContent = $"Test fajl kreiran {DateTime.Now}\nOvo je test sadržaj za FSW.\nAlgoritam: {GetCurrentAlgorithmName()}\nTest podatak za šifrovanje.";

                File.WriteAllText(testFilePath, testContent, Encoding.UTF8);
                LogMessageThreadSafe($"✓ Test fajl kreiran: {testFileName}", Color.Green);
                LogMessageThreadSafe($"  Lokacija: {testFilePath}", Color.Gray);
            }
            catch (Exception ex)
            {
                LogMessageThreadSafe($"✗ Greška pri kreiranju test fajla: {ex.Message}", Color.Red);
                MessageBox.Show($"Greška pri kreiranju test fajla: {ex.Message}", "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTestAlgorithms_Click(object sender, EventArgs e)
        {
            var rtb = this.Controls.Find("rtbTestResults", true)[0] as RichTextBox;
            rtb.Clear();

            progressBar.Visible = true;
            lblStatus.Text = "Pokretanje testova algoritama...";

            try
            {
                AppendTestResult(rtb, "=== TESTIRANJE ALGORITAMA ===", Color.Yellow);
                AppendTestResult(rtb, $"Vreme pokretanja: {DateTime.Now}", Color.Gray);
                AppendTestResult(rtb, "", Color.White);

                TestRailfence(rtb);
                TestXXTEA(rtb);
                TestAESCBC(rtb);

                AppendTestResult(rtb, "=== SVI TESTOVI ZAVRŠENI ===", Color.Green);
                AppendTestResult(rtb, $"Vreme završetka: {DateTime.Now}", Color.Gray);
            }
            catch (Exception ex)
            {
                AppendTestResult(rtb, $"Greška u testovima: {ex.Message}", Color.Red);
            }
            finally
            {
                progressBar.Visible = false;
                lblStatus.Text = "Spreman";
            }
        }

        private void BtnTestHash_Click(object sender, EventArgs e)
        {
            var rtb = this.Controls.Find("rtbTestResults", true)[0] as RichTextBox;
            rtb.Clear();

            string[] testCases = {
                "", "a", "abc", "Hello World", "Test message for Tiger Hash",
                "The quick brown fox jumps over the lazy dog",
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit"
            };

            AppendTestResult(rtb, "=== TIGER HASH TESTOVI ===", Color.Yellow);
            AppendTestResult(rtb, "", Color.White);

            foreach (string testCase in testCases)
            {
                byte[] data = Encoding.UTF8.GetBytes(testCase);
                byte[] hash = TigerHash.ComputeHash(data);

                AppendTestResult(rtb, $"Input: '{testCase}'", Color.White);
                AppendTestResult(rtb, $"Hash: {BitConverter.ToString(hash).Replace("-", "")}", Color.Cyan);
                AppendTestResult(rtb, $"Base64: {Convert.ToBase64String(hash)}", Color.LightBlue);
                AppendTestResult(rtb, $"Dužina: {hash.Length} bajtova", Color.Gray);
                AppendTestResult(rtb, "", Color.White);
            }
        }

        private void RunAllTests()
        {
            var rtb = this.Controls.Find("rtbTestResults", true)[0] as RichTextBox;
            rtb.Clear();

            progressBar.Visible = true;
            lblStatus.Text = "Pokretanje svih testova...";

            try
            {
                AppendTestResult(rtb, "=== POKRETANJE SVIH TESTOVA ===", Color.Yellow);
                AppendTestResult(rtb, $"Vreme pokretanja: {DateTime.Now}", Color.Gray);
                AppendTestResult(rtb, "", Color.White);

                TestRailfence(rtb);
                TestXXTEA(rtb);
                TestAESCBC(rtb);

                AppendTestResult(rtb, "=== TIGER HASH TESTOVI ===", Color.Yellow);
                string[] hashTestCases = { "", "a", "abc", "Hello World", "Tiger Hash test" };

                foreach (string testCase in hashTestCases)
                {
                    byte[] data = Encoding.UTF8.GetBytes(testCase);
                    byte[] hash = TigerHash.ComputeHash(data);
                    AppendTestResult(rtb, $"'{testCase}' -> {Convert.ToBase64String(hash)}", Color.Cyan);
                }
                AppendTestResult(rtb, "", Color.White);

                AppendTestResult(rtb, "=== SVI TESTOVI ZAVRŠENI USPEŠNO ===", Color.Green);
                AppendTestResult(rtb, $"Vreme završetka: {DateTime.Now}", Color.Gray);
            }
            catch (Exception ex)
            {
                AppendTestResult(rtb, $"Greška u testovima: {ex.Message}", Color.Red);
            }
            finally
            {
                progressBar.Visible = false;
                lblStatus.Text = "Spreman";
            }
        }

        #endregion

        #region Test Methods

        private void TestRailfence(RichTextBox rtb)
        {
            AppendTestResult(rtb, "=== RAILFENCE CIPHER TEST ===", Color.Yellow);
            AppendTestResult(rtb, "", Color.White);

            string[] testCases = { "HELLOWORLD", "MEETMEATMIDNIGHT", "ATTACKATDAWN" };
            int[] railCounts = { 3, 4, 5 };

            int passedTests = 0, totalTests = 0;

            foreach (string testCase in testCases)
            {
                foreach (int rails in railCounts)
                {
                    totalTests++;
                    string encrypted = RailfenceCipher.Encrypt(testCase, rails);
                    string decrypted = RailfenceCipher.Decrypt(encrypted, rails);

                    bool passed = testCase == decrypted;
                    if (passed) passedTests++;

                    Color color = passed ? Color.Green : Color.Red;

                    AppendTestResult(rtb, $"Text: '{testCase}', Rails: {rails}, Passed: {passed}", color);
                    AppendTestResult(rtb, $"  Encrypted: '{encrypted}'", Color.Cyan);
                    if (!passed)
                    {
                        AppendTestResult(rtb, $"  Expected: '{testCase}', Got: '{decrypted}'", Color.Orange);
                    }
                }
            }

            AppendTestResult(rtb, $"Railfence rezultat: {passedTests}/{totalTests} testova prošlo",
                passedTests == totalTests ? Color.Green : Color.Red);
            AppendTestResult(rtb, "", Color.White);
        }

        private void TestXXTEA(RichTextBox rtb)
        {
            AppendTestResult(rtb, "=== XXTEA TEST ===", Color.Yellow);
            AppendTestResult(rtb, "", Color.White);

            string[] testTexts = {
                "Hello World", "This is a test", "1234567890",
                "Special chars: !@#$%", "Short", "A much longer text for testing"
            };
            byte[] key = Encoding.UTF8.GetBytes("1234567890123456");

            int passedTests = 0, totalTests = testTexts.Length;

            foreach (string testText in testTexts)
            {
                try
                {
                    byte[] plaintext = Encoding.UTF8.GetBytes(testText);
                    byte[] encrypted = XXTEA.Encrypt(plaintext, key);
                    byte[] decrypted = XXTEA.Decrypt(encrypted, key);
                    string result = Encoding.UTF8.GetString(decrypted);

                    bool passed = testText == result;
                    if (passed) passedTests++;

                    Color color = passed ? Color.Green : Color.Red;

                    AppendTestResult(rtb, $"Text: '{testText}', Passed: {passed}", color);
                    AppendTestResult(rtb, $"  Encrypted size: {encrypted.Length} bytes", Color.Cyan);
                }
                catch (Exception ex)
                {
                    AppendTestResult(rtb, $"Text: '{testText}' - ERROR: {ex.Message}", Color.Red);
                }
            }

            AppendTestResult(rtb, $"XXTEA rezultat: {passedTests}/{totalTests} testova prošlo",
                passedTests == totalTests ? Color.Green : Color.Red);
            AppendTestResult(rtb, "", Color.White);
        }

        private void TestAESCBC(RichTextBox rtb)
        {
            AppendTestResult(rtb, "=== AES-CBC TEST ===", Color.Yellow);
            AppendTestResult(rtb, "", Color.White);

            string[] testTexts = {
                "Hello World", "Short", "This is a longer message to test padding",
                "Exactly16Bytes!!", "123", "AES-CBC test with various lengths"
            };
            byte[] key = Encoding.UTF8.GetBytes("1234567890123456");
            byte[] iv = Encoding.UTF8.GetBytes("abcdefghijklmnop");

            int passedTests = 0, totalTests = testTexts.Length;

            foreach (string testText in testTexts)
            {
                try
                {
                    byte[] plaintext = Encoding.UTF8.GetBytes(testText);
                    byte[] encrypted = AESCBC.Encrypt(plaintext, key, iv);
                    byte[] decrypted = AESCBC.Decrypt(encrypted, key, iv);
                    string result = Encoding.UTF8.GetString(decrypted).TrimEnd('\0');

                    bool passed = testText == result;
                    if (passed) passedTests++;

                    Color color = passed ? Color.Green : Color.Red;

                    AppendTestResult(rtb, $"Text: '{testText}', Passed: {passed}", color);
                    AppendTestResult(rtb, $"  Original: {plaintext.Length} bytes, Encrypted: {encrypted.Length} bytes", Color.Cyan);
                }
                catch (Exception ex)
                {
                    AppendTestResult(rtb, $"Text: '{testText}' - ERROR: {ex.Message}", Color.Red);
                }
            }

            AppendTestResult(rtb, $"AES-CBC rezultat: {passedTests}/{totalTests} testova prošlo",
                passedTests == totalTests ? Color.Green : Color.Red);
            AppendTestResult(rtb, "", Color.White);
        }

        #endregion

        #region Helper Methods

        // THREAD-SAFE LOGGING METHOD
        private void LogMessage(string message, Color color)
        {
            LogMessageThreadSafe(message, color);
        }

        private void LogMessageThreadSafe(string message, Color color)
        {
            if (rtbLog == null) return;

            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string, Color>(LogMessageThreadSafe), message, color);
                return;
            }

            try
            {
                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.SelectionLength = 0;
                rtbLog.SelectionColor = color;
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                rtbLog.ScrollToCaret();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogMessage error: {ex.Message}");
            }
        }

        private void AppendTestResult(RichTextBox rtb, string text, Color color)
        {
            if (rtb == null) return;

            if (rtb.InvokeRequired)
            {
                rtb.Invoke(new Action<RichTextBox, string, Color>(AppendTestResult), rtb, text, color);
                return;
            }

            try
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = color;
                rtb.AppendText(text + "\n");
                rtb.ScrollToCaret();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppendTestResult error: {ex.Message}");
            }
        }

        private void BrowseFolder(TextBox textBox)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Izaberite direktorijum";
                fbd.SelectedPath = textBox.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void OpenDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    MessageBox.Show($"Direktorijum ne postoji: {path}", "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri otvaranju direktorijuma: {ex.Message}", "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSettings(string targetDir, string outputDir)
        {
            try
            {
                _settings.TargetDirectory = targetDir;
                _settings.OutputDirectory = outputDir;

                if (!Directory.Exists(_settings.TargetDirectory))
                    Directory.CreateDirectory(_settings.TargetDirectory);
                if (!Directory.Exists(_settings.OutputDirectory))
                    Directory.CreateDirectory(_settings.OutputDirectory);

                UpdateUI();
                LogMessageThreadSafe("Podešavanja sačuvana!", Color.Green);
                MessageBox.Show("Podešavanja su uspešno sačuvana!", "Uspeh", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessageThreadSafe($"Greška pri čuvanju podešavanja: {ex.Message}", Color.Red);
                MessageBox.Show($"Greška pri čuvanju podešavanja: {ex.Message}", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateUI()
        {
            if (txtTargetDir != null)
                txtTargetDir.Text = _settings.TargetDirectory;
            if (txtOutputDir != null)
                txtOutputDir.Text = _settings.OutputDirectory;

            UpdateFSWUI();
        }

        private void UpdateFSWUI()
        {
            bool isWatching = _fileWatcher.IsWatching;

            if (btnStartFSW != null)
                btnStartFSW.Enabled = !isWatching;
            if (btnStopFSW != null)
                btnStopFSW.Enabled = isWatching;

            if (lblFSWStatus != null)
            {
                lblFSWStatus.Text = isWatching ? "Status: AKTIVAN" : "Status: Neaktivan";
                lblFSWStatus.ForeColor = isWatching ? Color.Green : Color.Red;
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

        private string GetCurrentAlgorithmName()
        {
            if (rbRailfence.Checked) return "Railfence Cipher";
            if (rbXXTEA.Checked) return "XXTEA";
            if (rbAESCBC.Checked) return "AES-CBC";
            return "Nije izabran algoritam";
        }

      
            protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_fileWatcher.IsWatching)
            {
                var result = MessageBox.Show("File System Watcher je aktivan. Da li želite da ga zaustavite i zatvorite aplikaciju?",
                    "Zatvaranje aplikacije", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _fileWatcher.StopWatching();
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Zaustavi TCP server i sve timer-e
            if (_tcpManager != null && _tcpManager.IsListening)
            {
                _tcpManager.StopServer();
            }

            base.OnFormClosing(e);
        }
    
        private async Task SendSelectedFile(string serverIP, int port, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    LogMessageThreadSafe("Molim prvo izaberite fajl!", Color.Orange);
                    return;
                }

                if (!File.Exists(filePath))
                {
                    LogMessageThreadSafe("Izabrani fajl ne postoji!", Color.Red);
                    return;
                }

                var (algorithmChoice, parameters) = GetSelectedAlgorithm();
                if (algorithmChoice == -1)
                {
                    LogMessageThreadSafe("Molim prvo izaberite algoritam u prvom tabu!", Color.Red);
                    MessageBox.Show("Molim prvo izaberite algoritam u tabu 'Šifrovanje/Dešifrovanje'!",
                        "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                progressBar.Visible = true;
                lblStatus.Text = "Slanje fajla u toku...";

                LogMessageThreadSafe($"Početak slanja fajla: {Path.GetFileName(filePath)}", Color.Yellow);
                LogMessageThreadSafe($"Server: {serverIP}:{port}", Color.Gray);
                LogMessageThreadSafe($"Algoritam: {GetAlgorithmName(algorithmChoice)}", Color.Cyan);

                bool success = await _tcpManager.SendFileAsync(serverIP, port, filePath, algorithmChoice, parameters);

                if (success)
                {
                    LogMessageThreadSafe("Fajl uspešno poslat!", Color.Green);
                    MessageBox.Show("Fajl je uspešno poslat kolegi!", "Uspeh",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessageThreadSafe("Neuspešno slanje fajla!", Color.Red);
                    MessageBox.Show("Dogodila se greška pri slanju fajla.", "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessageThreadSafe($"Greška pri slanju: {ex.Message}", Color.Red);
                MessageBox.Show($"Greška: {ex.Message}", "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
                lblStatus.Text = "Spreman";
            }
        }
        #endregion
    }

    public class Settings
    {
        public string TargetDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Target");
        public string OutputDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "X");

        public Settings()
        {
            if (!Directory.Exists(TargetDirectory))
                Directory.CreateDirectory(TargetDirectory);
            if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory);
        }
    }

    public class UserInterface
    {
        public void Start()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}