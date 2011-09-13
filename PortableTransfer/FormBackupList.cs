using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace PortableTransfer {
    public partial class FormBackupList : Form {
        List<LocalInfo> result;
        Dictionary<LocalInfo, ListViewItem> infoDictionary = new Dictionary<LocalInfo, ListViewItem>();

        public List<LocalInfo> Result { get { return result; } set { result = value; } }
        public FormBackupList(List<LocalInfo> infoList) {
            InitializeComponent();
            ListViewItem firstItem = null;
            for (int i = 0; i < infoList.Count; i++) {
                LocalInfo lbInfo = infoList[i];
                ListViewItem lvItem = AddListViewItem(lbInfo);
                firstItem = lvItem;
            }
            if (firstItem != null) {
                firstItem.Selected = true;
                firstItem.Focused = true;
            }
            lvBackupList.Columns[0].Width = lvBackupList.Width * 19 / 100;
            lvBackupList.Columns[1].Width = lvBackupList.Width * 40 / 100;
            lvBackupList.Columns[2].Width = lvBackupList.Width * 40 / 100;           
        }

        ListViewItem AddListViewItem(LocalInfo lbInfo) {
            ListViewItem lvItem = new ListViewItem();
            lbInfo.SetListViewItemData(lvItem);
            lvBackupList.Items.Add(lvItem);
            infoDictionary.Add(lbInfo, lvItem);
            return lvItem;
        }

        private void lvBackupList_SelectedIndexChanged(object sender, EventArgs e) {
            foreach (int index in lvBackupList.SelectedIndices) {
                pgBackup.SelectedObject = lvBackupList.Items[index].Tag;
            }
        }

        private void lvBackupList_ItemChecked(object sender, ItemCheckedEventArgs e) {
            if (e.Item == null) return;
            ((LocalInfo)e.Item.Tag).Enabled = e.Item.Checked;
            pgBackup.SelectedObject = e.Item.Tag;
        }

        private void pgBackup_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
            if(pgBackup.SelectedObject as LocalInfo == null) return;
            LocalInfo localInfo = (LocalInfo)pgBackup.SelectedObject;
            ListViewItem lvItem = infoDictionary[localInfo];
            localInfo.SetListViewItemData(lvItem);
        }

        private void btAdd_Click(object sender, EventArgs e) {
            LocalBackupInfo lbi = new LocalBackupInfo(Guid.NewGuid());
            LocalInfo li = new LocalInfo(lbi);
            li.Name = "New Backup";
            ListViewItem lvItem = AddListViewItem(li);
            lvItem.Focused = true;
            lvItem.Selected = true;
        }

        private void btDelete_Click(object sender, EventArgs e) {
            if (lvBackupList.SelectedIndices.Count > 0 && MessageBox.Show(this, "Are you sure to delete selected backup from list.", "Portable Transfer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                ArrayList selectedItems = new ArrayList(lvBackupList.SelectedItems);
                foreach(ListViewItem lvItem in selectedItems){
                    LocalInfo lInfo = (LocalInfo)lvItem.Tag;
                    infoDictionary.Remove(lInfo);
                    lvBackupList.Items.Remove(lvItem);
                }
                if (lvBackupList.Items.Count > 0) {
                    lvBackupList.Items[0].Focused = true;
                    lvBackupList.Items[0].Selected = true;
                    pgBackup.SelectedObject = (LocalInfo)lvBackupList.Items[0].Tag;
                } else {
                    pgBackup.SelectedObject = null;
                }
            }
        }

        private void btSave_Click(object sender, EventArgs e) {
            List<LocalInfo> resultList = new List<LocalInfo>(lvBackupList.Items.Count);
            //TODO Check parameters
            foreach (ListViewItem lvItem in lvBackupList.Items) {
                resultList.Add((LocalInfo)lvItem.Tag);
            }
            result = resultList;
            DialogResult = DialogResult.OK;
        }

    }
}
