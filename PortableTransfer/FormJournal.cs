using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PortableTransfer.Helpers;

namespace PortableTransfer {
    public partial class FormJournal : Form {
        Dictionary<Guid, ComputerNameInfo> computerNameDict;
        public FormJournal(TransferJournalItem[] source, ComputerNameInfo[] computerNames) {
            InitializeComponent();
            computerNameDict = CollectionHelper.CollectionToDictionary<Guid, ComputerNameInfo>(computerNames, k => { return k.Guid; });
            transferJournalItemBindingSource.DataSource = source;
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e) {
            ComputerNameInfo name;
            if (e.ColumnIndex == 3 && e.Value is Guid && computerNameDict.TryGetValue((Guid)e.Value, out name)) {
                e.Value = name.Name;
                e.FormattingApplied = true;
            }
        }

    }
}
