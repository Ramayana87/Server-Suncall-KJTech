namespace Server
{
    partial class TestClientForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnLoadMockup = new System.Windows.Forms.Button();
            this.btnTestMockup = new System.Windows.Forms.Button();
            this.btnTestServerMockup = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtMachinePort = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtMachineIP = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtMachineNumber = new System.Windows.Forms.TextBox();
            this.chkUseDateFilter = new System.Windows.Forms.CheckBox();
            this.btnTestServer = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtServerIP = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.label4 = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnTestServerMockup);
            this.groupBox1.Controls.Add(this.btnTestMockup);
            this.groupBox1.Controls.Add(this.btnLoadMockup);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(250, 120);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Mockup Data Test";
            // 
            // btnLoadMockup
            // 
            this.btnLoadMockup.Location = new System.Drawing.Point(15, 25);
            this.btnLoadMockup.Name = "btnLoadMockup";
            this.btnLoadMockup.Size = new System.Drawing.Size(220, 25);
            this.btnLoadMockup.TabIndex = 0;
            this.btnLoadMockup.Text = "Load Mockup Data from Files";
            this.btnLoadMockup.UseVisualStyleBackColor = true;
            this.btnLoadMockup.Click += new System.EventHandler(this.btnLoadMockup_Click);
            // 
            // btnTestMockup
            // 
            this.btnTestMockup.Location = new System.Drawing.Point(15, 55);
            this.btnTestMockup.Name = "btnTestMockup";
            this.btnTestMockup.Size = new System.Drawing.Size(220, 25);
            this.btnTestMockup.TabIndex = 1;
            this.btnTestMockup.Text = "Test Filter on Mockup Data";
            this.btnTestMockup.UseVisualStyleBackColor = true;
            this.btnTestMockup.Click += new System.EventHandler(this.btnTestMockup_Click);
            // 
            // btnTestServerMockup
            // 
            this.btnTestServerMockup.Location = new System.Drawing.Point(15, 85);
            this.btnTestServerMockup.Name = "btnTestServerMockup";
            this.btnTestServerMockup.Size = new System.Drawing.Size(220, 25);
            this.btnTestServerMockup.TabIndex = 2;
            this.btnTestServerMockup.Text = "Test Server with Mockup Data";
            this.btnTestServerMockup.UseVisualStyleBackColor = true;
            this.btnTestServerMockup.Click += new System.EventHandler(this.btnTestServerMockup_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.txtMachinePort);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.txtMachineIP);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.txtMachineNumber);
            this.groupBox2.Controls.Add(this.chkUseDateFilter);
            this.groupBox2.Controls.Add(this.btnTestServer);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.txtServerPort);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.txtServerIP);
            this.groupBox2.Location = new System.Drawing.Point(280, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(250, 240);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Server Connection Test";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(140, 155);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(74, 13);
            this.label8.TabIndex = 14;
            this.label8.Text = "Machine Port:";
            // 
            // txtMachinePort
            // 
            this.txtMachinePort.Location = new System.Drawing.Point(140, 175);
            this.txtMachinePort.Name = "txtMachinePort";
            this.txtMachinePort.Size = new System.Drawing.Size(95, 20);
            this.txtMachinePort.TabIndex = 13;
            this.txtMachinePort.Text = "4370";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(15, 115);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(67, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Machine IP:";
            // 
            // txtMachineIP
            // 
            this.txtMachineIP.Location = new System.Drawing.Point(15, 135);
            this.txtMachineIP.Name = "txtMachineIP";
            this.txtMachineIP.Size = new System.Drawing.Size(220, 20);
            this.txtMachineIP.TabIndex = 11;
            this.txtMachineIP.Text = "192.168.1.201";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 155);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(94, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Machine Number:";
            // 
            // txtMachineNumber
            // 
            this.txtMachineNumber.Location = new System.Drawing.Point(15, 175);
            this.txtMachineNumber.Name = "txtMachineNumber";
            this.txtMachineNumber.Size = new System.Drawing.Size(115, 20);
            this.txtMachineNumber.TabIndex = 9;
            this.txtMachineNumber.Text = "1";
            // 
            // chkUseDateFilter
            // 
            this.chkUseDateFilter.AutoSize = true;
            this.chkUseDateFilter.Checked = true;
            this.chkUseDateFilter.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUseDateFilter.Location = new System.Drawing.Point(15, 85);
            this.chkUseDateFilter.Name = "chkUseDateFilter";
            this.chkUseDateFilter.Size = new System.Drawing.Size(153, 17);
            this.chkUseDateFilter.TabIndex = 4;
            this.chkUseDateFilter.Text = "Use Date Range Filter";
            this.chkUseDateFilter.UseVisualStyleBackColor = true;
            // 
            // btnTestServer
            // 
            this.btnTestServer.Location = new System.Drawing.Point(15, 205);
            this.btnTestServer.Name = "btnTestServer";
            this.btnTestServer.Size = new System.Drawing.Size(220, 25);
            this.btnTestServer.TabIndex = 5;
            this.btnTestServer.Text = "Test Server Connection";
            this.btnTestServer.UseVisualStyleBackColor = true;
            this.btnTestServer.Click += new System.EventHandler(this.btnTestServer_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(140, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Server Port:";
            // 
            // txtServerPort
            // 
            this.txtServerPort.Location = new System.Drawing.Point(140, 45);
            this.txtServerPort.Name = "txtServerPort";
            this.txtServerPort.Size = new System.Drawing.Size(95, 20);
            this.txtServerPort.TabIndex = 3;
            this.txtServerPort.Text = "9999";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Server IP:";
            // 
            // txtServerIP
            // 
            this.txtServerIP.Location = new System.Drawing.Point(15, 45);
            this.txtServerIP.Name = "txtServerIP";
            this.txtServerIP.Size = new System.Drawing.Size(115, 20);
            this.txtServerIP.TabIndex = 1;
            this.txtServerIP.Text = "127.0.0.1";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.dtpTo);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.dtpFrom);
            this.groupBox3.Location = new System.Drawing.Point(12, 138);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(250, 90);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Date Range Filter";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 55);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(52, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "To Date:";
            // 
            // dtpTo
            // 
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTo.Location = new System.Drawing.Point(80, 52);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.Size = new System.Drawing.Size(155, 20);
            this.dtpTo.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "From Date:";
            // 
            // dtpFrom
            // 
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFrom.Location = new System.Drawing.Point(80, 22);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.Size = new System.Drawing.Size(155, 20);
            this.dtpFrom.TabIndex = 1;
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.SystemColors.Window;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.Location = new System.Drawing.Point(12, 277);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(518, 250);
            this.txtLog.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 260);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Log:";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(12, 530);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(43, 13);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Ready";
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(455, 252);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 23);
            this.btnClearLog.TabIndex = 6;
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // TestClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(542, 553);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "TestClientForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Test Client - Attendance Data";
            this.Load += new System.EventHandler(this.TestClientForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnLoadMockup;
        private System.Windows.Forms.Button btnTestMockup;
        private System.Windows.Forms.Button btnTestServerMockup;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnTestServer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtServerPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtServerIP;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.CheckBox chkUseDateFilter;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtMachineNumber;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtMachineIP;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtMachinePort;
    }
}
