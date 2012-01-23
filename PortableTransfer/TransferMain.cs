using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using PortableTransfer.Helpers;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Windows.Forms;
using EncryptionClassLibrary.Encryption;

namespace PortableTransfer {
    public class DoRestoreArgs {
        private BackupProgressHandler maxValueHandler;
        private BackupProgressHandler progressHandler;
        private BackupShowDialog showDialog;
        private BackupFormMessageBox messageBox;
        private BackupEndHandler endHandler;
        private BackupGetPassword getPassword;
        private int? backupNum;
        /// <summary>
        /// Summary for DoRestoreArgs
        /// </summary>
        public DoRestoreArgs(BackupProgressHandler maxValueHandler, BackupProgressHandler progressHandler, BackupEndHandler endHandler, BackupShowDialog showDialog, BackupFormMessageBox messageBox, BackupGetPassword getPassword, int backupNum)
        :this(maxValueHandler, progressHandler, endHandler, showDialog, messageBox, getPassword){
            this.backupNum = backupNum;
        }
        public DoRestoreArgs(BackupProgressHandler maxValueHandler, BackupProgressHandler progressHandler, BackupEndHandler endHandler, BackupShowDialog showDialog, BackupFormMessageBox messageBox, BackupGetPassword getPassword) {
            this.getPassword = getPassword;
            this.maxValueHandler = maxValueHandler;
            this.progressHandler = progressHandler;
            this.endHandler = endHandler;
            this.showDialog = showDialog;
            this.messageBox = messageBox;
        }
        public BackupProgressHandler MaxValueHandler { get { return maxValueHandler; } }
        public BackupProgressHandler ProgressHandler { get { return progressHandler; } }
        public BackupShowDialog ShowDialog { get { return showDialog; } }
        public BackupFormMessageBox MessageBox { get { return messageBox; } }
        public BackupEndHandler EndHandler { get { return endHandler; } }
        public BackupGetPassword GetPassword { get { return getPassword; } }
        public int? BackupNum { get { return backupNum; } }
    }
    public class DoBackupArgs {
        private BackupProgressHandler maxValueHandler;
        private BackupProgressHandler progressHandler;
        private BackupEndHandler endHandler;
        private BackupShowDialog showDialog;
        private BackupFormMessageBox messageBox;       
        private BackupGetPassword getPassword;
        /// <summary>
        /// Summary for DoBackupArgs
        /// </summary>
        public DoBackupArgs(BackupProgressHandler maxValueHandler, BackupProgressHandler progressHandler, BackupEndHandler endHandler, BackupShowDialog showDialog, BackupFormMessageBox messageBox, BackupGetPassword getPassword) {
            this.maxValueHandler = maxValueHandler;
            this.progressHandler = progressHandler;
            this.endHandler = endHandler;
            this.showDialog = showDialog;
            this.getPassword = getPassword;
            this.messageBox = messageBox;
        }
        public BackupProgressHandler MaxValueHandler { get { return maxValueHandler; } }
        public BackupProgressHandler ProgressHandler { get { return progressHandler; } }
        public BackupEndHandler EndHandler { get { return endHandler; } }
        public BackupShowDialog ShowDialog { get { return showDialog; } }
        public BackupFormMessageBox MessageBox { get { return messageBox; } }
        public BackupGetPassword GetPassword { get { return getPassword; } }
    }
    public class TransferMain {
        TransferConfig config;
        readonly byte[] backupStorageHeaderBytes;
        readonly byte[] backupListHeaderBytes;
        Dictionary<string, int> backupDirectoryDict = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, int> targetDirectoryDict = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, FileTransferInfo> backupTransferDict = new Dictionary<string, FileTransferInfo>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, FileTransferInfo> targetTransferDict = new Dictionary<string, FileTransferInfo>(StringComparer.InvariantCultureIgnoreCase);
        List<FileTransferDiff> differencesList = new List<FileTransferDiff>();
        List<FileTransferInfo> equalsList = new List<FileTransferInfo>();
        public TransferConfig Config { get { return config; } }
        public TransferMain(TransferConfig config) {
            this.config = config;
            backupStorageHeaderBytes = Encoding.UTF8.GetBytes(TransferConst.BackupStorageHeader);
            backupListHeaderBytes = Encoding.UTF8.GetBytes(TransferConst.BackupListHeader);
        }
        public int GetLastBackupStorageNum() {
            string[] files = Directory.GetFiles(config.BackupPath, config.BackupStorageNameMask);
            if (files.Length == 0) return -1;
            Dictionary<string, bool> filesDict = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
            foreach (string file in files) {
                filesDict[file] = true;
            }
            int num = -1;
            for (int i = 0; i < 100; i++) {
                string currentName = Path.Combine(config.BackupPath, string.Format(config.BackupStorageNameFormat, i));
                if (filesDict.ContainsKey(currentName)) {
                    num = i;
                }
            }
            return num;
        }
        public TransferBackupVersionInfo[] GetBackupStorageVersions() {
            string[] files = Directory.GetFiles(config.BackupPath, config.BackupStorageNameMask);
            if (files.Length == 0) return new TransferBackupVersionInfo[0];
            Dictionary<string, bool> filesDict = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
            foreach (string file in files) {
                filesDict[file] = true;
            }
            List<TransferBackupVersionInfo> result = new List<TransferBackupVersionInfo>();
            for (int i = 0; i < 100; i++) {
                string currentName = Path.Combine(config.BackupPath, string.Format(config.BackupStorageNameFormat, i));
                if (filesDict.ContainsKey(currentName)) {
                    result.Add(new TransferBackupVersionInfo(i, currentName, File.GetCreationTime(currentName)));
                }
            }
            return result.ToArray();
        }
        public static byte[] CompressData(byte[] data) {
            return CompressHelper.TryToCompressData(data);
        }

        public static byte[] DecompressData(byte[] data) {
            return CompressHelper.TryToDecompressData(data);
        }
        long GetBackupSize() {
            string[] files = Directory.GetFiles(config.BackupPath, config.BackupStorageNameMask);
            long backupSize = 0;
            foreach (string fileName in files) {
                FileInfo fi = new FileInfo(fileName);
                backupSize += fi.Length;
            }
            return backupSize / (1024 * 1024);
        }
        void ClearBackup() {
            string[] backupFiles = Directory.GetFiles(config.BackupPath, config.BackupStorageNameMask);
            foreach (string file in backupFiles) {
                File.Delete(file);
            }
        }
        bool LoadBackupTransferDict(int lastExistsNum, out byte[] passwordCheckData) {
            backupDirectoryDict.Clear();
            backupTransferDict.Clear();
            if (lastExistsNum < 0) {
                passwordCheckData = new byte[0];
                return false;
            }
            string backupStorageFileName = Path.Combine(config.BackupPath, string.Format(config.BackupStorageNameFormat, lastExistsNum));
            FileTransferList fileTransferList = null;
            using (FileStream fs = new FileStream(backupStorageFileName, FileMode.Open, FileAccess.Read)) {
                fs.Seek(-4, SeekOrigin.End);
                byte[] lengthData = new byte[4];
                if (fs.Read(lengthData, 0, 4) != 4) throw new InvalidDataException("Wronge backup list file format.");
                int listDataLength = BitConverter.ToInt32(lengthData, 0);
                fs.Seek(-(listDataLength + 4 + 4), SeekOrigin.End);
                if (fs.Read(lengthData, 0, 4) != 4) throw new InvalidDataException("Wronge backup list file format.");
                int passwordCheckDataLength = BitConverter.ToInt32(lengthData, 0);
                if (passwordCheckDataLength == 0) passwordCheckData = new byte[0];
                else {
                    fs.Seek(-(listDataLength + 4 + 4 + passwordCheckDataLength), SeekOrigin.End);
                    passwordCheckData = new byte[passwordCheckDataLength];
                    fs.Read(passwordCheckData, 0, passwordCheckDataLength);
                }
                fs.Seek(-(listDataLength + 4), SeekOrigin.End);
                byte[] listData = new byte[listDataLength];
                fs.Read(listData, 0, listDataLength);
                listData = CompressHelper.DecompressData(listData);
                
                using (MemoryStream ms = new MemoryStream(listData)) {
                    ms.Position = CheckBackupListFormat(listData);
                    XmlSerializer xs = new XmlSerializer(typeof(FileTransferList));
                    fileTransferList = (FileTransferList)xs.Deserialize(ms);
                }
            }
            if (fileTransferList == null) return false;
            Dictionary<int, string> directoryOidDictionary = new Dictionary<int, string>();
            for (int i = 0; i < fileTransferList.Directories.Length; i++) {
                backupDirectoryDict[fileTransferList.Directories[i].Path] = fileTransferList.Directories[i].Oid;
                directoryOidDictionary[fileTransferList.Directories[i].Oid] = fileTransferList.Directories[i].Path;
            }
            for (int i = 0; i < fileTransferList.Items.Length; i++) {
                FileTransferInfo ftInfo = fileTransferList.Items[i];
                ftInfo.RelPath = Path.Combine(directoryOidDictionary[ftInfo.DirectoryOid], ftInfo.Name);
                backupTransferDict[ftInfo.RelPath] = ftInfo;
            }
            return true;
        }

        int CheckBackupListFormat(byte[] listData) {
            byte[] maybeBackupListHeader = new byte[backupListHeaderBytes.Length];
            Array.Copy(listData, maybeBackupListHeader, backupListHeaderBytes.Length);
            if (!CollectionHelper.BytesAreEquals(maybeBackupListHeader, backupListHeaderBytes)) throw new InvalidDataException("Wronge backup list file format.");
            return backupListHeaderBytes.Length;
        }

        void LoadTargetTransferDict() {
            targetDirectoryDict.Clear();
            targetTransferDict.Clear();
            string[] directories = Directory.GetDirectories(config.TargetPath, "*", SearchOption.AllDirectories);
            int directoryOidCounter = 1;
            targetDirectoryDict.Add(string.Empty, directoryOidCounter++);
            for (int i = 0; i < directories.Length; i++) {
                string directoryCut = directories[i].Substring(config.TargetPath.Length);
                targetDirectoryDict.Add(directoryCut, directoryOidCounter++);
            }
            string[] files = Directory.GetFiles(config.TargetPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++) {
                string fileName = files[i];
                FileInfo fi = new FileInfo(fileName);
                string fileNameCut = fileName.Substring(config.TargetPath.Length);
                targetTransferDict.Add(fileNameCut, new FileTransferInfo(fileNameCut, fi.LastWriteTimeUtc, fi.Attributes));
            }
        }
        void ProcessDifferenceList() {
            differencesList.Clear();
            equalsList.Clear();
            foreach (FileTransferInfo targetInfo in targetTransferDict.Values) {
                FileTransferInfo backupInfo;
                if (backupTransferDict.TryGetValue(targetInfo.RelPath, out backupInfo)) {
                    if (targetInfo.LastWriteTime == backupInfo.LastWriteTime && targetInfo.Attributes == backupInfo.Attributes) {
                        equalsList.Add(backupInfo);
                        continue;
                    }
                    differencesList.Add(new FileTransferDiff(FileTransferDiffType.UpdateBackup, backupInfo, targetInfo));
                    continue;
                }
                differencesList.Add(new FileTransferDiff(FileTransferDiffType.NewBackup, null, targetInfo));
            }
            foreach (FileTransferInfo backupInfo in backupTransferDict.Values) {
                if (targetTransferDict.ContainsKey(backupInfo.RelPath)) continue;
                differencesList.Add(new FileTransferDiff(FileTransferDiffType.DeleteBackup, backupInfo, null));
            }
        }
        void PrepareDirectoriesForRestore() {
            foreach (string backupDir in backupDirectoryDict.Keys) {
                if (targetDirectoryDict.ContainsKey(backupDir)) continue;
                Directory.CreateDirectory(Path.Combine(config.TargetPath, backupDir));
            }
            if (!config.SafeMode) {
                foreach (string targetDir in targetDirectoryDict.Keys) {
                    if (backupDirectoryDict.ContainsKey(targetDir)) continue;
                    string targetDirFull = Path.Combine(config.TargetPath, targetDir);
                    string[] filesToDelete = Directory.GetFiles(targetDirFull, "*", SearchOption.AllDirectories);
                    foreach (string fileToDelete in filesToDelete) {
                        try {
                            File.SetAttributes(fileToDelete, FileAttributes.Normal);
                            File.Delete(fileToDelete);
                        } catch (Exception) { }
                    }
                    try {
                        Directory.Delete(targetDir, true);
                    } catch (Exception) { }
                }
            }
        }
        static bool CheckPassword(Symmetric sym, Data passwordData, byte[] passwordCheckData) {
            bool wrongPassword = false;
            try {
                using (MemoryStream encMS = new MemoryStream(passwordCheckData)) {
                    passwordCheckData = sym.Decrypt(encMS, passwordData).Bytes;
                }
                byte[] guid = new byte[16];
                Array.Copy(passwordCheckData, guid, 16);
                byte[] md5InFile = new byte[passwordCheckData.Length - 16];
                Array.Copy(passwordCheckData, 16, md5InFile, 0, passwordCheckData.Length - 16);
                byte[] md5 = CompressHelper.Md5.ComputeHash(guid);
                if (!CollectionHelper.BytesAreEquals(md5, md5InFile))
                    wrongPassword = true;
            } catch (Exception) {
                wrongPassword = true;
            }
            return wrongPassword;
        }
        public TransferJournalItem DoRestore(DoRestoreArgs doRestoreArgs) {
            Exception resultException = null;
            try {
                TransferLog.Log(string.Format("Starting restore: {0}", config.Name));
                if (!Directory.Exists(config.TargetPath)) {
                    TransferLog.Log(string.Format("Create directory: {0}", config.TargetPath));
                    Directory.CreateDirectory(config.TargetPath);
                }
                Symmetric sym = null;
                Data passwordData = null;
                if (config.UseEncryption) {
                    string password = doRestoreArgs.GetPassword(config.Name, false);
                    if (password == null) throw new ArgumentException("No password.");
                    sym = new Symmetric(Symmetric.Provider.Rijndael, true);
                    passwordData = new Data(password);
                }
                TransferLog.Log("Load backup info...");
                int backupNum = doRestoreArgs.BackupNum ?? GetLastBackupStorageNum();
                byte[] passwordCheckData;
                if (!LoadBackupTransferDict(backupNum, out passwordCheckData)) return null;
                if (passwordCheckData.Length > 0 && config.UseEncryption) {
                    if (CheckPassword(sym, passwordData, passwordCheckData)) {
                        doRestoreArgs.ShowDialog("Wrong password!", "Portable Transfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
                TransferLog.Log("Load target info...");
                LoadTargetTransferDict();
                TransferLog.Log("Prepare directory for restore...");
                PrepareDirectoriesForRestore();
                TransferLog.Log("Process differences...");
                ProcessDifferenceList();
                if (doRestoreArgs.MaxValueHandler != null) {
                    doRestoreArgs.MaxValueHandler(differencesList.Count);
                }
                int pos = 0;
                TransferLog.Log(string.Format("Differences count: {0}", differencesList.Count));
                if (differencesList.Count == 0) return null;
                for (int i = 0; i < differencesList.Count; i++) {
                    if (differencesList[i].Type == FileTransferDiffType.UpdateTarget) {
                        if (differencesList[i].BackupInfo.LastWriteTime < differencesList[i].TargetInfo.LastWriteTime) {
                            string message = string.Format("Target folder contains file '{0}' with last write time({1}) later then file you want to overwrite({2});\nDo you want to continue?",
                                differencesList[i].BackupInfo.Name, differencesList[i].TargetInfo.LastWriteTime,
                                differencesList[i].BackupInfo.LastWriteTime);
                            if (doRestoreArgs.ShowDialog(message, "Portable Transfer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) {
                                return null;
                            }
                        }
                    }
                }
                List<FileTransferDiff> filesToDelete = new List<FileTransferDiff>();
                Dictionary<int, List<FileTransferDiff>> filesListDict = new Dictionary<int, List<FileTransferDiff>>();
                Dictionary<string, bool> targetDirectoriesDict = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
                for (int i = 0; i < differencesList.Count; i++) {
                    FileTransferDiff diff = differencesList[i];
                    switch (diff.Type) {
                        case FileTransferDiffType.DeleteTarget:
                            filesToDelete.Add(diff);
                            break;
                        case FileTransferDiffType.NewTarget:
                        case FileTransferDiffType.UpdateTarget:
                            string targetFileName = diff.Type == FileTransferDiffType.NewTarget ? diff.BackupInfo.GetFullTargetPath(config) : diff.TargetInfo.GetFullTargetPath(config);
                            string targetDirName = Path.GetDirectoryName(targetFileName);
                            targetDirectoriesDict[targetDirName] = true;
                            List<FileTransferDiff> diffList;
                            if (!filesListDict.TryGetValue(diff.BackupInfo.ArchiveNumber, out diffList)) {
                                diffList = new List<FileTransferDiff>();
                                filesListDict.Add(diff.BackupInfo.ArchiveNumber, diffList);
                            }
                            diffList.Add(diff);
                            break;
                        default:
                            break;
                    }
                }
                foreach (string targetDirName in targetDirectoriesDict.Keys) {
                    if (!Directory.Exists(targetDirName)) {
                        TransferLog.Log(string.Format("Create directory: {0}", targetDirName));
                        Directory.CreateDirectory(targetDirName);
                    }
                }
                using (AutoResetEvent nextWriteFileEvent = new AutoResetEvent(true)) {
                    foreach (KeyValuePair<int, List<FileTransferDiff>> pair in filesListDict) {
                        string fileName = Path.Combine(config.BackupPath, string.Format(config.BackupStorageNameFormat, pair.Key));
                        List<FileTransferDiff> list = pair.Value;
                        list.Sort(new FileTransferDiffByShiftInArchiveComparer());
                        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
                            byte[] maybeBackupStorageHeader = new byte[backupStorageHeaderBytes.Length];
                            fs.Read(maybeBackupStorageHeader, 0, maybeBackupStorageHeader.Length);
                            if (!CollectionHelper.BytesAreEquals(maybeBackupStorageHeader, backupStorageHeaderBytes))
                                throw new InvalidDataException("Wronge backup file format.");
                            foreach (FileTransferDiff diff in list) {
                                byte[] compressedData = new byte[diff.BackupInfo.SizeInArchive];
                                fs.Seek(diff.BackupInfo.Shift, SeekOrigin.Begin);
                                fs.Read(compressedData, 0, diff.BackupInfo.SizeInArchive);
                                if (config.UseEncryption) {
                                    using (MemoryStream encMS = new MemoryStream(compressedData)) {
                                        compressedData = sym.Decrypt(encMS, passwordData).Bytes;
                                    }
                                }
                                byte[] data = DecompressData(compressedData);
                                nextWriteFileEvent.WaitOne();
                                switch (diff.Type) {
                                    case FileTransferDiffType.NewTarget: {
                                            string targetFileName = diff.BackupInfo.GetFullTargetPath(config);
                                            DateTime lastWriteTime = diff.BackupInfo.LastWriteTime;
                                            FileAttributes attributes = diff.BackupInfo.Attributes;
                                            ThreadPool.QueueUserWorkItem(o => {
                                                try {
                                                    TransferLog.Log(string.Format("Create file: {0}", targetFileName));
                                                    File.WriteAllBytes(targetFileName, data);
                                                    File.SetLastWriteTimeUtc(targetFileName, lastWriteTime);
                                                    File.SetAttributes(targetFileName, attributes);
                                                } catch (Exception ex) {
                                                    TransferLog.LogException(ex);
                                                } finally {
                                                    nextWriteFileEvent.Set();
                                                }
                                            });
                                        }
                                        break;
                                    case FileTransferDiffType.UpdateTarget: {
                                            string targetFileName = diff.TargetInfo.GetFullTargetPath(config);
                                            if (File.Exists(targetFileName)) {
                                                File.SetAttributes(targetFileName, FileAttributes.Normal);
                                                File.Delete(targetFileName);
                                            }
                                            DateTime lastWriteTime = diff.BackupInfo.LastWriteTime;
                                            FileAttributes attributes = diff.TargetInfo.Attributes;
                                            ThreadPool.QueueUserWorkItem(o => {
                                                try {
                                                    TransferLog.Log(string.Format("Write file: {0}", targetFileName));
                                                    File.WriteAllBytes(targetFileName, data);
                                                    File.SetLastWriteTimeUtc(targetFileName, lastWriteTime);
                                                    File.SetAttributes(targetFileName, attributes);
                                                } catch (Exception ex) {
                                                    TransferLog.LogException(ex);
                                                } finally {
                                                    nextWriteFileEvent.Set();
                                                }
                                            });
                                        }
                                        break;
                                    default:
                                        nextWriteFileEvent.Set();
                                        break;
                                }
                                if (((pos % 5) == 0) && doRestoreArgs.ProgressHandler != null)
                                    doRestoreArgs.ProgressHandler(pos);
                                pos++;
                            }
                        }
                    }
                    nextWriteFileEvent.WaitOne();
                }
                if (!config.SafeMode) {
                    foreach (FileTransferDiff diff in filesToDelete) {
                        string targetFileName = diff.TargetInfo.GetFullTargetPath(config);
                        if (File.Exists(targetFileName)) {
                            TransferLog.Log(string.Format("Delete file: {0}", targetFileName));
                            File.SetAttributes(targetFileName, FileAttributes.Normal);
                            File.Delete(targetFileName);
                        }
                        if (((pos % 5) == 0) && doRestoreArgs.ProgressHandler != null) doRestoreArgs.ProgressHandler(pos);
                        pos++;
                    }
                }
                return new TransferJournalItem(DateTime.Now, config.BackupGuid, backupNum, TransferJournalItemType.Restore);
            } catch (Exception ex) {
                TransferLog.LogException(ex);
                resultException = ex;
                return null;
            } finally {
                TransferLog.Log(string.Format("Completed successfuly: {0}", config.Name));
                if (doRestoreArgs.EndHandler != null) doRestoreArgs.EndHandler(resultException);
            }
        }
        public TransferJournalItem DoBackup(DoBackupArgs doBackupArgs) {
            Exception resultException = null;
            string nextBackupFileName = string.Empty;
            Symmetric sym = null;
            Data passwordData = null;
            Data ivData = null;
            try {
                TransferLog.Log(string.Format("Starting backup: {0}", config.Name));
                if (!Directory.Exists(config.BackupPath)) {
                    TransferLog.Log(string.Format("Create directory: {0}", config.TargetPath));
                    Directory.CreateDirectory(config.BackupPath);
                }
                int lastBackupNum = GetLastBackupStorageNum();
                if (config.UseEncryption) {
                    string password = doBackupArgs.GetPassword(config.Name, lastBackupNum < 0);
                    if (password == null) throw new ArgumentException("No password.");
                    sym = new Symmetric(Symmetric.Provider.Rijndael, true);
                    passwordData = new Data(password);
                    ivData = sym.IntializationVector;
                }
                TransferLog.Log("Load backup info...");
                byte[] passwordCheckData;
                LoadBackupTransferDict(lastBackupNum, out passwordCheckData);
                if (passwordCheckData.Length > 0 && config.UseEncryption) {
                    if (CheckPassword(sym, passwordData, passwordCheckData)) {
                        doBackupArgs.ShowDialog("Wrong password!", "Portable Transfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
                TransferLog.Log("Load taget info...");
                LoadTargetTransferDict();
                TransferLog.Log("Process differences...");
                ProcessDifferenceList();
                if (doBackupArgs.MaxValueHandler != null) doBackupArgs.MaxValueHandler(differencesList.Count);
                if (differencesList.Count == 0) return null;
                bool yesAll = false;
                for (int i = 0; i < differencesList.Count; i++) {
                    if (differencesList[i].Type == FileTransferDiffType.UpdateBackup) {
                        if (differencesList[i].BackupInfo.LastWriteTime > differencesList[i].TargetInfo.LastWriteTime) {
                            string message = string.Format("Backup contains file '{0}' with last write time({1}) later then file you want to overwrite({2});\nDo you want to continue?",
                                differencesList[i].BackupInfo.Name, differencesList[i].BackupInfo.LastWriteTime,
                                differencesList[i].TargetInfo.LastWriteTime);
                            if (!yesAll) {
                                switch (doBackupArgs.MessageBox(message, "Portable Transfer")) {
                                    case FormMessageBoxResult.No:
                                    case FormMessageBoxResult.NoAll:
                                        return null;
                                    case FormMessageBoxResult.YesAll:
                                        yesAll = true;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                //long backupSize = GetBackupSize();
                //if (backupSize > config.MaxBackupsSizeMB) {
                //    TransferLog.Log("Clear backup...");
                //    ClearBackup();
                //    TransferLog.Log("Load backup info...");
                //    LoadBackupTransferDict(GetLastBackupStorageNum());
                //    TransferLog.Log("Load taget info...");
                //    LoadTargetTransferDict();
                //    TransferLog.Log("Process differences...");
                //    ProcessDifferenceList();
                //}
                int lastBackupStorageNum = GetLastBackupStorageNum();
                int nextBackupStorageNum = lastBackupStorageNum + 1;
                TransferLog.Log(string.Format("Next backup storage number: {0}", nextBackupStorageNum));
                nextBackupFileName = Path.Combine(config.BackupPath, string.Format(config.BackupStorageNameFormat, nextBackupStorageNum));
                long position = 0;
                Exception exceptionInFilesReading = null;
                using (FileStream fs = new FileStream(nextBackupFileName, FileMode.Create, FileAccess.Write)) {
                    using (AutoResetEvent nextWriteFileEvent = new AutoResetEvent(true)) {
                        TransferLog.Log(string.Format("Create file: {0}", nextBackupFileName));
                        fs.Write(backupStorageHeaderBytes, 0, backupStorageHeaderBytes.Length);
                        position += backupStorageHeaderBytes.Length;
                        for (int i = 0; i < differencesList.Count; i++) {
                            FileTransferDiff diff = differencesList[i];
                            switch (diff.Type) {
                                case FileTransferDiffType.UpdateBackup:
                                case FileTransferDiffType.NewBackup: {
                                        diff.TargetInfo.ArchiveNumber = nextBackupStorageNum;
                                        diff.TargetInfo.Shift = position;
                                        string targetFileName = diff.TargetInfo.GetFullTargetPath(config);
                                        byte[] data = CompressData(CommonHelper.ReadAllBytes(targetFileName));
                                        if (config.UseEncryption) {
                                            using (MemoryStream encMS = new MemoryStream(data)) {
                                                data = sym.Encrypt(encMS, passwordData).Bytes;
                                            }
                                        }
                                        diff.TargetInfo.SizeInArchive = data.Length;
                                        position += data.Length;
                                        equalsList.Add(diff.TargetInfo);
                                        nextWriteFileEvent.WaitOne();
                                        if (exceptionInFilesReading != null) {
                                            throw exceptionInFilesReading;
                                        }
                                        ThreadPool.QueueUserWorkItem(o => {
                                            try {
                                                TransferLog.Log(string.Format("Backup file: {0}", targetFileName));
                                                fs.Write(data, 0, data.Length);
                                            } catch (Exception ex) {
                                                exceptionInFilesReading = ex;
                                            } finally {
                                                nextWriteFileEvent.Set();
                                            }
                                        });
                                    }
                                    break;
                                default:
                                    break;
                            }
                            if (((i % 5) == 0) && doBackupArgs.ProgressHandler != null)
                                doBackupArgs.ProgressHandler(i);
                        }
                        nextWriteFileEvent.WaitOne();
                    }
                    for (int i = 0; i < equalsList.Count; i++) {
                        string directory = Path.GetDirectoryName(equalsList[i].RelPath);
                        string name = Path.GetFileName(equalsList[i].RelPath);
                        int directoryOid = targetDirectoryDict[directory];
                        equalsList[i].Name = name;
                        equalsList[i].DirectoryOid = directoryOid;
                    }
                    List<DirectoryTransferInfo> directoriesToSave = new List<DirectoryTransferInfo>();
                    foreach (KeyValuePair<string, int> directory in targetDirectoryDict) {
                        directoriesToSave.Add(new DirectoryTransferInfo(directory.Value, directory.Key));
                    }
                    if (config.UseEncryption) {
                        byte[] guid = Guid.NewGuid().ToByteArray();
                        byte[] md5 = CompressHelper.Md5.ComputeHash(guid);
                        byte[] data = new byte[guid.Length + md5.Length];
                        Array.Copy(guid, data, guid.Length);
                        Array.Copy(md5, 0, data, guid.Length, md5.Length);
                        using (MemoryStream encMS = new MemoryStream(data)) {
                            data = sym.Encrypt(encMS, passwordData).Bytes;
                            fs.Write(data, 0, data.Length);
                        }
                        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
                        fs.Write(lengthBytes, 0, lengthBytes.Length);
                    } else {
                        byte[] lengthBytes = BitConverter.GetBytes((int)0);
                        fs.Write(lengthBytes, 0, lengthBytes.Length);
                    }
                    TransferLog.Log(string.Format("Next backup list number: {0}", nextBackupStorageNum));
                    SaveBackupFileList(fs, directoriesToSave);
                    return new TransferJournalItem(File.GetCreationTime(nextBackupFileName), config.BackupGuid, nextBackupStorageNum, TransferJournalItemType.Backup);
                }
            } catch (Exception ex) {
                if (File.Exists(nextBackupFileName)) {
                    File.Delete(nextBackupFileName);
                }
                TransferLog.LogException(ex);
                resultException = ex;
                return null;
            } finally {
                TransferLog.Log(string.Format("Completed successfuly: {0}", config.Name));
                backupTransferDict.Clear();
                targetTransferDict.Clear();
                equalsList.Clear();
                differencesList.Clear();
                if (doBackupArgs.EndHandler != null) doBackupArgs.EndHandler(resultException);
            }
        }

        void SaveBackupFileList(Stream resultStream, List<DirectoryTransferInfo> directoriesToSave) {
            XmlSerializer xs = new XmlSerializer(typeof(FileTransferList));
            FileTransferList ftl = new FileTransferList { Items = equalsList.ToArray(), Directories = directoriesToSave.ToArray() };
            byte[] data;
            using (MemoryStream ms = new MemoryStream()) {
                ms.Write(backupListHeaderBytes, 0, backupListHeaderBytes.Length);
                xs.Serialize(ms, ftl);
                data = ms.ToArray();
            }
            data = CompressHelper.TryToCompressData(data);
            resultStream.Write(data, 0, data.Length);
            byte[] lengthData = BitConverter.GetBytes(data.Length);
            resultStream.Write(lengthData, 0, lengthData.Length);
        }
    }
    public delegate void BackupProgressHandler(int value);
    public delegate void BackupEndHandler(Exception ex);
    public delegate DialogResult BackupShowDialog(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon);
    public delegate FormMessageBoxResult BackupFormMessageBox(string text, string caption);
    public delegate string BackupGetPassword(string backupName, bool isNew);

    [XmlRoot("transferList")]
    public class FileTransferList {
        [XmlElement("item")]
        public FileTransferInfo[] Items { get; set; }
        [XmlElement("directory")]
        public DirectoryTransferInfo[] Directories { get; set; }
    }

    public class TransferBackupVersionInfo {
        int version;
        string fileName;
        DateTime date;
        public int Version { get { return version; } }
        public string FileName { get { return fileName; } }
        public DateTime Date { get { return date; } }
        public TransferBackupVersionInfo(int version, string fileName, DateTime date) {
            this.version = version;
            this.fileName = fileName;
            this.date = date;
        }
    }
    public class DirectoryTransferInfo {
        [XmlAttribute("oid")]
        public int Oid { get; set; }
        [XmlAttribute("path")]
        public string Path { get; set; }
        public DirectoryTransferInfo() { }
        public DirectoryTransferInfo(int oid, string path) {
            Oid = oid;
            Path = path;
        }
        public override bool Equals(object obj) {
            DirectoryTransferInfo other = obj as DirectoryTransferInfo;
            if (other == null) return false;
            return Oid.Equals(other.Oid) && string.Equals(Path, other.Path);
        }
        public override int GetHashCode() {
            return Oid.GetHashCode() ^ (Path == null ? 0x64231549 : Path.GetHashCode());
        }
    }
    public class FileTransferInfo {
        [XmlIgnore()]
        public string RelPath { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("directoryOid")]
        public int DirectoryOid { get; set; }
        [XmlAttribute("lastWriteTime")]
        public DateTime LastWriteTime { get; set; }
        [XmlAttribute("shift")]
        public long Shift { get; set; }
        [XmlAttribute("sizeInArchive")]
        public int SizeInArchive { get; set; }
        [XmlAttribute("archNum")]
        public int ArchiveNumber { get; set; }
        [XmlAttribute("attributes")]
        public FileAttributes Attributes { get; set; }
        public FileTransferInfo() { }
        public FileTransferInfo(string path, DateTime lastWriteTime, FileAttributes attributes) {
            RelPath = path;
            LastWriteTime = lastWriteTime;
            Shift = -1;
            ArchiveNumber = -1;
            Attributes = attributes;
        }
        public string GetFullTargetPath(TransferConfig config) {
            return Path.Combine(config.TargetPath, RelPath);
        }
    }

    public class FileTransferDiffByShiftInArchiveComparer : IComparer<FileTransferDiff> {
        public int Compare(FileTransferDiff x, FileTransferDiff y) {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            if (x.BackupInfo == null && y.BackupInfo == null) return 0;
            if (x.BackupInfo == null) return -1;
            if (y.BackupInfo == null) return 1;
            return x.BackupInfo.Shift.CompareTo(y.BackupInfo.Shift);
        }
    }
    public class FileTransferDiff {
        FileTransferDiffType type;
        FileTransferInfo backupInfo;
        FileTransferInfo targetInfo;
        public FileTransferDiffType Type { get { return type; } }
        public FileTransferInfo BackupInfo { get { return backupInfo; } }
        public FileTransferInfo TargetInfo { get { return targetInfo; } }
        public FileTransferDiff(FileTransferDiffType type, FileTransferInfo backupInfo, FileTransferInfo targetInfo) {
            this.type = type;
            this.backupInfo = backupInfo;
            this.targetInfo = targetInfo;
        }
    }
    public enum FileTransferDiffType {
        NewBackup = 0,
        UpdateBackup = 1,
        DeleteBackup = 2,
        DeleteTarget = 0,
        UpdateTarget = 1,
        NewTarget = 2
    }

    public class TransferException : Exception {
        public TransferException() { }
        public TransferException(string message)
            : base(message) { }
        public TransferException(string message, Exception innerException)
            : base(message, innerException) { }
        public TransferException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
    public class AlreadyRestoredTransferException : TransferException {
        public AlreadyRestoredTransferException() { }
        public AlreadyRestoredTransferException(string message)
            : base(message) { }
        public AlreadyRestoredTransferException(string message, Exception innerException)
            : base(message, innerException) { }
        public AlreadyRestoredTransferException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}

