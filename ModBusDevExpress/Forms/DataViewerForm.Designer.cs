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
        private DevExpress.XtraEditors.SimpleButton btnToday;
        private DevExpress.XtraEditors.SimpleButton btnYesterday;
        private DevExpress.XtraEditors.SimpleButton btnThisWeek;
        private DevExpress.XtraEditors.SimpleButton btnThisMonth;

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
            this.btnToday = new DevExpress.XtraEditors.SimpleButton();
            this.btnYesterday = new DevExpress.XtraEditors.SimpleButton();
            this.btnThisWeek = new DevExpress.XtraEditors.SimpleButton();
            this.btnThisMonth = new DevExpress.XtraEditors.SimpleButton();

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
            this.dateFrom.Size = new System.Drawing.Size(130, 20);
            // 🎯 달력 형태 설정
            this.dateFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFrom.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFrom.Properties.ShowDropDown = DevExpress.XtraEditors.Controls.ShowDropDown.SingleClick;
            this.dateFrom.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.dateFrom.Properties.Mask.EditMask = "yyyy-MM-dd";
            this.dateFrom.Properties.DisplayFormat.FormatString = "yyyy-MM-dd";
            this.dateFrom.Properties.EditFormat.FormatString = "yyyy-MM-dd";

            this.lblTo.Text = "종료일:";
            this.lblTo.Location = new System.Drawing.Point(220, 20);
            this.lblTo.Size = new System.Drawing.Size(50, 14);

            this.dateTo.Location = new System.Drawing.Point(290, 18);
            this.dateTo.Size = new System.Drawing.Size(130, 20);
            // 🎯 달력 형태 설정
            this.dateTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateTo.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateTo.Properties.ShowDropDown = DevExpress.XtraEditors.Controls.ShowDropDown.SingleClick;
            this.dateTo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.dateTo.Properties.Mask.EditMask = "yyyy-MM-dd";
            this.dateTo.Properties.DisplayFormat.FormatString = "yyyy-MM-dd";
            this.dateTo.Properties.EditFormat.FormatString = "yyyy-MM-dd";

            this.lblFacility.Text = "설비:";
            this.lblFacility.Location = new System.Drawing.Point(440, 20);
            this.lblFacility.Size = new System.Drawing.Size(40, 14);

            this.cmbFacility.Location = new System.Drawing.Point(490, 18);
            this.cmbFacility.Size = new System.Drawing.Size(150, 20);
            // 🎯 드롭다운 설정
            this.cmbFacility.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cmbFacility.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cmbFacility.Properties.ShowDropDown = DevExpress.XtraEditors.Controls.ShowDropDown.SingleClick;
            this.cmbFacility.Properties.DropDownRows = 10;

            this.btnFilter.Text = "조회";
            this.btnFilter.Location = new System.Drawing.Point(660, 16);
            this.btnFilter.Size = new System.Drawing.Size(75, 25);
            this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);

            // 버튼 영역
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.Location = new System.Drawing.Point(750, 16);
            this.btnRefresh.Size = new System.Drawing.Size(80, 25);
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.btnExport.Text = "엑셀 내보내기";
            this.btnExport.Location = new System.Drawing.Point(840, 16);
            this.btnExport.Size = new System.Drawing.Size(100, 25);
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);

            this.btnDelete.Text = "선택 삭제";
            this.btnDelete.Location = new System.Drawing.Point(950, 16);
            this.btnDelete.Size = new System.Drawing.Size(80, 25);
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

            // 🎯 빠른 날짜 선택 버튼들 (2줄째)
            this.btnToday.Text = "오늘";
            this.btnToday.Location = new System.Drawing.Point(80, 45);
            this.btnToday.Size = new System.Drawing.Size(50, 25);
            this.btnToday.Click += new System.EventHandler(this.btnToday_Click);

            this.btnYesterday.Text = "어제";
            this.btnYesterday.Location = new System.Drawing.Point(140, 45);
            this.btnYesterday.Size = new System.Drawing.Size(50, 25);
            this.btnYesterday.Click += new System.EventHandler(this.btnYesterday_Click);

            this.btnThisWeek.Text = "이번주";
            this.btnThisWeek.Location = new System.Drawing.Point(200, 45);
            this.btnThisWeek.Size = new System.Drawing.Size(55, 25);
            this.btnThisWeek.Click += new System.EventHandler(this.btnThisWeek_Click);

            this.btnThisMonth.Text = "이번달";
            this.btnThisMonth.Location = new System.Drawing.Point(265, 45);
            this.btnThisMonth.Size = new System.Drawing.Size(55, 25);
            this.btnThisMonth.Click += new System.EventHandler(this.btnThisMonth_Click);

            // 레코드 수 표시
            this.lblRecordCount.Text = "총 0건";
            this.lblRecordCount.Location = new System.Drawing.Point(20, 680);
            this.lblRecordCount.Size = new System.Drawing.Size(200, 14);

            // 그리드 (빠른 날짜 버튼 공간 확보)
            this.gridControl1.Location = new System.Drawing.Point(20, 80);
            this.gridControl1.Size = new System.Drawing.Size(1160, 580);
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
            this.Controls.Add(this.btnToday);
            this.Controls.Add(this.btnYesterday);
            this.Controls.Add(this.btnThisWeek);
            this.Controls.Add(this.btnThisMonth);
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