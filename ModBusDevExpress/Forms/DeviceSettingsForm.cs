using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using ModBusDevExpress.Models;
using ModBusDevExpress.Service;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ModBusDevExpress.Forms
{
    public partial class DeviceSettingsForm : XtraForm
    {
        private List<ModbusDeviceSettings> deviceSettings;
        private ModbusDeviceSettings currentDevice;
        private bool isEditMode = false;

        public DeviceSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
            SetupGrid();
            SetupSaveIntervalValidation(); // 🎯 저장주기 유효성 검사 설정
        }

        // 🎯 저장주기 유효성 검사 설정
        private void SetupSaveIntervalValidation()
        {
            // 수집주기 변경 시 저장주기 자동 조정
            nudInterval.ValueChanged += (s, e) =>
            {
                if (nudSaveInterval.Value < nudInterval.Value)
                {
                    nudSaveInterval.Value = Math.Max(60, nudInterval.Value * 6);
                }
                nudSaveInterval.Minimum = nudInterval.Value; // 최소값을 수집주기로 설정
            };

            // 저장주기 최소값 설정
            nudSaveInterval.Minimum = 10;
            nudSaveInterval.Maximum = 3600;
            nudSaveInterval.Value = 60;
        }

        private void LoadSettings()
        {
            deviceSettings = DeviceConfigManager.LoadDeviceSettings();
            RefreshDeviceList();
        }

        private void RefreshDeviceList()
        {
            gridControl1.DataSource = null;
            gridControl1.DataSource = deviceSettings;
            gridView1.RefreshData();
        }

        private void SetupGrid()
        {
            gridView1.Columns.Clear();

            // 그리드 컬럼 설정
            var colName = gridView1.Columns.AddVisible("DeviceName");
            colName.Caption = "설비명";
            colName.Width = 120;

            var colCode = gridView1.Columns.AddVisible("DeviceCode");
            colCode.Caption = "설비코드";
            colCode.Width = 80;

            var colIP = gridView1.Columns.AddVisible("IPAddress");
            colIP.Caption = "IP 주소";
            colIP.Width = 100;

            var colPort = gridView1.Columns.AddVisible("Port");
            colPort.Caption = "포트";
            colPort.Width = 50;

            var colInterval = gridView1.Columns.AddVisible("Interval");
            colInterval.Caption = "수집주기(초)";
            colInterval.Width = 80;

            // 🎯 저장주기 컬럼 추가
            var colSaveInterval = gridView1.Columns.AddVisible("SaveInterval");
            colSaveInterval.Caption = "저장주기(초)";
            colSaveInterval.Width = 80;

            var colActive = gridView1.Columns.AddVisible("IsActive");
            colActive.Caption = "활성화";
            colActive.Width = 60;

            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.OptionsBehavior.Editable = false;
            gridView1.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ClearForm();
            isEditMode = false;
            currentDevice = new ModbusDeviceSettings();
            EnableForm(true);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var selected = gridView1.GetFocusedRow() as ModbusDeviceSettings;
            if (selected == null)
            {
                XtraMessageBox.Show("수정할 디바이스를 선택하세요.", "알림");
                return;
            }

            isEditMode = true;
            currentDevice = selected;
            LoadDeviceToForm(selected);
            EnableForm(true);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            var selected = gridView1.GetFocusedRow() as ModbusDeviceSettings;
            if (selected == null)
            {
                XtraMessageBox.Show("복사할 디바이스를 선택하세요.", "알림");
                return;
            }

            // 선택된 디바이스를 복사하여 새 디바이스 생성
            isEditMode = false;
            currentDevice = CopyDevice(selected);
            LoadDeviceToForm(currentDevice);
            
            // 복사된 디바이스는 이름에 "_복사" 추가하고 비활성화
            txtDeviceName.Text = (selected.DeviceName ?? "") + "_복사";
            txtDeviceCode.Text = (selected.DeviceCode ?? "") + "_copy";
            chkActive.Checked = false;
            
            EnableForm(true);
            txtDeviceName.Focus();
            txtDeviceName.SelectAll();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selected = gridView1.GetFocusedRow() as ModbusDeviceSettings;
            if (selected == null)
            {
                XtraMessageBox.Show("삭제할 디바이스를 선택하세요.", "알림");
                return;
            }

            if (XtraMessageBox.Show($"'{selected.DeviceName}' 디바이스를 삭제하시겠습니까?",
                "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                deviceSettings.Remove(selected);
                SaveSettings();
                RefreshDeviceList();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            SaveFormToDevice();

            if (!isEditMode)
            {
                deviceSettings.Add(currentDevice);
            }

            SaveSettings();
            RefreshDeviceList();
            ClearForm();
            EnableForm(false);

            // 메인폼에 변경사항 알림
            if (this.Owner is MainForm mainForm)
            {
                _ = System.Threading.Tasks.Task.Run(async () => await mainForm.RefreshDevices());
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ClearForm();
            EnableForm(false);
        }

        private void LoadDeviceToForm(ModbusDeviceSettings device)
        {
            txtDeviceName.Text = device.DeviceName;
            txtDeviceCode.Text = device.DeviceCode;
            txtIPAddress.Text = device.IPAddress;
            nudPort.Value = device.Port;
            nudInterval.Value = device.Interval;
            nudSaveInterval.Value = device.SaveInterval; // 🎯 저장주기 로드
            nudStartAddress.Value = device.StartAddress;
            nudDataLength.Value = device.DataLength;
            nudSlaveId.Value = device.SlaveId;
            chkActive.Checked = device.IsActive;

            // 항목 리스트
            lbItems.Items.Clear();
            foreach (var item in device.Items)
            {
                lbItems.Items.Add(item.Name);
            }

            // 메모리 맵핑
            lbMappings.Items.Clear();
            foreach (var mapping in device.Mappings)
            {
                lbMappings.Items.Add($"{mapping.Address}#{mapping.DataType}#{mapping.Format}");
            }
        }

        private void SaveFormToDevice()
        {
            currentDevice.DeviceName = txtDeviceName.Text.Trim();
            currentDevice.DeviceCode = txtDeviceCode.Text.Trim();
            currentDevice.IPAddress = txtIPAddress.Text.Trim();
            currentDevice.Port = (int)nudPort.Value;
            currentDevice.Interval = (int)nudInterval.Value;
            currentDevice.SaveInterval = (int)nudSaveInterval.Value; // 🎯 저장주기 저장
            currentDevice.StartAddress = (int)nudStartAddress.Value;
            currentDevice.DataLength = (int)nudDataLength.Value;
            currentDevice.SlaveId = (int)nudSlaveId.Value;
            currentDevice.IsActive = chkActive.Checked;

            // 항목 리스트
            currentDevice.Items.Clear();
            for (int i = 0; i < lbItems.Items.Count; i++)
            {
                currentDevice.Items.Add(new DeviceItem
                {
                    Index = i + 1,
                    Name = lbItems.Items[i].ToString()
                });
            }

            // 메모리 맵핑
            currentDevice.Mappings.Clear();
            foreach (var item in lbMappings.Items)
            {
                var parts = item.ToString().Split('#');
                if (parts.Length >= 2)
                {
                    currentDevice.Mappings.Add(new MemoryMapping
                    {
                        Address = int.Parse(parts[0]),
                        DataType = parts[1],
                        Format = parts.Length > 2 ? parts[2] : "1"
                    });
                }
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                XtraMessageBox.Show("설비명을 입력하세요.", "입력 오류");
                txtDeviceName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDeviceCode.Text))
            {
                XtraMessageBox.Show("설비코드를 입력하세요.", "입력 오류");
                txtDeviceCode.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtIPAddress.Text))
            {
                XtraMessageBox.Show("IP 주소를 입력하세요.", "입력 오류");
                txtIPAddress.Focus();
                return false;
            }

            // IP 주소 유효성 검사
            System.Net.IPAddress ip;
            if (!System.Net.IPAddress.TryParse(txtIPAddress.Text, out ip))
            {
                XtraMessageBox.Show("올바른 IP 주소를 입력하세요.", "입력 오류");
                txtIPAddress.Focus();
                return false;
            }

            // 🎯 저장주기 유효성 검사
            if (nudSaveInterval.Value < nudInterval.Value)
            {
                XtraMessageBox.Show("저장주기는 수집주기보다 크거나 같아야 합니다.", "입력 오류");
                nudSaveInterval.Focus();
                return false;
            }

            if (lbItems.Items.Count == 0)
            {
                XtraMessageBox.Show("최소 하나 이상의 항목을 추가하세요.", "입력 오류");
                return false;
            }

            if (lbMappings.Items.Count == 0)
            {
                XtraMessageBox.Show("최소 하나 이상의 메모리 맵핑을 추가하세요.", "입력 오류");
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            txtDeviceName.Text = "";
            txtDeviceCode.Text = "";
            txtIPAddress.Text = "";
            nudPort.Value = 502;
            nudInterval.Value = 10;
            nudSaveInterval.Value = 60; // 🎯 저장주기 기본값
            nudStartAddress.Value = 0;
            nudDataLength.Value = 10;
            nudSlaveId.Value = 1;
            chkActive.Checked = true;
            lbItems.Items.Clear();
            lbMappings.Items.Clear();
            txtItemName.Text = "";
            nudMappingAddress.Value = 0;
            cmbDataType.SelectedIndex = 0;
            txtFormat.Text = "1";
        }

        private void EnableForm(bool enable)
        {
            grpDevice.Enabled = enable;
            grpItems.Enabled = enable;
            grpMapping.Enabled = enable;
            btnSave.Enabled = enable;
            btnCancel.Enabled = enable;
            btnTest.Enabled = enable;
            btnAdd.Enabled = !enable;
            btnEdit.Enabled = !enable;
            btnDelete.Enabled = !enable;
            btnCopy.Enabled = !enable;
        }

        private void SaveSettings()
        {
            DeviceConfigManager.SaveDeviceSettings(deviceSettings);
            // 과도한 팝업 제거: 저장 완료 알림 생략
        }

        private ModbusDeviceSettings CopyDevice(ModbusDeviceSettings source)
        {
            var copy = new ModbusDeviceSettings
            {
                Id = Guid.NewGuid(), // 새로운 ID 생성
                DeviceName = source.DeviceName ?? "",
                DeviceCode = source.DeviceCode ?? "",
                IPAddress = source.IPAddress ?? "",
                Port = source.Port,
                Interval = source.Interval,
                SaveInterval = source.SaveInterval,
                StartAddress = source.StartAddress,
                DataLength = source.DataLength,
                SlaveId = source.SlaveId,
                IsActive = false, // 복사된 디바이스는 기본적으로 비활성화
                Items = new List<DeviceItem>(),
                Mappings = new List<MemoryMapping>()
            };

            // 항목 복사
            if (source.Items != null)
            {
                foreach (var item in source.Items)
                {
                    copy.Items.Add(new DeviceItem
                    {
                        Index = item.Index,
                        Name = item.Name ?? ""
                    });
                }
            }

            // 메모리 맵핑 복사
            if (source.Mappings != null)
            {
                foreach (var mapping in source.Mappings)
                {
                    copy.Mappings.Add(new MemoryMapping
                    {
                        Address = mapping.Address,
                        DataType = mapping.DataType ?? "B",
                        Format = mapping.Format ?? "1"
                    });
                }
            }

            return copy;
        }

        // 항목 추가/제거
        private void btnAddItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                XtraMessageBox.Show("항목명을 입력하세요.", "입력 오류");
                txtItemName.Focus();
                return;
            }

            lbItems.Items.Add(txtItemName.Text.Trim());
            txtItemName.Text = "";
            txtItemName.Focus();
        }

        private void btnRemoveItem_Click(object sender, EventArgs e)
        {
            if (lbItems.SelectedItem != null)
            {
                lbItems.Items.Remove(lbItems.SelectedItem);
            }
        }

        // 메모리 맵핑 추가/제거
        private void btnAddMapping_Click(object sender, EventArgs e)
        {
            string mapping = $"{nudMappingAddress.Value}#{cmbDataType.Text}#{txtFormat.Text}";
            lbMappings.Items.Add(mapping);

            nudMappingAddress.Value++;
            txtFormat.Text = "1";
        }

        private void btnRemoveMapping_Click(object sender, EventArgs e)
        {
            if (lbMappings.SelectedItem != null)
            {
                lbMappings.Items.Remove(lbMappings.SelectedItem);
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            // 연결 테스트
            var testForm = new ConnectionTestForm();
            testForm.TestConnection(txtIPAddress.Text, (int)nudPort.Value, (byte)nudSlaveId.Value);
            testForm.ShowDialog();
        }
    }
}