using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;
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
        private UnitOfWork uow; // 🎯 별도 UnitOfWork 사용 (동시성 문제 해결)
        // 📄 페이징 상태
        private int pageSize = 50;
        private int currentPage = 1;
        private int totalRecords = 0;
        private int totalPages = 1;
        private Panel pagingPanel;
        private System.Windows.Forms.ComboBox cmbPageSize;
        private Button btnPrev;
        private Button btnNext;
        private Label lblPageInfo;

        public DataViewerForm()
        {
            InitializeComponent();
            InitializeUOW();
            InitializeData();
            SetupGrid();
            SetupAutoRefresh();
            SetupPagingUI();
        }
        
        // 🎯 별도 UnitOfWork 초기화
        private void InitializeUOW()
        {
            try
            {
                uow = new UnitOfWork();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"데이터베이스 연결 실패: {ex.Message}", "오류",
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeData()
        {
            try
            {
                // 🎯 안전한 초기화 순서 변경
                // 1. 기본 날짜 설정 먼저
                DateTime fromDate = DateTime.Now.AddDays(-1);
                DateTime toDate = DateTime.Now;
                dateFrom.DateTime = fromDate;
                dateTo.DateTime = toDate;

                // 2. 설비 목록 로드 (빈 데이터 처리 포함)
                LoadFacilityList();

                // 3. 초기 데이터는 로드하지 않음 (사용자가 조회 버튼 클릭 시에만)
                gridControl1.DataSource = null;
                lblRecordCount.Text = "조회 버튼을 클릭하세요";
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
            // 🚫 자동 새로고침 비활성화 (시스템 멈춤 방지)
            // 필요 시 수동으로 새로고침 버튼 사용
            /*
            refreshTimer = new Timer();
            refreshTimer.Interval = 30000; // 30초
            refreshTimer.Tick += (s, e) => {
                if (this.Visible && !this.IsDisposed)
                {
                    RefreshData();
                }
            };
            refreshTimer.Start();
            */
        }

        // 📄 페이징 UI 구성
        private void SetupPagingUI()
        {
            pagingPanel = new Panel
            {
                Height = 36,
                Dock = DockStyle.Bottom
            };
            this.Controls.Add(pagingPanel);

            cmbPageSize = new System.Windows.Forms.ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 80,
                Left = 10,
                Top = 6
            };
            cmbPageSize.Items.AddRange(new object[] { 50, 100 });
            cmbPageSize.SelectedIndex = 0; // 기본 50
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                pageSize = Convert.ToInt32(cmbPageSize.SelectedItem);
                currentPage = 1;
                RefreshData();
            };
            pagingPanel.Controls.Add(cmbPageSize);

            btnPrev = new Button
            {
                Text = "이전",
                Width = 60,
                Left = 110,
                Top = 6
            };
            btnPrev.Click += (s, e) =>
            {
                if (currentPage > 1)
                {
                    currentPage--;
                    RefreshData();
                }
            };
            pagingPanel.Controls.Add(btnPrev);

            btnNext = new Button
            {
                Text = "다음",
                Width = 60,
                Left = 180,
                Top = 6
            };
            btnNext.Click += (s, e) =>
            {
                if (currentPage < totalPages)
                {
                    currentPage++;
                    RefreshData();
                }
            };
            pagingPanel.Controls.Add(btnNext);

            lblPageInfo = new Label
            {
                AutoSize = true,
                Left = 250,
                Top = 10,
                Text = "페이지 1/1"
            };
            pagingPanel.Controls.Add(lblPageInfo);
        }

        private void UpdatePagingInfo()
        {
            lblPageInfo.Text = $"페이지 {currentPage}/{totalPages} (페이지당 {pageSize}개)";
            btnPrev.Enabled = currentPage > 1;
            btnNext.Enabled = currentPage < totalPages;
        }

        private void LoadData(DateTime fromDate, DateTime toDate, string facilityCode)
        {
            try
            {
                // 🔍 조회 시작 상태 표시
                lblRecordCount.Text = "조회 중...";
                gridControl1.DataSource = null;
                Application.DoEvents(); // UI 업데이트

                // 🚀 XPO CriteriaOperator 사용 (하드코딩 제거)
                var dateFilter = CriteriaOperator.And(
                    new BinaryOperator(nameof(AcquiredData.CreatedDateTime), fromDate, BinaryOperatorType.GreaterOrEqual),
                    new BinaryOperator(nameof(AcquiredData.CreatedDateTime), toDate.AddDays(1), BinaryOperatorType.Less)
                );

                CriteriaOperator finalCriteria = dateFilter;

                if (!string.IsNullOrEmpty(facilityCode))
                {
                    var facilityFilter = new BinaryOperator(nameof(AcquiredData.FacilityCode), facilityCode, BinaryOperatorType.Equal);
                    finalCriteria = CriteriaOperator.And(dateFilter, facilityFilter);
                }

                // 🎯 총 건수 계산
                totalRecords = (int)uow.Query<AcquiredData>().Where(a =>
                    a.CreatedDateTime >= fromDate && a.CreatedDateTime < toDate.AddDays(1) &&
                    (string.IsNullOrEmpty(facilityCode) ? true : a.FacilityCode == facilityCode)
                ).Count();

                // 페이지 유효성 보정
                totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
                if (currentPage > totalPages) currentPage = totalPages;
                int skip = (currentPage - 1) * pageSize;

                // 🎯 페이지 데이터 조회
                var pageQuery = uow.Query<AcquiredData>()
                    .Where(a => a.CreatedDateTime >= fromDate && a.CreatedDateTime < toDate.AddDays(1) &&
                                (string.IsNullOrEmpty(facilityCode) ? true : a.FacilityCode == facilityCode))
                    .OrderByDescending(a => a.CreatedDateTime)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                var pageData = pageQuery; // List<AcquiredData>

                // 🔍 결과 확인 및 안전 처리
                if (pageData != null)
                {
                    gridControl1.DataSource = pageData;
                    
                    if (totalRecords == 0)
                    {
                        lblRecordCount.Text = "조회 결과가 없습니다 (0건)";
                        XtraMessageBox.Show("선택한 조건에 해당하는 데이터가 없습니다.", "조회 결과", 
                                           MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblRecordCount.Text = $"총 {totalRecords:N0}건";
                        UpdatePagingInfo();
                    }
                }
                else
                {
                    lblRecordCount.Text = "조회 실패 (0건)";
                }
            }
            catch (Exception ex)
            {
                gridControl1.DataSource = null;
                lblRecordCount.Text = "총 0건 (오류 발생)";
                XtraMessageBox.Show($"데이터 로드 실패: {ex.Message}\n\n조건을 다시 확인해주세요.", "조회 오류", 
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadFacilityList()
        {
            try
            {
                // 🎯 드롭다운 목록 초기화
                cmbFacility.Properties.Items.Clear();
                
                // 🔍 데이터 존재 여부 먼저 확인
                int dataCount = uow.Query<AcquiredData>().Count();
                if (dataCount == 0)
                {
                    cmbFacility.Properties.Items.Add("데이터 없음");
                    cmbFacility.SelectedIndex = 0;
                    cmbFacility.Enabled = false;
                    lblRecordCount.Text = "총 0건 (데이터가 없습니다)";
                    return;
                }

                // 🎯 설비 목록 로드 (개수 표시는 제거)
                var facilityData = uow.Query<AcquiredData>()
                    .Where(x => !string.IsNullOrEmpty(x.FacilityCode))
                    .Select(x => x.FacilityCode)
                    .Distinct()
                    .OrderBy(x => x)
                    .Take(100)
                    .ToList();

                if (facilityData.Count == 0)
                {
                    cmbFacility.Properties.Items.Add("설비 없음");
                    cmbFacility.SelectedIndex = 0;
                    cmbFacility.Enabled = false;
                    lblRecordCount.Text = "총 0건 (설비 코드가 없습니다)";
                    return;
                }

                // 🎯 "전체" 옵션 추가 (개수 표시 제거)
                int totalCount = uow.Query<AcquiredData>().Count();
                cmbFacility.Properties.Items.Add("전체");

                // 🎯 각 설비 추가 (개수 표시 제거)
                foreach (var facility in facilityData)
                {
                    cmbFacility.Properties.Items.Add(facility);
                }

                cmbFacility.SelectedIndex = 0; // "전체" 선택
                cmbFacility.Enabled = true;
                lblRecordCount.Text = $"설비: {facilityData.Count}개";
            }
            catch (Exception ex)
            {
                cmbFacility.Properties.Items.Clear();
                cmbFacility.Properties.Items.Add("오류 발생");
                cmbFacility.SelectedIndex = 0;
                cmbFacility.Enabled = false;
                lblRecordCount.Text = "총 0건 (오류 발생)";
                XtraMessageBox.Show($"설비 목록 로드 실패: {ex.Message}", "오류");
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime fromDate = dateFrom.DateTime.Date;
                DateTime toDate = dateTo.DateTime.Date;
                
            // 🎯 설비 코드 추출 (개수 표시 제거되었으므로 그대로 사용)
                string selectedText = cmbFacility.Text;
                string facilityCode = ExtractFacilityCode(selectedText);

                if (fromDate > toDate)
                {
                    XtraMessageBox.Show("시작일이 종료일보다 클 수 없습니다.", "입력 오류", 
                                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LoadData(fromDate, toDate, facilityCode);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"조회 실패: {ex.Message}", "오류");
            }
        }
        
        // 🎯 설비 코드 추출 헬퍼 메서드
        private string ExtractFacilityCode(string displayText)
        {
            if (string.IsNullOrEmpty(displayText))
                return "";
                
            // "전체 (123건)" → "전체"
            // "전력계1 (45건)" → "전력계1"
            // "데이터 없음" → ""
            
            // 괄호 표기를 제거했으므로 그대로 반환
            return displayText == "전체" ? "" : displayText;
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
                
                // 🎯 설비 코드 추출
                string selectedText = cmbFacility.Text;
                string facilityCode = ExtractFacilityCode(selectedText);

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
                        uow.Delete(item);
                    }

                    uow.CommitChanges();
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
            // 🎯 리소스 정리
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            
            // 🎯 별도 UnitOfWork 정리
            uow?.Dispose();
            
            base.OnFormClosed(e);
        }
        
        // 🎯 빠른 날짜 선택 이벤트 핸들러들
        private void btnToday_Click(object sender, EventArgs e)
        {
            var today = DateTime.Now.Date;
            dateFrom.DateTime = today;
            dateTo.DateTime = today;
        }
        
        private void btnYesterday_Click(object sender, EventArgs e)
        {
            var yesterday = DateTime.Now.Date.AddDays(-1);
            dateFrom.DateTime = yesterday;
            dateTo.DateTime = yesterday;
        }
        
        private void btnThisWeek_Click(object sender, EventArgs e)
        {
            var today = DateTime.Now.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // 일요일 시작
            var endOfWeek = startOfWeek.AddDays(6); // 토요일 종료
            
            dateFrom.DateTime = startOfWeek;
            dateTo.DateTime = endOfWeek;
        }
        
        private void btnThisMonth_Click(object sender, EventArgs e)
        {
            var today = DateTime.Now.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            
            dateFrom.DateTime = startOfMonth;
            dateTo.DateTime = endOfMonth;
        }
    }
}