using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using VistaControls.Dwm;
using VistaControls;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;


namespace PortableTransfer {
    public partial class FormMain : Form {
        bool tracking;
        Point lastPos;
        bool glassMode;
        const int FormMainHeightDiff = 214 - 175;
        readonly int FormMainHeight;
        TransferConfigManager configManager = new TransferConfigManager();
        List<LocalInfo> localInfoList;
        public FormMain() {
            InitializeComponent();
            configManager.LoadData();
            localInfoList = configManager.GetLocalInfoList();
            if (OsSupport.IsVistaOrBetter && OsSupport.IsCompositionEnabled) {
                glassMode = true;
                BackColor = Color.Black;
                DwmManager.EnableGlassSheet(this);
            }
            FormMainHeight = Height;
            UpdateComboBox();
        }
        void UpdateComboBox() {
            object selectedItem = cbBackups.SelectedItem;
            cbBackups.Items.Clear();
            if (localInfoList != null) {
                cbBackups.Items.Add("All enabled backups");
                for (int i = 0; i < localInfoList.Count; i++) {
                    cbBackups.Items.Add(localInfoList[i]);
                }
            }
            if (cbBackups.Items.Count == 0) return;
            if (selectedItem == null || selectedItem is string) {
                cbBackups.SelectedIndex = 0;
                return;
            }
            LocalInfo lInfo = selectedItem as LocalInfo;
            if (lInfo == null) return;
            for (int i = 1; i < cbBackups.Items.Count; i++) {
                if (((LocalInfo)cbBackups.Items[i]).BackupInfo.Guid == lInfo.BackupInfo.Guid) {
                    cbBackups.SelectedIndex = i;
                    return;
                }
            }
        }
        void ActiveControls() {
            backupButton.Enabled = true;
            restoreButton.Enabled = true;
            cbBackups.Enabled = true;
            settingsButton.Enabled = true;
        }
        void BlockControls() {
            backupButton.Enabled = false;
            restoreButton.Enabled = false;
            cbBackups.Enabled = false;
            settingsButton.Enabled = false;
        }

        delegate void InvokeHandler();

        bool TransferConfigManagerMessage(string message) {
            bool result = false;
            this.Invoke(new InvokeHandler(delegate(){
                result = MessageBox.Show(this, message, "Portable Transfer", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Yes;
            }));
            return result;
        }
        private void backupButton_Click(object sender, EventArgs e) {
            if (cbBackups.SelectedIndex < 0) return;
            if (MessageBox.Show(this, "Do you realy want to start backup?", "PortableTransfer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (cbBackups.SelectedIndex > 0) {
                TransferConfig tConfig = ((LocalInfo)cbBackups.Items[cbBackups.SelectedIndex]).GetTransferConfig();
                if (TransferConfigManager.CheckNextTransferOperationSafety(tConfig.BackupGuid, tConfig.Name, 0, TransferJournalItemType.Backup, TransferConfigManagerMessage)) {
                    BlockControls();
                    TransferMain tm = new TransferMain(tConfig);
                    ThreadPool.QueueUserWorkItem(o => {
                        Thread.CurrentThread.IsBackground = true;
                        TransferJournalItem result = tm.DoBackup(new DoBackupArgs(SetMax, SetPosition, EndOperation, ShowDialogHandler, ShowMessageBoxHandler, GetPasswordHandler));
                        if (result == null) return;
                        TransferConfigManager.AddRestoreJournalItem(result);
                    });
                }
                return;
            }
            List<TransferConfig> transferList = new List<TransferConfig>();
            List<int> transferIndexList = new List<int>();
            for (int i = 1; i < cbBackups.Items.Count; i++) {
                LocalInfo lInfo = (LocalInfo)cbBackups.Items[i];
                if (!lInfo.Enabled) continue;
                TransferConfig tConfig = lInfo.GetTransferConfig();
                if (TransferConfigManager.CheckNextTransferOperationSafety(tConfig.BackupGuid, tConfig.Name, 0, TransferJournalItemType.Backup, TransferConfigManagerMessage)) {
                    transferList.Add(tConfig);
                    transferIndexList.Add(i);
                }
            }
            if (transferList.Count == 0) return;
            BlockControls();
            ThreadPool.QueueUserWorkItem(o => {
                Thread.CurrentThread.IsBackground = true;
                Exception exception = null;
                for (int i = 0; i < transferList.Count; i++) {
                    cbBackups.Invoke(new InvokeHandler(delegate {
                        SetProgressBackupName(transferList[i].Name);
                        cbBackups.SelectedIndex = transferIndexList[i];
                    }));
                    TransferMain tm = new TransferMain(transferList[i]);
                    TransferJournalItem result = tm.DoBackup(new DoBackupArgs(SetMax, SetPosition, ex => exception = ex, ShowDialogHandler, ShowMessageBoxHandler, GetPasswordHandler));
                    if (exception != null)
                        break;
                    if (result == null) continue;
                    TransferConfigManager.AddRestoreJournalItem(result);
                }
                cbBackups.Invoke(new InvokeHandler(delegate {
                    cbBackups.SelectedIndex = 0;
                }));
                EndOperation(exception);
            });
        }
        private void restoreButton_Click(object sender, EventArgs e) {
            if (cbBackups.SelectedIndex < 0) return;
            if (MessageBox.Show(this, "Do you realy want to start restore?", "PortableTransfer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (cbBackups.SelectedIndex > 0) {
                TransferMain tm = new TransferMain(((LocalInfo)cbBackups.Items[cbBackups.SelectedIndex]).GetTransferConfig());
                int lastBackupNum = tm.GetLastBackupStorageNum();
                if (TransferConfigManager.CheckNextTransferOperationSafety(tm.Config.BackupGuid, tm.Config.Name, lastBackupNum, TransferJournalItemType.Restore, TransferConfigManagerMessage)) {
                    BlockControls();                    
                    ThreadPool.QueueUserWorkItem(o => {
                        Thread.CurrentThread.IsBackground = true;
                        TransferJournalItem result = tm.DoRestore(new DoRestoreArgs(SetMax, SetPosition, EndOperation, ShowDialogHandler, ShowMessageBoxHandler, GetPasswordHandler, lastBackupNum));
                        if (result == null) return;
                        TransferConfigManager.AddRestoreJournalItem(result);
                    });
                }
                return;
            }
            List<TransferMain> transferList = new List<TransferMain>();
            List<int> transferIndexList = new List<int>();
            List<int> transferVersionList = new List<int>();
            for (int i = 1; i < cbBackups.Items.Count; i++) {
                LocalInfo lInfo = (LocalInfo)cbBackups.Items[i];
                if (!lInfo.Enabled) continue;
                TransferMain tm = new TransferMain(lInfo.GetTransferConfig());
                int lastBackupNum = tm.GetLastBackupStorageNum();
                if (TransferConfigManager.CheckNextTransferOperationSafety(tm.Config.BackupGuid, tm.Config.Name, lastBackupNum, TransferJournalItemType.Restore, TransferConfigManagerMessage)) {
                    transferList.Add(tm);
                    transferIndexList.Add(i);
                    transferVersionList.Add(lastBackupNum);
                }
            }
            if (transferList.Count == 0) return;
            BlockControls();
            ThreadPool.QueueUserWorkItem(o => {
                Thread.CurrentThread.IsBackground = true;
                Exception exception = null;
                for (int i = 0; i < transferList.Count; i++) {
                    cbBackups.Invoke(new InvokeHandler(delegate {
                        SetProgressBackupName(transferList[i].Config.Name);
                        cbBackups.SelectedIndex = transferIndexList[i];
                    }));
                    TransferMain tm = transferList[i];
                    TransferJournalItem result = tm.DoRestore(new DoRestoreArgs(SetMax, SetPosition, ex => exception = ex, ShowDialogHandler, ShowMessageBoxHandler, GetPasswordHandler, transferVersionList[i]));
                    if (exception != null)
                        break;
                    if (result == null) continue;
                    TransferConfigManager.AddRestoreJournalItem(result);
                }
                cbBackups.Invoke(new InvokeHandler(delegate {
                    cbBackups.SelectedIndex = 0;
                }));
                EndOperation(exception);
            });
        }
        void SetMax(int max) {
            Invoke(new BackupProgressHandler(value => {
                pbProgress.Value = 0;
                pbProgress.Minimum = 0;
                pbProgress.Maximum = value;
                pbProgress.Visible = true;
                Height = FormMainHeight + FormMainHeightDiff;
            }), max);
        }
        void SetPosition(int pos) {
            Invoke(new BackupProgressHandler(value => {
                pbProgress.Value = value;
            }), pos);
        }
        void EndOperation(Exception ex) {
            Invoke(new BackupEndHandler(e => {
                if (e != null)
                    MessageBox.Show(this, e.ToString(), "Portable Transfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this, "Operation complite successful.", "Portable Transfer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pbProgress.Value = 0;
                pbProgress.Minimum = 0;
                pbProgress.Visible = false;
                ActiveControls();
                Height = FormMainHeight;
            }), ex);
        }

        FormMessageBoxResult ShowMessageBoxHandler(string text, string caption) {
            return (FormMessageBoxResult)Invoke(new BackupFormMessageBox(delegate(string text1, string caption1) {
                return FormMessageBox.Show(this, text1, caption1);
            }), text, caption);
        }
        DialogResult ShowDialogHandler(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) {
            return (DialogResult)Invoke(new BackupShowDialog(delegate(string text1, string caption1, MessageBoxButtons buttons1, MessageBoxIcon icon1) {
                return MessageBox.Show(this, text1, caption1, buttons1, icon1);
            }), text, caption, buttons, icon);
        }

        string GetPasswordHandler(string backupName, bool isNew) {
            return (string)Invoke(new BackupGetPassword(delegate(string bName, bool isN) {
                string password;
                if (FormPassword.GetPassword(this, bName, isN, out password)) {
                    return "pAr23orFDGSBIgfdfo90" + password + "43o543t_+_rEw3w54hg" + password + "oshgoDSdsFgwoeithieyTew454tyu3";
                }
                throw new InvalidOperationException("Password not entered.");
            }), backupName, isNew);
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left && glassMode) {
                tracking = true;
                lastPos = base.PointToScreen(e.Location);
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            if (tracking) {
                Point point = base.PointToScreen(e.Location);
                Point p = new Point(point.X - lastPos.X, point.Y - lastPos.Y);
                Point location = base.Location;
                location.Offset(p);
                base.Location = location;
                lastPos = point;
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left && glassMode) {
                tracking = false;
            }
            base.OnMouseUp(e);
        }
        bool needSaveMainData;
        private void backupListButton_Click(object sender, EventArgs e) {
            cmsSettings.Show(settingsButton, new Point(0, settingsButton.Height));
        }
        private void FormMain_FormClosed(object sender, FormClosedEventArgs e) {
            SaveDataIfNeed();
        }
        private void saveTimer_Tick(object sender, EventArgs e) {
            SaveDataIfNeed();
        }
        void SaveDataIfNeed() {
            if (needSaveMainData) {
                configManager.SaveMainData();
                needSaveMainData = false;
            }
        }

        void SetProgressBackupName(string text) {
            pbProgress.ShowLabel = true;
            pbProgress.LabelText = string.Format("Processing: {0}", text);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            using (FormBackupList fbl = new FormBackupList(localInfoList, configManager.Backups.ComputerNames)) {
                if (fbl.ShowDialog(this) == DialogResult.OK) {
                    localInfoList = fbl.Result;
                    configManager.SetComputerNameDict(fbl.ComputerNamesResult);
                    configManager.SetLocalInfoList(localInfoList);
                    configManager.SaveUserData();
                    needSaveMainData = true;
                    UpdateComboBox();
                }
            }
        }

        private void journalToolStripMenuItem_Click(object sender, EventArgs e) {
            using (FormJournal fj = new FormJournal(TransferConfigManager.LoadTransferJournal(), configManager.Backups.ComputerNames)) {
                fj.ShowDialog(this);
            }
        }

        private void showLogToolStripMenuItem_Click(object sender, EventArgs e) {
            if (File.Exists(TransferLog.MainLogPath)) {
                Process.Start(TransferLog.MainLogPath);
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e) {
            if (File.Exists(TransferLog.MainLogPath)) {
                File.Delete(TransferLog.MainLogPath);
            }
        }

    }
}
