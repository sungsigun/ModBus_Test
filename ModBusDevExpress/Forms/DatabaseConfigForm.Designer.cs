namespace ModBusDevExpress.Forms
{
    partial class DatabaseConfigForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkRememberPassword;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox grpDbType;
        private System.Windows.Forms.RadioButton rbPostgreSQL;
        private System.Windows.Forms.RadioButton rbSqlServer;
        private System.Windows.Forms.GroupBox grpServer;
        private System.Windows.Forms.GroupBox grpAuth;
        private System.Windows.Forms.Label lblCompany;
        private System.Windows.Forms.ComboBox cmbCompany;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grpDbType = new System.Windows.Forms.GroupBox();
            this.rbPostgreSQL = new System.Windows.Forms.RadioButton();
            this.rbSqlServer = new System.Windows.Forms.RadioButton();
            this.lblServer = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.lblDatabase = new System.Windows.Forms.Label();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkRememberPassword = new System.Windows.Forms.CheckBox();
            this.lblCompany = new System.Windows.Forms.Label();
            this.cmbCompany = new System.Windows.Forms.ComboBox();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpServer = new System.Windows.Forms.GroupBox();
            this.grpAuth = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.grpDbType.SuspendLayout();
            this.grpServer.SuspendLayout();
            this.grpAuth.SuspendLayout();
            this.SuspendLayout();

            // 
            // grpDbType
            // 
            this.grpDbType.Controls.Add(this.rbPostgreSQL);
            this.grpDbType.Controls.Add(this.rbSqlServer);
            this.grpDbType.Location = new System.Drawing.Point(15, 15);
            this.grpDbType.Name = "grpDbType";
            this.grpDbType.Size = new System.Drawing.Size(420, 60);
            this.grpDbType.TabIndex = 0;
            this.grpDbType.TabStop = false;
            this.grpDbType.Text = "데이터베이스 종류";

            // 
            // rbPostgreSQL
            // 
            this.rbPostgreSQL.AutoSize = true;
            this.rbPostgreSQL.Location = new System.Drawing.Point(15, 25);
            this.rbPostgreSQL.Name = "rbPostgreSQL";
            this.rbPostgreSQL.Size = new System.Drawing.Size(82, 16);
            this.rbPostgreSQL.TabIndex = 0;
            this.rbPostgreSQL.Text = "PostgreSQL";
            this.rbPostgreSQL.UseVisualStyleBackColor = true;
            this.rbPostgreSQL.CheckedChanged += new System.EventHandler(this.DatabaseType_CheckedChanged);

            // 
            // rbSqlServer
            // 
            this.rbSqlServer.AutoSize = true;
            this.rbSqlServer.Checked = true;
            this.rbSqlServer.Location = new System.Drawing.Point(120, 25);
            this.rbSqlServer.Name = "rbSqlServer";
            this.rbSqlServer.Size = new System.Drawing.Size(77, 16);
            this.rbSqlServer.TabIndex = 1;
            this.rbSqlServer.TabStop = true;
            this.rbSqlServer.Text = "SQL Server";
            this.rbSqlServer.UseVisualStyleBackColor = true;
            this.rbSqlServer.CheckedChanged += new System.EventHandler(this.DatabaseType_CheckedChanged);

            // 
            // grpServer
            // 
            this.grpServer.Controls.Add(this.lblServer);
            this.grpServer.Controls.Add(this.txtServer);
            this.grpServer.Controls.Add(this.lblPort);
            this.grpServer.Controls.Add(this.nudPort);
            this.grpServer.Controls.Add(this.lblDatabase);
            this.grpServer.Controls.Add(this.txtDatabase);
            this.grpServer.Location = new System.Drawing.Point(15, 85);
            this.grpServer.Name = "grpServer";
            this.grpServer.Size = new System.Drawing.Size(420, 100);
            this.grpServer.TabIndex = 1;
            this.grpServer.TabStop = false;
            this.grpServer.Text = "서버 정보";

            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(15, 25);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(57, 12);
            this.lblServer.TabIndex = 0;
            this.lblServer.Text = "서버 주소:";

            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(80, 22);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(200, 21);
            this.txtServer.TabIndex = 1;
            this.txtServer.Text = "175.45.202.13";
            this.txtServer.ReadOnly = true;
            this.txtServer.BackColor = System.Drawing.SystemColors.Control;

            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(290, 25);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(33, 12);
            this.lblPort.TabIndex = 2;
            this.lblPort.Text = "포트:";

            // 
            // nudPort
            // 
            this.nudPort.Location = new System.Drawing.Point(330, 22);
            this.nudPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.nudPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(70, 21);
            this.nudPort.TabIndex = 3;
            this.nudPort.Value = new decimal(new int[] { 11433, 0, 0, 0 });
            this.nudPort.ReadOnly = true;
            this.nudPort.BackColor = System.Drawing.SystemColors.Control;

            // 
            // lblDatabase
            // 
            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Location = new System.Drawing.Point(15, 60);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.Size = new System.Drawing.Size(69, 12);
            this.lblDatabase.TabIndex = 4;
            this.lblDatabase.Text = "데이터베이스:";

            // 
            // txtDatabase
            // 
            this.txtDatabase.Location = new System.Drawing.Point(120, 57);  // 🎯 30픽셀 오른쪽으로 이동
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(280, 21);  // 🎯 폭 조정
            this.txtDatabase.TabIndex = 5;

            // 
            // grpAuth
            // 
            this.grpAuth.Controls.Add(this.lblUsername);
            this.grpAuth.Controls.Add(this.txtUsername);
            this.grpAuth.Controls.Add(this.lblPassword);
            this.grpAuth.Controls.Add(this.txtPassword);
            this.grpAuth.Controls.Add(this.chkRememberPassword);
            this.grpAuth.Controls.Add(this.lblCompany);
            this.grpAuth.Controls.Add(this.cmbCompany);
            this.grpAuth.Location = new System.Drawing.Point(15, 195);
            this.grpAuth.Name = "grpAuth";
            this.grpAuth.Size = new System.Drawing.Size(420, 155);
            this.grpAuth.TabIndex = 6;
            this.grpAuth.TabStop = false;
            this.grpAuth.Text = "인증 정보";

            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(15, 25);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(53, 12);
            this.lblUsername.TabIndex = 0;
            this.lblUsername.Text = "사용자명:";

            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(80, 22);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(320, 21);
            this.txtUsername.TabIndex = 1;

            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(15, 60);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(53, 12);
            this.lblPassword.TabIndex = 2;
            this.lblPassword.Text = "비밀번호:";

            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(80, 57);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '●';
            this.txtPassword.Size = new System.Drawing.Size(320, 21);
            this.txtPassword.TabIndex = 3;
            this.txtPassword.UseSystemPasswordChar = false;

            // 
            // chkRememberPassword
            // 
            this.chkRememberPassword.AutoSize = true;
            this.chkRememberPassword.Checked = true;
            this.chkRememberPassword.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRememberPassword.Location = new System.Drawing.Point(80, 90);
            this.chkRememberPassword.Name = "chkRememberPassword";
            this.chkRememberPassword.Size = new System.Drawing.Size(104, 16);
            this.chkRememberPassword.TabIndex = 4;
            this.chkRememberPassword.Text = "비밀번호 기억하기";
            this.chkRememberPassword.UseVisualStyleBackColor = true;

            // 
            // lblCompany
            // 
            this.lblCompany.AutoSize = true;
            this.lblCompany.Location = new System.Drawing.Point(15, 120);
            this.lblCompany.Name = "lblCompany";
            this.lblCompany.Size = new System.Drawing.Size(41, 12);
            this.lblCompany.TabIndex = 5;
            this.lblCompany.Text = "회사명:";

            // 
            // cmbCompany
            // 
            this.cmbCompany.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCompany.Location = new System.Drawing.Point(80, 117);
            this.cmbCompany.Name = "cmbCompany";
            this.cmbCompany.Size = new System.Drawing.Size(320, 20);
            this.cmbCompany.TabIndex = 6;

            // 
            // btnTest
            // 
            this.btnTest.BackColor = System.Drawing.Color.LightSteelBlue;
            this.btnTest.Location = new System.Drawing.Point(160, 365);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(100, 35);
            this.btnTest.TabIndex = 7;
            this.btnTest.Text = "연결 테스트";
            this.btnTest.UseVisualStyleBackColor = false;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);

            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.Color.LightGreen;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(250, 415);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 35);
            this.btnOK.TabIndex = 8;
            this.btnOK.Text = "확인";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.LightCoral;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(350, 415);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 35);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = false;

            // 
            // DatabaseConfigForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(450, 475);
            this.Controls.Add(this.grpDbType);
            this.Controls.Add(this.grpServer);
            this.Controls.Add(this.grpAuth);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DatabaseConfigForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "데이터베이스 연결 설정";
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.grpDbType.ResumeLayout(false);
            this.grpDbType.PerformLayout();
            this.grpServer.ResumeLayout(false);
            this.grpServer.PerformLayout();
            this.grpAuth.ResumeLayout(false);
            this.grpAuth.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}