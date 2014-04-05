using Microsoft.Win32.SafeHandles;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices; 

///<summary>
/// Project: GenericHid
/// 
/// ***********************************************************************
/// Software License Agreement
///
/// Licensor grants any person obtaining a copy of this software ("You") 
/// a worldwide, royalty-free, non-exclusive license, for the duration of 
/// the copyright, free of charge, to store and execute the Software in a 
/// computer system and to incorporate the Software or any portion of it 
/// in computer programs You write.   
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// ***********************************************************************
/// 
/// Author             
/// Jan Axelson        
/// 
/// This software was written using Visual Studio 2008 Standard Edition and building   
/// for the .NET Framework 2.0.
/// 
/// Purpose: 
/// Demonstrates USB communications with a generic HID-class device
/// 
/// Requirements:
/// Windows 98 or later and an attached USB generic Human Interface Device (HID).
/// 
/// Description:
/// Finds an attached device that matches the vendor and product IDs in the form's 
/// text boxes.
/// 
/// Retrieves the device's capabilities.
/// Sends and requests HID reports.
/// 
/// Uses RegisterDeviceNotification() and WM_DEVICE_CHANGE messages
/// to detect when a device is attached or removed.
/// RegisterDeviceNotification doesn't work under Windows 98 (not sure why).
/// 
/// A list box displays the data sent and received,
/// along with error and status messages.
/// Combo boxes select data to send, and 1-time or timed, periodic transfers.
/// 
/// You can change the size of the host's Input report buffer and request to use control
/// transfers only to exchange Input and Output reports.
/// 
/// To view additional debugging messages, in the Visual Studio development environment,
/// select the Debug build (Build > Configuration Manager > Active Solution Configuration)
/// and view the Output window (View > Other Windows > Output)
/// 
/// The application uses an asynchronous FileStream to read
/// Input reports asynchronously, so the application's main thread doesn't have to
/// wait for the device to return an Input report when the HID driver's buffer is empty. 
/// 
/// For code that finds a device and opens handles to it, see the FindTheHid routine in frmMain.cs.
/// For code that reads from the device, search for fileStreamDeviceData.BeginRead and GetInputReportData
/// in FrmMain.cs and GetInputReportViaControlTransfer and GetFeatureReport in Hid.cs.
/// For code that writes to the device, search for fileStreamDeviceData.Write in FrmMain.cs and 
/// SendOutputReportViaControlTransfer and SendFeatureReport in Hid.cs.
/// 
/// This project includes the following modules:
/// 
/// GenericHid.cs - runs the application.
/// FrmMain.cs - routines specific to the form.
/// Hid.cs - routines specific to HID communications.
/// DeviceManagement.cs - routines for obtaining a handle to a device from its GUID
/// and receiving device notifications. This routines are not specific to HIDs.
/// Debugging.cs - contains a routine for displaying API error messages.
/// HidDeclarations.cs - Declarations for API functions used by Hid.cs.
/// FileIODeclarations.cs - Declarations for file-related API functions.
/// DeviceManagementDeclarations.cs - Declarations for API functions used by DeviceManagement.cs.
/// DebuggingDeclarations.cs - Declarations for API functions used by Debugging.cs.
/// 
/// Companion device firmware for several device CPUs is available from www.Lvr.com/hidpage.htm
/// You can use any generic HID (not a system mouse or keyboard) that sends and receives reports.
/// 
/// V5.0
/// 3/30/11
/// Replaced ReadFile and WriteFile with FileStreams. Thanks to Joe Dunne and John on my Ports forum for tips on this.
/// Simplified Hid.cs.
/// Replaced the form timer with a system timer.
/// 
/// V4.6
/// 1/12/10
/// Supports Vendor IDs and Product IDs up to FFFFh.
///
/// V4.52
/// 11/10/09
/// Changed HIDD_ATTRIBUTES to use UInt16
/// 
/// V4.51
/// 2/11/09
/// Moved Free_ and similar to Finally blocks to ensure they execute.
/// 
/// V4.5
/// 2/9/09
/// Changes to support 64-bit systems, memory management, and other corrections. 
/// Big thanks to Peter Nielsen.
/// 
/// For more information about HIDs and USB, and additional example device firmware to use
/// with this application, visit Lakeview Research at http://www.Lvr.com .
/// Send comments, bug reports, etc. to jan@Lvr.com 
/// 
/// </summary>

using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Timers;

using System.Windows.Forms;

namespace GenericHid
{
    internal class FrmMain 
        
        : System.Windows.Forms.Form 
    {         
        #region '"Windows Form Designer generated code "' 
        public FrmMain() : base() 
        {               
            // This call is required by the Windows Form Designer.
            InitializeComponent(); 
        } 
        // Form overrides dispose to clean up the component list.
        protected override void Dispose( bool Disposing ) 
        { 
            if ( Disposing ) 
            { 
                if ( !( components == null ) ) 
                { 
                    components.Dispose(); 
                } 
            } 
            base.Dispose( Disposing ); 
        } 
        
        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components; 
        public System.Windows.Forms.ToolTip ToolTip1;         
        public System.Windows.Forms.Button cmdContinuous; 
        public System.Windows.Forms.Button cmdOnce; 
        public System.Windows.Forms.GroupBox fraSendAndReceive; 
        public System.Windows.Forms.TextBox txtBytesReceived; 
        public System.Windows.Forms.GroupBox fraBytesReceived; 
        public System.Windows.Forms.CheckBox chkAutoincrement; 
        public System.Windows.Forms.ComboBox cboByte1; 
        public System.Windows.Forms.ComboBox cboByte0;
		public System.Windows.Forms.GroupBox fraBytesToSend; 
        public System.Windows.Forms.ListBox lstResults; 
        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.
        // Do not modify it using the code editor.
        internal System.Windows.Forms.GroupBox fraReportTypes; 
        internal System.Windows.Forms.RadioButton optInputOutput; 
        internal System.Windows.Forms.RadioButton optFeature; 
        internal System.Windows.Forms.GroupBox fraInputReportBufferSize; 
        internal System.Windows.Forms.TextBox txtInputReportBufferSize; 
        internal System.Windows.Forms.Button cmdInputReportBufferSize; 
        internal System.Windows.Forms.GroupBox fraDeviceIdentifiers; 
        internal System.Windows.Forms.Label lblVendorID; 
        internal System.Windows.Forms.TextBox txtVendorID; 
        internal System.Windows.Forms.Label lblProductID; 
        internal System.Windows.Forms.TextBox txtProductID;
		internal System.Windows.Forms.Button cmdFindDevice; 
        internal System.Windows.Forms.CheckBox chkUseControlTransfersOnly; 
       
        [ System.Diagnostics.DebuggerStepThrough() ]
        private void InitializeComponent() 
        {
			this.components = new System.ComponentModel.Container();
			this.ToolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.fraSendAndReceive = new System.Windows.Forms.GroupBox();
			this.cmdContinuous = new System.Windows.Forms.Button();
			this.cmdOnce = new System.Windows.Forms.Button();
			this.fraBytesReceived = new System.Windows.Forms.GroupBox();
			this.txtBytesReceived = new System.Windows.Forms.TextBox();
			this.fraBytesToSend = new System.Windows.Forms.GroupBox();
			this.chkAutoincrement = new System.Windows.Forms.CheckBox();
			this.cboByte1 = new System.Windows.Forms.ComboBox();
			this.cboByte0 = new System.Windows.Forms.ComboBox();
			
			this.lstResults = new System.Windows.Forms.ListBox();
			this.fraReportTypes = new System.Windows.Forms.GroupBox();
			this.chkUseControlTransfersOnly = new System.Windows.Forms.CheckBox();
			this.optFeature = new System.Windows.Forms.RadioButton();
			this.optInputOutput = new System.Windows.Forms.RadioButton();
			this.fraInputReportBufferSize = new System.Windows.Forms.GroupBox();
			this.cmdInputReportBufferSize = new System.Windows.Forms.Button();
			this.txtInputReportBufferSize = new System.Windows.Forms.TextBox();
			this.fraDeviceIdentifiers = new System.Windows.Forms.GroupBox();
			this.txtProductID = new System.Windows.Forms.TextBox();
			this.lblProductID = new System.Windows.Forms.Label();
			this.txtVendorID = new System.Windows.Forms.TextBox();
			this.lblVendorID = new System.Windows.Forms.Label();
			this.cmdFindDevice = new System.Windows.Forms.Button();
			
			this.fraSendAndReceive.SuspendLayout();
			this.fraBytesReceived.SuspendLayout();
			this.fraBytesToSend.SuspendLayout();
			this.fraReportTypes.SuspendLayout();
			this.fraInputReportBufferSize.SuspendLayout();
			this.fraDeviceIdentifiers.SuspendLayout();
			this.SuspendLayout();			
			// 
			// fraSendAndReceive
			// 
			this.fraSendAndReceive.BackColor = System.Drawing.SystemColors.Control;
			this.fraSendAndReceive.Controls.Add(this.cmdContinuous);
			this.fraSendAndReceive.Controls.Add(this.cmdOnce);
			this.fraSendAndReceive.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.fraSendAndReceive.ForeColor = System.Drawing.SystemColors.ControlText;
			this.fraSendAndReceive.Location = new System.Drawing.Point(608, 128);
			this.fraSendAndReceive.Name = "fraSendAndReceive";
			this.fraSendAndReceive.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.fraSendAndReceive.Size = new System.Drawing.Size(176, 136);
			this.fraSendAndReceive.TabIndex = 7;
			this.fraSendAndReceive.TabStop = false;
			this.fraSendAndReceive.Text = "Send and Receive Data";
			// 
			// cmdContinuous
			// 
			this.cmdContinuous.BackColor = System.Drawing.SystemColors.Control;
			this.cmdContinuous.Cursor = System.Windows.Forms.Cursors.Default;
			this.cmdContinuous.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cmdContinuous.ForeColor = System.Drawing.SystemColors.ControlText;
			this.cmdContinuous.Location = new System.Drawing.Point(16, 88);
			this.cmdContinuous.Name = "cmdContinuous";
			this.cmdContinuous.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.cmdContinuous.Size = new System.Drawing.Size(121, 36);
			this.cmdContinuous.TabIndex = 9;
			this.cmdContinuous.Text = "Continuous";
			this.cmdContinuous.UseVisualStyleBackColor = false;
			this.cmdContinuous.Click += new System.EventHandler(this.cmdContinuous_Click);
			// 
			// cmdOnce
			// 
			this.cmdOnce.BackColor = System.Drawing.SystemColors.Control;
			this.cmdOnce.Cursor = System.Windows.Forms.Cursors.Default;
			this.cmdOnce.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cmdOnce.ForeColor = System.Drawing.SystemColors.ControlText;
			this.cmdOnce.Location = new System.Drawing.Point(16, 32);
			this.cmdOnce.Name = "cmdOnce";
			this.cmdOnce.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.cmdOnce.Size = new System.Drawing.Size(121, 36);
			this.cmdOnce.TabIndex = 8;
			this.cmdOnce.Text = "Once";
			this.cmdOnce.UseVisualStyleBackColor = false;
			this.cmdOnce.Click += new System.EventHandler(this.cmdOnce_Click);
			// 
			// fraBytesReceived
			// 
			this.fraBytesReceived.BackColor = System.Drawing.SystemColors.Control;
			this.fraBytesReceived.Controls.Add(this.txtBytesReceived);
			this.fraBytesReceived.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.fraBytesReceived.ForeColor = System.Drawing.SystemColors.ControlText;
			this.fraBytesReceived.Location = new System.Drawing.Point(208, 128);
			this.fraBytesReceived.Name = "fraBytesReceived";
			this.fraBytesReceived.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.fraBytesReceived.Size = new System.Drawing.Size(128, 136);
			this.fraBytesReceived.TabIndex = 4;
			this.fraBytesReceived.TabStop = false;
			this.fraBytesReceived.Text = "Bytes Received";
			// 
			// txtBytesReceived
			// 
			this.txtBytesReceived.AcceptsReturn = true;
			this.txtBytesReceived.BackColor = System.Drawing.SystemColors.Window;
			this.txtBytesReceived.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.txtBytesReceived.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtBytesReceived.ForeColor = System.Drawing.SystemColors.WindowText;
			this.txtBytesReceived.Location = new System.Drawing.Point(16, 24);
			this.txtBytesReceived.MaxLength = 0;
			this.txtBytesReceived.Multiline = true;
			this.txtBytesReceived.Name = "txtBytesReceived";
			this.txtBytesReceived.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.txtBytesReceived.Size = new System.Drawing.Size(72, 96);
			this.txtBytesReceived.TabIndex = 5;
			// 
			// fraBytesToSend
			// 
			this.fraBytesToSend.BackColor = System.Drawing.SystemColors.Control;
			this.fraBytesToSend.Controls.Add(this.chkAutoincrement);
			this.fraBytesToSend.Controls.Add(this.cboByte1);
			this.fraBytesToSend.Controls.Add(this.cboByte0);
			this.fraBytesToSend.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.fraBytesToSend.ForeColor = System.Drawing.SystemColors.ControlText;
			this.fraBytesToSend.Location = new System.Drawing.Point(16, 128);
			this.fraBytesToSend.Name = "fraBytesToSend";
			this.fraBytesToSend.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.fraBytesToSend.Size = new System.Drawing.Size(181, 136);
			this.fraBytesToSend.TabIndex = 1;
			this.fraBytesToSend.TabStop = false;
			this.fraBytesToSend.Text = "Bytes to Send";
			// 
			// chkAutoincrement
			// 
			this.chkAutoincrement.BackColor = System.Drawing.SystemColors.Control;
			this.chkAutoincrement.Cursor = System.Windows.Forms.Cursors.Default;
			this.chkAutoincrement.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.chkAutoincrement.ForeColor = System.Drawing.SystemColors.ControlText;
			this.chkAutoincrement.Location = new System.Drawing.Point(8, 96);
			this.chkAutoincrement.Name = "chkAutoincrement";
			this.chkAutoincrement.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.chkAutoincrement.Size = new System.Drawing.Size(201, 35);
			this.chkAutoincrement.TabIndex = 6;
			this.chkAutoincrement.Text = "Autoincrement values";
			this.chkAutoincrement.UseVisualStyleBackColor = false;
			// 
			// cboByte1
			// 
			this.cboByte1.BackColor = System.Drawing.SystemColors.Window;
			this.cboByte1.Cursor = System.Windows.Forms.Cursors.Default;
			this.cboByte1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboByte1.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cboByte1.ForeColor = System.Drawing.SystemColors.WindowText;
			this.cboByte1.Location = new System.Drawing.Point(8, 64);
			this.cboByte1.Name = "cboByte1";
			this.cboByte1.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.cboByte1.Size = new System.Drawing.Size(101, 22);
			this.cboByte1.TabIndex = 3;
			// 
			// cboByte0
			// 
			this.cboByte0.BackColor = System.Drawing.SystemColors.Window;
			this.cboByte0.Cursor = System.Windows.Forms.Cursors.Default;
			this.cboByte0.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboByte0.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.cboByte0.ForeColor = System.Drawing.SystemColors.WindowText;
			this.cboByte0.Location = new System.Drawing.Point(8, 24);
			this.cboByte0.Name = "cboByte0";
			this.cboByte0.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.cboByte0.Size = new System.Drawing.Size(101, 22);
			this.cboByte0.TabIndex = 2;
			
			// 
			// lstResults
			// 
			this.lstResults.BackColor = System.Drawing.SystemColors.Window;
			this.lstResults.Cursor = System.Windows.Forms.Cursors.Default;
			this.lstResults.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lstResults.ForeColor = System.Drawing.SystemColors.WindowText;
			this.lstResults.HorizontalScrollbar = true;
			this.lstResults.ItemHeight = 14;
			this.lstResults.Location = new System.Drawing.Point(12, 275);
			this.lstResults.Name = "lstResults";
			this.lstResults.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.lstResults.Size = new System.Drawing.Size(760, 270);
			this.lstResults.TabIndex = 0;
			// 
			// fraReportTypes
			// 
			this.fraReportTypes.Controls.Add(this.chkUseControlTransfersOnly);
			this.fraReportTypes.Controls.Add(this.optFeature);
			this.fraReportTypes.Controls.Add(this.optInputOutput);
			this.fraReportTypes.Location = new System.Drawing.Point(344, 128);
			this.fraReportTypes.Name = "fraReportTypes";
			this.fraReportTypes.Size = new System.Drawing.Size(248, 104);
			this.fraReportTypes.TabIndex = 8;
			this.fraReportTypes.TabStop = false;
			this.fraReportTypes.Text = "Report Options";
			// 
			// chkUseControlTransfersOnly
			// 
			this.chkUseControlTransfersOnly.Location = new System.Drawing.Point(24, 48);
			this.chkUseControlTransfersOnly.Name = "chkUseControlTransfersOnly";
			this.chkUseControlTransfersOnly.Size = new System.Drawing.Size(200, 24);
			this.chkUseControlTransfersOnly.TabIndex = 2;
			this.chkUseControlTransfersOnly.Text = "Use ControlTransfers Only";
			// 
			// optFeature
			// 
			this.optFeature.Location = new System.Drawing.Point(8, 80);
			this.optFeature.Name = "optFeature";
			this.optFeature.Size = new System.Drawing.Size(248, 24);
			this.optFeature.TabIndex = 1;
			this.optFeature.Text = "Exchange Feature Reports";
			// 
			// optInputOutput
			// 
			this.optInputOutput.Checked = true;
			this.optInputOutput.Location = new System.Drawing.Point(8, 24);
			this.optInputOutput.Name = "optInputOutput";
			this.optInputOutput.Size = new System.Drawing.Size(256, 24);
			this.optInputOutput.TabIndex = 0;
			this.optInputOutput.TabStop = true;
			this.optInputOutput.Text = "Exchange Input and Output Reports";
			// 
			// fraInputReportBufferSize
			// 
			this.fraInputReportBufferSize.Controls.Add(this.cmdInputReportBufferSize);
			this.fraInputReportBufferSize.Controls.Add(this.txtInputReportBufferSize);
			this.fraInputReportBufferSize.Location = new System.Drawing.Point(248, 16);
			this.fraInputReportBufferSize.Name = "fraInputReportBufferSize";
			this.fraInputReportBufferSize.Size = new System.Drawing.Size(208, 96);
			this.fraInputReportBufferSize.TabIndex = 9;
			this.fraInputReportBufferSize.TabStop = false;
			this.fraInputReportBufferSize.Text = "Input Report Buffer Size";
			// 
			// cmdInputReportBufferSize
			// 
			this.cmdInputReportBufferSize.Location = new System.Drawing.Point(96, 32);
			this.cmdInputReportBufferSize.Name = "cmdInputReportBufferSize";
			this.cmdInputReportBufferSize.Size = new System.Drawing.Size(96, 56);
			this.cmdInputReportBufferSize.TabIndex = 1;
			this.cmdInputReportBufferSize.Text = "Change Buffer Size";
			this.cmdInputReportBufferSize.Click += new System.EventHandler(this.cmdInputReportBufferSize_Click);
			// 
			// txtInputReportBufferSize
			// 
			this.txtInputReportBufferSize.Location = new System.Drawing.Point(16, 40);
			this.txtInputReportBufferSize.Name = "txtInputReportBufferSize";
			this.txtInputReportBufferSize.Size = new System.Drawing.Size(56, 20);
			this.txtInputReportBufferSize.TabIndex = 0;
			// 
			// fraDeviceIdentifiers
			// 
			this.fraDeviceIdentifiers.Controls.Add(this.txtProductID);
			this.fraDeviceIdentifiers.Controls.Add(this.lblProductID);
			this.fraDeviceIdentifiers.Controls.Add(this.txtVendorID);
			this.fraDeviceIdentifiers.Controls.Add(this.lblVendorID);
			this.fraDeviceIdentifiers.Location = new System.Drawing.Point(16, 16);
			this.fraDeviceIdentifiers.Name = "fraDeviceIdentifiers";
			this.fraDeviceIdentifiers.Size = new System.Drawing.Size(208, 96);
			this.fraDeviceIdentifiers.TabIndex = 10;
			this.fraDeviceIdentifiers.TabStop = false;
			this.fraDeviceIdentifiers.Text = "Device Identifiers";
			// 
			// txtProductID
			// 
			this.txtProductID.Location = new System.Drawing.Point(120, 56);
			this.txtProductID.Name = "txtProductID";
			this.txtProductID.Size = new System.Drawing.Size(72, 20);
			this.txtProductID.TabIndex = 3;
			this.txtProductID.Text = "1299";
			this.txtProductID.TextChanged += new System.EventHandler(this.txtProductID_TextChanged);
			// 
			// lblProductID
			// 
			this.lblProductID.Location = new System.Drawing.Point(16, 56);
			this.lblProductID.Name = "lblProductID";
			this.lblProductID.Size = new System.Drawing.Size(112, 23);
			this.lblProductID.TabIndex = 2;
			this.lblProductID.Text = "Product ID (hex):";
			// 
			// txtVendorID
			// 
			this.txtVendorID.Location = new System.Drawing.Point(120, 24);
			this.txtVendorID.Name = "txtVendorID";
			this.txtVendorID.Size = new System.Drawing.Size(72, 20);
			this.txtVendorID.TabIndex = 1;
			this.txtVendorID.Text = "0925";
			this.txtVendorID.TextChanged += new System.EventHandler(this.txtVendorID_TextChanged);
			// 
			// lblVendorID
			// 
			this.lblVendorID.Location = new System.Drawing.Point(16, 24);
			this.lblVendorID.Name = "lblVendorID";
			this.lblVendorID.Size = new System.Drawing.Size(112, 23);
			this.lblVendorID.TabIndex = 0;
			this.lblVendorID.Text = "Vendor ID (hex):";
			// 
			// cmdFindDevice
			// 
			this.cmdFindDevice.Location = new System.Drawing.Point(464, 48);
			this.cmdFindDevice.Name = "cmdFindDevice";
			this.cmdFindDevice.Size = new System.Drawing.Size(136, 40);
			this.cmdFindDevice.TabIndex = 11;
			this.cmdFindDevice.Text = "Find My Device";
			this.cmdFindDevice.Click += new System.EventHandler(this.cmdFindDevice_Click);
			// 
			// FrmMain
			// 
			this.ClientSize = new System.Drawing.Size(800, 561);
			this.Controls.Add(this.cmdFindDevice);
			this.Controls.Add(this.fraDeviceIdentifiers);
			this.Controls.Add(this.fraInputReportBufferSize);
			this.Controls.Add(this.fraReportTypes);
			this.Controls.Add(this.fraSendAndReceive);
			this.Controls.Add(this.fraBytesReceived);
			this.Controls.Add(this.fraBytesToSend);
			this.Controls.Add(this.lstResults);
			this.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Location = new System.Drawing.Point(21, 28);
			this.Name = "FrmMain";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Generic HID Tester";
			this.Load += new System.EventHandler(this.frmMain_Load);
			this.Closed += new System.EventHandler(this.frmMain_Closed);
			this.fraSendAndReceive.ResumeLayout(false);
			this.fraBytesReceived.ResumeLayout(false);
			this.fraBytesReceived.PerformLayout();
			this.fraBytesToSend.ResumeLayout(false);
			this.fraReportTypes.ResumeLayout(false);
			this.fraInputReportBufferSize.ResumeLayout(false);
			this.fraInputReportBufferSize.PerformLayout();
			this.fraDeviceIdentifiers.ResumeLayout(false);
			this.fraDeviceIdentifiers.PerformLayout();
			this.ResumeLayout(false);

        }         
        #endregion 
        
        private IntPtr deviceNotificationHandle; 
        private Boolean exclusiveAccess;
		private FileStream fileStreamDeviceData;
        private SafeFileHandle hidHandle; 
        private String hidUsage; 
        private Boolean myDeviceDetected; 
        private String myDevicePathName;
		private Boolean transferInProgress = false;
        
        private Debugging MyDebugging = new Debugging(); //  For viewing results of API calls via Debug.Write.
        private DeviceManagement MyDeviceManagement = new DeviceManagement(); 
        private Hid MyHid = new Hid();
		private static System.Timers.Timer tmrReadTimeout;
		private static System.Timers.Timer tmrContinuousDataCollect;
    
        internal FrmMain FrmMy; 
        
        //  This delegate has the same parameters as AccessForm.
        //  Used in accessing the application's form from a different thread.
        
        private delegate void MarshalToForm( String action, String textToAdd );
        
        ///  <summary>
        ///  Called when a WM_DEVICECHANGE message has arrived,
        ///  indicating that a device has been attached or removed.
        ///  </summary>
        ///  
        ///  <param name="m"> a message with information about the device </param>
        
        internal void OnDeviceChange( Message m ) 
        {             
            Debug.WriteLine( "WM_DEVICECHANGE" ); 
            
            try 
            {
                if ((m.WParam.ToInt32() == DeviceManagement.DBT_DEVICEARRIVAL)) 
                {                     
                    //  If WParam contains DBT_DEVICEARRIVAL, a device has been attached.
                    
                    Debug.WriteLine( "A device has been attached." ); 
                    
                    //  Find out if it's the device we're communicating with.
                    
                    if ( MyDeviceManagement.DeviceNameMatch( m, myDevicePathName ) ) 
                    { 
                        lstResults.Items.Add( "My device attached." ); 
                    } 
                    
                }
                else if ((m.WParam.ToInt32() == DeviceManagement.DBT_DEVICEREMOVECOMPLETE)) 
                { 
                    
                    //  If WParam contains DBT_DEVICEREMOVAL, a device has been removed.
                    
                    Debug.WriteLine( "A device has been removed." ); 
                    
                    //  Find out if it's the device we're communicating with.
                    
                    if ( MyDeviceManagement.DeviceNameMatch( m, myDevicePathName ) ) 
                    { 
                        
                        lstResults.Items.Add( "My device removed." ); 
                        
                        //  Set MyDeviceDetected False so on the next data-transfer attempt,
                        //  FindTheHid() will be called to look for the device 
                        //  and get a new handle.
                        
                        FrmMy.myDeviceDetected = false; 
                    } 
                }                 
                ScrollToBottomOfListBox();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        }         
        
        ///  <summary>
        ///  Uses a series of API calls to locate a HID-class device
        ///  by its Vendor ID and Product ID.
        ///  </summary>
        ///          
        ///  <returns>
        ///   True if the device is detected, False if not detected.
        ///  </returns>
        
        private Boolean FindTheHid() 
        {             
            Boolean deviceFound = false; 
            String[] devicePathName = new String[ 128 ];
            String functionName = "";
            Guid hidGuid = Guid.Empty; 
            Int32 memberIndex = 0;
			Int32 myProductID = 0;
			Int32 myVendorID = 0; 
            Boolean success = false;            
            
            try 
            { 
                myDeviceDetected = false;
				CloseCommunications();
                
                //  Get the device's Vendor ID and Product ID from the form's text boxes.
                
                GetVendorAndProductIDsFromTextBoxes( ref myVendorID, ref myProductID ); 
                
                //  ***
                //  API function: 'HidD_GetHidGuid
                
                //  Purpose: Retrieves the interface class GUID for the HID class.
                
                //  Accepts: 'A System.Guid object for storing the GUID.
                //  ***
                
                Hid.HidD_GetHidGuid( ref hidGuid );

                functionName = "GetHidGuid"; 
                Debug.WriteLine( MyDebugging.ResultOfAPICall( functionName ) ); 
                Debug.WriteLine( "  GUID for system HIDs: " + hidGuid.ToString() ); 
                
                //  Fill an array with the device path names of all attached HIDs.
                
                deviceFound = MyDeviceManagement.FindDeviceFromGuid( hidGuid, ref devicePathName ); 
                
                //  If there is at least one HID, attempt to read the Vendor ID and Product ID
                //  of each device until there is a match or all devices have been examined.
                
                if ( deviceFound ) 
                {                     
                    memberIndex = 0; 
                    
                    do 
                    { 
                        //  ***
                        //  API function:
                        //  CreateFile
                        
                        //  Purpose:
                        //  Retrieves a handle to a device.
                        
                        //  Accepts:
                        //  A device path name returned by SetupDiGetDeviceInterfaceDetail
                        //  The type of access requested (read/write).
                        //  FILE_SHARE attributes to allow other processes to access the device while this handle is open.
                        //  A Security structure or IntPtr.Zero. 
                        //  A creation disposition value. Use OPEN_EXISTING for devices.
                        //  Flags and attributes for files. Not used for devices.
                        //  Handle to a template file. Not used.
                        
                        //  Returns: a handle without read or write access.
                        //  This enables obtaining information about all HIDs, even system
                        //  keyboards and mice. 
                        //  Separate handles are used for reading and writing.
                        //  ***

						// Open the handle without read/write access to enable getting information about any HID, even system keyboards and mice.
                       						
						hidHandle = FileIO.CreateFile(devicePathName[memberIndex], 0,  FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);
					
                        functionName = "CreateFile"; 
                        Debug.WriteLine( MyDebugging.ResultOfAPICall( functionName ) );
						
						Debug.WriteLine( "  Returned handle: " + hidHandle.ToString() );

					    if (!hidHandle.IsInvalid)  
                        {                             
                            //  The returned handle is valid, 
                            //  so find out if this is the device we're looking for.
                            
                            //  Set the Size property of DeviceAttributes to the number of bytes in the structure.
                            
                            MyHid.DeviceAttributes.Size = Marshal.SizeOf( MyHid.DeviceAttributes ); 
                            
                            //  ***
                            //  API function:
                            //  HidD_GetAttributes
                            
                            //  Purpose:
                            //  Retrieves a HIDD_ATTRIBUTES structure containing the Vendor ID, 
                            //  Product ID, and Product Version Number for a device.
                            
                            //  Accepts:
                            //  A handle returned by CreateFile.
                            //  A pointer to receive a HIDD_ATTRIBUTES structure.
                            
                            //  Returns:
                            //  True on success, False on failure.
                            //  ***                            
							
                            success = Hid.HidD_GetAttributes(hidHandle, ref MyHid.DeviceAttributes); 
                            
                            if ( success ) 
                            {                                
                                Debug.WriteLine( "  HIDD_ATTRIBUTES structure filled without error." ); 
                                Debug.WriteLine( "  Structure size: " + MyHid.DeviceAttributes.Size );                                                                                               
                                Debug.WriteLine("  Vendor ID: " + Convert.ToString(MyHid.DeviceAttributes.VendorID, 16));                               
                                Debug.WriteLine("  Product ID: " + Convert.ToString(MyHid.DeviceAttributes.ProductID, 16));                               
                                Debug.WriteLine("  Version Number: " + Convert.ToString(MyHid.DeviceAttributes.VersionNumber, 16)); 
                                
                                //  Find out if the device matches the one we're looking for.
                                
                                if ( ( MyHid.DeviceAttributes.VendorID == myVendorID ) && ( MyHid.DeviceAttributes.ProductID == myProductID ) ) 
                                {                                     
                                    Debug.WriteLine( "  My device detected" ); 
                                    
                                    //  Display the information in form's list box.
                                    
                                    lstResults.Items.Add( "Device detected:" );                                   
                                    lstResults.Items.Add("  Vendor ID= " + Convert.ToString(MyHid.DeviceAttributes.VendorID, 16));                                  
                                    lstResults.Items.Add("  Product ID = " + Convert.ToString(MyHid.DeviceAttributes.ProductID, 16));
                                    
                                    ScrollToBottomOfListBox(); 
                                    
                                    myDeviceDetected = true; 
                                    
                                    //  Save the DevicePathName for OnDeviceChange().
                                    
                                    myDevicePathName = devicePathName[ memberIndex ]; 
                                } 
                                else 
                                {                                     
                                    //  It's not a match, so close the handle.
                                    
                                    myDeviceDetected = false;									
                                    hidHandle.Close();                                     
                                }                                 
                            } 
                            else 
                            { 
                                //  There was a problem in retrieving the information.
                                
                                Debug.WriteLine( "  Error in filling HIDD_ATTRIBUTES structure." ); 
                                myDeviceDetected = false;								
                                hidHandle.Close(); 
                            }                             
                        } 
                        
                        //  Keep looking until we find the device or there are no devices left to examine.
                        
                        memberIndex = memberIndex + 1;                         
                    } 
                    while (  ! ( ( myDeviceDetected || ( memberIndex == devicePathName.Length ) ) ) );                     
                } 
                
                if ( myDeviceDetected ) 
                {                     
                    //  The device was detected.
                    //  Register to receive notifications if the device is removed or attached.
                    
                    success = MyDeviceManagement.RegisterForDeviceNotifications( myDevicePathName, FrmMy.Handle, hidGuid, ref deviceNotificationHandle ); 
                    
                    Debug.WriteLine( "RegisterForDeviceNotifications = " + success ); 
                    
                    //  Learn the capabilities of the device.
					
                    MyHid.Capabilities = MyHid.GetDeviceCapabilities( hidHandle ); 
                    
                    if ( success ) 
                    {                         
                        //  Find out if the device is a system mouse or keyboard.
                        
                        hidUsage = MyHid.GetHidUsage( MyHid.Capabilities ); 
                        
                        //  Get the Input report buffer size.
                        
                        GetInputReportBufferSize(); 
                        cmdInputReportBufferSize.Enabled = true;          
						
						//Close the handle and reopen it with read/write access.
						
						hidHandle.Close();
						hidHandle = FileIO.CreateFile(myDevicePathName, FileIO.GENERIC_READ | FileIO.GENERIC_WRITE, FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);
                        
						if ( hidHandle.IsInvalid ) 
                        {                      
							exclusiveAccess = true; 
                            lstResults.Items.Add( "The device is a system " + hidUsage + "." ); 
                            lstResults.Items.Add( "Windows 2000 and Windows XP obtain exclusive access to Input and Output reports for this devices." ); 
                            lstResults.Items.Add( "Applications can access Feature reports only." ); 
                            ScrollToBottomOfListBox();                             
                        } 
							
                        else 
                        {
							if (MyHid.Capabilities.InputReportByteLength > 0)
							{
								//  Set the size of the Input report buffer. 

								Byte[] inputReportBuffer = null;

								inputReportBuffer = new Byte[MyHid.Capabilities.InputReportByteLength];	
							
								fileStreamDeviceData = new FileStream(hidHandle, FileAccess.Read | FileAccess.Write, inputReportBuffer.Length, false);
							}

							if (MyHid.Capabilities.OutputReportByteLength > 0)
							{
								Byte[] outputReportBuffer = null;
								outputReportBuffer = new Byte[MyHid.Capabilities.OutputReportByteLength];								
							} 
                            
                            //  Flush any waiting reports in the input buffer. (optional)
                            
                            MyHid.FlushQueue( hidHandle );                             
                        } 
                    } 
                } 
                else 
                { 
                    //  The device wasn't detected.
                    
                    lstResults.Items.Add( "Device not found." ); 
                    cmdInputReportBufferSize.Enabled = false; 
                    cmdOnce.Enabled = true; 
                    
                    Debug.WriteLine( " Device not found." ); 
                    
                    ScrollToBottomOfListBox(); 
                }                 
                return myDeviceDetected;                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        }         
        
        ///  <summary>
        ///  Performs various application-specific functions that
        ///  involve accessing the application's form.
        ///  </summary>
        ///  
        ///  <param name="action"> a String that names the action to perform on the form</param>
        ///  <param name="formText"> text that the form displays or the code uses for 
        ///  another purpose. Actions that don't use text ignore this parameter. </param>
        
        private void AccessForm( String action, String formText ) 
        {             
            try 
            { 
                //  Select an action to perform on the form:
                
                switch ( action ) 
                {
                    case "AddItemToListBox":
                        
                        lstResults.Items.Add( formText ); 
                        
                        break;
                    case "AddItemToTextBox":
                        
                        txtBytesReceived.SelectedText = formText + "\r\n"; 
                        
                        break;
                    case "EnableCmdOnce":
                        
                        //  If it's a single transfer, re-enable the command button.
                        
                        if ( cmdContinuous.Text == "Continuous" ) 
                        { 
                            cmdOnce.Enabled = true; 
                        }                         
                        break;
                    case "ScrollToBottomOfListBox":
                        
                        lstResults.SelectedIndex = lstResults.Items.Count - 1; 
                        
                        break;
                    case "TextBoxSelectionStart":
                        
                        txtBytesReceived.SelectionStart = formText.Length; 
                        
                        break;
                    default:
                        
                        break;
                } 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
		/// <summary>
		/// Close the handle and FileStreams for a device.
		/// </summary>
		/// 
		private void CloseCommunications()
		{
			if (fileStreamDeviceData != null)
			{
				fileStreamDeviceData.Close();
			}

			if ((hidHandle != null) && (!(hidHandle.IsInvalid)))
			{
				hidHandle.Close();
			}

			// The next attempt to communicate will get new handles and FileStreams.

			myDeviceDetected = false;
		}

        ///  <summary>
        ///  Start or stop a series of periodic transfers.
        ///  </summary>
        
        private void cmdContinuous_Click( System.Object eventSender, System.EventArgs eventArgs ) 
        {             
            try 
            { 
                if ( cmdContinuous.Text == "Continuous" ) 
                {                     
                    //  Start doing periodic transfers.
                    
                    if ( !( cmdOnce.Enabled ) ) 
                    {                         
                        AccessForm( "AddItemToListBox", "A previous transfer hasn't completed. Please try again." ); 
                    } 
                    else 
                    {                         
                        cmdOnce.Enabled = false; 
                        
                        //  Change the command button's text to "Cancel Continuous"
                        
                        cmdContinuous.Text = "Cancel Continuous"; 
                        
                        //  Enable the timer event to trigger a set of transfers.


                        tmrContinuousDataCollect.Enabled = true;
						tmrContinuousDataCollect.Start(); 
                        ReadAndWriteToDevice(); 
                    }                     
                } 
                else 
                {                     
                    //  Stop doing continuous transfers.
                    //  Change the command button's text to "Continuous"
                    
                    cmdContinuous.Text = "Continuous"; 
                    
                    // D isable the timer that triggers the transfers.

	
					tmrContinuousDataCollect.Enabled = false;
					tmrContinuousDataCollect.Stop(); 
                    
                    cmdOnce.Enabled = true; 
                }                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        }         
        
        ///  <summary>
        ///  Set the number of Input reports the host will store.
        ///  </summary>
        
        private void cmdInputReportBufferSize_Click( System.Object sender, System.EventArgs e ) 
        {             
            try 
            { 
                SetInputReportBufferSize();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        } 
                
        ///  <summary>
        ///  Search for a specific device.
        ///  </summary>
        
        private void cmdFindDevice_Click( System.Object sender, System.EventArgs e ) 
        {            
            try 
            { 
                FindTheHid();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Attempt to write a report and read a report.
        ///  </summary>
          
        private void cmdOnce_Click( System.Object eventSender, System.EventArgs eventArgs ) 
        {             
            try 
            { 
                //  Don't allow another transfer request until this one completes.
                //  Move the focus away from cmdOnce to prevent the focus from 
                //  switching to the next control in the tab order on disabling the button.
                
                fraSendAndReceive.Focus(); 
                cmdOnce.Enabled = false; 
                
                ReadAndWriteToDevice();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        }        
        
        ///  <summary>
        ///  Called if the user changes the Vendor ID or Product ID in the text box.
        ///  </summary>
          
        private void DeviceHasChanged() 
        {             
            try 
            { 
                //  If a device was previously detected, stop receiving notifications about it.

				if (myDeviceDetected)
				{
					MyDeviceManagement.StopReceivingDeviceNotifications(deviceNotificationHandle);

					CloseCommunications();
				}  
             } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        }         
        
        ///  <summary>
        ///  Sends a Feature report, then retrieves one.
        ///  Assumes report ID = 0 for both reports.
        ///  </summary>
        
        private void ExchangeFeatureReports() 
        {             
            String byteValue = null; 
            Int32 count = 0; 
            Byte[] inFeatureReportBuffer = null; 
            Byte[] outFeatureReportBuffer = null; 
            Boolean success = false; 
            
            try 
            {   
                inFeatureReportBuffer = null; 
                
                if ( ( MyHid.Capabilities.FeatureReportByteLength > 0 ) ) 
                {                     
                    //  The HID has a Feature report.
                    
                    //  Set the size of the Feature report buffer. 
                    //  Subtract 1 from the value in the Capabilities structure because 
                    //  the array begins at index 0.
                    
                    outFeatureReportBuffer = new Byte[ MyHid.Capabilities.FeatureReportByteLength ]; 
                    
                    //  Store the report ID in the buffer:
                    
                    outFeatureReportBuffer[ 0 ] = 0; 
                    
                    //  Store the report data following the report ID.
                    //  Use the data in the combo boxes on the form.
                    
                    outFeatureReportBuffer[ 1 ] = Convert.ToByte( cboByte0.SelectedIndex ); 
                    
                    if ( Information.UBound( outFeatureReportBuffer, 1 ) > 1 ) 
                    { 
                        outFeatureReportBuffer[ 2 ] = Convert.ToByte( cboByte1.SelectedIndex ); 
                    } 
                    
                    //  Write a report to the device

                    success = MyHid.SendFeatureReport(hidHandle, outFeatureReportBuffer);

					if (success)
					{
						lstResults.Items.Add("A Feature report has been written.");

						//  Display the report data sent in the form's list box.

						lstResults.Items.Add(" Feature Report ID: " + String.Format("{0:X2} ", outFeatureReportBuffer[0]));
						lstResults.Items.Add(" Feature Report Data:");

						for (count = 0; count <= outFeatureReportBuffer.Length - 1; count++)
						{
							//  Display bytes as 2-character Hex strings.

							byteValue = String.Format("{0:X2} ", outFeatureReportBuffer[count]);

							lstResults.Items.Add(" " + byteValue);
						}
					}
					else
					{
						CloseCommunications();
						lstResults.Items.Add("The attempt to send a Feature report failed."); 
					}
					                                        
                    //  Read a report from the device.
                    
                    //  Set the size of the Feature report buffer. 
                    //  Subtract 1 from the value in the Capabilities structure because 
                    //  the array begins at index 0.
                    
                    if ( ( MyHid.Capabilities.FeatureReportByteLength > 0 ) ) 
                    { 
                        inFeatureReportBuffer = new Byte[ MyHid.Capabilities.FeatureReportByteLength ]; 
                    } 
                    
                    //  Read a report.

					success = MyHid.GetFeatureReport (hidHandle, ref inFeatureReportBuffer);           
                         
                    if ( success ) 
                    { 
                        lstResults.Items.Add( "A Feature report has been read." ); 
                        
                        //  Display the report data received in the form's list box.
                        
                        lstResults.Items.Add( " Feature Report ID: " + String.Format( "{0:X2} ", inFeatureReportBuffer[ 0 ] ) ); 
                        lstResults.Items.Add( " Feature Report Data:" ); 
                        txtBytesReceived.Text = "";                         
                        
                        for (count = 0; count <= inFeatureReportBuffer.Length - 1; count++) 
                        {                             
                            //  Display bytes as 2-character Hex strings.
                            
                            byteValue = String.Format( "{0:X2} ", inFeatureReportBuffer[ count ] ); 
                            
                            lstResults.Items.Add( " " + byteValue ); 
                            
                            //  Display the received bytes in the text box.

                            txtBytesReceived.SelectionStart = txtBytesReceived.Text.Length; //* TRANSINFO: .NET Equivalent of Microsoft.VisualBasic NameSpace */ System.Runtime.InteropServices.Marshal.SizeOf( txtBytesReceived.Text ); 
                            txtBytesReceived.SelectedText = byteValue + "\r\n";                             
                        } 
                    } 
                    else 
                    {
						CloseCommunications();
                        lstResults.Items.Add( "The attempt to read a Feature report failed." ); 
                    }                     
                } 
                else 
                { 
                    lstResults.Items.Add( "The HID doesn't have a Feature report." ); 
                } 
                
                ScrollToBottomOfListBox();                 
                cmdOnce.Enabled = true;                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Sends an Output report, then retrieves an Input report.
        ///  Assumes report ID = 0 for both reports.
        ///  </summary>
        
        private void ExchangeInputAndOutputReports() 
        {             
            String byteValue = null; 
            Int32 count = 0; 
            Byte[] inputReportBuffer = null; 
            Byte[] outputReportBuffer = null; 
            Boolean success = false;

			try
			{
				success = false;

				//  Don't attempt to exchange reports if valid handles aren't available
				//  (as for a mouse or keyboard under Windows 2000/XP.)

				if (!hidHandle.IsInvalid)
				{
					//  Don't attempt to send an Output report if the HID has no Output report.

					if (MyHid.Capabilities.OutputReportByteLength > 0)
					{
						//  Set the size of the Output report buffer.   

						outputReportBuffer = new Byte[MyHid.Capabilities.OutputReportByteLength];

						//  Store the report ID in the first byte of the buffer:

						outputReportBuffer[0] = 0;

						//  Store the report data following the report ID.
						//  Use the data in the combo boxes on the form.

						outputReportBuffer[1] = Convert.ToByte(cboByte0.SelectedIndex);

						if (Information.UBound(outputReportBuffer, 1) > 1)
						{
							outputReportBuffer[2] = Convert.ToByte(cboByte1.SelectedIndex);
						}

						//  Write a report.

						if ((chkUseControlTransfersOnly.Checked) == true)
						{

							//  Use a control transfer to send the report,
							//  even if the HID has an interrupt OUT endpoint.

							success = MyHid.SendOutputReportViaControlTransfer(hidHandle, outputReportBuffer);
						}
						else
						{
						// If the HID has an interrupt OUT endpoint, the host uses an 
						// interrupt transfer to send the report. 
						// If not, the host uses a control transfer.

							if (fileStreamDeviceData.CanWrite)
							{
								fileStreamDeviceData.Write(outputReportBuffer, 0, outputReportBuffer.Length);
								success = true;
							}							
						}
						if (success)
						{
							lstResults.Items.Add("An Output report has been written.");

							//  Display the report data in the form's list box.

							lstResults.Items.Add(" Output Report ID: " + String.Format("{0:X2} ", outputReportBuffer[0]));
							lstResults.Items.Add(" Output Report Data:");

							txtBytesReceived.Text = "";
							for (count = 0; count <= outputReportBuffer.Length - 1; count++)
							{
								//  Display bytes as 2-character hex strings.

								byteValue = String.Format("{0:X2} ", outputReportBuffer[count]);
								lstResults.Items.Add(" " + byteValue);
							}
						}
						else
						{
							CloseCommunications();
							lstResults.Items.Add("The attempt to write an Output report failed.");							
						}
					}
					else
					{
						lstResults.Items.Add("The HID doesn't have an Output report.");
					}

					//  Read an Input report.

					success = false;

					//  Don't attempt to send an Input report if the HID has no Input report.
					//  (The HID spec requires all HIDs to have an interrupt IN endpoint,
					//  which suggests that all HIDs must support Input reports.)

					if (MyHid.Capabilities.InputReportByteLength > 0)
					{
						//  Set the size of the Input report buffer. 

						inputReportBuffer = new Byte[MyHid.Capabilities.InputReportByteLength];

						if (chkUseControlTransfersOnly.Checked)
						{
							//  Read a report using a control transfer.

							success = MyHid.GetInputReportViaControlTransfer(hidHandle, ref inputReportBuffer);

							if (success)
							{
								lstResults.Items.Add("An Input report has been read.");

								//  Display the report data received in the form's list box.

								lstResults.Items.Add(" Input Report ID: " + String.Format("{0:X2} ", inputReportBuffer[0]));
								lstResults.Items.Add(" Input Report Data:");

								txtBytesReceived.Text = "";

								for (count = 0; count <= inputReportBuffer.Length - 1; count++)
								{
									//  Display bytes as 2-character Hex strings.

									byteValue = String.Format("{0:X2} ", inputReportBuffer[count]);

									lstResults.Items.Add(" " + byteValue);

									//  Display the received bytes in the text box.

									txtBytesReceived.SelectionStart = txtBytesReceived.Text.Length; ///* TRANSINFO: .NET Equivalent of Microsoft.VisualBasic NameSpace */ System.Runtime.InteropServices.Marshal.SizeOf(txtBytesReceived.Text); 
									txtBytesReceived.SelectedText = byteValue + "\r\n";
								}
							}
							else
							{
								CloseCommunications();
								lstResults.Items.Add("The attempt to read an Input report has failed.");
							}

							ScrollToBottomOfListBox();

							//  Enable requesting another transfer.

							AccessForm("EnableCmdOnce", "");
						}
						else
						{
							//  Read a report using interrupt transfers.                
							//  To enable reading a report without blocking the main thread, this
							//  application uses an asynchronous delegate.

							IAsyncResult ar = null;
							transferInProgress = true;

							// Timeout if no report is available.

							tmrReadTimeout.Start();

							if (fileStreamDeviceData.CanRead)
							{
								fileStreamDeviceData.BeginRead(inputReportBuffer, 0, inputReportBuffer.Length, new AsyncCallback(GetInputReportData), inputReportBuffer);
							}
							else
							{
								CloseCommunications();
								lstResults.Items.Add("The attempt to read an Input report has failed.");
							}
						}
					}
					else
					{
						lstResults.Items.Add("No attempt to read an Input report was made.");
						lstResults.Items.Add("The HID doesn't have an Input report.");
						AccessForm("EnableCmdOnce", "");
					}
				}
				else
				{
					lstResults.Items.Add("Invalid handle. The device is probably a system mouse or keyboard.");
					lstResults.Items.Add("No attempt to write an Output report or read an Input report was made.");
					AccessForm("EnableCmdOnce", "");
				}
				ScrollToBottomOfListBox();
			}
			catch (Exception ex)
			{
				DisplayException(this.Name, ex);
				throw;
			}			
        }         
        
        ///  <summary>
        ///  Perform shutdown operations.
        ///  </summary>
        
        private void frmMain_Closed( System.Object eventSender, System.EventArgs eventArgs ) 
        {             
            try 
            { 
                Shutdown();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        }         
        
        ///  <summary>
        ///  Perform startup operations.
        ///  </summary>
        
        private void frmMain_Load( System.Object eventSender, System.EventArgs eventArgs ) 
        {             
            try 
            { 
                FrmMy = this; 
                Startup();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Finds and displays the number of Input buffers
        ///  (the number of Input reports the host will store). 
        ///  </summary>
        
        private void GetInputReportBufferSize() 
        {             
            Int32 numberOfInputBuffers = 0;
            Boolean success;
            
            try 
            { 
                //  Get the number of input buffers.
				               
               success =  MyHid.GetNumberOfInputBuffers( hidHandle, ref numberOfInputBuffers ); 
                
                //  Display the result in the text box.
                
                txtInputReportBufferSize.Text = Convert.ToString( numberOfInputBuffers );                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Retrieves Input report data and status information.
        ///  This routine is called automatically when myInputReport.Read
        ///  returns. Calls several marshaling routines to access the main form.
        ///  </summary>
        ///  
        ///  <param name="ar"> an object containing status information about 
        ///  the asynchronous operation. </param>
        
        private void GetInputReportData( IAsyncResult ar ) 
        {             
            String byteValue = null; 
            Int32 count = 0; 
            Byte[] inputReportBuffer = null; 
            Boolean success = false; 
            
            try 
            { 
				 inputReportBuffer = (byte[])ar.AsyncState;	
          
                fileStreamDeviceData.EndRead(ar);

				tmrReadTimeout.Stop();

                if ( ( ar.IsCompleted ) ) 
                {
					MyMarshalToForm("AddItemToListBox", "An Input report has been read.");                     
                    MyMarshalToForm( "AddItemToListBox", " Input Report ID: " + String.Format( "{0:X2} ", inputReportBuffer[ 0 ] ) );                     
                    MyMarshalToForm( "AddItemToListBox", " Input Report Data:" ); 
                    
                    for ( count=0; count <= inputReportBuffer.Length -1 ; count++ ) 
                    {                         
                        //  Display bytes as 2-character Hex strings.
                        
                        byteValue = String.Format( "{0:X2} ", inputReportBuffer[ count ] ); 
                        
                        MyMarshalToForm( "AddItemToListBox", " " + byteValue ); 
                        MyMarshalToForm( "TextBoxSelectionStart", txtBytesReceived.Text ); 
                        MyMarshalToForm( "AddItemToTextBox", byteValue );                         
                    }                     
                } 
                else 
                { 
                    MyMarshalToForm( "AddItemToListBox", "The attempt to read an Input report has failed." ); 
                    Debug.Write( "The attempt to read an Input report has failed" );					
                } 
                
                MyMarshalToForm( "ScrollToBottomOfListBox", "" ); 
                
                //  Enable requesting another transfer.
                
                MyMarshalToForm( "EnableCmdOnce", "" );
				transferInProgress = false;
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Retrieves a Vendor ID and Product ID in hexadecimal 
        ///  from the form's text boxes and converts the text to Int16s.
        ///  </summary>
        ///  
        ///  <param name="myVendorID"> the Vendor ID</param>
        ///  <param name="myProductID"> the Product ID</param>

		private void GetVendorAndProductIDsFromTextBoxes(ref Int32 myVendorID, ref Int32 myProductID) 
        {             
            try 
            {
				myVendorID = Int32.Parse(txtVendorID.Text, NumberStyles.AllowHexSpecifier); 
				myProductID = Int32.Parse(txtProductID.Text, NumberStyles.AllowHexSpecifier);                
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Initialize the elements on the form.
        ///  </summary>
          
        private void InitializeDisplay() 
        {           
            Int16 count = 0; 
            String byteValue = null; 
            
            try 
            { 
                //  Create a dropdown list box for each byte to send in a report.
                //  Display the values as 2-character hex strings.
                
                for ( count=0; count <= 255; count++ ) 
                {                     
                    byteValue = String.Format( "{0:X2} ", count ); 
                    FrmMy.cboByte0.Items.Insert( count, byteValue ); 
                    FrmMy.cboByte1.Items.Insert( count, byteValue );                     
                } 
                
                //  Select a default value for each box
                
                FrmMy.cboByte0.SelectedIndex = 0; 
                FrmMy.cboByte1.SelectedIndex = 128; 
                
                //  Check the autoincrement box to increment the values each time a report is sent.
                
                chkAutoincrement.CheckState = System.Windows.Forms.CheckState.Checked; 
                
                //  Don't allow the user to select an input report buffer size until there is
                //  a handle to a HID.
                
                cmdInputReportBufferSize.Focus(); 
                cmdInputReportBufferSize.Enabled = false; 
                
                if ( MyHid.IsWindowsXpOrLater() ) 
                {                     
                    chkUseControlTransfersOnly.Enabled = true;                     
                } 
                else 
                { 
                    //  If the operating system is earlier than Windows XP,
                    //  disable the option to force Input and Output reports to use control transfers.
                    
                    chkUseControlTransfersOnly.Enabled = false;                     
                } 
                
                lstResults.Items.Add( "For a more detailed event log, view debug statements in Visual Studio's Output window:" ); 
                lstResults.Items.Add( "Click Build > Configuration Manager > Active Solution Configuration > Debug > Close." ); 
                lstResults.Items.Add( "Then click View > Output." ); 
                lstResults.Items.Add( "" );                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Enables accessing a form's controls from another thread 
        ///  </summary>
        ///  
        ///  <param name="action"> a String that names the action to perform on the form </param>
        ///  <param name="textToDisplay"> text that the form displays or the code uses for 
        ///  another purpose. Actions that don't use text ignore this parameter.  </param>
        
        private void MyMarshalToForm( String action, String textToDisplay ) 
        {             
            object[] args = { action, textToDisplay }; 
            MarshalToForm MarshalToFormDelegate = null; 
            
            //  The AccessForm routine contains the code that accesses the form.
            
            MarshalToFormDelegate = new MarshalToForm( AccessForm ); 
            
            //  Execute AccessForm, passing the parameters in args.
            
            base.Invoke( MarshalToFormDelegate, args );             
        }

		///  <summary>
		///  Exchange data with the device.
		///  </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		///  <remarks>
		///  The timer is enabled only if cmdContinous has been clicked, 
		///  selecting continous (periodic) transfers.
		///  </remarks>
		private void OnDataCollect(object source, ElapsedEventArgs e)
		{
			try
			{
				if (transferInProgress == false)
				{
					ReadAndWriteToDevice();
				}
			}
			catch (Exception ex)
			{
				DisplayException(this.Name, ex);
				throw;
			}
		}
		
		/// <summary>
		/// ystem timer timeout if read via interrupt transfer doesn't return.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		
		private void OnReadTimeout(object source, ElapsedEventArgs e)
		{
			MyMarshalToForm("AddItemToListBox", "The attempt to read a report timed out.");

			CloseCommunications();

			tmrReadTimeout.Stop();

			//  Enable requesting another transfer.

			MyMarshalToForm("EnableCmdOnce", "");
			MyMarshalToForm("ScrollToBottomOfListBox", ""); 			
		}
		        
        ///  <summary>
        ///  Initiates exchanging reports. 
        ///  The application sends a report and requests to read a report.
        ///  </summary>
        
        private void ReadAndWriteToDevice() 
        {             
            // Report header for the debug display:
            
            Debug.WriteLine( "" ); 
            Debug.WriteLine( "***** HID Test Report *****" ); 
            Debug.WriteLine( DateAndTime.Today + ": " + DateAndTime.TimeOfDay ); 
            
            try 
            { 
                //  If the device hasn't been detected, was removed, or timed out on a previous attempt
                //  to access it, look for the device.
                
                if ( ( myDeviceDetected == false ) ) 
                {                     
                    myDeviceDetected = FindTheHid();                     
                } 
                
                if ( ( myDeviceDetected == true ) ) 
                {                     
                    //  Get the bytes to send in a report from the combo boxes.
                    //  Increment the values if the autoincrement check box is selected.
                    
                    if ( Convert.ToDouble( chkAutoincrement.CheckState ) == 1 ) 
                    { 
                        if ( cboByte0.SelectedIndex < 255 ) 
                        { 
                            cboByte0.SelectedIndex = cboByte0.SelectedIndex + 1; 
                        } 
                        else 
                        { 
                            cboByte0.SelectedIndex = 0; 
                        } 
                        if ( cboByte1.SelectedIndex < 255 ) 
                        { 
                            cboByte1.SelectedIndex = cboByte1.SelectedIndex + 1; 
                        } 
                        else 
                        { 
                            cboByte1.SelectedIndex = 0; 
                        } 
                    } 
                    
                    //  An option button selects whether to exchange Input and Output reports
                    //  or Feature reports.
                    
                    if ( ( optInputOutput.Checked == true ) ) 
                    {                        
                        ExchangeInputAndOutputReports();                        
                    } 
                    else 
                    { 
                        ExchangeFeatureReports(); 
                    }                     
                }                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        } 
                
        ///  <summary>
        ///  Scroll to the bottom of the list box and trim as needed.
        ///  </summary>
        
        private void ScrollToBottomOfListBox() 
        {            
            try 
            { 
                Int32 count = 0; 
                
                lstResults.SelectedIndex = lstResults.Items.Count - 1; 
                
                //  If the list box is getting too large, trim its contents by removing the earliest data.
                
                if ( lstResults.Items.Count > 1000 ) 
                {                     
                    for ( count=1; count <= 500; count++ ) 
                    { 
                        lstResults.Items.RemoveAt( 4 ); 
                    }                    
                }                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        } 
                
        ///  <summary>
        ///  Set the number of Input buffers (the number of Input reports 
        ///  the host will store) from the value in the text box.
        ///  </summary>
        
        private void SetInputReportBufferSize() 
        {             
            Int32 numberOfInputBuffers = 0; 
            
            try 
            { 
                //  Get the number of buffers from the text box.
                
                numberOfInputBuffers = Convert.ToInt32( Conversion.Val( txtInputReportBufferSize.Text ) ); 
                
                //  Set the number of buffers.
				
                MyHid.SetNumberOfInputBuffers( hidHandle, numberOfInputBuffers ); 
                
                //  Verify and display the result.
                
                GetInputReportBufferSize();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        }         
        
        ///  <summary>
        ///  Perform actions that must execute when the program ends.
        ///  </summary>
        
        private void Shutdown() 
        {             
            try 
            {

				CloseCommunications();	

                //  Stop receiving notifications.
                
                MyDeviceManagement.StopReceivingDeviceNotifications( deviceNotificationHandle );                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        } 
                
        ///  <summary>
        ///  Perform actions that must execute when the program starts.
        ///  </summary>
        
        private void Startup() 
        {            
            try 
            { 
                MyHid = new Hid(); 
                InitializeDisplay();			
								
				tmrContinuousDataCollect = new System.Timers.Timer(1000);
				tmrContinuousDataCollect.Elapsed  += new ElapsedEventHandler(OnDataCollect);
				tmrContinuousDataCollect.Stop();
				tmrContinuousDataCollect.SynchronizingObject = this;

				tmrReadTimeout = new System.Timers.Timer(5000);			
				tmrReadTimeout.Elapsed += new ElapsedEventHandler(OnReadTimeout);				
				tmrReadTimeout.Stop();
                
                //  Default USB Vendor ID and Product ID:
                
                txtVendorID.Text = "0925"; 
                txtProductID.Text = "7001";                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }

               
        ///  <summary>
        ///  The Product ID has changed in the text box. Call a routine to handle it.
        ///  </summary>
        
        private void txtProductID_TextChanged( System.Object sender, System.EventArgs e ) 
        {            
            try 
            { 
                DeviceHasChanged();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            } 
        } 
                
        ///  <summary>
        ///  The Vendor ID has changed in the text box. Call a routine to handle it.
        ///  </summary>
        
        private void txtVendorID_TextChanged( System.Object sender, System.EventArgs e ) 
        {            
            try 
            { 
                DeviceHasChanged();                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Finalize method.
        ///  </summary>
        
        ~FrmMain() 
        { 
        } 
                
        ///  <summary>
        ///   Overrides WndProc to enable checking for and handling WM_DEVICECHANGE messages.
        ///  </summary>
        ///  
        ///  <param name="m"> a Windows Message </param>
        
        protected override void WndProc( ref Message m ) 
        {            
            try 
            { 
                //  The OnDeviceChange routine processes WM_DEVICECHANGE messages.
                
                if ( m.Msg == DeviceManagement.WM_DEVICECHANGE ) 
                { 
                    OnDeviceChange( m ); 
                } 
                
                //  Let the base form process the message.
                
                base.WndProc( ref m );                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( this.Name, ex ); 
                throw ; 
            }             
        }         
        
        ///  <summary>
        ///  Provides a central mechanism for exception handling.
        ///  Displays a message box that describes the exception.
        ///  </summary>
        ///  
        ///  <param name="moduleName"> the module where the exception occurred. </param>
        ///  <param name="e"> the exception </param>
        
        internal static void DisplayException( String moduleName, Exception e ) 
        {             
            String message = null; 
            String caption = null; 
            
            //  Create an error message.
            
            message = "Exception: " + e.Message + ControlChars.CrLf + "Module: " + moduleName + ControlChars.CrLf + "Method: " + e.TargetSite.Name; 
            
            caption = "Unexpected Exception"; 
            
            MessageBox.Show( message, caption, MessageBoxButtons.OK ); 
            Debug.Write( message );             
        } 
                
        [STAThread]
        internal static void Main() { Application.Run( new FrmMain() ); }       
        private static FrmMain transDefaultFormFrmMain = null;
        internal static FrmMain TransDefaultFormFrmMain
        { 
        	get
        	{ 
        		if (transDefaultFormFrmMain == null)
        		{
        			transDefaultFormFrmMain = new FrmMain();
        		}
        		return transDefaultFormFrmMain;
        	} 
        } 
    }      
} 
