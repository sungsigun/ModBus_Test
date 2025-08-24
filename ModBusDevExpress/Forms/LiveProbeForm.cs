using System;
using System.Windows.Forms;
using ModBusDevExpress.Models;

namespace ModBusDevExpress.Forms
{
    public partial class LiveProbeForm : Form
    {
        private ModbusDeviceSettings _deviceSettings;

        public LiveProbeForm(ModbusDeviceSettings deviceSettings)
        {
            InitializeComponent();
            _deviceSettings = deviceSettings;
        }
    }
}