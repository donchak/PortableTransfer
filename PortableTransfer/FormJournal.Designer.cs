namespace PortableTransfer {
    partial class FormJournal {
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
            this.components = new System.ComponentModel.Container();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.transferJournalItemBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.typeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.actionTimeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.backupIdDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.computerIdDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.backupVersionDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btOk = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.transferJournalItemBindingSource)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.typeDataGridViewTextBoxColumn,
            this.actionTimeDataGridViewTextBoxColumn,
            this.backupIdDataGridViewTextBoxColumn,
            this.computerIdDataGridViewTextBoxColumn,
            this.backupVersionDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.transferJournalItemBindingSource;
            this.dataGridView1.Location = new System.Drawing.Point(12, 12);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(660, 321);
            this.dataGridView1.TabIndex = 0;
            // 
            // transferJournalItemBindingSource
            // 
            this.transferJournalItemBindingSource.DataSource = typeof(PortableTransfer.TransferJournalItem);
            // 
            // typeDataGridViewTextBoxColumn
            // 
            this.typeDataGridViewTextBoxColumn.DataPropertyName = "Type";
            this.typeDataGridViewTextBoxColumn.HeaderText = "Type";
            this.typeDataGridViewTextBoxColumn.Name = "typeDataGridViewTextBoxColumn";
            this.typeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // actionTimeDataGridViewTextBoxColumn
            // 
            this.actionTimeDataGridViewTextBoxColumn.DataPropertyName = "ActionTime";
            this.actionTimeDataGridViewTextBoxColumn.HeaderText = "ActionTime";
            this.actionTimeDataGridViewTextBoxColumn.Name = "actionTimeDataGridViewTextBoxColumn";
            this.actionTimeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // backupIdDataGridViewTextBoxColumn
            // 
            this.backupIdDataGridViewTextBoxColumn.DataPropertyName = "BackupId";
            this.backupIdDataGridViewTextBoxColumn.HeaderText = "BackupId";
            this.backupIdDataGridViewTextBoxColumn.Name = "backupIdDataGridViewTextBoxColumn";
            this.backupIdDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // computerIdDataGridViewTextBoxColumn
            // 
            this.computerIdDataGridViewTextBoxColumn.DataPropertyName = "ComputerId";
            this.computerIdDataGridViewTextBoxColumn.HeaderText = "ComputerId";
            this.computerIdDataGridViewTextBoxColumn.Name = "computerIdDataGridViewTextBoxColumn";
            this.computerIdDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // backupVersionDataGridViewTextBoxColumn
            // 
            this.backupVersionDataGridViewTextBoxColumn.DataPropertyName = "BackupVersion";
            this.backupVersionDataGridViewTextBoxColumn.HeaderText = "BackupVersion";
            this.backupVersionDataGridViewTextBoxColumn.Name = "backupVersionDataGridViewTextBoxColumn";
            this.backupVersionDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btOk);
            this.panel1.Location = new System.Drawing.Point(-16, 352);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(753, 46);
            this.panel1.TabIndex = 5;
            // 
            // btOk
            // 
            this.btOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btOk.Location = new System.Drawing.Point(600, 5);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(75, 23);
            this.btOk.TabIndex = 1;
            this.btOk.Text = "OK";
            this.btOk.UseVisualStyleBackColor = true;
            // 
            // FormJournal
            // 
            this.AcceptButton = this.btOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 386);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "FormJournal";
            this.Text = "Journal";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.transferJournalItemBindingSource)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn typeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn actionTimeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn backupIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn computerIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn backupVersionDataGridViewTextBoxColumn;
        private System.Windows.Forms.BindingSource transferJournalItemBindingSource;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btOk;
    }
}