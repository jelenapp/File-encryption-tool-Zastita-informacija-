# ZastitaInfo - File Encryption Tool

A C# (.NET Framework 4.7.2) Windows Forms application for file encryption/decryption
with automatic directory monitoring via File System Watcher.

## Features

- Encrypt and decrypt files using multiple cryptographic algorithms
- Automatic file encryption when new files are added to a watched directory
- Tiger Hash integrity verification for encrypted files
- Built-in algorithm test suite

## Algorithms

| Algorithm | Type | Key Size |
|---|---|---|
| Railfence Cipher | Transposition cipher | Rail count (int) |
| XXTEA | Symmetric block cipher | 128-bit |
| AES-CBC | Symmetric block cipher | 128-bit + 128-bit IV |
| Tiger Hash | Hash function | 192-bit output |

## Requirements

- Windows OS
- .NET Framework 4.7.2
- Visual Studio 2019 or newer (to build)

## Getting Started

1. Clone the repository
2. Open `zastitaInfoProjekat.csproj` in Visual Studio
3. Build and run the project

To run algorithm tests in console mode:
```
zastitaInfoProjekat.exe --console
```

## Project Structure
```
├── Program.cs               # Entry point
├── UserInterface.cs         # Windows Forms UI (MainForm)
├── FileManager.cs           # File read/write and encryption logic
├── FileSystemWatcher.cs     # Automatic directory monitoring
├── RailfenceCipher.cs       # Railfence cipher implementation
├── XXTEA.cs                 # XXTEA cipher implementation
├── AESCBC.cs                # AES-CBC cipher implementation
├── TigerHash.cs             # Tiger hash implementation
└── AlgorithmTests.cs        # Unit tests for all algorithms
```

## Usage

1. Select an encryption algorithm and configure its parameters
2. Choose a target directory to monitor and an output directory
3. Start the File System Watcher — any new file dropped into the target directory
   will be automatically encrypted and saved to the output directory
4. To decrypt, select the encrypted file and provide the same key/parameters used during encryption