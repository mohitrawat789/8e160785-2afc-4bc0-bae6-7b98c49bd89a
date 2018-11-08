﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Parking.Common;
using Parking.Common.Enums;
using Flurl.Http;
using System.Threading.Tasks;

namespace Parking
{
    public partial class TicketDispenserServerForm : Form
    {
        
        private Timer _gridViewTicketDispenserDataFetchTimer = null;
        private Timer _gridViewManualPayStationDataFetchTimer = null;
        private bool _isTicketDispenserTaskInProgress = false;
        private bool _isManualPayStationTaskInProgress = false;
        List<string> _ticketDispenserUploadedRecords = new List<string>();
        List<string> _manualPayStationUploadedRecords = new List<string>();
        private const string _ManualPayStationDataUploadURL = "http://codeshow.in/api/";
        private const string _TicketDispenserDataUploadURL = "http://codeshow.in/api/";

        private readonly ParkingDatabaseFactory _parkingDatabaseFactory;

        public TicketDispenserServerForm()
        {
            InitializeComponent();
            _parkingDatabaseFactory = new ParkingDatabaseFactory(Common.Enums.Application.TickerDispenserServer);
        }
        
        private void TicketDispenserServerForm_Load(object sender, EventArgs e)
        {
            _gridViewTicketDispenserDataFetchTimer = new Timer() { Interval = 1000 };
            _gridViewTicketDispenserDataFetchTimer.Tick += RefreshTicketDispenserGridView;

            _gridViewManualPayStationDataFetchTimer = new Timer() { Interval = 1000 };
            _gridViewManualPayStationDataFetchTimer.Tick += RefreshManualPayStationGridView;

            GetMasterSettingsForTDClientDeviceConfig();
            GetMasterSettingsForMPSDeviceConfig();
        }
        /** TICKET DISPENSER DATA FETCH & UPLOAD **/
        private void RefreshTicketDispenserGridView(object sender, EventArgs e)
        {
            if (_isTicketDispenserTaskInProgress)
                return;

            _gridViewTicketDispenserDataFetchTimer.Start();
            LoadTicketDispenserGridView();
        }
        private void btnLoadTicketDispenserData_Click(object sender, EventArgs e)
        {
            btnLoadTicketDispenserData.Enabled = false;
            lblTicketDispenserStatus.Text = "Please wait while fetching data...";
            try
            {
                _isTicketDispenserTaskInProgress = true;
                LoadTicketDispenserGridView();

                if (_gridViewTicketDispenserDataFetchTimer != null)
                    _gridViewTicketDispenserDataFetchTimer.Start();

            }
            catch (Exception ex)
            {
                btnLoadTicketDispenserData.Enabled = true;
            }
        }
        public void LoadTicketDispenserGridView()
        {
            try
            {
                var records = _parkingDatabaseFactory.GetVehicleEntryDataForWebServerUpload();
                if (records.Rows.Count > 0)
                {
                    gridViewTicketDispenser.DataSource = records;
                    UploadTickerDispenserDataToServer();
                }
                else
                {
                    lblTicketDispenserStatus.Text = "No result found";
                }
            }
            catch (Exception ex)
            {
                lblTicketDispenserStatus.Text = "Problem in fetching ticket dispenser data. Please try again later";
                return;
            }
        }
        public void UploadTickerDispenserDataToServer()
        {
            for (var i = 0; i <= gridViewTicketDispenser.Rows.Count - 1; i++)
            {
                var row = gridViewTicketDispenser.Rows[i];
                var parkingId = row.Cells[0].Value.ToString();
                var tdClientDeviceId = row.Cells[1].Value.ToString();
                var ticketNumber = row.Cells[2].Value.ToString();
                var validationNumber = row.Cells[3].Value.ToString();
                var vehicleNumber = row.Cells[4].Value.ToString();
                var vehicleType = row.Cells[5].Value.ToString();
                var entryTime = row.Cells[6].ToString();

                this.Invoke((MethodInvoker)delegate {
                    var response = _TicketDispenserDataUploadURL
                                           .PostUrlEncodedAsync(new
                                           {
                                               ParkingId = parkingId,
                                               TdClientDeviceId = tdClientDeviceId,
                                               TicketNumber = ticketNumber,
                                               ValidationNumber = validationNumber,
                                               VehicleNumber = vehicleNumber,
                                               VehicleType = vehicleType,
                                               EntryTime = entryTime
                                           })
                                           .ReceiveString();
                    if (response != null)
                    {
                        _parkingDatabaseFactory.UpdateVehicleEntryWebServerUploadStatus(parkingId);
                        _ticketDispenserUploadedRecords.Add(ticketNumber);
                        gridViewTicketDispenser.Rows.Remove(row);
                        lblTicketDispenserStatus.Text = "Uploaded " + _ticketDispenserUploadedRecords.Count + " of " + gridViewTicketDispenser.Rows.Count + " Records";

                        if (_ticketDispenserUploadedRecords.Count == gridViewTicketDispenser.Rows.Count)
                        {
                            _isTicketDispenserTaskInProgress = false;
                            _ticketDispenserUploadedRecords = new List<string>();
                            lblTicketDispenserDataLastUpdated.Text = string.Format(@"Last Updated: {0}", DateTime.Now.ToString());
                        }
                    }
                });
            }
        }

        /** MANUAL PAY STATION DATA FETCH & UPLOAD **/
        private void RefreshManualPayStationGridView(object sender, EventArgs e)
        {
            if (_isManualPayStationTaskInProgress)
                return;

            _gridViewManualPayStationDataFetchTimer.Start();
            LoadManualPayStationGridView();
        }
        private void btnLoadManualPayStationData_Click(object sender, EventArgs e)
        {
            btnLoadManualPayStationData.Enabled = false;
            lblTicketDispenserStatus.Text = "Please wait while fetching data...";
            try
            {
                _isManualPayStationTaskInProgress = true;
                LoadManualPayStationGridView();

                if (_gridViewManualPayStationDataFetchTimer != null)
                    _gridViewManualPayStationDataFetchTimer.Start();
            }
            catch (Exception ex)
            {
                btnLoadManualPayStationData.Enabled = true;
            }
        }
        public void LoadManualPayStationGridView()
        {
            try
            {
                var records = _parkingDatabaseFactory.GetVehicleExitDataForWebServerUpload();
                if(records.Rows.Count > 0)
                {
                    gridViewManualPaySation.DataSource = records;
                    UploadManualPayStationDataToServer();
                }
                else
                {
                    lblTicketDispenserStatus.Text = "No result found";
                }
            }
            catch (Exception ex)
            {
                lblTicketDispenserStatus.Text = "Problem in fetching manual pay station data. Please try again later";
                return;
            }
        }
        public void UploadManualPayStationDataToServer()
        {
            for (var i = 0; i <= gridViewManualPaySation.Rows.Count - 1; i++)
            {
                var row = gridViewManualPaySation.Rows[i];
                var parkingId = row.Cells[0].Value.ToString();
                var mpsDeviceId = row.Cells[1].Value.ToString();
                var ticketNumber = row.Cells[2].Value.ToString();
                var entryTime = row.Cells[3].Value.ToString();
                var parkingDuration = row.Cells[4].Value.ToString();
                var parkingCharge = row.Cells[5].Value.ToString();
                var penalityCharge = row.Cells[6].Value.ToString();
                var totalAmount = row.Cells[7].Value.ToString();

                this.Invoke((MethodInvoker)delegate {
                    var response = _ManualPayStationDataUploadURL
                                            .PostUrlEncodedAsync(new
                                            {
                                                ParkingId = parkingId,
                                                MpsDeviceId = mpsDeviceId,
                                                TicketNumber = ticketNumber,
                                                EntryTime = entryTime,
                                                ParkingDuration = parkingDuration,
                                                ParkingCharge = parkingCharge,
                                                PenalityCharge = penalityCharge,
                                                TotalAmount = totalAmount
                                            })
                                            .ReceiveString();
                    if (response != null)
                    {
                        _parkingDatabaseFactory.UpdateVehicleExitWebServerUploadStatus(parkingId);
                        _manualPayStationUploadedRecords.Add(ticketNumber);
                        gridViewManualPaySation.Rows.Remove(row);
                        lblManualPayStationStatus.Text = "Uploaded " + _manualPayStationUploadedRecords.Count + " of " + gridViewManualPaySation.Rows.Count + " Records";

                        if (_manualPayStationUploadedRecords.Count == gridViewManualPaySation.Rows.Count)
                        {
                            _isManualPayStationTaskInProgress = false;
                            _manualPayStationUploadedRecords = new List<string>();
                            lblManualPaySationDataLastUpdated.Text = string.Format(@"Last Updated: {0}", DateTime.Now.ToString());
                        }
                    }
                });
            }
        }

        /** TICKET DISPENSER CONFIGURATION SETTINGS **/
        private void btnTDClientGenerateDeviceConfig_Click(object sender, EventArgs e)
        {
            try
            {
                var TDClientDeviceId = txtTDClientDeviceId.Text.Trim().ToString();
                var TDClientUserId = txtTDClientUserId.Text.Trim().ToString();
                var TDClientPassword = txtTDClientPassword.Text.Trim().ToString();
                var TDClientLongLat = txtTDClientLongLat.Text.Trim().ToString();
                var TDClientPLCBoardNumber = txtTDClientPLCBoardNumber.Text.Trim().ToString();
                var TDClientDriverCameraIPAddress = txtTDClientDriverCameraIPAddress.Text.Trim().ToString();
                var TDClientDriverCameraUsername = txtTDClientDriverCameraUsername.Text.Trim().ToString();
                var TDClientDriverCameraPassword = txtTDClientDriverCameraPassword.Text.Trim().ToString();
                var TDClientVehicleCameraIPAddress = txtTDClientVehicleCameraIPAddress.Text.Trim().ToString();
                var TDClientVehicleCameraUsername = txtTDClientVehicleCameraUsername.Text.Trim().ToString();
                var TDClientVehicleCameraPassword = txtTDClientVehicleCameraPassword.Text.Trim().ToString();
                var TDClientTDServerIPAddress = txtTDClientTDServerIPAddress.Text.Trim().ToString();
                var TDClientTDServerPortNumber = txtTDClientTDServerPortNumber.Text.Trim().ToString();
                var TDClientTDServerUsername = txtTDClientTDServerUsername.Text.Trim().ToString();
                var TDClientTDServerPassword = txtTDClientTDServerPassword.Text.Trim().ToString();
                var TDClientFourWheelerParkingSpace = txtTDClientFourWheelerParkingSpace.Text.Trim().ToString();
                var TDClientTwoWheelerParkingSpace = txtTDClientTwoWheelerParkingSpace.Text.Trim().ToString();

                TickerDispenserClientSettings setting = new TickerDispenserClientSettings();
                setting.DeviceId = TDClientDeviceId;
                setting.UserId = TDClientUserId;
                setting.Password = TDClientPassword;
                setting.LongLat = TDClientLongLat;
                setting.PLCBoardPortNumber = TDClientPLCBoardNumber;
                setting.DriverCameraIPAddress = TDClientDriverCameraIPAddress;
                setting.DriverCameraUsername = TDClientDriverCameraUsername;
                setting.DriverCameraPassword = TDClientDriverCameraPassword;
                setting.VehicleCameraIPAddress = TDClientVehicleCameraIPAddress;
                setting.VehicleCameraUsername = TDClientVehicleCameraUsername;
                setting.VehicleCameraPassword = TDClientVehicleCameraPassword;
                setting.TdServerIPAddress = TDClientTDServerIPAddress;
                setting.TdServerPort = TDClientTDServerPortNumber;
                setting.TdServerUsername = TDClientTDServerUsername;
                setting.TdServerPassword = TDClientTDServerPassword;
                setting.FourWheelerParkingSpace = TDClientFourWheelerParkingSpace;
                setting.TwoWheelerParkingSpace = TDClientTwoWheelerParkingSpace;

                _parkingDatabaseFactory.UpdateMasterSettingsForTDClientDeviceConfig(
                    TDClientDeviceId,
                    TDClientUserId,
                    TDClientPassword,
                    TDClientLongLat,
                    TDClientPLCBoardNumber,
                    TDClientDriverCameraIPAddress,
                    TDClientDriverCameraUsername,
                    TDClientDriverCameraPassword,
                    TDClientVehicleCameraIPAddress,
                    TDClientVehicleCameraUsername,
                    TDClientVehicleCameraPassword,
                    TDClientTDServerIPAddress,
                    TDClientTDServerPortNumber,
                    TDClientTDServerUsername,
                    TDClientTDServerPassword,
                    TDClientFourWheelerParkingSpace,
                    TDClientTwoWheelerParkingSpace);

                SaveFileDialog sfdTickerDispenser = new SaveFileDialog
                {
                    InitialDirectory = @"C:\",
                    Title = "Save Manual Pay Station (DeviceConfig.json)",
                    CheckFileExists = false,
                    CheckPathExists = false,
                    DefaultExt = "json",
                    FileName = "DeviceConfig.json",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };

                if (sfdTickerDispenser.ShowDialog() == DialogResult.OK)
                {
                    string ticketDispenserDeviceConfigFileName = sfdTickerDispenser.FileName;
                    string json = JsonConvert.SerializeObject(setting);
                    File.WriteAllText(ticketDispenserDeviceConfigFileName, json);
                    MessageBox.Show("Ticket dispenser DeviceConfig.json successfully created");
                }
            }
            catch (Exception exception)
            {
                FileLogger.Log($"Problem saving ticket dispenser DeviceConfig.json as : {exception.Message} ");
            }
        }
        public void GetMasterSettingsForTDClientDeviceConfig()
        {
            try
            {
                DataRow record = _parkingDatabaseFactory.GetMasterSettingsForTDClientDeviceConfig();
                txtTDClientDeviceId.Text = record[0].ToString();
                txtTDClientUserId.Text = record[1].ToString();
                txtTDClientPassword.Text = record[2].ToString();
                txtTDClientLongLat.Text = record[3].ToString();
                txtTDClientPLCBoardNumber.Text = record[4].ToString();
                txtTDClientDriverCameraIPAddress.Text = record[5].ToString();
                txtTDClientDriverCameraUsername.Text = record[6].ToString();
                txtTDClientDriverCameraPassword.Text = record[7].ToString();
                txtTDClientVehicleCameraIPAddress.Text = record[8].ToString();
                txtTDClientVehicleCameraUsername.Text = record[9].ToString();
                txtTDClientVehicleCameraPassword.Text = record[10].ToString();
                txtTDClientTDServerIPAddress.Text = record[11].ToString();
                txtTDClientTDServerPortNumber.Text = record[12].ToString();
                txtTDClientTDServerUsername.Text = record[13].ToString();
                txtTDClientTDServerPassword.Text = record[14].ToString();
                txtTDClientFourWheelerParkingSpace.Text = record[15].ToString();
                txtTDClientTwoWheelerParkingSpace.Text = record[16].ToString();
            }
            catch (Exception ex){}
        }

        /** MANUAL PAY STATION CONFIGURATION SETTINGS **/
        private void btnMPSGenerateDeviceConfig_Click(object sender, EventArgs e)
        {
            try
            {
                string MPSDeviceId = txtMPSDeviceId.Text.Trim().ToString();
                string MPSUserId = txtMPSUserId.Text.Trim().ToString();
                string MPSPassword = txtMPSPassword.Text.Trim().ToString();
                string MPSTDServerIPAddress = txtMPSTDServerIPAddress.Text.Trim().ToString();
                string MPSTDServerPortNumber = txtMPSTDServerPortNumber.Text.Trim().ToString();
                string MPSTDServerUsername = txtMPSTDServerUsername.Text.Trim().ToString();
                string MPSTDServerPassword = txtMPSTDServerPassword.Text.Trim().ToString();
                string VehicleStatusPassword = txtVehicleStatusPassword.Text.Trim().ToString();
                string MPSFourWheelerParkingSpace = txtMPSFourWheelerParkingSpace.Text.Trim().ToString();
                string MPSTwoWheelerParkingSpace = txtMPSTwoWheelerParkingSpace.Text.Trim().ToString();
                string MPSFourWheelerParkingRate = txtMPSFourWheelerParkingRate.Text.Trim().ToString();
                string MPSTwoWheelerParkingRate = txtMPSTwoWheelerParkingRate.Text.Trim().ToString();
                string MPSLostTicketPenality = txtMPSLostTicketPenality.Text.Trim().ToString();

                _parkingDatabaseFactory.UpdateMasterSettingsForMPSDeviceConfig(
                        MPSDeviceId,
                        MPSUserId,
                        MPSPassword,
                        MPSTDServerIPAddress,
                        MPSTDServerPortNumber,
                        MPSTDServerUsername,
                        MPSTDServerPassword,
                        VehicleStatusPassword,
                        MPSFourWheelerParkingSpace,
                        MPSTwoWheelerParkingSpace,
                        MPSFourWheelerParkingRate,
                        MPSTwoWheelerParkingRate,
                        MPSLostTicketPenality);

                ManualPayStationSettings setting = new ManualPayStationSettings();
                setting.DeviceId = MPSDeviceId;
                setting.UserId = MPSUserId;
                setting.Password = MPSPassword;
                setting.TdServerIPAddress = MPSTDServerIPAddress;
                setting.TdServerPort = MPSTDServerPortNumber;
                setting.TdServerUsername = MPSTDServerUsername;
                setting.TdServerPassword = MPSTDServerPassword;
                setting.VehicleStatusPassword = VehicleStatusPassword;
                setting.FourWheelerParkingSpace = MPSFourWheelerParkingSpace;
                setting.TwoWheelerParkingSpace = MPSTwoWheelerParkingSpace;
                setting.FourWheelerParkingRate = MPSFourWheelerParkingRate;
                setting.TwoWheelerParkingRate = MPSTwoWheelerParkingRate;
                setting.LostTicketPenality = MPSLostTicketPenality;

                SaveFileDialog sfdManualPayStation = new SaveFileDialog
                {
                    InitialDirectory = @"C:\",
                    Title = "Save Ticket Dispenser (DeviceConfig.json)",
                    CheckFileExists = false,
                    CheckPathExists = false,
                    DefaultExt = "json",
                    FileName = "DeviceConfig.json",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };

                if (sfdManualPayStation.ShowDialog() == DialogResult.OK)
                {
                    string manualPayStationDeviceConfigFileName = sfdManualPayStation.FileName;
                    string json = JsonConvert.SerializeObject(setting);
                    File.WriteAllText(manualPayStationDeviceConfigFileName, json);
                    MessageBox.Show("Manual Pay Station DeviceConfig.json successfully created");
                }
            }
            catch (Exception exception)
            {
                FileLogger.Log($"Problem saving manual pay station DeviceConfig.json as : {exception.Message} ");
            }
        }
        public void GetMasterSettingsForMPSDeviceConfig()
        {
            try
            {
                DataRow record = _parkingDatabaseFactory.GetMasterSettingsForMPSDeviceConfig();
                txtMPSDeviceId.Text = record[0].ToString();
                txtMPSUserId.Text = record[1].ToString();
                txtMPSPassword.Text = record[2].ToString();
                txtMPSTDServerIPAddress.Text = record[3].ToString();
                txtMPSTDServerPortNumber.Text = record[4].ToString();
                txtMPSTDServerUsername.Text = record[5].ToString();
                txtMPSTDServerPassword.Text = record[6].ToString();
                txtVehicleStatusPassword.Text = record[7].ToString();
                txtMPSFourWheelerParkingSpace.Text = record[8].ToString();
                txtMPSTwoWheelerParkingSpace.Text = record[9].ToString();
                txtMPSFourWheelerParkingRate.Text = record[10].ToString();
                txtMPSTwoWheelerParkingRate.Text = record[11].ToString();
                txtMPSLostTicketPenality.Text = record[12].ToString();
            }
            catch (Exception ex) {

            }
        }

    }
}
