/// <summary>
/// Project: AHP Data Looger
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
/// Author               Date        Version
/// Jason Cai           12/2011     1.0
///                     12/2011      Production design
/// 
/// 
/// This software was created using Visual Studio 2008 Standard Edition with .NET Framework 2.0.
/// 
/// Purpose: 
/// Working with AHP devices through IP.
/// 
/// Requirements:
/// Windows XP or later and an attached USB device that uses the WinUSB driver.
/// 
/// Description:
/// Finds an attached device whose INF file contains a specific device interface GUID.
/// Enables sending and receiving data via bulk, interrupt, and control transfers.
/// 
/// Uses RegisterDeviceNotification() and WM_DEVICE_CHANGE messages
/// to detect when a device is attached or removed.
/// 
/// For bulk and interrupt transfers, the application uses a Delegate and the BeginInvoke 
/// and EndInvoke methods to read data asynchronously, so the application's main thread 
/// doesn't have to wait for the device to return data. A callback routine uses 
/// marshaling to send data to the form, whose code runs in a different thread. 
///  
/// This software, an example INF file, and companion device firmware are available from 
/// www.Lvr.com
/// 
/// Send comments, bug reports, etc. to jan@Lvr.com 
/// This application has been tested under Windows XP and Windows Vista.
/// </summary>



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using ZedGraph;
using System.Net.Sockets;

using JLibrary;

namespace AHPDataLogger
{
    public partial class Form1 : Form
    {


        public static AHPDataProcess TCPQ;
        private Boolean myDeviceDetected = false;
        int tickStart = 0;
        UInt64 dataTime = 0, oldDataTime = 0, startDataTime = 0,tempDataTime=0;
        double doubleDataTime;
        double displayDataTimeInterval = 100, displayScale =300;
        int test = 0;
        double displayYscale = 0.0023841857910;
        double showCurrentData = 0;
        string deviceDirectory = @"c:\AHPDataStorage\AHP1010000001";        // directory of the device

        private JLoggerTaskManager _taskManager;
        //private JDynamicGraph _zdgDynamic;

        public Form1()
        {
            InitializeComponent();

            _taskManager = new JLoggerTaskManager(zgcDynamic);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
//                frmMy = this;
                //Startup();
            }
            catch 
            {
                //throw;
            }

        }

        private void RealTimeData_Click(object sender, EventArgs e)
        {

        }

        // this is a utility class for hex related work
        public class HexEncoding
        {
            public HexEncoding()
            {
                //
                // TODO: Add constructor logic here
                //
            }
            public static int GetByteCount(string hexString)
            {
                int numHexChars = 0;
                char c;
                // remove all none A-F, 0-9, characters
                for (int i = 0; i < hexString.Length; i++)
                {
                    c = hexString[i];
                    if (IsHexDigit(c))
                        numHexChars++;
                }
                // if odd number of characters, discard last character
                if (numHexChars % 2 != 0)
                {
                    numHexChars--;
                }
                return numHexChars / 2; // 2 characters per byte
            }
            /// <summary>
            /// Creates a byte array from the hexadecimal string. Each two characters are combined
            /// to create one byte. First two hexadecimal characters become first byte in returned array.
            /// Non-hexadecimal characters are ignored. 
            /// </summary>
            /// <param name="hexString">string to convert to byte array</param>
            /// <param name="discarded">number of characters in string ignored</param>
            /// <returns>byte array, in the same left-to-right order as the hexString</returns>
            public static byte[] GetBytes(string hexString, out int discarded)
            {
                discarded = 0;
                string newString = "";
                char c;
                // remove all none A-F, 0-9, characters
                for (int i = 0; i < hexString.Length; i++)
                {
                    c = hexString[i];
                    if (IsHexDigit(c))
                        newString += c;
                    else
                        discarded++;
                }
                // if odd number of characters, discard last character
                if (newString.Length % 2 != 0)
                {
                    discarded++;
                    newString = newString.Substring(0, newString.Length - 1);
                }

                int byteLength = newString.Length / 2;
                byte[] bytes = new byte[byteLength];
                string hex;
                int j = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    hex = new String(new Char[] { newString[j], newString[j + 1] });
                    bytes[i] = HexToByte(hex);
                    j = j + 2;
                }
                return bytes;
            }
            public static string ToString(byte[] bytes)
            {
                string hexString = "";
                for (int i = 0; i < bytes.Length; i++)
                {
                    hexString += bytes[i].ToString("X2");
                }
                return hexString;
            }
            /// <summary>
            /// Determines if given string is in proper hexadecimal string format
            /// </summary>
            /// <param name="hexString"></param>
            /// <returns></returns>
            public static bool InHexFormat(string hexString)
            {
                bool hexFormat = true;

                foreach (char digit in hexString)
                {
                    if (!IsHexDigit(digit))
                    {
                        hexFormat = false;
                        break;
                    }
                }
                return hexFormat;
            }

            /// <summary>
            /// Returns true is c is a hexadecimal digit (A-F, a-f, 0-9)
            /// </summary>
            /// <param name="c">Character to test</param>
            /// <returns>true if hex digit, false if not</returns>
            public static bool IsHexDigit(Char c)
            {
                int numChar;
                int numA = Convert.ToInt32('A');
                int num1 = Convert.ToInt32('0');
                c = Char.ToUpper(c);
                numChar = Convert.ToInt32(c);
                if (numChar >= numA && numChar < (numA + 6))
                    return true;
                if (numChar >= num1 && numChar < (num1 + 10))
                    return true;
                return false;
            }
            /// <summary>
            /// Converts 1 or 2 character string into equivalant byte value
            /// </summary>
            /// <param name="hex">1 or 2 character string</param>
            /// <returns>byte</returns>
            private static byte HexToByte(string hex)
            {
                if (hex.Length > 2 || hex.Length <= 0)
                    throw new ArgumentException("hex must be 1 or 2 characters in length");
                byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                return newByte;
            }


        }

        ///  <summary>
        ///  Define a delegate with the same parameters as AccessForm.
        ///  Used in accessing the application's form from a different thread.
        ///  </summary>

        private delegate void MarshalToForm(String action, String textToAdd);

        ///  <summary>
        ///  Performs various application-specific functions that
        ///  involve accessing the application's form.
        ///  </summary>
        ///  
        ///  <param name="action"> A String that names the action to perform on the form. </param>
        ///  <param name="formText"> Text to display on the form or an empty String. </param>
        ///  
        /// <remarks>
        ///  In asynchronous calls to WinUsb_ReadPipe, the callback function 
        ///  uses this routine to access the application's form, which runs in 
        ///  a different thread.
        /// </remarks>
        /// 
        private void AccessForm(String action, String formText)
        {
            try
            {
                //  Select an action to perform on the form:

                switch (action)
                {
                    case "AddItemToListBox":

                        lstResults.Items.Add(formText);

                        break;

                    case "ChangeTextBox":
                        CurrentData.Text = formText;
                        break;
                    //case "ChangeLable":
                    //    //                       label2.Text = formText;
                    //    break;
                    case "ChangeDataTextBox":
                        textBox3.Text = formText;
                        break;

                    //case "AddItemToTextBox":

                    //    txtBulkDataToSend.SelectedText = formText + "\r\n";

                    //    break;
                    //case "EnableCmdSendandReceiveViaBulkTransfers":

                    //    cmdSendandReceiveViaBulkTransfers.Enabled = true;
                    //    cmdSendandReceiveViaBulkTransfers.Focus();

                    //    break;
                    //case "EnableCmdSendandReceiveViaInterruptTransfers":

                    //    cmdSendAndReceiveViaInterruptTransfers.Enabled = true;
                    //    cmdSendAndReceiveViaInterruptTransfers.Focus();

                    //    break;
                    //case "ScrollToBottomOfListBox":

                    //    lstResults.SelectedIndex = lstResults.Items.Count - 1;

                    //    break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        ///  <summary>
        ///  Enables accessing a form from another thread 
        ///  </summary>
        ///  
        ///  <param name="action"> A String that names the action to perform on the form. </param>
        ///  <param name="textToDisplay"> Text that the form displays or uses for 
        ///  another purpose. Actions that don't use text ignore this parameter. </param>

        private void MyMarshalToForm(String action, String textToDisplay)
        {
            object[] args = { action, textToDisplay };
            MarshalToForm MarshalToFormDelegate = null;

            try
            {
                //  The AccessForm routine contains the code that accesses the form.

                MarshalToFormDelegate = new MarshalToForm(AccessForm);

                //  Execute AccessForm, passing the parameters in args.

                base.Invoke(MarshalToFormDelegate, args);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ConnectDevice_Click(object sender, EventArgs e)
        {
            try
            {
                AHPTCPClient AHPClient = new AHPTCPClient();
                // disable the buttonFF
                    ConnectDevice.Enabled = false;

                // now we try to conect to the device
                if (AHPClient.ConnectToServer(textBox1.Text, System.Convert.ToInt32(textBox2.Text)))
 //                   if (AHPClient.ConnectToServer("192.168.1.151",5200))

                {
                    AHPClient.eventDataReceived += OnMessageReceived;
                    MyMarshalToForm("AddItemToListBox", "设备连接成功");
                    // prepare directory for data file saving
                    if (!Directory.Exists(deviceDirectory))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(deviceDirectory);
                    }
                    Thread TCPdataProcessThread = new Thread(QDataProcess);
                    TCPdataProcessThread.Start();
                    myDeviceDetected = true;
                    tickStart = Environment.TickCount;

                }

                else
                {

                    MyMarshalToForm("AddItemToListBox", "设备连接失败");
                    // re-enable the button
                    ConnectDevice.Enabled = true;
                    // set device detect flag
                    myDeviceDetected = false;

                }
            }
            catch
            {
            }
            

        }



        public void QDataProcess()
        {

            Byte[] buffer2 = new Byte[4];
            Byte[] dataRead2 = new Byte[64];
            String receivedtext = "";
            int zeddata=0;
            double doublezeddata=0;
            UInt32 dataConverted = 0;
            Int32 dataConvertedint = 0;
            int i;


            // data file related
                        string timeFileName,oldTimeFileName=null;
//            string deviceDirectory=@"c:\AHPDataStorage\AHP1010000001" ;        // directory of the device
            long currentHourFileName ;
            DateTime currentFileTime;
            FileStream fs = null; 
            BinaryWriter w = null;




            //if (File.Exists(timeFileName))
            //{
            //    FileStream fs = new FileStream(timeFileName, FileMode.Append);
            //    BinaryWriter w = new BinaryWriter(fs);
            //    for (int i = 0; i < 11; i++)
            //    {
            //        w.Write((int)i);
            //    }
            //    w.Close();
            //    fs.Close();

            //    return;
            //}
            //else
            //{
            //    FileStream fs = new FileStream(timeFileName, FileMode.Create);
            //    BinaryWriter w = new BinaryWriter(fs);
            //    for (int i = 0; i < 11; i++)
            //    {
            //        w.Write((int)i);
                    
            //    }
            //    w.Close();
            //    fs.Close();


                // data file related
            // save data to the file
            currentFileTime = DateTime.Now;

            currentHourFileName = currentFileTime.Year * 1000000 + currentFileTime.Month * 10000 + currentFileTime.Day * 100 + currentFileTime.Hour;


            timeFileName = deviceDirectory + @"\" + currentHourFileName.ToString() + @".AHP";

            if (File.Exists(timeFileName))
            {
                fs = new FileStream(timeFileName, FileMode.Append);
                w = new BinaryWriter(fs);
                oldTimeFileName = timeFileName;

//                w.Write(dataRead2);

            }
            else
            {
                oldTimeFileName = timeFileName;
                fs = new FileStream(timeFileName, FileMode.CreateNew);
                w = new BinaryWriter(fs);
//                w.Write(dataRead2);
            }



            
            
            while (myDeviceDetected)
            {
                try
                {
                    while (TCPQ.incomingTCPMessageQueue.IsQueueEmpty() == false)
                    {

                        IncomeTCPMessage TCPmessage2 = (IncomeTCPMessage)TCPQ.incomingTCPMessageQueue.Dequeue();
                        dataRead2 = TCPmessage2.TCPData;
                        // got new data, process from here

                        // save data to the file
                        currentFileTime = DateTime.Now;

                        currentHourFileName = currentFileTime.Year * 1000000 + currentFileTime.Month * 10000 + currentFileTime.Day * 100 + currentFileTime.Hour;
                             
                        
                        timeFileName = deviceDirectory+@"\" + currentHourFileName.ToString()+@".AHP" ;

                        if ((dataRead2[8] != 0) || (dataRead2[7] != 0))
                        {
                            MessageBox.Show("Large data time saving file error ");
                        }
                        if (oldTimeFileName==timeFileName)
                        {
                            for (int j=0;j<64;j++)
                            {
                                w.Write(dataRead2[j]);
                            }
                        

                        }
                        else
                        {
                            w.Close();
                            fs.Close();
                            fs = new FileStream(timeFileName, FileMode.CreateNew);
                            w = new BinaryWriter(fs);
                            for (int j=0;j<64;j++)
                            {
                                w.Write(dataRead2[j]);
                            }
                            oldTimeFileName=timeFileName;
                            
                        }





                        



                                // now we process the data
                                                //initial byte array
                        //------------------
                        //0F-00-00-00-00-80-00-00-10-00-00-F0-FF-00-CA-9A-3B-00-36-65-
                        //C4-F1-FF-FF-FF-7F

                        //index   array elements            int
                        //-----   --------------            ---
                        //    1      00-00-00-00              0
                        //    0      0F-00-00-00             15
                        //   21      F1-FF-FF-FF            -15
                        //    6      00-00-10-00        1048576
                        //    9      00-00-F0-FF       -1048576
                        //   13      00-CA-9A-3B     1000000000
                        //   17      00-36-65-C4    -1000000000
                        //   22      FF-FF-FF-7F     2147483647
                        //    2      00-00-00-80    -2147483648



                        //                           MyMarshalToForm("AddItemToListBox", "Data received via bulk transfer:");

                        //  Convert the received bytes to a String for display.

                        //                               receivedtext = myEncoder.GetString(buffer);
                        // from here we add data to main Q

                        // data processing need some special function.

                        // code here is for AD7716 
                        buffer2[3] = dataRead2[14];
                        buffer2[2] = dataRead2[15];
                        buffer2[1] = dataRead2[16];
                        buffer2[0] = dataRead2[17];

                        buffer2[1] &= 0xFC;

                        for (i = 0; i < 4; i++)
                        {
                            dataConvertedint = dataConvertedint << 8;
                            dataConvertedint += buffer2[3 - i];
                        }



 //                       dataConvertedint = BitConverter.ToInt32(buffer2, 0);
//                        dataConvertedint = dataConvertedint >> 10;

                        //// if the Most signifcent bit is 1, make all extra bit to 1, vs MSB 0, extra bit 0
                        //if ((buffer2[3] & 0x80) == 0x80)
                        //{
                        //    dataConverted |= 0xFFFC0000;
                        //}
                        //else
                        //{
                        //    dataConverted &= 0x003FFFFF;
                        //}


                        //                dataConverted = BitConverter.ToInt32(dataProcess, 0);
                        //               dataConverted = dataConverted / 400;

//                        dataConvertedint = (Int32)(dataConverted);
                        
                        
                        
                        
                        //dataConverted = BitConverter.ToInt32(buffer2, 0);
                        // PA the incoming dtattime is with precision 0.1ms
//                        tempDataTime = dataRead2[13]+dataRead2[12]*0x100+dataRead2[11]*10000+dataRead2[10]*1000000;
                        //the following code put timestamp in tempDataTime;
                        tempDataTime = 0;
                        for (i = 0; i < 8; i++)
                        {
                            tempDataTime = dataRead2[6 + i] + tempDataTime;
                            tempDataTime = tempDataTime << 8;
                        }


                        if (startDataTime == 0)
                        {
                            if (tempDataTime > 0)
                            {
                                startDataTime = tempDataTime;
                                oldDataTime = 0;
                                dataTime = 0;
                            }
                        }
                        else
                        {
                            dataTime = tempDataTime - startDataTime;

                            if ((dataTime - oldDataTime) >= displayDataTimeInterval)
                            {
                                oldDataTime = dataTime;
                                //now we put data into display array
                                // Get the first CurveItem in the graph
                                LineItem curve = zgcDynamic.GraphPane.CurveList[0] as LineItem;
                                if (curve != null)
                                {


                                    // Get the PointPairList
                                    IPointListEdit list = curve.Points as IPointListEdit;
                                    // If this is null, it means the reference at curve.Points does not
                                    // support IPointListEdit, so we won't be able to modify it
                                    if (list != null)
                                    {

                                        zeddata = dataConvertedint;
                                        doubleDataTime = (double)(dataTime) / 100000;
                                        doublezeddata = (double)(zeddata) * 5000 / 2097152;
                                        
                                        // test code
                                        if (doublezeddata < -4000) 
                                        {
                                            doublezeddata = (double)(zeddata) * 5000 / 2097152;

                                        }

                                        showCurrentData = doublezeddata;
                                        list.Add(doubleDataTime, doublezeddata);
                                    }
                                }
                            }
                        }













                        //receivedtext = BitConverter.ToString(dataRead2);

                        //MyMarshalToForm("ChangeLable", receivedtext);

                    }

                   // else
                   // {
                        Thread.Sleep(100);
                   // }

                }
                catch (Exception ex)
                {
                    TraceFile.Error("IO", ex);
                }
            }
            w.Close();
            fs.Close();


        }
        public void OnMessageReceived(object sender, UInt32 dwID, byte[] bsBody)
        {
            byte[] dataProcess = new byte[4];
            
           UInt32 dataConverted=0;
           Int32 dataConvertedint = 0;

            if (sender == null)
                return;
            String responseData = String.Empty;

            //          Sink sink = sender as Sink;
            //          String networkManagerName = sink.ClientName;

            try
            {
                //                responseData = System.Text.Encoding.ASCII.GetString(bsBody, 0, 64);
//                responseData = BitConverter.ToString(bsBody);


//                MyMarshalToForm("ChangeTextBox", responseData);
                //dataRead = BitConverter.ToInt32(buffer, 0);

                //This example of the BitConverter.ToInt32( byte[ ], int )
                //method generates the following output. It converts elements
                //of a byte array to int values.

                //initial byte array
                //------------------
                //0F-00-00-00-00-80-00-00-10-00-00-F0-FF-00-CA-9A-3B-00-36-65-
                //C4-F1-FF-FF-FF-7F

                //index   array elements            int
                //-----   --------------            ---
                //    1      00-00-00-00              0
                //    0      0F-00-00-00             15
                //   21      F1-FF-FF-FF            -15
                //    6      00-00-10-00        1048576
                //    9      00-00-F0-FF       -1048576
                //   13      00-CA-9A-3B     1000000000
                //   17      00-36-65-C4    -1000000000
                //   22      FF-FF-FF-7F     2147483647
                //    2      00-00-00-80    -2147483648



                //                           MyMarshalToForm("AddItemToListBox", "Data received via bulk transfer:");

                //  Convert the received bytes to a String for display.

                //                               receivedtext = myEncoder.GetString(buffer);
                // from here we add data to main Q

                // data processing need some special function.

                // code here is for AD7716 
                //dataProcess[3] = bsBody[14];
                //dataProcess[2] = bsBody[15];
                //dataProcess[1] = bsBody[16];
                //dataProcess[0] = bsBody[17];

                


                //                           receivedTextConverted = Int32.ToString(dataConverted);
//                MyMarshalToForm("ChangeDataTextBox", dataConverted.ToString());
                IncomeTCPMessage TCPMessage = new IncomeTCPMessage(bsBody);
                if ((TCPMessage.TCPData[8] != 0) || (TCPMessage.TCPData[7] != 0))
                {
                    MessageBox.Show("Large data time error ");
                }
                TCPQ.OnNewIncomingTCPMessage(TCPMessage);


                //another way is walk around the Q system and directly put data into the display




            }
            catch
            {
            }
        }

        private void zedGraphControl1_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int zeddata;
            double doublezeddata;
            Byte[] buffer2 = new Byte[64];
            Byte[] dataRead2 = new Byte[64];
            String receivedtext = "";
            Byte[] dataProcess = new Byte[64];
            int i;
            Int32 receivedTextConverted;

            long dataConverted ;

            try
            {

                if (startDataTime != 0)
                {
                    // Make sure that the curvelist has at least one curve
                    if (zgcDynamic.GraphPane.CurveList.Count <= 0)
                        return;

                    // Get the first CurveItem in the graph
                    LineItem curve = zgcDynamic.GraphPane.CurveList[0] as LineItem;
                    if (curve == null)
                        return;

                    // Get the PointPairList
                    IPointListEdit list = curve.Points as IPointListEdit;
                    // If this is null, it means the reference at curve.Points does not
                    // support IPointListEdit, so we won't be able to modify it
                    if (list == null)
                        return;

                    // Time is measured in seconds
                    //double time = (Environment.TickCount - tickStart) / 1000.0;


                    //// put the read data in


                    //if (TCPQ.incomingTCPMessageQueue.IsQueueEmpty() == false)
                    //{
                    //    IncomeTCPMessage TCPMessage2 = new IncomeTCPMessage(buffer2);


                    //    //while (TCPQ.incomingTCPMessageQueue.IsQueueEmpty() == false)
                    //    //{
                    //        //IncomeTCPMessage TCPMessage2 = new IncomeTCPMessage(buffer2);

                    //        IncomeTCPMessage TCPmessage2 = (IncomeTCPMessage)TCPQ.incomingTCPMessageQueue.Dequeue();

                    //        dataRead2 = TCPmessage2.TCPData;
                    //    //}

                    //    // process data here, from 2's complement to normal long data
                    //    dataProcess[3] = dataRead2[14];
                    //    dataProcess[2] = dataRead2[15];
                    //    dataProcess[1] = dataRead2[16];
                    //    dataProcess[0] = dataRead2[17];

                    //    //dataProcess[1] &= 0xFC;

                    //    //// if the Most signifcent bit is 1, make all extra bit to 1, vs MSB 0, extra bit 0
                    //    //if ((dataProcess[3] & 0x80) == 0x80)
                    //    //{
                    //    //    dataProcess[1] |= 0x03;
                    //    //    dataProcess[0] = 0xFF;
                    //    //}
                    //    //else
                    //    //{
                    //    //    dataProcess[1] &= 0xFC;
                    //    //    dataProcess[0] = 0x00;
                    //    //}

                    //    dataConverted = BitConverter.ToInt32(dataProcess, 0)/4;




                    //    zeddata = (int)(dataConverted / 64);
                    //    list.Add(time, zeddata);

                    //display the very last value
//                    receivedTextConverted = double.ToString(showCurrentData);
                    
                    MyMarshalToForm("ChangeDataTextBox",showCurrentData.ToString());

                     


                    //           list.Add(time, Math.Sin(2.0 * Math.PI * time / 3.0));

                    // Keep the X scale at a rolling 30 second interval, with one
                    // major step between the max X value and the end of the axis
                    Scale xScale = zgcDynamic.GraphPane.XAxis.Scale;
                    //if ((doubleDataTime / displayDataTimeInterval) > (xScale.Max - xScale.MajorStep))
                    //{
                    //    xScale.Max = (doubleDataTime / displayDataTimeInterval) + xScale.MajorStep;
                    //    xScale.Min = xScale.Max - displayScale;
                    //}

                    if ((doubleDataTime ) > (xScale.Max - xScale.MajorStep))
                    {
                        xScale.Max = (doubleDataTime ) + xScale.MajorStep;
                        xScale.Min = xScale.Max - displayScale;
                    }


                    if (test != 0)
                    {
                        test =0;
                    }

                    // Make sure the Y axis is rescaled to accommodate actual data
                    zgcDynamic.AxisChange();
                    // Force a redraw
                    zgcDynamic.Invalidate();
                }
                }

 //           }
            catch (Exception ex)
            {
                TraceFile.Error("USBIO", ex);
            }


        }

        

        private void Startup()
        {
            try
            {
//                myWinUsbDevice = new WinUsbDevice();
//                InitializeDisplay();
                TCPQ = new AHPDataProcess();


                //===================== graph setting

                //zedGraphControl1.IsShowHScrollBar = true;
                //zedGraphControl1.IsEnableVPan = false;
                //zedGraphControl1.IsEnableVZoom = false;
                //zedGraphControl1.IsEnableWheelZoom = true;
                MasterPane masterPane = zgcDynamic.MasterPane;

                GraphPane myPane = zgcDynamic.GraphPane;
                myPane.Title.Text = "数据采集系统实时数据显示 (数据在25秒钟之后开始滚动显示)";
                myPane.XAxis.Title.Text = "时间,秒";
                myPane.YAxis.Title.Text = "实时输入电压值, 伏";

                // Save 1200 points.  At 50 ms sample rate, this is one minute
                // The RollingPointPairList is an efficient storage class that always
                // keeps a rolling set of point data without needing to shift any data values
                RollingPointPairList list = new RollingPointPairList(4800);

                // Initially, a curve is added with no data points (list is empty)
                // Color is blue, and there will be no symbols
                LineItem curve = myPane.AddCurve("电压，伏", list, Color.Blue, SymbolType.None);

                // Sample at 50ms intervals
                timer1.Interval = 100;
                timer1.Enabled = true;
                timer1.Start();

    


                // Just manually control the X axis range so it scrolls continuously
                // instead of discrete step-sized jumps
                myPane.XAxis.Scale.Min = 0;
                myPane.XAxis.Scale.Max = displayScale;
                myPane.XAxis.Scale.MinorStep = 1;
                myPane.XAxis.Scale.MajorStep = 5;
                                
                // Set the initial viewed range

                // Scale the axes
                zgcDynamic.AxisChange();

                // Save the beginning time for reference
                tickStart = Environment.TickCount;

                //=========================== graph init
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void Shutdown(object sender, FormClosedEventArgs e)
        {
            try
            {

                myDeviceDetected = false;
                //myWinUsbDevice.CloseDeviceHandle();

                //myDeviceManagement.StopReceivingDeviceNotifications
                //    (deviceNotificationHandle);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void Shutdown()
        {

        }

        private void VZoom_Click(object sender, EventArgs e)
        {
            zgcDynamic.IsEnableVZoom = true;
            zgcDynamic.IsEnableHZoom = false;
        }

        private void HZoom_Click(object sender, EventArgs e)
        {
            zgcDynamic.IsEnableVZoom = false;
            zgcDynamic.IsEnableHZoom = true;


        }

        private void button1_Click(object sender, EventArgs e)
        {
            //this is for test the date time function
        }

        private void SavedData_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            Byte[] fileData;
            Byte[] buffer3 = new Byte[4];
//            Byte[] dataRead2 = new Byte[64];
            int zeddata=0;
            double doublezeddata=0;
            UInt32 dataConverted = 0;
            Int32 dataConvertedint = 0;
            UInt64 dateTimeDisplay =0;
            UInt64 startDateTimeDisplay = 0;
            double doubleDateTimeDisplay=0;
                



            openFileDialog1.InitialDirectory = "c:\\AHPDataStorage\\AHP1010000001\\";
            openFileDialog1.Filter = "(*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;


            GraphPane myPaneReplay = zedGraphControl2.GraphPane;
            myPaneReplay.Title.Text = "数据采集系统回放数据显示";
            myPaneReplay.XAxis.Title.Text = "时间,秒";
            myPaneReplay.YAxis.Title.Text = "输入电压值, 伏";
            PointPairList listReplay = new PointPairList();

//            MessageBox.Show("Error: Could not read file from disk. Original error: ");
            // Initially, a curve is added with no data points (list is empty)
            // Color is blue, and there will be no symbols
            LineItem curveReplay = myPaneReplay.AddCurve("电压，伏", listReplay, Color.Blue, SymbolType.None);
 //           long dataNumber=7000000, page=2000000;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader fileReader = new BinaryReader(fs))
                        {
                            fileReader.BaseStream.Seek(0, SeekOrigin.Begin);
                            //fileReader.BaseStream.Seek(dataNumber, SeekOrigin.Current);
                            fileData=fileReader.ReadBytes((int)(fs.Length));
                            //fileData = fileReader.ReadBytes((int)page);

                            // data is in char array now, distract real datat from this arrary


                            for (int i = 0; i < (int)(fs.Length - 64); i++)
                            {

                               
                                if ((fileData[i] == 0x41) && (fileData[i + 1] == 0x53))
                                {
                                    // code here is for AD7716 
                                    buffer3[3] = fileData[i + 14];
                                    buffer3[2] = fileData[i + 15];
                                    buffer3[1] = fileData[i + 16];
                                    buffer3[0] = fileData[i + 17];

                                    buffer3[1] &= 0xFC;
                                    buffer3[0] = 0;


                                    if ((fileData[i + 8] > 0) || (fileData[i + 7] > 0))
                                    {
                                        // MessageBox.Show("Large data time saving file error ");
                                    }
                                    else
                                    {



                                        dataConvertedint = 0;
                                        for (int j = 0; j < 4; j++)
                                        {
                                            dataConvertedint = dataConvertedint << 8;
                                            dataConvertedint += buffer3[3 - j];
                                        }
                                        dateTimeDisplay = 0;

                                        for (int k = 0; k < 8; k++)
                                        {
                                            dateTimeDisplay = dateTimeDisplay << 8;

                                            dateTimeDisplay += fileData[6 + i + k];
                                        }
                                        if (startDateTimeDisplay == 0)
                                        {
                                            startDateTimeDisplay = dateTimeDisplay;
                                        }

                                        zeddata = dataConvertedint;
                                        doubleDateTimeDisplay = (double)(dateTimeDisplay - startDateTimeDisplay) / 10000;
                                        doublezeddata = (double)(zeddata) * 5000 / 2097152;
                                        if (doubleDateTimeDisplay > 10000000)
                                        {
                                            int l = 0;
                                        }

                                        listReplay.Add(doubleDateTimeDisplay, doublezeddata);
                                    }
                                }
                            }
                            // Make sure the Y axis is rescaled to accommodate actual data
                            zedGraphControl2.AxisChange();
                            // Force a redraw
                            zedGraphControl2.Invalidate();




                        }
                    }
                    
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            // Insert code to read the stream here.
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }

        }

        private void zedGraphControl2_Load(object sender, EventArgs e)
        {
            MasterPane master = zedGraphControl2.MasterPane;
            master.PaneList.Clear();


            master.Fill = new Fill(Color.White, Color.FromArgb(220, 220, 255), 45.0f);
            master.PaneList.Clear();
            master.Title.Text = "Multi-channel show";

            master.Margin.All = 0;
            master.InnerPaneGap = 0;
            master.TitleGap = 0;
            master.Legend.Gap = 0;
            ColorSymbolRotator rotator = new ColorSymbolRotator();

            // Create some GraphPane's (normally you would add some curves too
            GraphPane pane1 = new GraphPane();
            GraphPane pane2 = new GraphPane();
            GraphPane pane3 = new GraphPane();
            GraphPane pane4 = new GraphPane();
            GraphPane pane5 = new GraphPane();

            // Add all the GraphPanes to the MasterPane
            master.Add(pane1);
            master.Add(pane2);
            master.Add(pane3);
            master.Add(pane4);
            master.Add(pane5);

            zedGraphControl2.AxisChange();
      // Layout the GraphPanes using a default Pane Layout
            using (Graphics g = this.CreateGraphics())
            {
                master.SetLayout(g, PaneLayout.SingleColumn);

            }

            master.Draw(this.CreateGraphics());
            zedGraphControl2.Invalidate();


            for (int j = 0; j < 3; j++)
            {
                // Create a new graph with topLeft at (40,40) and size 600x400
                GraphPane myPaneT = new GraphPane(new Rectangle(40, 40, 600, 400),
                    "Case #" + (j + 1).ToString(),
                    "Time, Days",
                    "Rate, m/s");

                myPaneT.Fill.IsVisible = false;

                myPaneT.Chart.Fill = new Fill(Color.White, Color.LightYellow, 45.0F);
                myPaneT.BaseDimension = 3.0F;
                myPaneT.XAxis.Title.IsVisible = false;
                myPaneT.XAxis.Scale.IsVisible = false;
                myPaneT.Legend.IsVisible = false;
                myPaneT.Border.IsVisible = false;
                myPaneT.Title.IsVisible = false;
                myPaneT.XAxis.MajorTic.IsOutside = false;
                myPaneT.XAxis.MinorTic.IsOutside = false;
                myPaneT.XAxis.MajorGrid.IsVisible = true;   //show the grids
                myPaneT.XAxis.MinorGrid.IsVisible = true;
                myPaneT.Margin.All = 0;
                if (j == 0)               // the top one show some margin on the end
                    myPaneT.Margin.Top = 20;
                if (j == 2)               // the last one shows the x axis information like tile and scale
                {
                    myPaneT.XAxis.Title.IsVisible = true;
                    myPaneT.XAxis.Scale.IsVisible = true;
                    myPaneT.Margin.Bottom = 10;

                }

                if (j > 0)
                    //					myPaneT.YAxis.Scale.IsSkipLastLabel = true;     //skip last lable so that it won't inteferance with the pane next to it
                    myPaneT.YAxis.Scale.IsVisible = false;     //skip last lable so that it won't inteferance with the pane next to it

                // This sets the minimum amount of space for the left and right side, respectively
                // The reason for this is so that the ChartRect's all end up being the same size.
                myPaneT.YAxis.MinSpace = 60;
                myPaneT.Y2Axis.MinSpace = 20;

                myPaneT.Margin.Bottom = 0;
                myPaneT.Margin.Top = 0;

                // very important modification, can make sub panel attache to each other
                myPaneT.XAxis.Scale.LabelGap = 0;
                myPaneT.XAxis.Title.Gap = 0;



                // Make up some data arrays based on the Sine function
                double x, y;
                PointPairList list = new PointPairList();
                for (int i = 0; i < 36; i++)
                {
                    x = (double)i + 5 + j * 3;
                    y = 3.0 * (1.5 + Math.Sin((double)i * 0.2 + (double)j));
                    list.Add(x, y);
                }

                LineItem myCurve = myPaneT.AddCurve("Type " + j.ToString(),
                    list, rotator.NextColor, rotator.NextSymbol);
                myCurve.Symbol.Fill = new Fill(Color.White);

                ///                master.PaneList;

                master.Add(myPaneT);      //add the sub panel to the master panel, so that it can make multi panel in one big one.

            }


        }

        private void button3_Click(object sender, EventArgs e)
        {
            MasterPane master = zedGraphControl2.MasterPane;
            master.PaneList.Clear();


            master.Fill = new Fill(Color.White, Color.FromArgb(220, 220, 255), 45.0f);
            master.PaneList.Clear();
            master.Title.Text = "Multi-channel show";

            master.Margin.All = 0;
            master.InnerPaneGap = 0;
            master.TitleGap = 0;
            master.Legend.Gap = 0;
            ColorSymbolRotator rotator = new ColorSymbolRotator();

            // Create some GraphPane's (normally you would add some curves too
            GraphPane pane1 = new GraphPane();
            GraphPane pane2 = new GraphPane();
            GraphPane pane3 = new GraphPane();
            GraphPane pane4 = new GraphPane();
            GraphPane pane5 = new GraphPane();

            // Add all the GraphPanes to the MasterPane
            master.Add(pane1);
            master.Add(pane2);
            master.Add(pane3);
            master.Add(pane4);
            master.Add(pane5);

            zedGraphControl2.AxisChange();
            // Layout the GraphPanes using a default Pane Layout
            using (Graphics g = this.CreateGraphics())
            {
                master.SetLayout(g, PaneLayout.SquareColPreferred);

            }

            master.Draw(this.CreateGraphics());
            zedGraphControl2.Invalidate();


        }

        private void EnableStartTask(bool enabled)
        {
            menuTest_Start.Enabled = enabled;
            menuTest_Stop.Enabled = !enabled;
        }


        private void StartTask()
        {
            EnableStartTask(false);
            try
            {
                _taskManager.Start();

                MessageBox.Show("Succeeded in starting all tasks!");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Failed to start all tasks: {0}", e.Message));
                EnableStartTask(true);
            }
        }

        private void StopTask()
        {
            EnableStartTask(true);
            try
            {
                _taskManager.Stop();

                MessageBox.Show("Succeeded in stopping all tasks!");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Failed to stop all tasks: {0}", e.Message));
                EnableStartTask(false);
            }
        }


        private void menuTest_Start_Click(object sender, EventArgs e)
        {
            StartTask();
        }

        private void menuTest_Stop_Click(object sender, EventArgs e)
        {
            StopTask();
        }




       








    }
}
