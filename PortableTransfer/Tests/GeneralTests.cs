#if DEBUGTEST
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;
using PortableTransfer.Helpers;
using System.Xml.Serialization;
using System.Threading;

namespace PortableTransfer.Tests {
    [TestFixture]
    public class GeneralTests {
        const string FilesForTestOriginal = @"d:\Temp\FilesForTest\Original";
        const string FilesForTestTarget = @"d:\Temp\FilesForTest\Target";
        const string FilesForTestBackup = @"d:\Temp\FilesForTest\Backup";

        [Test]
        public void CompressDecompress() {
            string[] files = Directory.GetFiles(FilesForTestOriginal);
            foreach (string file in files) {
                byte[] data = File.ReadAllBytes(file);
                byte[] compressedData = TransferMain.CompressData(data);
                byte[] decompressedData = TransferMain.DecompressData(compressedData);
                Assert.IsTrue(CollectionHelper.BytesAreEquals(data, decompressedData));
                Console.WriteLine(string.Format("{0}\t\t\tOK", file));
            }
        }
        [Test]
        public void Gd() {
            LocalBackupInfo lbi = new LocalBackupInfo { Guid = Guid.NewGuid(), 
                                                          SafeMode = false, 
                                                          BackupPath = "sddfdf", 
                                                          MaxBackupsSizeMB = 2323 };
            XmlSerializer xs = new XmlSerializer(typeof(LocalBackupInfo));
            using (FileStream fs = new FileStream("c:\\t.xml", FileMode.Create, FileAccess.Write)) {
                xs.Serialize(fs, lbi);
            }
        }
        [Test]
        public void BackupAndRestore() {
            //ClearDirectory(FilesForTestBackup);
            //TransferMain tm = new TransferMain(new TransferConfig(Guid.Empty, "backup", FilesForTestOriginal, FilesForTestBackup, 1024, false));
            //tm.DoBackup(new DoBackupArgs(null, null, null, null, null));
            //TransferBackupVersionInfo[] vsBS = tm.GetBackupStorageVersions();
            //TransferBackupVersionInfo[] vsBL = tm.GetTransferListVersions();
            //Assert.AreEqual(vsBS.Length, vsBL.Length);
            //ClearDirectory(FilesForTestTarget);
            //new TransferMain(new TransferConfig(Guid.Empty, "backup", FilesForTestTarget, FilesForTestBackup, 1024, false)).DoRestore(new DoRestoreArgs(null, null, null));
            //string[] filesOriginal = Directory.GetFiles(FilesForTestOriginal, "*", SearchOption.AllDirectories);
            //foreach (string fileOriginal in filesOriginal) {
            //    string fileTarget = fileOriginal.Replace(FilesForTestOriginal, FilesForTestTarget);
            //    Assert.IsTrue(CollectionHelper.BytesAreEquals(File.ReadAllBytes(fileOriginal), File.ReadAllBytes(fileTarget)));
            //}
            //string[] directoriesOriginal = Directory.GetDirectories(FilesForTestOriginal, "*", SearchOption.AllDirectories);
            //foreach (string directoryOriginal in directoriesOriginal) {
            //    string directoryTarget = directoryOriginal.Replace(FilesForTestOriginal, FilesForTestTarget);
            //    Assert.IsTrue(Directory.Exists(directoryTarget));
            //}
            //ClearNotAllDirectory(FilesForTestTarget);
            //new TransferMain(new TransferConfig(Guid.Empty, "backup", FilesForTestTarget, FilesForTestBackup, 1024, false)).DoRestore(new DoRestoreArgs(null, null, null));
            //filesOriginal = Directory.GetFiles(FilesForTestOriginal, "*", SearchOption.AllDirectories);
            //foreach (string fileOriginal in filesOriginal) {
            //    string fileTarget = fileOriginal.Replace(FilesForTestOriginal, FilesForTestTarget);
            //    Assert.IsTrue(CollectionHelper.BytesAreEquals(File.ReadAllBytes(fileOriginal), File.ReadAllBytes(fileTarget)));
            //}
            //directoriesOriginal = Directory.GetDirectories(FilesForTestOriginal, "*", SearchOption.AllDirectories);
            //foreach (string directoryOriginal in directoriesOriginal) {
            //    string directoryTarget = directoryOriginal.Replace(FilesForTestOriginal, FilesForTestTarget);
            //    Assert.IsTrue(Directory.Exists(directoryTarget));
            //}
        }
        [Test]
        public void ComperssDecompressThunderbird() {
            byte[] testData = CommonHelper.ReadAllBytes(@"d:\portable2\ThunderbirdPortable\ThunderbirdPortable.exe");
            byte[] compressedData = CompressHelper.TryToCompressData(testData);
            byte[] decompressData = CompressHelper.TryToDecompressData(compressedData);
            Assert.IsTrue(CollectionHelper.BytesAreEquals(testData, decompressData));
        }

        [Test]
        public void RestoreJournalTest() {
            int threadCounter = 0;
            List<int> countList = new List<int>();
            using (ManualResetEvent mre = new ManualResetEvent(false)) {
                for (int i = 0; i < 1; i++) {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(o => {
                        threadCounter++;
                        mre.WaitOne();
                        string fileName = Path.GetTempFileName();
                        try {
                            TransferConfigManager.AddRestoreJournalItemInternal(new TransferJournalItem(DateTime.Now, Guid.NewGuid(), 0, TransferJournalItemType.Backup), fileName);
                            TransferConfigManager.AddRestoreJournalItemInternal(new TransferJournalItem(DateTime.Now, Guid.NewGuid(), 1, TransferJournalItemType.Backup), fileName);
                            TransferConfigManager.AddRestoreJournalItemInternal(new TransferJournalItem(DateTime.Now, Guid.NewGuid(), 2, TransferJournalItemType.Backup), fileName);
                            TransferConfigManager.AddRestoreJournalItemInternal(new TransferJournalItem(DateTime.Now, Guid.NewGuid(), 3, TransferJournalItemType.Backup), fileName);
                            TransferConfigManager.AddRestoreJournalItemInternal(new TransferJournalItem(DateTime.Now, Guid.NewGuid(), 4, TransferJournalItemType.Backup), fileName);
                            TransferJournalItem[] items = TransferConfigManager.LoadRestoreJournalInternal(fileName);
                            lock (countList) {
                                countList.Add(items.Length);
                            }
                        } finally {
                            try {
                                if (File.Exists(fileName)) {
                                    File.Delete(fileName);
                                }
                            } catch (Exception) { }
                            threadCounter--;
                        }
                    }));
                }
                while (threadCounter < 1) {
                    Thread.Sleep(100);
                }
                mre.Set();
                while (threadCounter > 0) {
                    Thread.Sleep(100);
                }
                for (int i = 0; i < countList.Count; i++) {
                    Assert.AreEqual(5, countList[i]);
                }
            }
        }
        List<Guid> getComputerGuidTestList = new List<Guid>();

        [Test]
        public void GetComputerGuidTest() {
            try {
                int threadCounter = 0;
                getComputerGuidTestList.Clear();
                using (ManualResetEvent mre = new ManualResetEvent(false)) {
                    for (int i = 0; i < 10; i++) {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(o => {
                            threadCounter++;
                            mre.WaitOne();
                            try {
                                Guid cg = TransferConfigManager.GetComputerGuid();
                                lock (getComputerGuidTestList) {
                                    getComputerGuidTestList.Add(cg);
                                }
                            } finally {
                                threadCounter--;
                            }
                        }));
                    }
                    while (threadCounter < 10) {
                        Thread.Sleep(100);
                    }
                    mre.Set();
                    while (threadCounter > 0) {
                        Thread.Sleep(100);
                    }
                }
                for (int i = 1; i < getComputerGuidTestList.Count; i++) {
                    Assert.AreEqual(getComputerGuidTestList[i - 1], getComputerGuidTestList[i]);
                }
            } finally {
                try {
                    if (File.Exists(TransferConfigManager.ComputerGuidFileName)) {
                        File.Delete(TransferConfigManager.ComputerGuidFileName);
                    }
                } catch (Exception ex) { }
            }
        }
        [Test]
        public void BackupListForm() {
            List<LocalInfo> list = new List<LocalInfo>();
            list.Add(new LocalInfo(new LocalBackupInfo(Guid.NewGuid())));
            list.Add(new LocalInfo(new LocalBackupInfo(Guid.NewGuid())));
            list.Add(new LocalInfo(new LocalBackupInfo(Guid.NewGuid())));
            list[0].BackupInfo.Name = "Name1";
            list[0].BackupInfo.BackupPath = "media:\\Backup1\\";
            list[0].TargetInfo = new LocalTargetInfo(Guid.NewGuid(), "d:\\Portable\\");
            list[1].BackupInfo.Enabled = true;
            list[1].BackupInfo.Name = "Name2";
            list[1].BackupInfo.BackupPath = "media:\\Backup2\\";
            list[1].TargetInfo = new LocalTargetInfo(Guid.NewGuid(), "d:\\Portable2\\");
            list[2].BackupInfo.Name = "Name3";
            list[2].BackupInfo.BackupPath = "media:\\Backup3\\";
            list[2].TargetInfo = new LocalTargetInfo(Guid.NewGuid(), "d:\\Portable3\\");
            using (FormBackupList fbl = new FormBackupList(list)) {
                fbl.Show();
                fbl.Close();
            }
        }

        static void ClearDirectory(string dir) {
            if (Directory.Exists(dir)) {
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                foreach (string file in files) {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                string[] directories = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);
                foreach (string directory in directories) {
                    if(Directory.Exists(directory))Directory.Delete(directory, true);
                }
            }
        }
        static void ClearNotAllDirectory(string dir) {
            if (Directory.Exists(dir)) {
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                int i = 0;
                foreach (string file in files) {
                    if ((i % 3) != 0) {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    i++;
                }
                string[] directories = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories);
                if (directories.Length > 0) {
                    Directory.Delete(directories[directories.Length - 1]);
                }
            }
        }

        //[Test]
        //public void MessageBox1() {
        //    Assert.AreEqual(FormMessageBoxResult.NoAll, FormMessageBox.Show(null, "232323asdkfklerhwer\nk krjk wnel;rkwek\n werweporu pwenrpo po23", "232323"));
        //}

    }
}
#endif