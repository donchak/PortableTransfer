using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PortableTransfer {
    public partial class FormPassword : Form {
        bool isNew = true;
        public string Password {
            get { return !isNew || (tbPassword.Text == tbRepeat.Text) ? tbPassword.Text : null; }
        }
        
        public FormPassword() {
            InitializeComponent();
        }
        public FormPassword(string backupName, bool isNew)
            : this() {
            tbName.Text = backupName;
            this.isNew = isNew;
            if (!isNew) {
                tbRepeat.Visible = false;
                label2.Visible = false;
                Height = Height - (tbRepeat.Top - tbPassword.Top);
            }
        }

        private void btOK_Click(object sender, EventArgs e) {
            if (!isNew || tbPassword.Text == tbRepeat.Text) {
                this.DialogResult = DialogResult.OK;
                return;
            }
            MessageBox.Show(this, "Passwords are not equals!", "PortableTransfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool GetPassword(IWin32Window owner, string backupName, bool isNew, out string password){
            password = string.Empty;
            using (FormPassword fp = new FormPassword(backupName, isNew)) {
                if (fp.ShowDialog(owner) != DialogResult.OK) return false;
                if (fp.Password == null) return false;
                password = fp.Password;
                return true;
            }
        }
    }
}
