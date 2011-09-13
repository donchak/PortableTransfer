using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PortableTransfer {
    public class DWMComboBox : ComboBox {
        public DWMComboBox() {
            this.DropDownStyle = ComboBoxStyle.DropDownList;
        }
        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            switch ((WindowsMessage)m.Msg) {
                case WindowsMessage.WM_PAINT:
                case WindowsMessage.WM_CHAR:
                case WindowsMessage.WM_KEYDOWN:
                case WindowsMessage.WM_MOUSEMOVE:
                case WindowsMessage.WM_PRINT:
                    RedrawControlAsBitmap(this.Handle);
                    break;
                default:
                    break;
            }
        }

        static void RedrawControlAsBitmap(IntPtr handle) {
            Control c = Control.FromHandle(handle);
            if (c != null)
                using (Bitmap bm = new Bitmap(c.Width, c.Height)) {
                    c.DrawToBitmap(bm, c.ClientRectangle);
                    using (Graphics g = c.CreateGraphics()) {
                        g.DrawImage(bm, new Point(0, 0));
                    }
                }
            c = null;
        }
    }

    public enum WindowsMessage{
        WM_CHAR = 0x100,
        WM_KEYDOWN = 0x102,
        WM_MOUSEMOVE = 0x200,
        WM_PAINT = 15,
        WM_PRINT = 0x314
    }
}
