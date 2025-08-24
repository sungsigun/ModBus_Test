namespace ModBusDevExpress.Forms
{
    partial class DataViewerForm
    {
        private System.ComponentModel.IContainer components = null;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SimpleButton btnRefresh;
        private DevExpress.XtraEditors.SimpleButton btnExport;
        private DevExpress.XtraEditors.SimpleButton btnDelete;
        private DevExpress.XtraEditors.DateEdit dateFrom;
        private DevExpress.XtraEditors.DateEdit dateTo;
        private DevExpress.XtraEditors.ComboBoxEdit cmbFacility;
        private DevExpress.XtraEditors.SimpleButton btnFilter;
        private DevExpress.XtraEditors.LabelControl lblFrom;
        private DevExpress.XtraEditors.LabelControl lblTo;
        private DevExpress.XtraEditors.LabelControl lblFacility;
        private DevExpress.XtraEditors.LabelControl lblRecordCount;

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
            this.btnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.btnExport = new DevExpress.XtraEditors.SimpleButton();
            this.btnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.dateFrom = new DevExpress.XtraEditors.DateEdit();
            this.dateTo = new DevExpress.XtraEditors.DateEdit();
            this.cmbFacility = new DevExpress.XtraEditors.ComboBoxEdit();
            this.btnFilter = new DevExpress.XtraEditors.SimpleButton();
            this.lblFrom = new DevExpress.XtraEditors.LabelControl();
            this.lblTo = new DevExpress.XtraEditors.LabelControl();
            this.lblFacility = new DevExpress.XtraEditors.LabelControl();
            this.lblRecordCount = new DevExpress.XtraEditors.LabelControl();

            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cmbFacility.Properties)).BeginInit();
            this.SuspendLayout();

            // Form
            this.Text = "데이터 조회";
            this.Size = new System.Drawing.Size(1200, 700);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            // 필터 영역
            this.lblFrom.Text = "시작일:";
            this.lblFrom.Location = new System.Drawing.Point(20, 20);
            this.lblFrom.Size = new System.Drawing.Size(50, 14);

            this.dateFrom.Location = new System.Drawing.Point(80, 18);
            this.dateFrom.Size = new System.Drawing.Size(120, 20);
            this.dateFrom.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});

            this.lblTo.Text = "종료일:";
            this.lblTo.Location = new System.Drawing.Point(220, 20);
            this.lblTo.Size = new System.Drawing.Size(50, 14);

            this.dateTo.Location = new System.Drawing.Point(280, 18);
            this.dateTo.Size = new System.Drawing.Size(120, 20);
            this.dateTo.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});

            this.lblFacility.Text = "설비:";
            this.lblFacility.Location = new System.Drawing.Point(420, 20);
            this.lblFacility.Size = new System.Drawing.Size(40, 14);

            this.cmbFacility.Location = new System.Drawing.Point(470, 18);
            this.cmbFacility.Size = new System.Drawing.Size(150, 20);

            this.btnFilter.Text = "조회";
            this.btnFilter.Location = new System.Drawing.Point(640, 16);
            this.btnFilter.Size = new System.Drawing.Size(75, 25);
            this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);

            // 버튼 영역
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.Location = new System.Drawing.Point(740, 16);
            this.btnRefresh.Size = new System.Drawing.Size(80, 25);
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.btnExport.Text = "엑셀 내보내기";
            this.btnExport.Location = new System.Drawing.Point(830, 16);
            this.btnExport.Size = new System.Drawing.Size(100, 25);
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);

            this.btnDelete.Text = "선택 삭제";
            this.btnDelete.Location = new System.Drawing.Point(940, 16);
            this.btnDelete.Size = new System.Drawing.Size(80, 25);
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

            // 레코드 수 표시
            this.lblRecordCount.Text = "총 0건";
            this.lblRecordCount.Location = new System.Drawing.Point(20, 680);
            this.lblRecordCount.Size = new System.Drawing.Size(200, 14);

            // 그리드
            this.gridControl1.Location = new System.Drawing.Point(20, 60);
            this.gridControl1.Size = new System.Drawing.Size(1160, 600);
            this.gridControl1.MainView = this.gridView1;

            // 그리드뷰 설정
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.OptionsView.ShowGroupPanel = false;
            this.gridView1.OptionsSelection.MultiSelect = true;
            this.gridView1.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;

            // 컨트롤 추가
            this.Controls.Add(this.lblFrom);
            this.Controls.Add(this.dateFrom);
            this.Controls.Add(this.lblTo);
            this.Controls.Add(this.dateTo);
            this.Controls.Add(this.lblFacility);
            this.Controls.Add(this.cmbFacility);
            this.Controls.Add(this.btnFilter);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.gridControl1);
            this.Controls.Add(this.lblRecordCount);

            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cmbFacility.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}