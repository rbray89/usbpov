using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using System.Collections;
using System.Diagnostics;
using System.Timers;
using GenericHid;


namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

        private IntPtr deviceNotificationHandle;
        private Boolean exclusiveAccess;
        private FileStream fileStreamDeviceData;
        private SafeFileHandle hidHandle;
        private String hidUsage;
        private Boolean myDeviceDetected = false;
        private String myDevicePathName;
        private Boolean transferInProgress = false;
        private string txtInputReportBufferSize;

        Int32 myProductID = 0x05df;
        Int32 myVendorID = 0x16C0;
        public Byte[] FeatureReportBuffer;
        Byte reportID = 2;
        Byte featureRegister = 0;
        Byte memLoc = 0;
        int eepromSize = 256;
        int eepromReadSize = 128;
        int messageSize = 200;
        int messageTiming = 1600;

        PictureBox[] pixelList; 

        private Byte[][] extraChars;

        private Debugging MyDebugging = new Debugging(); //  For viewing results of API calls via Debug.Write.
        private DeviceManagement MyDeviceManagement = new DeviceManagement();
        private Hid MyHid = new Hid();
        private static System.Timers.Timer tmrReadTimeout;
        private static System.Timers.Timer tmrContinuousDataCollect;
        private int MAXDEFCHARS = 96;

        public Form1()
        {
            InitializeComponent();
            if(FindTheHid())
                statusText.Text = "Connected!";
           

            extraChars = new Byte[8][];
            for (int i = 0; i < 8; i++ )
                extraChars[i] = new Byte[6];

            pixelList = new PictureBox[48];



            pixelList[0] = pictureBox1;
            pixelList[1] = pictureBox2;
pixelList[2] = pictureBox3;
pixelList[3] = pictureBox4;
pixelList[4] = pictureBox5;
pixelList[5] = pictureBox6;
pixelList[6] = pictureBox7;
pixelList[7] = pictureBox8;
pixelList[8] = pictureBox9;
pixelList[9] = pictureBox10;
pixelList[10] = pictureBox11;
pixelList[11] = pictureBox12;
pixelList[12] = pictureBox13;
pixelList[13] = pictureBox14;
pixelList[14] = pictureBox15;
pixelList[15] = pictureBox16;
pixelList[16] = pictureBox17;
pixelList[17] = pictureBox18;
pixelList[18] = pictureBox19;
pixelList[19] = pictureBox20;
pixelList[20] = pictureBox21;
pixelList[21] = pictureBox22;
pixelList[22] = pictureBox23;
pixelList[23] = pictureBox24;
pixelList[24] = pictureBox25;
pixelList[25] = pictureBox26;
pixelList[26] = pictureBox27;
pixelList[27] = pictureBox28;
pixelList[28] = pictureBox29;
pixelList[29] = pictureBox30;
pixelList[30] = pictureBox31;
pixelList[31] = pictureBox32;
pixelList[32] = pictureBox33;
pixelList[33] = pictureBox34;
pixelList[34] = pictureBox35;
pixelList[35] = pictureBox36;
pixelList[36] = pictureBox37;
pixelList[37] = pictureBox38;
pixelList[38] = pictureBox39;
pixelList[39] = pictureBox40;
pixelList[40] = pictureBox41;
pixelList[41] = pictureBox42;
pixelList[42] = pictureBox43;
pixelList[43] = pictureBox44;
pixelList[44] = pictureBox45;
pixelList[45] = pictureBox46;
pixelList[46] = pictureBox47;
pixelList[47] = pictureBox48;

extraSelected.SelectedIndex = 0;
       }


        //  This delegate has the same parameters as AccessForm.
        //  Used in accessing the application's form from a different thread.

        private delegate void MarshalToForm(String action, String textToAdd);

        ///  <summary>
        ///   Overrides WndProc to enable checking for and handling WM_DEVICECHANGE messages.
        ///  </summary>
        ///  
        ///  <param name="m"> a Windows Message </param>

        protected override void WndProc(ref Message m)
        {
            try
            {
                //  The OnDeviceChange routine processes WM_DEVICECHANGE messages.

                if (m.Msg == DeviceManagement.WM_DEVICECHANGE)
                {
                    OnDeviceChange(m);
                }

                //  Let the base form process the message.

                base.WndProc(ref m);
            }
            catch (Exception ex)
            {
                DisplayException(this.Name, ex);
                throw;
            }
        } 

        ///  <summary>
        ///  Called when a WM_DEVICECHANGE message has arrived,
        ///  indicating that a device has been attached or removed.
        ///  </summary>
        ///  
        ///  <param name="m"> a message with information about the device </param>

        internal void OnDeviceChange(Message m)
        {
            Debug.WriteLine("WM_DEVICECHANGE");

            try
            {
                if ((m.WParam.ToInt32() == DeviceManagement.DBT_DEVICEARRIVAL))
                {
                    //  If WParam contains DBT_DEVICEARRIVAL, a device has been attached.

                    Debug.WriteLine("A device has been attached.");

                    //  Find out if it's the device we're communicating with.

                    if (MyDeviceManagement.DeviceNameMatch(m, myDevicePathName))
                    {
                        lstResults.Items.Add("POV device attached.");
                        myDeviceDetected = FindTheHid();
                        statusText.Text = "Connected!";
                    }

                }
                else if ((m.WParam.ToInt32() == DeviceManagement.DBT_DEVICEREMOVECOMPLETE))
                {

                    //  If WParam contains DBT_DEVICEREMOVAL, a device has been removed.

                    Debug.WriteLine("A device has been removed.");

                    //  Find out if it's the device we're communicating with.

                    if (MyDeviceManagement.DeviceNameMatch(m, myDevicePathName))
                    {

                        lstResults.Items.Add("POV device removed.");
                        statusText.Text = "Not Connected!";
                        //  Set MyDeviceDetected False so on the next data-transfer attempt,
                        //  FindTheHid() will be called to look for the device 
                        //  and get a new handle.

                        this.myDeviceDetected = false;
                    }
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
            String[] devicePathName = new String[128];
            String functionName = "";
            Guid hidGuid = Guid.Empty;
            Int32 memberIndex = 0;
            Boolean success = false;

            try
            {
                myDeviceDetected = false;
                CloseCommunications();

                //  Get the device's Vendor ID and Product ID from the form's text boxes

                //  ***
                //  API function: 'HidD_GetHidGuid

                //  Purpose: Retrieves the interface class GUID for the HID class.

                //  Accepts: 'A System.Guid object for storing the GUID.
                //  ***

                Hid.HidD_GetHidGuid(ref hidGuid);

                functionName = "GetHidGuid";
                Debug.WriteLine(MyDebugging.ResultOfAPICall(functionName));
                Debug.WriteLine("  GUID for system HIDs: " + hidGuid.ToString());

                //  Fill an array with the device path names of all attached HIDs.

                deviceFound = MyDeviceManagement.FindDeviceFromGuid(hidGuid, ref devicePathName);

                //  If there is at least one HID, attempt to read the Vendor ID and Product ID
                //  of each device until there is a match or all devices have been examined.

                if (deviceFound)
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

                        hidHandle = FileIO.CreateFile(devicePathName[memberIndex], 0, FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);

                        functionName = "CreateFile";
                        Debug.WriteLine(MyDebugging.ResultOfAPICall(functionName));

                        Debug.WriteLine("  Returned handle: " + hidHandle.ToString());

                        if (!hidHandle.IsInvalid)
                        {
                            //  The returned handle is valid, 
                            //  so find out if this is the device we're looking for.

                            //  Set the Size property of DeviceAttributes to the number of bytes in the structure.

                            MyHid.DeviceAttributes.Size = Marshal.SizeOf(MyHid.DeviceAttributes);

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

                            if (success)
                            {
                                Debug.WriteLine("  HIDD_ATTRIBUTES structure filled without error.");
                                Debug.WriteLine("  Structure size: " + MyHid.DeviceAttributes.Size);
                                Debug.WriteLine("  Vendor ID: " + Convert.ToString(MyHid.DeviceAttributes.VendorID, 16));
                                Debug.WriteLine("  Product ID: " + Convert.ToString(MyHid.DeviceAttributes.ProductID, 16));
                                Debug.WriteLine("  Version Number: " + Convert.ToString(MyHid.DeviceAttributes.VersionNumber, 16));

                                //  Find out if the device matches the one we're looking for.

                                if ((MyHid.DeviceAttributes.VendorID == myVendorID) && (MyHid.DeviceAttributes.ProductID == myProductID))
                                {
                                    Debug.WriteLine("  My device detected");

                                    //  Display the information in form's list box.

                                    lstResults.Items.Add("Device detected:");
                                    lstResults.Items.Add("  Vendor ID= " + Convert.ToString(MyHid.DeviceAttributes.VendorID, 16));
                                    lstResults.Items.Add("  Product ID = " + Convert.ToString(MyHid.DeviceAttributes.ProductID, 16));

                                    ScrollToBottomOfListBox();

                                    myDeviceDetected = true;

                                    //  Save the DevicePathName for OnDeviceChange().

                                    myDevicePathName = devicePathName[memberIndex];
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

                                Debug.WriteLine("  Error in filling HIDD_ATTRIBUTES structure.");
                                myDeviceDetected = false;
                                hidHandle.Close();
                            }
                        }

                        //  Keep looking until we find the device or there are no devices left to examine.

                        memberIndex = memberIndex + 1;
                    }
                    while (!((myDeviceDetected || (memberIndex == devicePathName.Length))));
                }

                if (myDeviceDetected)
                {
                    //  The device was detected.
                    //  Register to receive notifications if the device is removed or attached.

                    success = MyDeviceManagement.RegisterForDeviceNotifications(myDevicePathName, this.Handle, hidGuid, ref deviceNotificationHandle);

                    Debug.WriteLine("RegisterForDeviceNotifications = " + success);

                    //  Learn the capabilities of the device.

                    MyHid.Capabilities = MyHid.GetDeviceCapabilities(hidHandle);

                    if (success)
                    {
                        //  Find out if the device is a system mouse or keyboard.

                        hidUsage = MyHid.GetHidUsage(MyHid.Capabilities);

                        //  Get the Input report buffer size.

                        GetInputReportBufferSize();
                        

                        //Close the handle and reopen it with read/write access.

                        hidHandle.Close();
                        hidHandle = FileIO.CreateFile(myDevicePathName, FileIO.GENERIC_READ | FileIO.GENERIC_WRITE, FileIO.FILE_SHARE_READ | FileIO.FILE_SHARE_WRITE, IntPtr.Zero, FileIO.OPEN_EXISTING, 0, 0);

                        if (hidHandle.IsInvalid)
                        {
                            exclusiveAccess = true;
                            lstResults.Items.Add("The device is a system " + hidUsage + ".");
                            lstResults.Items.Add("Windows 2000 and Windows XP obtain exclusive access to Input and Output reports for this devices.");
                            lstResults.Items.Add("Applications can access Feature reports only.");
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

                            MyHid.FlushQueue(hidHandle);
                        }
                    }
                }
                else
                {
                    //  The device wasn't detected.

                    lstResults.Items.Add("Device not found.");
                    statusText.Text = "Not Connected!";
                    Debug.WriteLine(" Device not found.");

                    ScrollToBottomOfListBox();
                }
                return myDeviceDetected;
            }
            catch (Exception ex)
            {
                DisplayException(this.Name, ex);
                throw;
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

                success = MyHid.GetNumberOfInputBuffers(hidHandle, ref numberOfInputBuffers);

                //  Display the result in the text box.

                txtInputReportBufferSize = Convert.ToString(numberOfInputBuffers);
            }
            catch (Exception ex)
            {
                DisplayException(this.Name, ex);
                throw;
            }
        }         

        //  <summary>
        ///  Scroll to the bottom of the list box and trim as needed.
        ///  </summary>

        private void ScrollToBottomOfListBox()
        {
            try
            {
                Int32 count = 0;

                lstResults.SelectedIndex = lstResults.Items.Count - 1;

                //  If the list box is getting too large, trim its contents by removing the earliest data.

                if (lstResults.Items.Count > 1000)
                {
                    for (count = 1; count <= 500; count++)
                    {
                        lstResults.Items.RemoveAt(4);
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayException(this.Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Provides a central mechanism for exception handling.
        ///  Displays a message box that describes the exception.
        ///  </summary>
        ///  
        ///  <param name="moduleName"> the module where the exception occurred. </param>
        ///  <param name="e"> the exception </param>

        internal static void DisplayException(String moduleName, Exception e)
        {
            String message = null;
            String caption = null;

            //  Create an error message.

            message = "Exception: " + e.Message + ControlChars.CrLf + "Module: " + moduleName + ControlChars.CrLf + "Method: " + e.TargetSite.Name;

            caption = "Unexpected Exception";

            MessageBox.Show(message, caption, MessageBoxButtons.OK);
            Debug.Write(message);
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (FindTheHid())
                statusText.Text = "Connected!";
        }

        private void buttonWriteData_Click(object sender, EventArgs e)
        {
            if (myDeviceDetected)
            {

                Byte[] EEPROM;

                EEPROM = new Byte[eepromSize];

                string message = messageText.Text.Replace("\r\n","\n");
                message = message.Replace("\\#1", (char)((char)MAXDEFCHARS + ' ') + "");
                message = message.Replace("\\#2", (char)(1 + (char)MAXDEFCHARS + ' ') + "");
                message = message.Replace("\\#3", (char)(2 + (char)MAXDEFCHARS + ' ') + "");
                message = message.Replace("\\#4", (char)(3 + (char)MAXDEFCHARS + ' ') + "");
                message = message.Replace("\\#5", (char)(4 + (char)MAXDEFCHARS + ' ') + "");
                message = message.Replace("\\#6", (char)(5 + (char)MAXDEFCHARS + ' ') + "");
                message = message.Replace("\\#7", (char)(6 + (char)MAXDEFCHARS + ' ') + "");
                message = message.Replace("\\#8", (char)(7 + (char)MAXDEFCHARS + ' ') + "");

                message += '\n';
                int messageLength = message.Length;

                messageTiming = ( 12000 *Convert.ToInt32(timeText.Text))/2048;

                for( int i = 0; i < eepromSize; ++i)
                {

                    if (i == 0)
                        EEPROM[0] = (Byte)(messageLength);
                    else if (i < messageLength +1)
                        EEPROM[i] = (byte)((char)message[i-1]);
                    else if (i < messageSize + 1)
                        EEPROM[i] = (byte)0;
                    else if (i < messageSize + 3)
                    {
                        
                        EEPROM[i++] = (Byte)((int)(messageTiming) & 0xFF);
                        EEPROM[i] = (Byte)(((int)(messageTiming) & 0xff00) >> 8);
                        continue;
                    }
                    else if (i < (203 + 6*8))
                        EEPROM[i] = extraChars[(i - 203) / 6][(i - 203) % 6];
                }




              
                    reportID = 1;
                    FeatureReportBuffer = new Byte[eepromReadSize + 1];
                    FeatureReportBuffer[0] = reportID;
                    for (int n = 0; n < eepromReadSize; ++n)
                        FeatureReportBuffer[n + 1] = EEPROM[n];
                    MyHid.SendFeatureReport(hidHandle, FeatureReportBuffer);

                    reportID = 2;
                    FeatureReportBuffer = new Byte[eepromReadSize + 1];
                    FeatureReportBuffer[0] = reportID;
                    for (int n = 0; n < eepromReadSize; ++n)
                        FeatureReportBuffer[n + 1] = EEPROM[eepromReadSize + n];
                    MyHid.SendFeatureReport(hidHandle, FeatureReportBuffer);

                
               
            }
            else
            {
                MessageBox.Show("No Device Connected!");

            }

            
        }

        private void messageText_TextChanged(object sender, EventArgs e)
        {
            textCharsRemaining.Text = Convert.ToString( 199 - messageText.Text.Replace("\r", "").Length);
        }

        private void buttonReadMessage_Click(object sender, EventArgs e)
        {
            if (myDeviceDetected)
            {
                messageText.Text = "";

                Byte[] EEPROM;

                EEPROM = new Byte[eepromSize];


                    reportID = 1;
                    FeatureReportBuffer = new Byte[eepromReadSize + 1];
                    FeatureReportBuffer[0] = reportID;
                    MyHid.GetFeatureReport(hidHandle, ref FeatureReportBuffer);

                    for (int n = 0; n < eepromReadSize; n++)
                        EEPROM[n] = FeatureReportBuffer[n];

                    reportID = 2;
                    FeatureReportBuffer = new Byte[eepromReadSize + 1];
                    FeatureReportBuffer[0] = reportID;
                    MyHid.GetFeatureReport(hidHandle, ref FeatureReportBuffer);

                    for (int n = 0; n < eepromReadSize; n++)
                        EEPROM[n+eepromReadSize] = FeatureReportBuffer[n];



                string message = "";
                int messageLength = EEPROM[0];
                for (int i = 0; i < messageLength-1; i++)
                    message += (char)EEPROM[1 + i];

                messageText.Text = message.Replace("\n", "\r\n");
                messageTiming = (EEPROM[202]<<8) | EEPROM[201];
                //timeText.Text = messageTiming.ToString();
                timeText.Text = ((messageTiming * 2048) / 12000).ToString();

                messageText.Text = messageText.Text.Replace((char)(0x80) +"", "\\#1");
                messageText.Text = messageText.Text.Replace((char)(0x81) + "", "\\#2");
                messageText.Text = messageText.Text.Replace((char)(0x82) + "", "\\#3");
                messageText.Text = messageText.Text.Replace((char)(0x83) + "", "\\#4");
                messageText.Text = messageText.Text.Replace((char)(0x84) + "", "\\#5");
                messageText.Text = messageText.Text.Replace((char)(0x85) + "", "\\#6");
                messageText.Text = messageText.Text.Replace((char)(0x86) + "", "\\#7");
                messageText.Text = messageText.Text.Replace((char)(0x87) + "", "\\#8");

                for (int i = 0; i < 8; i++)
                    for (int n = 0; n < 6; n++)
                        extraChars[i][n] = EEPROM[203 + (i * 6) + n];

                //string hex = BitConverter.ToString(EEPROM);
                //hex = hex.Replace("-"," 0x");

                //MessageBox.Show(hex);
                foreach (PictureBox pB in pixelList)
                    pB.BackColor = Color.White;

                if (extraChars != null)
                    for (int i = 0; i < 48; i++)
                        if (((int)(extraChars[extraSelected.SelectedIndex][i / 8]) & (1 << (i % 8))) != 0)
                            pixelList[i].BackColor = Color.Black;


            }
            else
            {
                MessageBox.Show("No Device Connected!");

            }

        }


        private void pictureBoxColorToggle(PictureBox pB)
        {
            if (pB.BackColor == Color.White)
                pB.BackColor = Color.Black;
            else
                pB.BackColor = Color.White;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox1);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox2);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox3);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox4);
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox5);
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox6);
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox7);
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox8);
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox16);
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox15);
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox14);
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox13);
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox12);
        }

        private void pictureBox14_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox11);
        }

        private void pictureBox15_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox10);
        }

        private void pictureBox16_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox9);
        }

        private void pictureBox17_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox17);
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox18);
        }

        private void pictureBox19_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox19);
        }

        private void pictureBox20_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox20);
        }

        private void pictureBox21_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox21);
        }

        private void pictureBox22_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox22);
        }

        private void pictureBox23_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox23);
        }

        private void pictureBox24_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox24);
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox25);
        }

        private void pictureBox26_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox26);
        }

        private void pictureBox27_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox27);
        }

        private void pictureBox28_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox28);
        }

        private void pictureBox29_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox29);
        }

        private void pictureBox30_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox30);
        }

        private void pictureBox31_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox31);
        }

        private void pictureBox32_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox32);
        }

        private void pictureBox33_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox33);
        }

        private void pictureBox34_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox34);
        }

        private void pictureBox35_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox35);
        }

        private void pictureBox36_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox36);
        }

        private void pictureBox37_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox37);
        }

        private void pictureBox38_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox38);
        }

        private void pictureBox39_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox39);
        }

        private void pictureBox40_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox40);
        }

        private void pictureBox41_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox41);
        }

        private void pictureBox42_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox42);
        }

        private void pictureBox43_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox43);
        }

        private void pictureBox44_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox44);
        }

        private void pictureBox45_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox45);
        }

        private void pictureBox46_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox46);
        }

        private void pictureBox47_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox47);
        }

        private void pictureBox48_Click(object sender, EventArgs e)
        {
            pictureBoxColorToggle(pictureBox48);
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {

            foreach (PictureBox pB in pixelList)
                pB.BackColor = Color.White;
    
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            foreach (PictureBox pB in pixelList)
                pB.BackColor = Color.White;

            if(extraChars != null)
            for (int i = 0; i < 48; i++)
                if (((int)(extraChars[extraSelected.SelectedIndex][i / 8]) & (1 << (i % 8))) != 0)
                    pixelList[i].BackColor = Color.Black;

        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (extraChars != null)
                extraChars[extraSelected.SelectedIndex] = new Byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < 48; i++)
                    if (pixelList[i].BackColor == Color.Black)
                        extraChars[extraSelected.SelectedIndex][i / 8] |= (byte)(1 << (i % 8));
       //         MessageBox.Show(" " + extraChars[extraSelected.SelectedIndex][0] + " "+ extraChars[extraSelected.SelectedIndex][1]);
        }

        private void buttonInsert_Click(object sender, EventArgs e)
        {
            messageText.Text += "\\#"+(extraSelected.SelectedIndex+1);
            
        }



    }


        




}
