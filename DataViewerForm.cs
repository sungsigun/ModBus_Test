using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.Xpo;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraGrid.Columns;
using DevExpress.Utils;

namespace ModBusDevExpress.Forms
{
    public partial class DataViewerForm : XtraForm
    {
        private XPCollection<AcquiredData> dataCollection;
        private Timer refreshTimer;

        public DataViewerForm()
        {
            InitializeComponent();
            InitializeData();
            SetupGrid();
            SetupAutoRefresh();
        }

        private void InitializeData()
        {
            try
            {
                // 🎯 초기 데이터 로드 (최근 1일)
                DateTime fromDate = DateTime.Now.AddDays(-1);
                DateTime toDate = DateTime.Now;

                LoadData(fromDate, toDate, "");
                LoadFacilityList();

                // 기본 날짜 설정
                dateFrom.DateTime = fromDate;
                dateTo.DateTime = toDate;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"데이터 초기화 실패: {ex.Message}", "오류",
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupGrid()
        {
            try
            {
                // 🎯 그리드 컬럼 설정
                gridView1.Columns.Clear();

                // ID 컬럼 (숨김)
                GridColumn colId = gridView1.Columns.AddVisible("Oid");
                colId.Caption = "ID";
                colId.Visible = false;

                // 설비 코드
                GridColumn colFacility = gridView1.Columns.AddVisible("FacilityCode");
                colFacility.Caption = "설비 코드";
                colFacility.Width = 100;

                // 수치 데이터
                GridColumn colNumeric = gridView1.Columns.AddVisible("NumericData");
                colNumeric.Caption = "수치 데이터";
                colNumeric.Width = 100;
                colNumeric.DisplayFormat.FormatType = FormatType.Numeric;
                colNumeric.DisplayFormat.FormatString = "N2";

                // 문자 데이터
                GridColumn colString = gridView1.Columns.AddVisible("StringData");
                colString.Caption = "문자 데이터";
                colString.Width = 150;

                // IP 주소
                GridColumn colIP = gridView1.Columns.AddVisible("IPAddres");
                colIP.Caption = "IP 주소";
                colIP.Width = 120;

                // 생성 시간
                GridColumn colCreated = gridView1.Columns.AddVisible("CreatedDateTime");
                colCreated.Caption = "생성 시간";
                colCreated.Width = 150;
                colCreated.DisplayFormat.FormatType = FormatType.DateTime;
                colCreated.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm:ss";

                // 🔧 그리드 옵션 설정 (호환성 개선)
                gridView1.OptionsView.ShowAutoFilterRow = true; // 자동 필터
                gridView1.OptionsView.ShowGroupPanel = false;
                gridView1.OptionsBehavior.Editable = false; // 읽기 전용
                gridView1.OptionsSelection.EnableAppearanceFocusedCell = false;
                gridView1.FocusRectStyle = DrawFocusRectStyle.RowFocus;
                gridView1.OptionsSelection.MultiSelect = true;
                gridView1.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;

                // 정렬 설정 (최신 데이터가 위로)
                gridView1.Columns["CreatedDateTime"].SortOrder = DevExpress.Data.ColumnSortOrder.Descending;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"그리드 설정 실패: {ex.Message}", "오류");
            }
        }

        private void SetupAutoRefresh()
        {
            // 🎯 30초마다 자동 새로고침
            refreshTimer = new Timer();
            refreshTimer.Interval = 30000; // 30초
            refreshTimer.Tick += (s, e) => {
                if (this.Visible && !this.IsDisposed)
                {
                    RefreshData();
                }
            };
            refreshTimer.Start();
        }

        private void LoadData(DateTime fromDate, DateTime toDate, string facilityCode)
        {
            try
            {
                string criteria = $"CreatedDateTime >= '{fromDate:yyyy-MM-dd}' AND CreatedDateTime <= '{toDate:yyyy-MM-dd 23:59:59}'";

                if (!string.IsNullOrEmpty(facilityCode) && facilityCode != "전체")
                {
                    criteria += $" AND FacilityCode = '{facilityCode}'";
                }

                dataCollection = new XPCollection<AcquiredData>(SessionService.Instance.UOW,
                    DevExpress.Data.Filtering.CriteriaOperator.Parse(criteria));

                gridControl1.DataSource = dataCollection;

                // 레코드 수 업데이트
                lblRecordCount.Text = $"총 {dataCollection.Count:N0}건";
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"데이터 로드 실패: {ex.Message}", "오류");
                lblRecordCount.Text = "총 0건";
            }
        }

        private void LoadFacilityList()
        {
            try
            {
                // 🎯 설비 목록 로드
                var facilities = new XPCollection<AcquiredData>(SessionService.Instance.UOW)
                    .Select(x => x.FacilityCode)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                cmbFacility.Properties.Items.Clear();
                cmbFacility.Properties.Items.Add("전체");

                foreach (string facility in facilities)
                {
                    cmbFacility.Properties.Items.Add(facility);
                }

                cmbFacility.SelectedIndex = 0; // "전체" 선택
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"설비 목록 로드 실패: {ex.Message}", "오류");
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime fromDate = dateFrom.DateTime.Date;
                DateTime toDate = dateTo.DateTime.Date;
                string facilityCode = cmbFacility.Text;

                if (fromDate > toDate)
                {
                    XtraMessageBox.Show("시작일이 종료일보다 클 수 없습니다.", "입력 오류");
                    return;
                }

                LoadData(fromDate, toDate, facilityCode);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"조회 실패: {ex.Message}", "오류");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            try
            {
                DateTime fromDate = dateFrom.DateTime.Date;
                DateTime toDate = dateTo.DateTime.Date;
                string facilityCode = cmbFacility.Text;

                LoadData(fromDate, toDate, facilityCode);

                // 상태 표시
                this.Text = $"데이터 조회 - 마지막 업데이트: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"새로고침 실패: {ex.Message}", "오류");
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Excel 파일 (*.xlsx)|*.xlsx|CSV 파일 (*.csv)|*.csv";
                saveDialog.FileName = $"데이터_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (saveDialog.FilterIndex == 1) // Excel
                    {
                        gridView1.ExportToXlsx(saveDialog.FileName);
                    }
                    else // CSV
                    {
                        gridView1.ExportToCsv(saveDialog.FileName);
                    }

                    XtraMessageBox.Show($"파일이 저장되었습니다.\n{saveDialog.FileName}", "내보내기 완료");
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"파일 내보내기 실패: {ex.Message}", "오류");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedRows = gridView1.GetSelectedRows();
                if (selectedRows.Length == 0)
                {
                    XtraMessageBox.Show("삭제할 항목을 선택하세요.", "알림");
                    return;
                }

                var result = XtraMessageBox.Show(
                    $"선택한 {selectedRows.Length}개 항목을 삭제하시겠습니까?",
                    "삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    List<AcquiredData> itemsToDelete = new List<AcquiredData>();

                    foreach (int rowHandle in selectedRows)
                    {
                        if (rowHandle >= 0)
                        {
                            var item = gridView1.GetRow(rowHandle) as AcquiredData;
                            if (item != null)
                            {
                                itemsToDelete.Add(item);
                            }
                        }
                    }

                    foreach (var item in itemsToDelete)
                    {
                        SessionService.Instance.UOW.Delete(item);
                    }

                    SessionService.Instance.UOW.CommitChanges();
                    RefreshData();

                    XtraMessageBox.Show($"{itemsToDelete.Count}개 항목이 삭제되었습니다.", "삭제 완료");
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"삭제 실패: {ex.Message}", "오류");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}