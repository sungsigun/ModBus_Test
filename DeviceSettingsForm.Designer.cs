namespace ModBusDevExpress.Forms
{
    partial class DeviceSettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SimpleButton btnAdd;
        private DevExpress.XtraEditors.SimpleButton btnEdit;
        private DevExpress.XtraEditors.SimpleButton btnDelete;
        private DevExpress.XtraEditors.SimpleButton btnSave;
        private DevExpress.XtraEditors.SimpleButton btnCancel;
        private DevExpress.XtraEditors.SimpleButton btnTest;
        private System.Windows.Forms.GroupBox grpDevice;
        private System.Windows.Forms.GroupBox grpItems;
        private System.Windows.Forms.GroupBox grpMapping;
        private DevExpress.XtraEditors.TextEdit txtDeviceName;
        private DevExpress.XtraEditors.TextEdit txtDeviceCode;
        private DevExpress.XtraEditors.TextEdit txtIPAddress;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.NumericUpDown nudInterval;
        private System.Windows.Forms.NumericUpDown nudSaveInterval;
        private System.Windows.Forms.NumericUpDown nudStartAddress;
        private System.Windows.Forms.NumericUpDown nudDataLength;
        private System.Windows.Forms.NumericUpDown nudSlaveId;
        private DevExpress.XtraEditors.CheckEdit chkActive;
        private System.Windows.Forms.ListBox lbItems;
        private System.Windows.Forms.ListBox lbMappings;
        private DevExpress.XtraEditors.TextEdit txtItemName;
        private DevExpress.XtraEditors.SimpleButton btnAddItem;
        private DevExpress.XtraEditors.SimpleButton btnRemoveItem;
        private System.Windows.Forms.NumericUpDown nudMappingAddress;
        private System.Windows.Forms.ComboBox cmbDataType;
        private DevExpress.XtraEditors.TextEdit txtFormat;
        private DevExpress.XtraEditors.SimpleButton btnAddMapping;
        private DevExpress.XtraEditors.SimpleButton btnRemoveMapping;

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
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.btnAdd = new DevExpress.XtraEditors.SimpleButton();
            this.btnEdit = new DevExpress.XtraEditors.SimpleButton();
            this.btnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.btnSave = new DevExpress.XtraEditors.SimpleButton();
            this.btnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.btnTest = new DevExpress.XtraEditors.SimpleButton();
            this.grpDevice = new System.Windows.Forms.GroupBox();
            this.grpItems = new System.Windows.Forms.GroupBox();
            this.grpMapping = new System.Windows.Forms.GroupBox();
            this.txtDeviceName = new DevExpress.XtraEditors.TextEdit();
            this.txtDeviceCode = new DevExpress.XtraEditors.TextEdit();
            this.txtIPAddress = new DevExpress.XtraEditors.TextEdit();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.nudInterval = new System.Windows.Forms.NumericUpDown();
            this.nudSaveInterval = new System.Windows.Forms.NumericUpDown();
            this.nudStartAddress = new System.Windows.Forms.NumericUpDown();
            this.nudDataLength = new System.Windows.Forms.NumericUpDown();
            this.nudSlaveId = new System.Windows.Forms.NumericUpDown();
            this.chkActive = new DevExpress.XtraEditors.CheckEdit();
            this.lbItems = new System.Windows.Forms.ListBox();
            this.lbMappings = new System.Windows.Forms.ListBox();
            this.txtItemName = new DevExpress.XtraEditors.TextEdit();
            this.btnAddItem = new DevExpress.XtraEditors.SimpleButton();
            this.btnRemoveItem = new DevExpress.XtraEditors.SimpleButton();
            this.nudMappingAddress = new System.Windows.Forms.NumericUpDown();
            this.cmbDataType = new System.Windows.Forms.ComboBox();
            this.txtFormat = new DevExpress.XtraEditors.TextEdit();
            this.btnAddMapping = new DevExpress.XtraEditors.SimpleButton();
            this.btnRemoveMapping = new DevExpress.XtraEditors.SimpleButton();

            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.grpDevice.SuspendLayout();
            this.grpItems.SuspendLayout();
            this.grpMapping.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtDeviceName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDeviceCode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtIPAddress.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartAddress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDataLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSlaveId)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkActive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtItemName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMappingAddress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFormat.Properties)).BeginInit();
            this.SuspendLayout();

            // Form
            this.Text = "디바이스 설정 관리";
            this.Size = new System.Drawing.Size(1000, 720);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            // Grid Control
            this.gridControl1.Location = new System.Drawing.Point(12, 12);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(600, 200);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});

            // Grid View
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";

            // Buttons
            this.btnAdd.Location = new System.Drawing.Point(630, 12);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(80, 30);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "추가";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);

            this.btnEdit.Location = new System.Drawing.Point(630, 48);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(80, 30);
            this.btnEdit.TabIndex = 2;
            this.btnEdit.Text = "수정";
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);

            this.btnDelete.Location = new System.Drawing.Point(630, 84);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(80, 30);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "삭제";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

            // Device Group
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "설비명:", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(60, 20) });
            this.grpDevice.Controls.Add(this.txtDeviceName);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "설비코드:", Location = new System.Drawing.Point(10, 55), Size = new System.Drawing.Size(60, 20) });
            this.grpDevice.Controls.Add(this.txtDeviceCode);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "IP 주소:", Location = new System.Drawing.Point(10, 85), Size = new System.Drawing.Size(60, 20) });
            this.grpDevice.Controls.Add(this.txtIPAddress);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "포트:", Location = new System.Drawing.Point(250, 85), Size = new System.Drawing.Size(40, 20) });
            this.grpDevice.Controls.Add(this.nudPort);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "수집주기(초):", Location = new System.Drawing.Point(10, 115), Size = new System.Drawing.Size(80, 20) });
            this.grpDevice.Controls.Add(this.nudInterval);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "저장주기(초):", Location = new System.Drawing.Point(250, 115), Size = new System.Drawing.Size(80, 20) });
            this.grpDevice.Controls.Add(this.nudSaveInterval);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "시작주소:", Location = new System.Drawing.Point(10, 145), Size = new System.Drawing.Size(60, 20) });
            this.grpDevice.Controls.Add(this.nudStartAddress);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "데이터길이:", Location = new System.Drawing.Point(250, 145), Size = new System.Drawing.Size(80, 20) });
            this.grpDevice.Controls.Add(this.nudDataLength);
            this.grpDevice.Controls.Add(new System.Windows.Forms.Label() { Text = "Slave ID:", Location = new System.Drawing.Point(10, 175), Size = new System.Drawing.Size(60, 20) });
            this.grpDevice.Controls.Add(this.nudSlaveId);
            this.grpDevice.Controls.Add(this.chkActive);
            this.grpDevice.Location = new System.Drawing.Point(12, 230);
            this.grpDevice.Name = "grpDevice";
            this.grpDevice.Size = new System.Drawing.Size(450, 210);
            this.grpDevice.TabIndex = 4;
            this.grpDevice.TabStop = false;
            this.grpDevice.Text = "디바이스 정보";
            this.grpDevice.Enabled = false;

            // Device controls
            this.txtDeviceName.Location = new System.Drawing.Point(90, 22);
            this.txtDeviceName.Size = new System.Drawing.Size(340, 20);

            this.txtDeviceCode.Location = new System.Drawing.Point(90, 52);
            this.txtDeviceCode.Size = new System.Drawing.Size(340, 20);

            this.txtIPAddress.Location = new System.Drawing.Point(90, 82);
            this.txtIPAddress.Size = new System.Drawing.Size(140, 20);

            this.nudPort.Location = new System.Drawing.Point(320, 82);
            this.nudPort.Size = new System.Drawing.Size(110, 22);
            this.nudPort.Minimum = 1;
            this.nudPort.Maximum = 65535;
            this.nudPort.Value = 502;

            this.nudInterval.Location = new System.Drawing.Point(90, 112);
            this.nudInterval.Size = new System.Drawing.Size(140, 22);
            this.nudInterval.Minimum = 1;
            this.nudInterval.Maximum = 3600;
            this.nudInterval.Value = 10;

            this.nudSaveInterval.Location = new System.Drawing.Point(330, 112);
            this.nudSaveInterval.Size = new System.Drawing.Size(100, 22);
            this.nudSaveInterval.Minimum = 10;
            this.nudSaveInterval.Maximum = 3600;
            this.nudSaveInterval.Value = 60;

            this.nudStartAddress.Location = new System.Drawing.Point(90, 142);
            this.nudStartAddress.Size = new System.Drawing.Size(140, 22);
            this.nudStartAddress.Maximum = 65535;

            this.nudDataLength.Location = new System.Drawing.Point(330, 142);
            this.nudDataLength.Size = new System.Drawing.Size(100, 22);
            this.nudDataLength.Minimum = 1;
            this.nudDataLength.Maximum = 125;
            this.nudDataLength.Value = 10;

            this.nudSlaveId.Location = new System.Drawing.Point(90, 172);
            this.nudSlaveId.Size = new System.Drawing.Size(140, 22);
            this.nudSlaveId.Minimum = 1;
            this.nudSlaveId.Maximum = 247;
            this.nudSlaveId.Value = 1;

            this.chkActive.Location = new System.Drawing.Point(330, 172);
            this.chkActive.Properties.Caption = "활성화";
            this.chkActive.Size = new System.Drawing.Size(100, 20);

            // Items Group
            this.grpItems.Controls.Add(this.txtItemName);
            this.grpItems.Controls.Add(this.btnAddItem);
            this.grpItems.Controls.Add(this.btnRemoveItem);
            this.grpItems.Controls.Add(this.lbItems);
            this.grpItems.Location = new System.Drawing.Point(480, 230);
            this.grpItems.Name = "grpItems";
            this.grpItems.Size = new System.Drawing.Size(250, 210);
            this.grpItems.TabIndex = 5;
            this.grpItems.TabStop = false;
            this.grpItems.Text = "표시 항목";
            this.grpItems.Enabled = false;

            this.txtItemName.Location = new System.Drawing.Point(10, 25);
            this.txtItemName.Size = new System.Drawing.Size(150, 20);

            this.btnAddItem.Location = new System.Drawing.Point(165, 23);
            this.btnAddItem.Size = new System.Drawing.Size(35, 23);
            this.btnAddItem.Text = "+";
            this.btnAddItem.Click += new System.EventHandler(this.btnAddItem_Click);

            this.btnRemoveItem.Location = new System.Drawing.Point(205, 23);
            this.btnRemoveItem.Size = new System.Drawing.Size(35, 23);
            this.btnRemoveItem.Text = "-";
            this.btnRemoveItem.Click += new System.EventHandler(this.btnRemoveItem_Click);

            this.lbItems.Location = new System.Drawing.Point(10, 55);
            this.lbItems.Size = new System.Drawing.Size(230, 145);

            // Mapping Group
            this.grpMapping.Controls.Add(new System.Windows.Forms.Label() { Text = "주소:", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(40, 20) });
            this.grpMapping.Controls.Add(this.nudMappingAddress);
            this.grpMapping.Controls.Add(new System.Windows.Forms.Label() { Text = "타입:", Location = new System.Drawing.Point(120, 25), Size = new System.Drawing.Size(40, 20) });
            this.grpMapping.Controls.Add(this.cmbDataType);
            this.grpMapping.Controls.Add(new System.Windows.Forms.Label() { Text = "형식:", Location = new System.Drawing.Point(10, 55), Size = new System.Drawing.Size(40, 20) });
            this.grpMapping.Controls.Add(this.txtFormat);
            this.grpMapping.Controls.Add(this.btnAddMapping);
            this.grpMapping.Controls.Add(this.btnRemoveMapping);
            this.grpMapping.Controls.Add(this.lbMappings);
            this.grpMapping.Location = new System.Drawing.Point(750, 230);
            this.grpMapping.Name = "grpMapping";
            this.grpMapping.Size = new System.Drawing.Size(230, 210);
            this.grpMapping.TabIndex = 6;
            this.grpMapping.TabStop = false;
            this.grpMapping.Text = "메모리 맵핑";
            this.grpMapping.Enabled = false;

            this.nudMappingAddress.Location = new System.Drawing.Point(50, 22);
            this.nudMappingAddress.Size = new System.Drawing.Size(60, 22);
            this.nudMappingAddress.Maximum = 65535;

            this.cmbDataType.Location = new System.Drawing.Point(160, 22);
            this.cmbDataType.Size = new System.Drawing.Size(60, 23);
            this.cmbDataType.Items.AddRange(new object[] { "B", "W", "F" });
            this.cmbDataType.SelectedIndex = 0;

            this.txtFormat.Location = new System.Drawing.Point(50, 52);
            this.txtFormat.Size = new System.Drawing.Size(100, 20);
            this.txtFormat.EditValue = "1";

            this.btnAddMapping.Location = new System.Drawing.Point(155, 50);
            this.btnAddMapping.Size = new System.Drawing.Size(30, 23);
            this.btnAddMapping.Text = "+";
            this.btnAddMapping.Click += new System.EventHandler(this.btnAddMapping_Click);

            this.btnRemoveMapping.Location = new System.Drawing.Point(190, 50);
            this.btnRemoveMapping.Size = new System.Drawing.Size(30, 23);
            this.btnRemoveMapping.Text = "-";
            this.btnRemoveMapping.Click += new System.EventHandler(this.btnRemoveMapping_Click);

            this.lbMappings.Location = new System.Drawing.Point(10, 80);
            this.lbMappings.Size = new System.Drawing.Size(210, 120);

            // Bottom buttons
            this.btnTest.Location = new System.Drawing.Point(480, 660);
            this.btnTest.Size = new System.Drawing.Size(100, 35);
            this.btnTest.Text = "연결 테스트";
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            this.btnTest.Enabled = false;

            this.btnSave.Location = new System.Drawing.Point(720, 660);
            this.btnSave.Size = new System.Drawing.Size(80, 35);
            this.btnSave.Text = "저장";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnSave.Enabled = false;

            this.btnCancel.Location = new System.Drawing.Point(810, 660);
            this.btnCancel.Size = new System.Drawing.Size(80, 35);
            this.btnCancel.Text = "취소";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.btnCancel.Enabled = false;

            // Add all controls to form
            this.Controls.Add(this.gridControl1);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.grpDevice);
            this.Controls.Add(this.grpItems);
            this.Controls.Add(this.grpMapping);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);

            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.grpDevice.ResumeLayout(false);
            this.grpItems.ResumeLayout(false);
            this.grpMapping.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtDeviceName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDeviceCode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtIPAddress.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartAddress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDataLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSlaveId)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkActive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtItemName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMappingAddress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFormat.Properties)).EndInit();
            this.ResumeLayout(false);
        }
    }
}