using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PortableTransfer {
    public partial class FormJournal : Form {
        public FormJournal(TransferJournalItem[] source) {
            InitializeComponent();
            transferJournalItemBindingSource.DataSource = source;
        }
    }
}
