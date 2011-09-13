using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Design;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Xml.Serialization;
using PortableTransfer.Helpers;
using System.IO.Compression;

namespace PortableTransfer {

    public class TransferConfigManager {
        static string assemblyLocation;
        static string mainDirectoryPath;
        static string mainBackupsPath;
        static string mainTransferJournalPath;
        static string userDirectoryPath;
        static string computerGuidFileName;
#if !DEBUG
        static Guid computerGuid;
#endif
        public static string AssemblyLocation { get { return TransferConfigManager.assemblyLocation; } }
        public static string MainDirectoryPath { get { return TransferConfigManager.mainDirectoryPath; } }
        public static string MainBackupsPath { get { return TransferConfigManager.mainBackupsPath; } }
        public static string UserDirectoryPath { get { return TransferConfigManager.userDirectoryPath; } }
        public static string MainTransferJournalPath { get { return TransferConfigManager.mainTransferJournalPath; } }
        public static string ComputerGuidFileName { get { return TransferConfigManager.computerGuidFileName; } }
        string userTargetsPath;
        LocalBackupCollection backups = new LocalBackupCollection();
        LocalTargetCollection targets = new LocalTargetCollection();
        public string UserTargetsPath { get { return userTargetsPath; } }
        public LocalBackupCollection Backups { get { return backups; } }
        public LocalTargetCollection Targets { get { return targets; } }
        static TransferConfigManager() {
            assemblyLocation = Assembly.GetExecutingAssembly().Location;
            mainDirectoryPath = Path.GetDirectoryName(assemblyLocation);
            mainBackupsPath = Path.Combine(mainDirectoryPath, "transfer.config");
            mainTransferJournalPath = Path.Combine(mainDirectoryPath, "transfer.journal");
            userDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PortableTransfer");
            computerGuidFileName = Path.Combine(UserDirectoryPath, "computer.guid");
        }

        public static bool CheckNextTransferOperationSafety(Guid backupGuid, string backupName, int backupVersion, TransferJournalItemType backupType, TransferConfigManagerMessageHandler showMessage) {
            if(showMessage == null){
                TransferLog.Log("Can't check safety next transfer operation, because of showMessageHandle is null.");
                return true;
            }
            Guid computerGuid = GetComputerGuid();
            TransferJournalItem[] journal = LoadTransferJournal();
            if (journal.Length == 0) return true;
            switch (backupType) {
                case TransferJournalItemType.Backup: {
                        for (int i = journal.Length - 1; i >= 0; i--) {
                            if ((journal[i].BackupId == backupGuid) && (journal[i].Type == TransferJournalItemType.Backup)) {
                                if (journal[i].ComputerId == computerGuid) return true;
                                for (int j = (i + 1); j < journal.Length; j++) {
                                    if ((journal[j].BackupId == backupGuid) && (journal[j].Type == TransferJournalItemType.Restore) && (journal[j].ComputerId == computerGuid)) {
                                        return true;
                                    }
                                }
                                return showMessage(string.Format("Backup '{0}' was produced on another computer, but the recovery has not been on the current computer."
                                                    + "\nDo you really want to back up '{0}'.", backupName));
                            }
                        }
                    }
                    break;
                case TransferJournalItemType.Restore: {
                        for (int i = journal.Length - 1; i >= 0; i--) {
                            if(journal[i].BackupId == backupGuid){
                                if (journal[i].ComputerId == computerGuid) {
                                    if (journal[i].BackupVersion != backupVersion) return true;
                                    switch(journal[i].Type){
                                        case TransferJournalItemType.Backup:
                                            return showMessage(string.Format("Previous recovery of '{0}' was made on the current computer."
                                                    + "\nDo you really want to make a second recovery of '{0}'?", backupName));
                                        case TransferJournalItemType.Restore:
                                            return showMessage(string.Format("Previous backup of '{0}' was made on the current computer."
                                                    + "\nDo you really want to make recovery of it?", backupName));
                                        default:
                                            return true;
                                    }
                                } else
                                    return true;
                            }
                        }
                    }
                    break;
                default:
                    return true;
            }
            return true;
        }

        public static Guid GetComputerGuid() {
#if DEBUG
            Guid computerGuid = new Guid();           
#endif
            CommonHelper.RunInMutex(TransferConst.PortableTransferComputerGuidMutex, delegate {
                byte[] portableTransferComputerGuidHeaderByte = Encoding.UTF8.GetBytes(TransferConst.PortableTransferComputerGuidHeader);
                if (computerGuid == Guid.Empty) {
                    if (File.Exists(computerGuidFileName)) {
                        try {                            
                            using (FileStream fs = new FileStream(computerGuidFileName, FileMode.Open, FileAccess.Read)) {
                                byte[] header = new byte[portableTransferComputerGuidHeaderByte.Length];
                                if (fs.Read(header, 0, header.Length) == header.Length) {
                                    if (CollectionHelper.BytesAreEquals(portableTransferComputerGuidHeaderByte, header)) {
                                        byte[] guidBuffer = new byte[16];
                                        if (fs.Read(guidBuffer, 0, 16) == 16) {
                                            computerGuid = new Guid(guidBuffer);
                                        }
                                    }
                                }
                            }
                        } catch (Exception ex) {
                            TransferLog.LogException(ex);
                        }
                    }
                    if (computerGuid == Guid.Empty) {
                        try {
                            using (FileStream fs = new FileStream(computerGuidFileName, FileMode.Create, FileAccess.Write)) {
                                fs.Write(portableTransferComputerGuidHeaderByte, 0, portableTransferComputerGuidHeaderByte.Length);
                                Guid newComputerGuid = Guid.NewGuid();
                                fs.Write(newComputerGuid.ToByteArray(), 0, 16);
                                computerGuid = newComputerGuid;
                            }
                        } catch (Exception ex) {
                            TransferLog.LogException(ex);
                        }
                    }
                }
            });
            return computerGuid;
        }

        public void LoadData() {
            backups.InfoDict.Clear();
            if (Directory.Exists(mainDirectoryPath)) {
                if (File.Exists(mainBackupsPath)) {
                    using (FileStream fs = new FileStream(mainBackupsPath, FileMode.Open, FileAccess.Read)) {
                        try {
                            XmlSerializer xs = new XmlSerializer(typeof(LocalBackupCollection));
                            backups = (LocalBackupCollection)xs.Deserialize(fs);
                        } catch { }
                    }
                }
            }
            if (backups.BackupConfigGuid == Guid.Empty) {
                backups.BackupConfigGuid = Guid.NewGuid();
            }
            userTargetsPath = Path.Combine(userDirectoryPath, backups.BackupConfigGuid.ToString() + ".config");
            LoadUserData();
        }
        public void LoadUserData() {
            targets.InfoDict.Clear();
            if (!Directory.Exists(userDirectoryPath)) return;
            if (!File.Exists(userTargetsPath)) return;
            using (FileStream fs = new FileStream(userTargetsPath, FileMode.Open, FileAccess.Read)) {
                XmlSerializer xs = new XmlSerializer(typeof(LocalTargetCollection));
                targets = (LocalTargetCollection)xs.Deserialize(fs);
            }
        }
        public void SaveMainData() {
            if (!Directory.Exists(mainDirectoryPath)) Directory.CreateDirectory(mainDirectoryPath);
            using (FileStream fs = new FileStream(mainBackupsPath, FileMode.Create, FileAccess.Write)) {
                XmlSerializer xs = new XmlSerializer(typeof(LocalBackupCollection));
                xs.Serialize(fs, backups);
            }
        }
        public void SaveUserData() {
            if (!Directory.Exists(userDirectoryPath)) Directory.CreateDirectory(userDirectoryPath);
            using (FileStream fs = new FileStream(userTargetsPath, FileMode.Create, FileAccess.Write)) {
                XmlSerializer xs = new XmlSerializer(typeof(LocalTargetCollection));
                xs.Serialize(fs, targets);
            }
        }

        public List<LocalInfo> GetLocalInfoList() {
            List<LocalInfo> list = new List<LocalInfo>(backups.InfoDict.Count);
            foreach (LocalBackupInfo lbInfo in backups.InfoDict.Values) {
                LocalInfo lInfo = new LocalInfo(lbInfo);
                LocalTargetInfo target;
                if (targets.InfoDict.TryGetValue(lbInfo.Guid, out target)) {
                    lInfo.TargetInfo = target;
                }
                list.Add(lInfo);
            }
            return list;
        }

        public void SetLocalInfoList(List<LocalInfo> localInfoList) {
            backups.InfoDict.Clear();
            for (int i = 0; i < localInfoList.Count; i++) {
                backups.InfoDict.Add(localInfoList[i].BackupInfo.Guid, localInfoList[i].BackupInfo);
                if (localInfoList[i].TargetInfo == null) continue;
                targets.InfoDict[localInfoList[i].TargetInfo.Guid] = localInfoList[i].TargetInfo;
            }
        }
        public void SetComputerNameDict(ComputerNameInfo[] computerNames) {
            this.Backups.ComputerNames = computerNames;
        }
        public static TransferJournalItem[] LoadTransferJournal() {
            string fileName = MainTransferJournalPath;
            return LoadRestoreJournalInternal(fileName);
        }

#if DEBUG
        public
#else
        private 
#endif
            static TransferJournalItem[] LoadRestoreJournalInternal(string fileName) {
            try {
                byte[] nextItemSign = Encoding.UTF8.GetBytes(TransferConst.PortableTransferJournalNextItemSign);
                byte[] nextItemSignBuffer = new byte[nextItemSign.Length];
                List<TransferJournalItem> result = new List<TransferJournalItem>();
                CommonHelper.RunInMutex(TransferConst.PortableTransferJournalMutex, delegate {
                    if (File.Exists(fileName)) {
                        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
                            using (BinaryReader br = new BinaryReader(fs)) {
                                while (br.Read(nextItemSignBuffer, 0, nextItemSign.Length) == nextItemSign.Length
                                    && CollectionHelper.BytesAreEquals(nextItemSign, nextItemSignBuffer)) {
                                    int bodyLength = br.ReadInt32();
                                    byte[] body = new byte[bodyLength];
                                    br.Read(body, 0, bodyLength);
                                    using (MemoryStream ms = new MemoryStream(body)) {
                                        using (GZipStream ds = new GZipStream(ms, CompressionMode.Decompress)) {
                                            BinaryFormatter bf = new BinaryFormatter();
                                            result.Add((TransferJournalItem)bf.Deserialize(ds));
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
                return result.ToArray();
            } catch (Exception ex) {
                TransferLog.LogException(ex);
            }
            return null;
        }
        public static bool AddRestoreJournalItem(TransferJournalItem item) {
            return AddRestoreJournalItemInternal(item, MainTransferJournalPath);
        }

#if DEBUG
        public
#else
        private 
#endif
            static bool AddRestoreJournalItemInternal(TransferJournalItem item, string fileName) {
            try {
                CommonHelper.RunInMutex(TransferConst.PortableTransferJournalMutex, delegate {
                    using (FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write)) {
                        using (BinaryWriter bw = new BinaryWriter(fs)) {
                            byte[] nextItemSign = Encoding.UTF8.GetBytes(TransferConst.PortableTransferJournalNextItemSign);
                            using (MemoryStream ms = new MemoryStream()) {
                                using (GZipStream ds = new GZipStream(ms, CompressionMode.Compress, true)) {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    bf.Serialize(ds, item);
                                    ds.Close();
                                }
                                byte[] body = ms.ToArray();
                                bw.Write(nextItemSign, 0, nextItemSign.Length);
                                bw.Write(body.Length);
                                bw.Write(body);
                            }
                        }
                    }
                });
                return true;
            } catch (Exception ex) {
                TransferLog.LogException(ex);
            }
            return false;
        }
    }

    public delegate bool TransferConfigManagerMessageHandler(string message);

    [XmlRoot("transferConfig")]
    public class TransferConfig {
        const string BackupStorageNameMaskEnd = "_*.bst";
        const string BackupStorageNameFormatEnd = "_{0:D3}.bst";
        string backupDiskLetter;
        [XmlElement("targetPath")]
        public string TargetPath { get; set; }
        [XmlElement("backupPath")]
        public string BackupPath { get; set; }
        [XmlElement("maxBackupsSizeMB")]
        public int MaxBackupsSizeMB { get; set; }
        [XmlElement("safeMode")]
        public bool SafeMode { get; set; }
        [XmlElement("encrypt")]
        public bool UseEncryption { get; set; }
        public string Name { get; set; }
        public string ConfigPath { get; set; }
        public string BackupStorageNameMask { get; private set; }
        public string BackupStorageNameFormat { get; private set; }
        public Guid BackupGuid { get; private set; }
        public TransferConfig() { }
        public TransferConfig(Guid backupGuid, string name, string targetPath, string backupPath, string maxBackupsSizeMB, bool useEncryption)
            : this(backupGuid, name, targetPath, backupPath, useEncryption) {
            int result;
            if (!Int32.TryParse(maxBackupsSizeMB, out result)) throw new InvalidOperationException("MaxBackupsSizeMB parameter has wrong value");
            MaxBackupsSizeMB = result;
        }
        public TransferConfig(Guid backupGuid, string name, string targetPath, string backupPath, int maxBackupsSizeMB, bool useEncryption)
            : this(backupGuid, name, targetPath, backupPath, useEncryption) {
            MaxBackupsSizeMB = maxBackupsSizeMB;
        }
        public TransferConfig(Guid backupGuid, string name, string targetPath, string backupPath, int maxBackupsSizeMB, bool useEncryption, bool safeMode)
            : this(backupGuid, name, targetPath, backupPath, maxBackupsSizeMB, useEncryption) {
            SafeMode = safeMode;
        }
        TransferConfig(Guid backupGuid, string name, string targetPath, string backupPath, bool useEncryption) {
            TargetPath = targetPath.TrimEnd('\\') + "\\";
            BackupPath = backupPath.TrimEnd('\\') + "\\";
            BackupGuid = backupGuid;
            UseEncryption = useEncryption;
            Name = name;
            BackupStorageNameFormat = name + BackupStorageNameFormatEnd;
            BackupStorageNameMask = name + BackupStorageNameMaskEnd;
            FixBackupPath();
        }
        void FixBackupPath() {
            backupDiskLetter = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Substring(0, 2);
            BackupPath = BackupPath.Replace("media:", backupDiskLetter).TrimEnd('\\') + "\\";
        }
        public static TransferConfig LoadConfig(string startExePath) {
            string fileConfigPath = Path.Combine(Path.GetDirectoryName(startExePath), "transfer.config");
            TransferConfig config;
            using (FileStream fs = new FileStream(fileConfigPath, FileMode.Open, FileAccess.Read)) {
                XmlSerializer xs = new XmlSerializer(typeof(TransferConfig));
                config = (TransferConfig)xs.Deserialize(fs);
                config.ConfigPath = fileConfigPath;
            }
            if (config != null) {
                config.FixBackupPath();
            }
            return config;
        }
        public override string ToString() {
            return string.Format("{0}:{1}{2}", BackupPath, TargetPath, SafeMode ? "(SafeMode)" : "");
        }
    }

    [XmlRoot("targetCollection")]
    public class LocalTargetCollection {
        Dictionary<Guid, LocalTargetInfo> infoDict = new Dictionary<Guid, LocalTargetInfo>();
        [XmlIgnore]
        public Dictionary<Guid, LocalTargetInfo> InfoDict { get { return infoDict; } }
        [XmlElement("target")]
        public LocalTargetInfo[] Items { 
            get { return CollectionHelper.EnumerableToArray(infoDict.Values); }
            set {
                infoDict = CollectionHelper.CollectionToDictionary<Guid, LocalTargetInfo>(value, t => t.Guid);
            }
        }
    }

    [XmlRoot("backupCollection")]
    public class LocalBackupCollection {
        Dictionary<Guid, LocalBackupInfo> infoDict = new Dictionary<Guid, LocalBackupInfo>();
        Dictionary<Guid, ComputerNameInfo> computerNameDict = new Dictionary<Guid, ComputerNameInfo>();
        [XmlIgnore]
        public Dictionary<Guid, LocalBackupInfo> InfoDict { get { return infoDict; } }
        [XmlIgnore]
        public Dictionary<Guid, ComputerNameInfo> ComputerNameDict { get { return computerNameDict; } }
        [XmlElement("backupConfigGuid")]
        public Guid BackupConfigGuid { get; set; }
        [XmlElement("backup", IsNullable = true)]
        public LocalBackupInfo[] Items {
            get { return CollectionHelper.EnumerableToArray(infoDict.Values); }
            set {
                infoDict = CollectionHelper.CollectionToDictionary<Guid, LocalBackupInfo>(value, t => t.Guid);
            }
        }
        [XmlElement("computerName", IsNullable = true)]
        public ComputerNameInfo[] ComputerNames {
            get { return CollectionHelper.EnumerableToArray(computerNameDict.Values); }
            set {
                computerNameDict = CollectionHelper.CollectionToDictionary<Guid, ComputerNameInfo>(value, t => t.Guid);
            }
        }
    }


    public class LocalInfo {
        [Browsable(false)]
        public LocalBackupInfo BackupInfo { get; private set; }
        [Browsable(false)]
        public LocalTargetInfo TargetInfo { get; set; }
        public LocalInfo(LocalBackupInfo backupInfo) {
            BackupInfo = backupInfo;
        }
        [Category("Main")]
        [Description("The path to the folder for backup files.")]
        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(UITypeEditor))]
        public string BackupPath { 
            get { return BackupInfo.BackupPath; } 
            set { BackupInfo.BackupPath = value; } 
        }
        [Category("Local")]
        [Description("The path to the folder with the files for backup.")]
        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(UITypeEditor))]
        public string TargetPath {
            get { return TargetInfo == null ? string.Empty : TargetInfo.TargetPath; }
            set {
                if (TargetInfo == null) {
                    TargetInfo = new LocalTargetInfo(BackupInfo.Guid, value);
                    return;
                }
                TargetInfo.TargetPath = value;
            }
        }
        [Category("Main")]
        [Description("The path to the folder with the files for backup.")]
        public bool Enabled {
            get { return BackupInfo.Enabled; }
            set { BackupInfo.Enabled = value; }
        }
        [Category("Main")]
        [Description("Use encryption for store file data.")]
        public bool UseEncryption {
            get { return BackupInfo.UseEncryption; }
            set { BackupInfo.UseEncryption = value; }
        }
        [Category("Main")]
        [Description("The name of the backup.")]
        public string Name {
            get { return BackupInfo.Name; }
            set { BackupInfo.Name = value; }
        }
        [Category("Main")]
        [Description("Local files will not be removed during the restoration.")]
        public bool SafeMode {
            get { return BackupInfo.SafeMode; }
            set { BackupInfo.SafeMode = value; }
        }
        [Category("Main")]
        [Description("The maximum size of the backup in MB.")]
        public int MaxBackupSize {
            get { return BackupInfo.MaxBackupsSizeMB; }
            set { BackupInfo.MaxBackupsSizeMB = value; }
        }
        
        public override string ToString() {
            return string.Format("{0}: \"{1}\" <-> \"{2}\" {3}", BackupInfo.Name, BackupInfo.BackupPath, TargetInfo == null ? "Empty" : TargetInfo.TargetPath , BackupInfo.SafeMode ? "(SafeMode)" : "");
        }
        public void SetListViewItemData(ListViewItem item) {
            item.Checked = Enabled;
            item.SubItems.Clear();
            item.Text = Name;
            item.SubItems.Add(BackupPath);
            item.SubItems.Add(TargetPath);
            item.Tag = this;
        }
        public TransferConfig GetTransferConfig() {
            return new TransferConfig(BackupInfo.Guid, Name, TargetPath, BackupPath, MaxBackupSize, UseEncryption, SafeMode);
        }
    }
    public class LocalTargetInfo {
        [XmlAttribute("guid")]
        public Guid Guid { get; set; }
        [XmlAttribute("targetPath")]
        public string TargetPath { get; set; }
        public LocalTargetInfo() { }
        public LocalTargetInfo(Guid guid, string targetPath) {
            Guid = guid;
            TargetPath = targetPath;
        }
    }
    public class ComputerNameInfo {
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("guid")]
        public Guid Guid { get; set; }
        public ComputerNameInfo() { }
        public ComputerNameInfo(string name, Guid guid) {
            Name = name;
            Guid = guid;
        }

    }
    public class LocalBackupInfo {
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("guid")]
        public Guid Guid { get; set; }
        [XmlElement("enabled")]
        public bool Enabled { get; set; }
        [XmlElement("backupPath")]
        public string BackupPath { get; set; }
        [XmlElement("maxBackupsSizeMB")]
        public int MaxBackupsSizeMB { get; set; }
        [XmlElement("safeMode")]
        public bool SafeMode { get; set; }
        [XmlElement("encrypt")]
        public bool UseEncryption { get; set; }
        public LocalBackupInfo() { }
        public LocalBackupInfo(Guid guid) {
            Guid = guid;
        }
    }

    public enum TransferJournalItemType {
        Backup,
        Restore
    }

    [Serializable]
    public class TransferJournalItem {
        public TransferJournalItemType Type { get; set; }
        public DateTime ActionTime { get; set; }
        public Guid BackupId { get; set; }
        public Guid ComputerId { get; set; }
        public int BackupVersion { get; set; }
        public TransferJournalItem(DateTime actionTime, Guid backupId, int backupVersion, TransferJournalItemType type) {
            ActionTime = actionTime;
            BackupId = backupId;
            BackupVersion = backupVersion;
            Type = type;
            ComputerId = TransferConfigManager.GetComputerGuid();
        }
    }
}
