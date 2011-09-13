namespace PortableTransfer {
    partial class FormMessageBox {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.btYes = new System.Windows.Forms.Button();
            this.btYesAll = new System.Windows.Forms.Button();
            this.btNo = new System.Windows.Forms.Button();
            this.btNoAll = new System.Windows.Forms.Button();
            this.lbMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btYes
            // 
            this.btYes.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btYes.Location = new System.Drawing.Point(12, 82);
            this.btYes.Name = "btYes";
            this.btYes.Size = new System.Drawing.Size(75, 23);
            this.btYes.TabIndex = 0;
            this.btYes.Text = "Yes";
            this.btYes.UseVisualStyleBackColor = true;
            this.btYes.Click += new System.EventHandler(this.btYes_Click);
            // 
            // btYesAll
            // 
            this.btYesAll.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btYesAll.Location = new System.Drawing.Point(93, 82);
            this.btYesAll.Name = "btYesAll";
            this.btYesAll.Size = new System.Drawing.Size(75, 23);
            this.btYesAll.TabIndex = 1;
            this.btYesAll.Text = "Yes All";
            this.btYesAll.UseVisualStyleBackColor = true;
            this.btYesAll.Click += new System.EventHandler(this.btYesAll_Click);
            // 
            // btNo
            // 
            this.btNo.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btNo.Location = new System.Drawing.Point(174, 82);
            this.btNo.Name = "btNo";
            this.btNo.Size = new System.Drawing.Size(75, 23);
            this.btNo.TabIndex = 2;
            this.btNo.Text = "No";
            this.btNo.UseVisualStyleBackColor = true;
            this.btNo.Click += new System.EventHandler(this.btNo_Click);
            // 
            // btNoAll
            // 
            this.btNoAll.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btNoAll.Location = new System.Drawing.Point(254, 82);
            this.btNoAll.Name = "btNoAll";
            this.btNoAll.Size = new System.Drawing.Size(75, 23);
            this.btNoAll.TabIndex = 3;
            this.btNoAll.Text = "No All";
            this.btNoAll.UseVisualStyleBackColor = true;
            this.btNoAll.Click += new System.EventHandler(this.btNoAll_Click);
            // 
            // lbMessage
            // 
            this.lbMessage.AutoSize = true;
            this.lbMessage.Location = new System.Drawing.Point(12, 9);
            this.lbMessage.Name = "lbMessage";
            this.lbMessage.Size = new System.Drawing.Size(35, 13);
            this.lbMessage.TabIndex = 4;
            this.lbMessage.Text = "label1";
            this.lbMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FormMessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(343, 117);
            this.Controls.Add(this.lbMessage);
            this.Controls.Add(this.btNoAll);
            this.Controls.Add(this.btNo);
            this.Controls.Add(this.btYesAll);
            this.Controls.Add(this.btYes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormMessageBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormMessageBox";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btYes;
        private System.Windows.Forms.Button btYesAll;
        private System.Windows.Forms.Button btNo;
        private System.Windows.Forms.Button btNoAll;
        private System.Windows.Forms.Label lbMessage;
    }
}