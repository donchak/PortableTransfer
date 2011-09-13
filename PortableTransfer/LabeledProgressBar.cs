using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PortableTransfer {
    public class LabeledProgressBar : ProgressBar {
        [DefaultValue(true)]
        [Bindable(true)]
        public bool ShowLabel { get; set; }
        [DefaultValue("Text")]
        [Bindable(true)]
        public string LabelText { get; set; }
        public LabeledProgressBar() { }
        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            switch ((WindowsMessage)m.Msg) {
                case WindowsMessage.WM_PAINT:
                    DrawLabelText();
                    break;
                default:
                    break;
            }
        }

        void DrawLabelText() {
            using (Graphics gs = CreateGraphics()) {
                SizeF size = gs.MeasureString(LabelText, Parent.Font);
                gs.DrawString(LabelText, Parent.Font, Brushes.Black, new PointF((ClientRectangle.Width - size.Width) / 2, (ClientRectangle.Height - size.Height) / 2));
            }
        }
        //protected override void OnPaint(PaintEventArgs pe) {
        //    base.OnPaint(pe);
        //    if (ShowLabel) {
        //        pe.Graphics.DrawString(Text, Parent.Font, Brushes.Black, pe.ClipRectangle);
        //    }
        //}
    }
}
