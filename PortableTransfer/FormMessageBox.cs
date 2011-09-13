using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PortableTransfer {
    public partial class FormMessageBox : Form {
        FormMessageBoxResult result = FormMessageBoxResult.No;
        public FormMessageBoxResult Result {
            get { return result; }
        }
        FormMessageBox() {
            InitializeComponent();
        }
        public static FormMessageBoxResult Show(IWin32Window owner, string text, string caption) {
            using (FormMessageBox form = new FormMessageBox()) {
                form.Text = caption;
                form.lbMessage.Text = text;
                if (form.lbMessage.Width > form.Width) {
                    form.Width = form.lbMessage.Width + 24;
                }
                form.lbMessage.Left = (form.Width - form.lbMessage.Width) / 2;
                int top = form.lbMessage.Top + form.lbMessage.Height + 12;
                int shift = top - form.btYes.Top;
                form.Height = form.Height + shift;
                form.ShowDialog(owner);
                return form.result;
            }
        }

        private void btYes_Click(object sender, EventArgs e) {
            result = FormMessageBoxResult.Yes;
            Close();
        }
        private void btYesAll_Click(object sender, EventArgs e) {
            result = FormMessageBoxResult.YesAll;
            Close();
        }
        private void btNo_Click(object sender, EventArgs e) {
            result = FormMessageBoxResult.No;
            Close();
        }
        private void btNoAll_Click(object sender, EventArgs e) {
            result = FormMessageBoxResult.NoAll;
            Close();
        }
    }
    public enum FormMessageBoxResult {
        Yes,
        YesAll,
        No,
        NoAll
    }
}
