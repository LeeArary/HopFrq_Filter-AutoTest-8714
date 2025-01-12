﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using NPOI.XSSF.Streaming;
using NPOI.HSSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF;
using NPOI.XSSF.UserModel;
using System.Threading;
using System.Runtime.InteropServices;
namespace Upper
{
    //下载Flash使用，用于接收下位机的校验数据
    public struct MessagsData
    {
        public byte FuncByte;
        public byte[] data;

    }
    
    public partial class Main_Form : Form
    {
        //USB中继设备使用
        USBHID usbHID = null;
        List<String> Device_list = new List<string>();
        private string Relay_ID;//USB中继设备ID
        private string Relay_path;//USB中继设备地址
        #region 系统信息定义
        //仪器与中继器配置信息
        public struct Config_Info
        {
            public int Config_Seq;//当前配置的序号，string[]下标
            public string[] Config_content;//配置信息
            public UInt16 Config_sta;//当前在配置什么
            #region 说明
            /*
             0：没有配置
             1：一键仪器配置
             2：串行接口第一段配置
             3：串行接口第二段配置
             */

            #endregion
        }

        //串行设备信息
        public struct Serial_Info
        {
            public double CurrenFrq;//当前比较值
            public double CentFrq1;//用于对比的频率，第一段108-174MHz
            public double CentFrq2;//用于对比的频率，第二段225-400.2MHz
            public double step;//增长步进Mhz
            public double ReBack;//回退次数


            //=======仪器配置界面参数=======//
            public double Bw1;//Bw1
            public double Bw2;//Bw2

            public double Start1Frq;//第一段起始频率
            public double Stop1Frq;//第一段终止频率

            public double Start2Frq;//第二段起始频率
            public double Stop2Frq;//第二段终止频率

            public double PWR_dBm;//矢网输出功率
            public double OffSet;//温飘
            //=========================//



            public int sample_cnt;//采样计数

            public UInt16 hex;//自动测试下的发往跳频滤波器的hex
            public HSSFWorkbook MyWorkbook;//Excel表实例

            public Form2 Autotest_Form;//自动测试后的界面展示


            //用于自动报表生成

            public int test_wait;

            public int test_sta;//固定频点
            public int test_step;//一个固定频点测试的测试步骤

            public double test_3dB;//BW-3dB
            public double test_40dB;//BW-40dB
            public double test_40_30dB;
            public double test_Loss;//插损
            public double test_FrqErr;//频率漂移
            public double test_standWave;//驻波

            public int temperture_mode;//三种温度
        }

        //并行设备信息
        public struct Parall_Info
        {
            //并行自动测试用的数组

            public List<AutoTest_Datas> AutoTest_datas;
            public double preFrq;//用于自动读取时，记录上一个频率值检查是否读取错误

            public double CurrenFrq;//当前比较值
            public double CentFrq1;//用于对比的频率，第一段108-174MHz
            public double CentFrq2;//用于对比的频率，第二段225-400MHz
            public double step1;//增长步进0.432Mhz
            public double step2;//增长步进0.7Mhz

            //=======仪器配置界面参数=======//
            public double Bw1;//Bw1


            public double Start1Frq;//第一段起始频率
            public double Stop1Frq;//第一段终止频率

            public double Start2Frq;//第二段起始频率
            public double Stop2Frq;//第二段终止频率

            public double PWR_dBm;//矢网输出功率
            public double OffSet;//温飘
            //=========================//



            public Form3 Autotest_resForm;

            public int sample_cnt;//采样计数

            public UInt16 hex;//自动测试下的发往跳频滤波器的hex
            public HSSFWorkbook MyWorkbook;//Excel表实例


            //用于自动报表生成



            public int test_sta;//频点
            public int test_step;//一个频点测试步骤

            public double test_Frq;//频率漂移
            public double test_Loss;//插损
            public double test_3d;
            public double test_Standwave;


            public double pos5;
            public double pos10;
            public double neg5;
            public double neg10;

            public int temperture_mode;//三种温度



            public string DownloadFilePath;//编程文件路径
            public int DownloadFileRow;//编程文件行数

            public List<UInt16> CaliData;


            public Random random;

        }
        //并行自动测试，存储采集数据
        public struct AutoTest_Datas
        {
            public int Seq;
            public UInt16 Hex;
            public double Frq;
            public double Loss;
        }


        //系统总体信息
        public struct Device_Info
        {
            //记录桌面路
            public string DesktopPath;

            //【信息】配置信息
            public Config_Info Config_info;

            //【信息】串行接口滤波器
            public Serial_Info Serial_info;

            //【信息】并行接口滤波器
            public Parall_Info Parall_info;

            public int Receive_mode;//串口读取数据的三种类型
            #region 说明
            //0：没有读取任务
            //1：读取自动测试
            //2：读取报表测试
            //3：读取手动测试的频率
            //4：并行接口自动测试

            #endregion

            //USB状态
            public bool connect_STM;
            public bool find_usb;//发现中继器的usb
            public bool error;
            public byte[] ReciveData;
            public byte[] SendData;


            //串口状态
            public bool connect_uart;//串口连接
            public bool find_uart;//发现GPIB连接器的串口

            public bool connect_NA;//连接矢网
            public bool find_NA;//发现矢量网络分析仪

            public int Scan_STA;

            public string[] portList;//串口设备列表
            public int cnt;//记录列表的下标

            public int wait_cnt;//等待回复时间计数

            public int SW_HeartCnt;//失网连接的心跳





        }
        public static Device_Info Device_info;
        #endregion
        //构造函数
        public Main_Form()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            //USB设备初始化
            usbHID = new USBHID(); //实例化USBHID

            Relay_ID = "vid_aabb&pid_0001";//中继器的唯一ID


            //初始未连接，未发现
            Device_info.connect_STM = false;
            Device_info.find_usb = false;

            Device_info.connect_NA = false;
            Device_info.find_NA = false;

            Device_info.connect_uart = false;
            Device_info.find_uart = false;

            Device_info.Scan_STA = 0;//还未开始扫描

            //分配接收与发送数组
            Device_info.ReciveData = new byte[10];
            Device_info.SendData = new byte[8];


            //测试阶段，暂时默认发送数据是这个
            Device_info.SendData[0] = 0xFA;//帧头
            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = 0x00;//数据1
            Device_info.SendData[3] = 0x00;//数据2
            Device_info.SendData[4] = 0x00;//数据3
            Device_info.SendData[5] = 0x00;//数据4
            Device_info.SendData[6] = 0x00;//数据5
            Device_info.SendData[7] = 0xFA;//数据5



            Device_info.Config_info.Config_content = new string[]{
                
                "SENS1:STAT ON",//MEAS1
                "SOUR1:POW:LEV:IMM:AMPL ",//变量 输出功率 
                "DISP:WIND1:TRAC:Y:RPOS 8E0",//参考位置 8
                "SENS1:FREQ:STAR ",//变量 起始50M第一段start
                "SENS1:FREQ:STOP ",//变量 终止500M第一段stop
                "CALC1:MARK1:BWID ",//变量 带宽-3dB
                "CALC1:MARK1:FUNC:TRAC ON"//自动追踪
                //"SENS2:STAT ON"//MEAS2
           
            };

            #region 串行数据初始化


            Device_info.Serial_info.CentFrq1 = 108;//第一段起始频率
            Device_info.Serial_info.CentFrq2 = 225;//第二段起始频率
            Device_info.Serial_info.step = 0.4;//步进
            Device_info.Serial_info.OffSet = 0;//温飘




            #endregion

            #region 并行数据初始化

            Device_info.Parall_info.AutoTest_datas = new List<AutoTest_Datas>();


            Device_info.Parall_info.CentFrq1 = 108;//第一段起始频率
            Device_info.Parall_info.CentFrq2 = 225;//第二段起始频率
            Device_info.Parall_info.step1 = 0.264;//第一段步进
            Device_info.Parall_info.step2 = 0.7;//第二段步进
            Device_info.Parall_info.OffSet = 0;//温飘
            Device_info.Parall_info.random = new Random();
            #endregion
            Device_info.Config_info.Config_sta = 0;//没有配置任务

            Device_info.Receive_mode = 0;//没有需要读取串口数据的


            //获取本机桌面路径
            string strDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Device_info.DesktopPath = strDesktopPath;
            label_DesktopPath.Text = Device_info.DesktopPath;

            //串行共605个数据
            progressBar1.Maximum = 605;
            progressBar1.Minimum = 0;

            //并行共2046个数据
            progressBar2.Maximum = 2048;
            progressBar1.Minimum = 0;
            progressBar3.Maximum = 1000;
            progressBar3.Minimum = 0;



            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            Device_info.Parall_info.DownloadFilePath = null;

            Device_info.Parall_info.CaliData = new List<UInt16>();


            //按键变化
            SerialButton_All(true,true,false);
            ParallButton_All(true,true,false);
            DownloadButton_All(false);


        }

        #region 按键变化函数
        public void SerialButton_All(bool IsAuto,bool IsManual,bool sw)
        {
            if (IsAuto)
            {
                button11.Enabled = sw;//【串行】一键配置

            }
            if (IsManual)
            {
                button24.Enabled = sw;//【串行】手动置数-1
                button25.Enabled = sw;//【串行】+1
                button22.Enabled = sw;//【串行】-10
                button23.Enabled = sw;//【串行】+10
                comboBox2.Enabled = sw;//【串行】手动置数下拉框

                button3.Enabled = sw;//【串行】-1
                button2.Enabled = sw;//【串行】+1
                button5.Enabled = sw;//【串行】-10
                button4.Enabled = sw;//【串行】+10

                button7.Enabled = sw;//【串行】108
                button8.Enabled = sw;//【串行】140
                button6.Enabled = sw;//【串行】174
                button9.Enabled = sw;//【串行】225
                button14.Enabled = sw;//【串行】260.2
                button13.Enabled = sw;//【串行】300.2
                button15.Enabled = sw;//【串行】340.2
                button12.Enabled = sw;//【串行】400.2


                button16.Enabled = sw;//【串行】跳频
                button10.Enabled = sw;//【串行】跳频 终止

            }
            if (IsAuto && IsManual)
            {
                butto.Enabled = sw;//【串行】自动测试
                button18.Enabled = sw;//【串行】自动测试终止

                button17.Enabled = sw;//【串行】自动生成报表
                button19.Enabled = sw;//【串行】自动生成报表 终止
            }


        }
        public void ParallButton_All(bool IsAuto, bool IsManual, bool sw)
        {
            if (IsAuto)
            {
                button42.Enabled = sw;//【并行】一键配置
            }
            if (IsManual)
            {
                button45.Enabled = sw;//【并行】手动置数-1
                button46.Enabled = sw;//【并行】+1
                button43.Enabled = sw;//【并行】-10
                button44.Enabled = sw;//【并行】+10
                comboBox4.Enabled = sw;//【并行】手动置数下拉框

                button38.Enabled = sw;//【并行】-1
                button39.Enabled = sw;//【并行】+1
                button36.Enabled = sw;//【并行】-10
                button37.Enabled = sw;//【并行】+10

                button34.Enabled = sw;//【并行】108
                button33.Enabled = sw;//【并行】129.9
                button35.Enabled = sw;//【并行】150.2
                button32.Enabled = sw;//【并行】174
                button30.Enabled = sw;//【并行】225
                button29.Enabled = sw;//【并行】250.2
                button31.Enabled = sw;//【并行】299.9
                button28.Enabled = sw;//【并行】350.3
                button54.Enabled = sw;//【并行】400

                button41.Enabled = sw;//【并行】跳频
                button40.Enabled = sw;//【并行】跳频 终止
            }
            if (IsAuto && IsManual)
            {
                button47.Enabled = sw;//【并行】自动测试
                button48.Enabled = sw;//【并行】自动测试终止

                button27.Enabled = sw;//【并行】自动生成报表
                button26.Enabled = sw;//【并行】自动生成报表 终止

            }


        }
        public void DownloadButton_All(bool sw)
        {
            button51.Enabled = sw;//【下载】擦除
            button50.Enabled = sw;//【下载】下载
            button52.Enabled = sw;//【下载】验证
            button53.Enabled = sw;//【下载】一键下载
        }
        #endregion

        #region 界面绘制逻辑
        //绘制PC与失网连接线
        private void DrawLine_GPIB_SW(bool connect)
        {
            Pen pen;
            if (connect)
            {
                pen = new Pen(Color.Red, 5);
            }
            else
            {
                pen = new Pen(Color.Black, 5);
            }
            Point point1 = new Point(pictureGPIB.Left + pictureGPIB.Size.Width, pictureGPIB.Top + pictureGPIB.Size.Height / 2);
            Point point2 = new Point(pictureSW.Left, pictureGPIB.Top + pictureGPIB.Size.Height / 2);

            Graphics g = Page_Sta.CreateGraphics();
            g.DrawLine(pen, point1, point2);

        }
        //绘制PC与GPIB连接线
        private void DrawLine_PC_GPIB(bool connect)
        {
            Pen pen;
            if (connect)
            {
                pen = new Pen(Color.Red, 5);

            }
            else
            {
                pen = new Pen(Color.Black, 5);
            }
            Point point1 = new Point(picturePc.Left + 30, picturePc.Top);
            Point point2 = new Point(picturePc.Left + 30, pictureGPIB.Top + pictureGPIB.Size.Height / 2);
            Point point3 = new Point(pictureGPIB.Left, pictureGPIB.Top + pictureGPIB.Size.Height / 2);
            Graphics g = Page_Sta.CreateGraphics();
            g.DrawLine(pen, point1, point2);
            g.DrawLine(pen, point2, point3);
        }
        //绘制PC与STM连接线
        private void DrawLine_PC_STM(bool connect)
        {
            Pen pen;
            if (connect)
            {
                pen = new Pen(Color.Red, 5);
            }
            else
            {
                pen = new Pen(Color.Black, 5);
            }
            Point point1 = new Point(picturePc.Left + 30, picturePc.Top + picturePc.Size.Height + 20);
            Point point2 = new Point(picturePc.Left + 30, pictureSTM.Top + pictureSTM.Size.Height / 2);
            Point point3 = new Point(pictureSTM.Left, pictureSTM.Top + pictureSTM.Size.Height / 2);
            Graphics g = Page_Sta.CreateGraphics();
            g.DrawLine(pen, point1, point2);
            g.DrawLine(pen, point2, point3);
        }
        //绘制STM与LBQ连接线
        private void DrawLine_STM_LBQ(bool connect)
        {
            Pen pen;
            if (connect)
            {
                pen = new Pen(Color.Red, 5);
            }
            else
            {
                pen = new Pen(Color.Black, 5);
            }
            Point point1 = new Point(pictureSTM.Left + pictureSTM.Size.Width, pictureSTM.Top + pictureSTM.Size.Height / 2);
            Point point2 = new Point(pictureLBQ.Left, pictureSTM.Top + pictureSTM.Size.Height / 2);

            Graphics g = Page_Sta.CreateGraphics();
            g.DrawLine(pen, point1, point2);

        }
        //连接状态页面重绘
        private void Page_Sta_Paint(object sender, PaintEventArgs e)
        {
            if (Device_info.connect_NA) DrawLine_GPIB_SW(true);
            else DrawLine_GPIB_SW(false);

            if (Device_info.connect_uart) DrawLine_PC_GPIB(true);
            else DrawLine_PC_GPIB(false);

            if (Device_info.connect_STM)
            {
                DrawLine_PC_STM(true);
                DrawLine_STM_LBQ(true);
            }
            else
            {
                DrawLine_PC_STM(false);
                DrawLine_STM_LBQ(false);
            }


        }
        #endregion

        #region 消息处理

        //处理来自下位机的消息
        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case MyMessage.DATARECIVE_MESSAGE:
                    {
                        MessagsData messagsData = new MessagsData();
                        Type t = messagsData.GetType();
                        messagsData = (MessagsData)message.GetLParam(t);//得到发来的数据
                        if (messagsData.FuncByte == 0x02)
                        {
                            if (messagsData.data[0] == 0xFF)
                            {
                                label_erase.Text = "×";
                                label_erase.ForeColor = Color.Red;
                            }
                            else if (messagsData.data[0] == 0x00)
                            {
                                label_erase.Text = "√";
                                label_erase.ForeColor = Color.Green;
                            }
         
                           
                        }
                        else if (messagsData.FuncByte == 0x04)
                        {
                            
                            if (messagsData.data[0] == 0xAA)
                            {
                               
                                Device_info.Parall_info.CaliData.Clear();
                            }
                            else if(messagsData.data[0] == 0xBB)
                            {
                                UInt16 hex1=(UInt16)(messagsData.data[1]<<8|messagsData.data[2]);
                                UInt16 hex2 = (UInt16)(messagsData.data[3]<< 8|messagsData.data[4]);
                                
                                Device_info.Parall_info.CaliData.Add(hex1);
                                Device_info.Parall_info.CaliData.Add(hex2);
                                progressBar3.Value = progressBar3.Maximum * (Device_info.Parall_info.CaliData.Count) / 512;
                                
                            }
                            else if(messagsData.data[0] == 0xCC)
                            {
                                progressBar3.Value = 1000;
                                richTextBox1.Text = "";
                                //显示
                                for(int i=0;i<Device_info.Parall_info.CaliData.Count;i++)
                                {
                                    richTextBox1.Text += "("+(i+1).ToString()+") "+Device_info.Parall_info.CaliData[i].ToString("X");
                                    if (i != Device_info.Parall_info.CaliData.Count - 1) richTextBox1.Text += "\r\n";
                                    progressBar3.Value = 1000;
                                }
                                //验证
                                StreamReader CaliFile=new StreamReader(Device_info.Parall_info.DownloadFilePath);
                                int j;
                                for ( j = 0; j < Device_info.Parall_info.CaliData.Count; j++)
                                {
                                    progressBar3.Value = 1000;
                                    try
                                    {
                                        string[] strs = CaliFile.ReadLine().Split(' ');
                                        UInt16 hex = Convert.ToUInt16(strs[3], 16);
                                        if (Device_info.Parall_info.CaliData[j] != hex) break;

                                    }
                                    catch
                                    {
                                        break;
                                    }

                                    
                                }
                                CaliFile.Close();

                                if (j == Device_info.Parall_info.CaliData.Count)
                                {
                                    label_cali.Text="√";
                                    label_cali.ForeColor=Color.Green;
                                    progressBar3.Value = 0;
                                    
                                }
                                else
                                {
                                    label_cali.Text = "×";
                                    label_cali.ForeColor=Color.Red;
                                    progressBar3.Value = 0;
                                    
                                }
                                
                            }
                        }

                        //MessageBox.Show("已擦除完毕！");
                        //MessageBox.Show("【功能帧：" + messagsData.FuncByte.ToString()+"】,"    
                        //                +messagsData.data[0].ToString()+","
                        //                + messagsData.data[1].ToString() + ","
                        //                + messagsData.data[2].ToString() + ","
                        //                + messagsData.data[3].ToString() + ","
                        //                + messagsData.data[4].ToString() + ","
                        //                );
                        break;
                    }
            }
            base.WndProc(ref message);//其余消息
        }
        #endregion

        #region 串口发送与接收
        //串口发送，带异常处理
        private void serialPort1_Write(string str)
        {
            try
            {
                SerialPort.WriteLine(str);
            }
            catch
            {
                Device_info.find_uart = false;
                Device_info.find_NA = false;
                Device_info.connect_NA = false;
                Device_info.connect_uart = false;
                Device_info.Scan_STA = 0;
                DrawLine_GPIB_SW(false);
                DrawLine_PC_GPIB(false);

            }

        }
        
        //public int static_hex = 0;
        //串口接收
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string str_res = "";

            try
            {
                str_res = SerialPort.ReadLine();
            }
            catch//异常处理：矢网，GPIB全部断开
            {
                Device_info.find_uart = false;
                Device_info.find_NA = false;
                Device_info.connect_NA = false;
                Device_info.connect_uart = false;
                Device_info.Scan_STA = 0;
                DrawLine_GPIB_SW(false);
                DrawLine_PC_GPIB(false);
            }

            //=======================数据判别======================//

            //GPIB连接器信息回复
            if (str_res.IndexOf("Prologix") != -1) Device_info.find_uart = true;
            //安捷伦矢网IDN回复
            else if (str_res.IndexOf("8714B") != -1)
            {
                Device_info.SW_HeartCnt = 0;//心跳复位
                Device_info.connect_NA = true;
                DrawLine_GPIB_SW(true);
            }
            else if (Device_info.Receive_mode == 1)//【串行】读取到的是矢网的Bw，CNT，Q，LOSS
            {
                string[] arrstr = str_res.Split(',');//分隔开四个数据
                double Cent = double.Parse(arrstr[1]) / Math.Pow(10, 6);//中心频率换为MHz


                double err = Math.Abs(Cent - (Device_info.Serial_info.CurrenFrq + Device_info.Serial_info.OffSet));
                if (err < 0.1 && Cent > 107 && Cent < 401)//是合适的值
                {
                    ListViewItem item = new ListViewItem();
                    string seq = (Device_info.Serial_info.sample_cnt + 1).ToString();//序号string化
                    string TheoryFrq = (Device_info.Serial_info.CurrenFrq + Device_info.Serial_info.OffSet).ToString("f2") + "MHz";//实际频率string化，保留2位小数
                    string RealFeq = Cent.ToString("f2") + "MHz";//中心频率string化，保留2位小数
                    string Hex_str;//减去13位

                    //窗口中，第二段hex显示依然从0开始
                    if (Device_info.Serial_info.hex > 4096)
                        Hex_str = (Device_info.Serial_info.hex - 4096).ToString("X");//Flash值16进制string化
                    else
                        Hex_str = Device_info.Serial_info.hex.ToString("X");//Flash值16进制string化
                    //显示格式:序号，理论频率，实际频率
                    item.SubItems[0].Text = seq; item.SubItems.Add(TheoryFrq); item.SubItems.Add(RealFeq); item.SubItems.Add(Hex_str);//listview添加一行数据
                    Device_info.Serial_info.Autotest_Form.listView1.Items.Add(item);

                    Device_info.Serial_info.sample_cnt++;//采样序号加1
                    Device_info.Serial_info.CurrenFrq += Device_info.Serial_info.step;//增长0.4
                    Device_info.Serial_info.ReBack = 0;//回退次数清零
                    progressBar1.Value = Device_info.Serial_info.sample_cnt;

                }
                else if ((Cent - (Device_info.Serial_info.CurrenFrq + Device_info.Serial_info.OffSet)) > 0.1)//实际超过了标准0.1
                {

                    Device_info.Serial_info.hex -= 1;//回退1重新测量    

                    Device_info.Serial_info.ReBack++;
                    if (Device_info.Serial_info.ReBack >= 20)//重新测量多次不行
                    {
                        string str = Device_info.Serial_info.CurrenFrq.ToString();
                        MessageBox.Show(str + "值没有找到");
                        Device_info.Serial_info.ReBack = 0;
                        Device_info.Serial_info.CurrenFrq += Device_info.Serial_info.step;//开始下一个吧，这个不管了

                    }
                    return;
                }
                if (Device_info.Config_info.Config_sta == 2)
                {
                    if (err > 10) Device_info.Serial_info.hex += 40;
                    if (err > 5) Device_info.Serial_info.hex += 15;
                    else if (err > 2) Device_info.Serial_info.hex += 2;
                    else Device_info.Serial_info.hex++;
                }
                else if (Device_info.Config_info.Config_sta == 3)
                {
                    if (err > 10) Device_info.Serial_info.hex += 20;
                    else if (err > 5) Device_info.Serial_info.hex += 7;
                    else Device_info.Serial_info.hex++;
                }




            }

            else if (Device_info.Receive_mode == 2)//【串行】生成报表的数据
            {
                if (Device_info.Serial_info.test_step == 2)//收到Bw，CNT，Q，LOSS
                {

                    string[] arrstr = str_res.Split(',');//分隔开四个数据
                    Device_info.Serial_info.test_3dB = Math.Round(double.Parse(arrstr[0]) / Math.Pow(10, 6), 2);//-3dB换为MHz
                    double Cent = double.Parse(arrstr[1]) / Math.Pow(10, 6);//中心频率换为MHz
                    Device_info.Serial_info.test_Loss = Math.Round(double.Parse(arrstr[3]), 2);//LossdB
                    //得到频率漂移
                    if (Device_info.Serial_info.test_sta == 2) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 108, 2);
                    else if (Device_info.Serial_info.test_sta == 3) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 140, 2);
                    else if (Device_info.Serial_info.test_sta == 4) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 174, 2);
                    else if (Device_info.Serial_info.test_sta == 5) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 225, 2);
                    else if (Device_info.Serial_info.test_sta == 6) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 260.2, 2);
                    else if (Device_info.Serial_info.test_sta == 7) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 300.2, 2);
                    else if (Device_info.Serial_info.test_sta == 8) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 340.2, 2);
                    else if (Device_info.Serial_info.test_sta == 9) Device_info.Serial_info.test_FrqErr = Math.Round(Cent - 400.2, 2);

     //               if (Device_info.Serial_info.test_FrqErr < -0.00) Device_info.Serial_info.test_FrqErr = 0;

                    //-3dB不能大于17MHz，重新设置
                    if (Device_info.Serial_info.test_3dB > 17) Device_info.Serial_info.test_step = 1;
                    else Device_info.Serial_info.test_step = 3;
                }
                else if (Device_info.Serial_info.test_step == 4)//读取-40dB的BW
                {
                    string[] arrstr = str_res.Split(',');//分隔开四个数据
                    Device_info.Serial_info.test_40dB = Math.Round(double.Parse(arrstr[0]) / Math.Pow(10, 6), 2);//-40dB换为MHz
                    Device_info.Serial_info.test_40_30dB = Math.Round(Device_info.Serial_info.test_40dB / Device_info.Serial_info.test_3dB, 2);//计算-40dB/-3dB，保留2位小数

                    //-40dB不能小于17MHz，重新设置
                    if (Device_info.Serial_info.test_40dB < 17) Device_info.Serial_info.test_step = 3;
                    else Device_info.Serial_info.test_step = 5;
                }
                else if (Device_info.Serial_info.test_step == 6)//读取mark1的Y
                {
                    string[] arrstr = str_res.Split(',');//分隔开四个数据
                    Device_info.Serial_info.test_standWave = Math.Round(double.Parse(str_res), 2);//Y值
                    Device_info.Serial_info.test_step = 7;
                }
                else if (Device_info.Serial_info.test_step == 8)
                {
                    string[] arrstr = str_res.Split(',');//分隔开四个数据
                    double temp = Math.Round(double.Parse(arrstr[0]) / Math.Pow(10, 6), 2);//-3dB换为MHz

                    //-3dB不能大于17MHz，重新设置
                    if (temp > 17) Device_info.Serial_info.test_step = 7;
                    else Device_info.Serial_info.test_step = 9;
                }

            }

            //else if (Device_info.Receive_mode == 3)//【串行】手动测试按钮
            //{
            //    string[] arrstr = str_res.Split(',');//分隔开四个数据
            //    double Cent = double.Parse(arrstr[1]) / Math.Pow(10, 6);//中心频率换为MHz

            //    label19.Text = Cent.ToString("f2") + "MHz";
            //    Device_info.Receive_mode = 0;//没有自动测试数据要接收
            //}

            else if (Device_info.Receive_mode == 4)//【并行】读取到的是矢网的Bw，CNT，Q，LOSS
            {
                string[] arrstr = str_res.Split(',');//分隔开四个数据

                double Cent = Math.Round(double.Parse(arrstr[1]) / Math.Pow(10, 6), 2);//中心频率换为MHz
                if (Device_info.Parall_info.sample_cnt == 0) Device_info.Parall_info.preFrq = Cent;
                else
                {
                    if (Math.Abs(Cent - Device_info.Parall_info.preFrq) < 0.01)//再读一次
                    {
                        ParaAutotest_bool = false;

                        return;
                    }
                    else//更新历史值
                        Device_info.Parall_info.preFrq = Cent;
                       
                }
                double Loss = Math.Round(double.Parse(arrstr[3]), 2);

                //存储在内存之中
                AutoTest_Datas temp=new AutoTest_Datas();
                temp.Seq=Device_info.Parall_info.sample_cnt + 1;
                temp.Frq=Cent;
                temp.Loss=Loss;
                temp.Hex = Device_info.Parall_info.hex;
                Device_info.Parall_info.AutoTest_datas.Add(temp);

                //存储在窗体控件中

                ListViewItem item = new ListViewItem();
                string seq = (Device_info.Parall_info.sample_cnt + 1).ToString();//序号string化

                string Hex_str = "";
                //窗口中，第二段hex显示依然从0开始
                if (Device_info.Serial_info.hex >= 1024)
                    Hex_str = (Device_info.Parall_info.hex - 1024).ToString("X");//Flash值16进制string化
                else
                    Hex_str = Device_info.Parall_info.hex.ToString("X");//Flash值16进制string化
                //显示格式:序号，理论频率，实际频率
                item.SubItems[0].Text = seq;
                item.SubItems.Add(Device_info.Parall_info.AutoTest_datas[Device_info.Parall_info.sample_cnt].Frq.ToString() + "MHz");
                item.SubItems.Add(Device_info.Parall_info.AutoTest_datas[Device_info.Parall_info.sample_cnt].Loss.ToString());
                item.SubItems.Add(Device_info.Parall_info.AutoTest_datas[Device_info.Parall_info.sample_cnt].Hex.ToString("X"));
                Device_info.Parall_info.Autotest_resForm.listView1.Items.Add(item);



           
                //进度条
                progressBar2.Value = Device_info.Parall_info.sample_cnt;

                Device_info.Parall_info.hex++;
                Device_info.Parall_info.sample_cnt++;

                //MessageBox.Show("【中心频率】" + Cent.ToString() + "【Loss】" + arrstr[3]);
                //
                //if (Device_info.Parall_info.sample_cnt >= 100)//自动测试完毕
                //{
                //    //Autotest_DataProcess();//数据处理
                    
                    
                //    Device_info.Receive_mode = 0;//没有自动测试数据要接收
                //    progressBar2.Value = 0;
                //    timer_paraAuotest.Stop();
                //    timer_heart.Start();//开启心跳定时器
                //}
            }

            else if (Device_info.Receive_mode == 5)//【并行】报表数据
            {
                if (Device_info.Parall_info.test_step == 2)
                {
                    
                    string[] arrstr = str_res.Split(',');//分隔开四个数据

                    Device_info.Parall_info.test_3d = Math.Round(Convert.ToDouble(arrstr[0]) / Math.Pow(10, 6), 2);//3dB
                    Device_info.Parall_info.test_Frq = Math.Round(Convert.ToDouble(arrstr[1]) / Math.Pow(10, 6), 2);//Frq
                    Device_info.Parall_info.test_Loss = Math.Round(Convert.ToDouble(arrstr[3]), 2);//Loss

                    Device_info.Parall_info.test_step = 3;
                }
                else if (Device_info.Parall_info.test_step == 3)
                {
                     string[] arrstr = str_res.Split(',');//分隔开四个数据
                     Device_info.Parall_info.test_Standwave = Math.Round(double.Parse(str_res), 2);//Y值
                     Device_info.Parall_info.test_step = 4;
                }
                else if (Device_info.Parall_info.test_step == 7)
                {


                    Device_info.Parall_info.neg5 = Convert.ToDouble(str_res);
                    Device_info.Parall_info.neg5 -= Device_info.Parall_info.test_Loss;
                    Device_info.Parall_info.neg5 = Math.Round(Device_info.Parall_info.neg5, 2);
                    Device_info.Parall_info.test_step = 8;
                }
                else if (Device_info.Parall_info.test_step == 9)
                {


                    Device_info.Parall_info.neg10 = Convert.ToDouble(str_res);
                    Device_info.Parall_info.neg10 -= Device_info.Parall_info.test_Loss;
                    Device_info.Parall_info.neg10 = Math.Round(Device_info.Parall_info.neg10, 2);
                    Device_info.Parall_info.test_step = 10;
                }
                else if (Device_info.Parall_info.test_step == 11)
                {


                    Device_info.Parall_info.pos5 = Convert.ToDouble(str_res);
                    Device_info.Parall_info.pos5 -= Device_info.Parall_info.test_Loss;
                    Device_info.Parall_info.pos5 = Math.Round(Device_info.Parall_info.pos5, 2);
                    Device_info.Parall_info.test_step = 12;
                }
                else if (Device_info.Parall_info.test_step == 13)
                {


                    Device_info.Parall_info.pos10 = Convert.ToDouble(str_res);
                    Device_info.Parall_info.pos10 -= Device_info.Parall_info.test_Loss;
                    Device_info.Parall_info.pos10 = Math.Round(Device_info.Parall_info.pos10, 2);
                    Device_info.Parall_info.test_step = 14;
                }
            }
           
            //====================================================//
        }
        #endregion
       
        #region 定时器与相关数据处理

        public bool NAcnectHis=false;//记录连接历史状态，避免按钮闪烁
        public bool STMcnectHis = false;
        //定时器：【公用】心跳
        private void Heart_Tick(object sender, EventArgs e)//定时器：心跳
        {
       

            if (Device_info.connect_NA) timer_heart.Interval = 1000;//找到了，就放慢扫描速度
            else timer_heart.Interval = 500;//没找到，加快扫描速度
            //GPIB连接器检测
            if (Device_info.find_uart == false)//还未找到GPIB设备
            {
                if (Device_info.Scan_STA == 0)//未扫描状态
                {
                    Device_info.portList = System.IO.Ports.SerialPort.GetPortNames();//扫描串口
                    if (Device_info.portList.Length == 0) Device_info.Scan_STA = 0;
                    else Device_info.Scan_STA = 1;//进入扫描状态 
                    Device_info.cnt = 0;//从第一个串口开始             
                }

                if (Device_info.Scan_STA == 1)//扫描状态
                {
                    string name = Device_info.portList[Device_info.cnt];
                    SerialPort.PortName = name;
                    bool a = SerialPort.IsOpen;
                    try//此处处理可能占用的异常
                    {
                        SerialPort.Open();
                    }
                    catch//当前串口被占用
                    {
                        Device_info.cnt++;
                        if (Device_info.cnt < Device_info.portList.Length) Device_info.Scan_STA = 1;//进入扫描状态
                        else Device_info.Scan_STA = 0;//列表中没有，重新开始新一轮扫描

                        return;
                    }

                    serialPort1_Write("++ver\r\n");
                    Device_info.wait_cnt = 0;
                    Device_info.Scan_STA = 2;//进入等待回复状态
                }
                else if (Device_info.Scan_STA == 2)//等待回复状态，期间如果找到设备不会进入此处了
                {

                    Device_info.wait_cnt++;
                    if (Device_info.wait_cnt == 1)//等待超时
                    {
                        SerialPort.Close();
                        Device_info.cnt++;

                        if (Device_info.cnt < Device_info.portList.Length) Device_info.Scan_STA = 1;//进入扫描状态
                        else Device_info.Scan_STA = 0;//列表中没有，重新开始新一轮扫描
                    }


                }
            }
            else if (Device_info.connect_uart == false)//找到GPIB设备，连接
            {
                Device_info.connect_uart = true;//没有连接操作，直接表示连接
                DrawLine_PC_GPIB(true);
            }
            else//GPIB心跳检测,检测错误直接标定GPIB与失网断开
            {
                serialPort1_Write("++auto 1");//发送心跳信息
            }

            //安捷伦失网检测
            if (Device_info.connect_uart == true)//前提，GPIB连接成功
            {

                if (Device_info.find_NA == false)//第一次上电
                {
                    serialPort1_Write("++addr 18");//仪器地址
                    serialPort1_Write("++auto 1");//打开自动回听
                    Device_info.find_NA = true;
                }
                else//心跳信息
                {
                    serialPort1_Write("*IDN?");//请求设备信息
                    Device_info.SW_HeartCnt++;
                    if (Device_info.SW_HeartCnt >= 6)
                    {
                        Device_info.connect_NA = false;
                        DrawLine_GPIB_SW(false);

                    }

                }
            }
    
            //USB设备检测
            Device_list.Clear();
            Device_list = usbHID.GetDeviceList();//得到设备列表
            int find = 0;
            foreach (string Device_ID in Device_list)//一个一个找
            {

                string[] Array = Device_ID.Split('#');//单个设备信息解析
                foreach (string str in Array)
                {
                    int sta = str.CompareTo(Relay_ID);
                    if (sta == 0)
                    {
                        //找到设备，更新状态，存储路径
                        find = 1;
                        Device_info.find_usb = true;
                        Relay_path = Device_ID;

                    }
                }
            }
            if (find == 0)//没有设备，更新状态
            {
                Device_info.find_usb = false;
                Device_info.connect_STM = false;
            }

            if (Device_info.connect_STM == false)//USB连接
            {
                if (Device_info.find_usb)//发现设备尝试连接
                {
                    bool sta = usbHID.OpenUSBHid(Relay_path);
                    if (sta == false)
                    {
                        Device_info.connect_STM = false;
                        if (Device_info.error == false)
                        {
                            Device_info.error = true;
                            DialogResult result = MessageBox.Show("错误：该设备异常！请检测是否运行了多个程序！", "", MessageBoxButtons.OK);
                            if (result == DialogResult.OK) Application.Exit();
                        }

                    }
                    else
                    {
                        Device_info.error = false;
                        DrawLine_PC_STM(true);//连接成功画红线
                        DrawLine_STM_LBQ(true);//连接成功画红线
                        Device_info.connect_STM = true;
                    }
                }
                else//没有发现USB设备，画图
                {
                    DrawLine_PC_STM(false);//连接失败画黑线
                    DrawLine_STM_LBQ(false);//连接失败画红线
                }
            }
            //对比连接历史状态，避免按钮闪烁
            if ((NAcnectHis != Device_info.connect_NA) || (Device_info.connect_STM != STMcnectHis))
            {
                if (Device_info.connect_STM && Device_info.connect_NA)
                {
                    //按键变化
                    SerialButton_All(true, true, true);
                    ParallButton_All(true, true, true);
                    DownloadButton_All(true);

                    button18.Enabled = false;//终止按键关闭
                    button19.Enabled = false;
                    button10.Enabled = false;
                    button48.Enabled = false;
                    button26.Enabled = false;
                    button40.Enabled = false;
                }
                else if (Device_info.connect_STM)
                {
                    //按键变化
                    SerialButton_All(true, true, false);
                    ParallButton_All(true, true, false);

                    SerialButton_All(false, true, true);
                    ParallButton_All(false, true, true);
                    DownloadButton_All(true);

                    button18.Enabled = false;//终止按键关闭
                    button19.Enabled = false;
                    button10.Enabled = false;
                    button48.Enabled = false;
                    button26.Enabled = false;
                    button40.Enabled = false;
                }
                else if (Device_info.connect_NA)
                {
                    //按键变化
                    SerialButton_All(true, true, false);
                    ParallButton_All(true, true, false);

                    SerialButton_All(true, false, true);
                    ParallButton_All(true, false, true);
                    DownloadButton_All(false);

                }
                else
                {
                    SerialButton_All(true, true, false);
                    ParallButton_All(true, true, false);
                    DownloadButton_All(false);
                }
            }
            NAcnectHis = Device_info.connect_NA;
            STMcnectHis = Device_info.connect_STM;
        }
        //定时器：【公用】配置
        private void Config_Tick(object sender, EventArgs e)//定时器：配置
        {

            if (Device_info.Config_info.Config_sta == 0)//【容错】没有配置，开启心跳
            {
                timer_Cofig.Stop();
                timer_heart.Start();
            }
            else
            {
                if (Device_info.Config_info.Config_sta == 1)//一键配置仪器
                {
                    
                    if (Device_info.Config_info.Config_Seq < Device_info.Config_info.Config_content.Length)
                    {
                        progressBar1.Value = progressBar1.Maximum*(Device_info.Config_info.Config_Seq+1) / Device_info.Config_info.Config_content.Length;
                        progressBar2.Value = progressBar2.Maximum * (Device_info.Config_info.Config_Seq+1) / Device_info.Config_info.Config_content.Length;
                        serialPort1_Write(Device_info.Config_info.Config_content[Device_info.Config_info.Config_Seq++]);

                    }
                        
                    else
                    {

                        //按键变化
                        SerialButton_All(true, false, true);
                        ParallButton_All(true, false, true);
                        
                        progressBar1.Value = 0;
                        progressBar2.Value = 0;
                        timer_Cofig.Stop();
                        timer_heart.Start();
                        MessageBox.Show("仪器配置完成！", "", MessageBoxButtons.OK);
                    }
                }
                else if (Device_info.Config_info.Config_sta == 2) //【串行】配置第一段
                {
                    if (Device_info.Config_info.Config_Seq == 0)//设置bw1
                    {
                        string temp = "CALC1:MARK1:BWID " + Device_info.Serial_info.Bw1.ToString();
                        serialPort1_Write(temp);
                        Device_info.Config_info.Config_Seq++;
                    }
                    else if (Device_info.Config_info.Config_Seq == 1)//设置star1
                    {
                        string temp = "SENS1:FREQ:STAR " + Device_info.Serial_info.Start1Frq.ToString() + "E6";
                        serialPort1_Write(temp);
                        Device_info.Config_info.Config_Seq++;
                    }
                    else if (Device_info.Config_info.Config_Seq == 2)//设置stop1
                    {
                        string temp = "SENS1:FREQ:STOP " + Device_info.Serial_info.Stop1Frq.ToString() + "E6";
                        serialPort1_Write(temp);

                        Device_info.Serial_info.CurrenFrq = Device_info.Serial_info.CentFrq1;//当前比较值是108MHz
                        Device_info.Serial_info.ReBack = 0;//重新测量次数
                        Device_info.Serial_info.sample_cnt = 0;//采集的数据多少
                        Device_info.Serial_info.hex = 0;//滤波器从x开始
                        Device_info.Receive_mode = 1;//串口接收的是自动测试数据
                        timer_Cofig.Stop();
                        timer_Autotest.Start();
                        return;
                    }
                }
                else if (Device_info.Config_info.Config_sta == 3)//【串行】配置第二段
                {
                    if (Device_info.Config_info.Config_Seq == 0)//设置bw1
                    {
                        string temp = "CALC1:MARK1:BWID " + Device_info.Serial_info.Bw1.ToString();
                        serialPort1_Write(temp);
                        Device_info.Config_info.Config_Seq++;
                    }
                    else if (Device_info.Config_info.Config_Seq == 1)//设置star2
                    {
                        string temp = "SENS1:FREQ:STAR " + Device_info.Serial_info.Start2Frq.ToString() + "E6";
                        serialPort1_Write(temp);
                        Device_info.Config_info.Config_Seq++;
                    }
                    else if (Device_info.Config_info.Config_Seq == 2)//设置stop2
                    {
                        string temp = "SENS1:FREQ:STOP " + Device_info.Serial_info.Stop2Frq.ToString() + "E6";
                        serialPort1_Write(temp);


                        Device_info.Serial_info.CurrenFrq = Device_info.Serial_info.CentFrq2;//当前比较值是225
                        Device_info.Serial_info.ReBack = 0;//重新测量次数
                        Device_info.Serial_info.hex = 4096;//第二段hex起始
                        Device_info.SendData[1] = 0x01;//功能帧
                        Device_info.SendData[2] = (byte)(Device_info.Serial_info.hex / 256);
                        Device_info.SendData[3] = (byte)(Device_info.Serial_info.hex % 256);
                        usbHID.WriteUSBHID(Device_info.SendData);



                        timer_Cofig.Stop();
                        timer_Autotest.Start();
                        return;

                    }
                }
                else if (Device_info.Config_info.Config_sta == 4)//【并行】配置第一段
                {
                    Device_info.SendData[1] = 0x02;//功能帧
                    Device_info.SendData[2] = (byte)(0 / 256);
                    Device_info.SendData[3] = (byte)(0 % 256);
                    usbHID.WriteUSBHID(Device_info.SendData);

                    if (Device_info.Config_info.Config_Seq == 0)//设置star1
                    {
                        string temp = "SENS1:FREQ:STAR " + Device_info.Parall_info.Start1Frq.ToString() + "E6";
                        serialPort1_Write(temp);
                        Device_info.Config_info.Config_Seq++;
                    }
                    else if (Device_info.Config_info.Config_Seq == 1)//设置stop1
                    {
                        string temp = "SENS1:FREQ:STOP " + Device_info.Parall_info.Stop1Frq.ToString() + "E6";
                        serialPort1_Write(temp);

                        //Device_info.Parall_info.CurrenFrq = Device_info.Serial_info.CentFrq1;//当前比较值是108MHz
                        Device_info.Parall_info.sample_cnt = 0;//采集的数据多少
                        Device_info.Parall_info.hex = 0;//滤波器从x开始
                        Device_info.Receive_mode = 4;//并口接收的是自动测试数据
                        timer_Cofig.Stop();
                        Thread.Sleep(2000);
                        timer_paraAuotest.Start();
                        ParaAutotest_bool = false;//确保第一段是从控制滤波器开始的
                        return;
                    }
                }
                else if (Device_info.Config_info.Config_sta == 5)//【并行】配置第二段
                {
                    Device_info.SendData[1] = 0x02;//功能帧
                    Device_info.SendData[2] = (byte)(400 / 256);
                    Device_info.SendData[3] = (byte)(400 % 256);
                    usbHID.WriteUSBHID(Device_info.SendData);

                    if (Device_info.Config_info.Config_Seq == 0)//设置star2
                    {
                        string temp = "SENS1:FREQ:STAR " + Device_info.Parall_info.Start2Frq.ToString() + "E6";
                        serialPort1_Write(temp);
                        Device_info.Config_info.Config_Seq++;
                    }
                    else if (Device_info.Config_info.Config_Seq == 1)//设置stop2
                    {
                        string temp = "SENS1:FREQ:STOP " + Device_info.Parall_info.Stop2Frq.ToString() + "E6";
                        serialPort1_Write(temp);

                       // Device_info.Parall_info.CurrenFrq = Device_info.Serial_info.CentFrq1;//当前比较值是108MHz
                        //Device_info.Parall_info.sample_cnt = 0;//采集的数据多少
                        Device_info.Parall_info.hex = 0x400;//滤波器从1024开始
                        Device_info.Receive_mode = 4;//并口接收的是自动测试数据
                        timer_Cofig.Stop();
                        Thread.Sleep(2000);
                        timer_paraAuotest.Start();
                        ParaAutotest_bool = false;//确保第二段是从控制滤波器开始的
                        return;
                    }
                }
            }

        }
        //数据处理：【串行】自动测试后
        public void Autotest_DataProcess()
        {
           
            StreamWriter hexBao = new StreamWriter(Device_info.DesktopPath + "\\Hex报表.txt");
            StreamWriter hexProgram = new StreamWriter(Device_info.DesktopPath + "\\Hex编程文件.hex");
            UInt16 cnt = 0;
            UInt16 First_hex = 0;
            foreach (ListViewItem item in Device_info.Serial_info.Autotest_Form.listView1.Items)
            {

                string frq = item.SubItems[1].Text;
                string cent = item.SubItems[2].Text;
                string hex = item.SubItems[3].Text;//读取Hex值
                hexBao.Write(frq + " " + cent + " " + hex + "\r\n");//报表文件写入

                UInt16 Hex = Convert.ToUInt16(hex, 16);
                string subFrq = frq.Substring(0, frq.Length - 3);//删除后面的单位
                if (double.Parse(subFrq) > 220)
                    Hex |= 0x2000;//添加13位段选
                else
                    Hex |= 0x1000;//添加13位段选


                byte hex1 = (byte)(Hex / 256);
                byte hex2 = (byte)(Hex % 256);


                byte cnt1 = (byte)(cnt / 256);
                byte cnt2 = (byte)(cnt % 256);


                //计算校验值=（ 2 + 地址 + 0 + 数据）%256 取补码
                byte sum = (byte)((2 + cnt1 + cnt2 + hex1 + hex2) % 256);
                sum = (byte)(~sum + 1);//取补码

                //合成hex
                string temp = ":02" + cnt.ToString("X4") + "00" + Hex.ToString("X4") + sum.ToString("X2");
                hexProgram.Write(temp + "\r\n");//编程文件写入

                if (cnt == 0) First_hex = Hex; //记录第一个数据用于最后填充

                cnt += 2;
            }
            for (; cnt < 0x2000; cnt += 2)
            {

                byte hex1 = (byte)(First_hex / 256);
                byte hex2 = (byte)(First_hex % 256);


                byte cnt1 = (byte)(cnt / 256);
                byte cnt2 = (byte)(cnt % 256);


                //计算校验值=（ 2 + 地址 + 0 + 数据）%256 取补码
                byte sum = (byte)((2 + cnt1 + cnt2 + hex1 + hex2) % 256);
                sum = (byte)(~sum + 1);//取补码


                string temp = ":02" + cnt.ToString("X4") + "00" + First_hex.ToString("X4") + sum.ToString("X2");

                hexProgram.Write(temp + "\r\n");//编程文件写入
                if (cnt + 2 >= 0x2000)//最后一个，再加一个文件结尾，没有回车
                {
                    hexProgram.Write(":00000001FF");

                }

            }
            Autotest_bool = false;//默认先发送hex，再读取
            hexBao.Close();//报表关闭
            hexProgram.Close();//编程文件关闭
        }
        //数据处理：【并行】自动测试后
        public void ParaAutotest_dataproces()
        {

            //数据样本排序
            Device_info.Parall_info.AutoTest_datas.Sort((x, y) => { return x.Frq.CompareTo(y.Frq); });

            List<AutoTest_Datas> BestData = new List<AutoTest_Datas>();//存储最终结果
            double deltaFrq = 0;//用于判别合适频率的范围
            Device_info.Parall_info.CurrenFrq = Device_info.Parall_info.CentFrq1;
            for (int i = 0; i < 502; i++)//共502个点
            {
                List<AutoTest_Datas> BadFrqdelta_ = new List<AutoTest_Datas>();//找不到合适频率，存储频率差值,左侧
                List<AutoTest_Datas> BadFrqdelta = new List<AutoTest_Datas>();//找不到合适频率，存储频率差值,右侧
                List<AutoTest_Datas> Frqsuitable = new List<AutoTest_Datas>();//找到合适频率，存储合适的频率的数据

                for (int j = 0; j < Device_info.Parall_info.AutoTest_datas.Count; j++)//一个频点遍历2048
                {
                    if (Device_info.Parall_info.CurrenFrq >= 108 && Device_info.Parall_info.CurrenFrq <= 135) deltaFrq = 0.1;
                    else if (Device_info.Parall_info.CurrenFrq > 135 && Device_info.Parall_info.CurrenFrq <= 174) deltaFrq = 0.2;//低频在0.1MHz内
                    else if (Device_info.Parall_info.CurrenFrq >= 225 && Device_info.Parall_info.CurrenFrq <= 300) deltaFrq = 0.2;
                    else if (Device_info.Parall_info.CurrenFrq > 300 && Device_info.Parall_info.CurrenFrq <= 400) deltaFrq = 0.3;

                    if (Math.Abs(Device_info.Parall_info.AutoTest_datas[j].Frq - Device_info.Parall_info.CurrenFrq) < deltaFrq)//频率差值在△以内
                    {
                        AutoTest_Datas temp = new AutoTest_Datas();
                        temp.Seq = Device_info.Parall_info.AutoTest_datas[j].Seq;
                        temp.Frq = Device_info.Parall_info.AutoTest_datas[j].Frq;
                        temp.Loss = Math.Abs(Device_info.Parall_info.AutoTest_datas[j].Loss); ;
                        temp.Hex = Device_info.Parall_info.AutoTest_datas[j].Hex;
                        Frqsuitable.Add(temp);

                    }

                }
                if (Frqsuitable.Count == 0)//没有找到合适的值
                {
                    for (int j = 0; j < Device_info.Parall_info.AutoTest_datas.Count; j++)//各个点对该点求差值
                    {

                        AutoTest_Datas temp = new AutoTest_Datas();

                        temp.Seq = Device_info.Parall_info.AutoTest_datas[j].Seq;
                        temp.Frq = Device_info.Parall_info.AutoTest_datas[j].Frq - Device_info.Parall_info.CurrenFrq;

                        if (temp.Frq > 0)
                            BadFrqdelta.Add(temp);//右侧统计
                        else
                            BadFrqdelta_.Add(temp);//左侧统计

                    }


                    BadFrqdelta.Sort((x, y) => { return x.Frq.CompareTo(y.Frq); });//右侧统计的数据排序
                    BadFrqdelta_.Sort((x, y) => { return y.Frq.CompareTo(x.Frq); });//左侧统计的数据排序
                    int BestSeq = 0;
                    if (Device_info.Parall_info.CurrenFrq >= 107.99 && Device_info.Parall_info.CurrenFrq <= 174.01)//第一段
                    {
                        if ((Device_info.Parall_info.CurrenFrq >= 149.99 && Device_info.Parall_info.CurrenFrq <= 174.01)
                            && (Math.Abs(BadFrqdelta[0].Frq - Math.Abs(BadFrqdelta_[0].Frq))) < 0.2)//范围0.2内选左侧的
                            BestSeq = BadFrqdelta_[0].Seq;
                        else//范围外间隔最小的
                        {
                            if (Math.Abs(BadFrqdelta[0].Frq) > Math.Abs(BadFrqdelta_[0].Frq))
                                BestSeq = BadFrqdelta_[0].Seq;
                            else
                                BestSeq = BadFrqdelta[0].Seq;
                        }

                    }
                    else if (Device_info.Parall_info.CurrenFrq >= 224.99 && Device_info.Parall_info.CurrenFrq <= 400.01)//第二段范围为0.3
                    {
                        if ((Device_info.Parall_info.CurrenFrq >= 349.99 && Device_info.Parall_info.CurrenFrq <= 400.01) &&
                            (Math.Abs(BadFrqdelta[0].Frq - Math.Abs(BadFrqdelta_[0].Frq))) < 0.3)//范围内选左侧的
                            BestSeq = BadFrqdelta_[0].Seq;
                        else//范围外间隔最小的
                        {
                            if (Math.Abs(BadFrqdelta[0].Frq) > Math.Abs(BadFrqdelta_[0].Frq))
                                BestSeq = BadFrqdelta_[0].Seq;
                            else
                                BestSeq = BadFrqdelta[0].Seq;
                        }
                    }
                    for (int h = 0; h < 2048; h++)//通过序号找到这个最佳值
                    {
                        if (Device_info.Parall_info.AutoTest_datas[h].Seq == BestSeq)
                        {
                            AutoTest_Datas temp = new AutoTest_Datas();
                            temp.Seq = Device_info.Parall_info.AutoTest_datas[h].Seq;
                            temp.Frq = Device_info.Parall_info.AutoTest_datas[h].Frq;
                            temp.Loss = Device_info.Parall_info.AutoTest_datas[h].Loss;
                            temp.Hex = Device_info.Parall_info.AutoTest_datas[h].Hex;

                            BestData.Add(temp);
                            break;
                        }
                    }

                }
                else//找到合适的值,选loss最低的
                {
                    Frqsuitable.Sort((x, y) => { return x.Loss.CompareTo(y.Loss); });

                    for (int h = 0; h < 2048; h++)
                    {
                        if (Device_info.Parall_info.AutoTest_datas[h].Seq == Frqsuitable[0].Seq)
                        {
                            AutoTest_Datas temp = new AutoTest_Datas();
                            temp.Seq = Device_info.Parall_info.AutoTest_datas[h].Seq;
                            temp.Frq = Device_info.Parall_info.AutoTest_datas[h].Frq;
                            temp.Loss = Device_info.Parall_info.AutoTest_datas[h].Loss;
                            temp.Hex = Device_info.Parall_info.AutoTest_datas[h].Hex;

                            BestData.Add(temp);
                            break;
                        }
                    }

                }


                //下一个比较的频率值
                if (i < 250) Device_info.Parall_info.CurrenFrq += Device_info.Parall_info.step1;
                else if (i == 250) Device_info.Parall_info.CurrenFrq = Device_info.Parall_info.CentFrq2;
                else if (i < 502) Device_info.Parall_info.CurrenFrq += Device_info.Parall_info.step2;

            }

            //显示筛选结果
            StreamWriter filewrite = new StreamWriter(Device_info.DesktopPath + "\\并行下载数据.txt");
            Form3 fm4 = new Form3();
            UInt16 BestDataidx = 0;
            for (int i = 0; i < 512; i++)
            {
                ListViewItem item = new ListViewItem();


                if (i <= 250)
                {
                    item.SubItems[0].Text = BestData[BestDataidx].Seq.ToString();
                    item.SubItems.Add(BestData[BestDataidx].Frq.ToString() + "MHz");
                    item.SubItems.Add(BestData[BestDataidx].Loss.ToString());
                    item.SubItems.Add(BestData[BestDataidx].Hex.ToString("X"));
                    fm4.listView1.Items.Add(item);

                    filewrite.Write((i + 1).ToString() + ":" + " "
                                    + BestData[BestDataidx].Frq.ToString() + "MHz" + " "
                                    + BestData[BestDataidx].Loss.ToString() + " "
                                    + (BestData[BestDataidx].Hex + 0x1400).ToString("X"));
                    BestDataidx++;
                }
                else if (i >= 251 && i <= 255)
                {
                    filewrite.Write((i + 1).ToString() + ":" + " "
                                    + "MaxMHz" + " "
                                    + "----" + " "
                                    + "17FF");
                }
                else if (i >= 256 && i <= 506)
                {
                    item.SubItems[0].Text = BestData[BestDataidx].Seq.ToString();
                    item.SubItems.Add(BestData[BestDataidx].Frq.ToString() + "MHz");
                    item.SubItems.Add(BestData[BestDataidx].Loss.ToString());
                    item.SubItems.Add(BestData[BestDataidx].Hex.ToString("X"));
                    fm4.listView1.Items.Add(item);

                    filewrite.Write((i + 1).ToString() + ":" + " "
                                       + BestData[BestDataidx].Frq.ToString() + "MHz" + " "
                                       + BestData[BestDataidx].Loss.ToString() + " "
                                       + (BestData[BestDataidx].Hex + 0x2400).ToString("X"));
                    BestDataidx++;
                }
                else if (i >= 507 && i <= 511)
                {
                    filewrite.Write((i + 1).ToString() + ":" + " "
                                    + "MaxMHz" + " "
                                    + "----" + " "
                                    + "2BFF");
                }

                if (i < 511) filewrite.Write("\r\n");

            }
            filewrite.Close();
            fm4.Show();

        }
        public bool Autotest_bool = false;
        //定时器：【串行】自动测试
        private void Autotest_Tick(object sender, EventArgs e)
        {
            if (Device_info.Serial_info.CurrenFrq > 400.2)//自动测试完毕
            {
                Device_info.Serial_info.Autotest_Form.Show();
                Autotest_DataProcess();

                //按键变化
                SerialButton_All(true, true, true);
                ParallButton_All(true, true, true);
                DownloadButton_All(true);
                button18.Enabled = false;//终止按键关闭
                button19.Enabled = false;
                button10.Enabled = false;
                button48.Enabled = false;
                button26.Enabled = false;
                button40.Enabled = false;


                Device_info.Receive_mode = 0;//没有自动测试数据要接收
                progressBar1.Value = 0;
                timer_Autotest.Stop();
                timer_heart.Start();//开启心跳定时器
            }
            if (Device_info.Serial_info.CurrenFrq > 174.2 && Device_info.Config_info.Config_sta == 2)
            {
                Device_info.Config_info.Config_sta = 3;//第二段配置
                Device_info.Config_info.Config_Seq = 0;//清零计数

                timer_Autotest.Stop();
                timer_Cofig.Start();//开启配置

            }

            if (Autotest_bool == false)//控制滤波器
            {
                Autotest_bool = true;
                //滤波器控制，逐渐增加
                Device_info.SendData[1] = 0x01;//功能帧
                Device_info.SendData[2] = (byte)(Device_info.Serial_info.hex / 256);
                Device_info.SendData[3] = (byte)(Device_info.Serial_info.hex % 256);
                usbHID.WriteUSBHID(Device_info.SendData);
            }
            else//读取矢网
            {
                Autotest_bool = false;
                serialPort1_Write("CALC:MARK:FUNC:RES?");
            }
        }
        //定时器：【串行】自动生成报表
        private void TestReport_Tick(object sender, EventArgs e)
        {
           
            progressBar1.Value = 605 * (Device_info.Serial_info.test_sta * 9 + Device_info.Serial_info.test_step) / (10 * 9);

            if (Device_info.Serial_info.test_sta == 0)//配置起始频率50M
            {
                serialPort1_Write("SENS1:FREQ:STAR 50E6");
                Device_info.Serial_info.test_sta++;
                Thread.Sleep(500);
            }
            else if (Device_info.Serial_info.test_sta == 1)//配置终止频率500M
            {
                serialPort1_Write("SENS1:FREQ:STOP 500E6");
                Device_info.Serial_info.test_sta++;
                Thread.Sleep(500);
            }
            else if (Device_info.Serial_info.test_sta >= 2)
            {
                //第一步发送配置 BW1 500ms
                if (Device_info.Serial_info.test_step == 0)
                {
                    UInt16 hex = 0;
                    if (Device_info.Serial_info.test_sta == 2)//108
                    {
                        hex = System.Convert.ToUInt16((108 - 108) / 0.4);
                        Device_info.SendData[1] = 0x01;//功能帧
                        Device_info.SendData[2] = (byte)(hex / 256);
                        Device_info.SendData[3] = (byte)(hex % 256);
                        usbHID.WriteUSBHID(Device_info.SendData);
                        Thread.Sleep(1000);
                    }
                       
                    else if (Device_info.Serial_info.test_sta == 3)//140
                        hex = System.Convert.ToUInt16((140 - 108) / 0.4);
                    else if (Device_info.Serial_info.test_sta == 4)//174
                        hex = System.Convert.ToUInt16((174 - 108) / 0.4);
                    else if (Device_info.Serial_info.test_sta == 5)//225
                        hex = System.Convert.ToUInt16((225 - 225) / 0.4 + 166);
                    else if (Device_info.Serial_info.test_sta == 6)//260.2
                        hex = System.Convert.ToUInt16((260.2 - 225) / 0.4 + 166);
                    else if (Device_info.Serial_info.test_sta == 7)//300.2
                        hex = System.Convert.ToUInt16((300.2 - 225) / 0.4 + 166);
                    else if (Device_info.Serial_info.test_sta == 8)//340.2
                        hex = System.Convert.ToUInt16((340.2 - 225) / 0.4 + 166);
                    else if (Device_info.Serial_info.test_sta == 9)//400.2
                        hex = System.Convert.ToUInt16((400.2 - 225) / 0.4 + 166);

                    Device_info.SendData[1] = 0x01;//功能帧
                    Device_info.SendData[2] = (byte)(hex / 256);
                    Device_info.SendData[3] = (byte)(hex % 256);
                    usbHID.WriteUSBHID(Device_info.SendData);
                    Thread.Sleep(1000);
                  

                    //serialPort1_Write("SENS1:STAT ON");//左上选为tr1
                    Device_info.Serial_info.test_step = 1;


                }
                //第二部配置滤波器 500ms
                else if (Device_info.Serial_info.test_step == 1)
                {
                    string temp = "CALC1:MARK1:BWID " + Device_info.Serial_info.Bw1.ToString();
                    serialPort1_Write(temp);

                    Device_info.Serial_info.test_step++;
                }

                //发送读取命令bw cent q loss 500ms,等待串口
                else if (Device_info.Serial_info.test_step == 2)
                {
                    serialPort1_Write("CALC:MARK:FUNC:RES?");
                }
                //数据-3B，插损，频飘 已收到，发送配置 BW2 500ms
                else if (Device_info.Serial_info.test_step == 3)
                {
                    string temp = "CALC1:MARK1:BWID " + Device_info.Serial_info.Bw2.ToString();
                    serialPort1_Write(temp);
                    Device_info.Serial_info.test_step++;
                }
                //发送读取命令bw cent q loss 100ms,等待串口
                else if (Device_info.Serial_info.test_step == 4)
                {
                    serialPort1_Write("CALC:MARK:FUNC:RES?");
                }
                //数据-40dB，-40dB/-3dB已收到，发送配置左上改为trace2
                else if (Device_info.Serial_info.test_step == 5)
                {
                    //serialPort1_Write("SENS2:STAT ON");
                    Device_info.Serial_info.test_step++;
                }
                //发送命令mark1 Y:,等待串口
                else if (Device_info.Serial_info.test_step == 6)
                {
                    serialPort1_Write("CALC2:MARK1:Y?");
                }
                else if (Device_info.Serial_info.test_step == 7)
                {
                    string temp = "CALC1:MARK1:BWID " + Device_info.Serial_info.Bw1.ToString();//BW1
                    serialPort1_Write(temp);
                    Device_info.Serial_info.test_step++;
                }
                else if (Device_info.Serial_info.test_step == 8)
                {
                    serialPort1_Write("CALC:MARK:FUNC:RES?");
                }
                //读取完毕，写入excel
                else if (Device_info.Serial_info.test_step == 9)
                {
                    if (Device_info.Serial_info.test_sta == 2)//108
                    {
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(3).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(3).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(3).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(3).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(3).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(3).SetCellValue(temp1);//写入频飘
                        }

                        Device_info.Serial_info.test_step = 0;
                        Device_info.Serial_info.test_sta++;
                    }
                    else if (Device_info.Serial_info.test_sta == 3)//140
                    {
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(6).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(6).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(6).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(6).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(6).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(6).SetCellValue(temp1);//写入频飘
                        }

                        Device_info.Serial_info.test_step = 0;
                        Device_info.Serial_info.test_sta++;

                    }
                    else if (Device_info.Serial_info.test_sta == 4)//174
                    {
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(9).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(9).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(9).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(9).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(9).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(9).SetCellValue(temp1);//写入频飘
                        }

                        Device_info.Serial_info.test_step = 0;
                        Device_info.Serial_info.test_sta++;
                    }
                    else if (Device_info.Serial_info.test_sta == 5)//225
                    {
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(10).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(10).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(10).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(10).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(10).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(10).SetCellValue(temp1);//写入频飘
                        }

                        Device_info.Serial_info.test_step = 0;
                        Device_info.Serial_info.test_sta++;
                    }
                    else if (Device_info.Serial_info.test_sta == 6)//260.2
                    {
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(12).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(12).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(12).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(12).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(12).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(12).SetCellValue(temp1);//写入频飘
                        }

                        Device_info.Serial_info.test_step = 0;
                        Device_info.Serial_info.test_sta++;
                    }
                    else if (Device_info.Serial_info.test_sta == 7)//300.2
                    {
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(14).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(14).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(14).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(14).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(14).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(14).SetCellValue(temp1);//写入频飘
                        }

                        Device_info.Serial_info.test_step = 0;
                        Device_info.Serial_info.test_sta++;
                    }
                    else if (Device_info.Serial_info.test_sta == 8)//340.2
                    {
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(16).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(16).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(16).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(16).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(16).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(16).SetCellValue(temp1);//写入频飘
                        }

                        Device_info.Serial_info.test_step = 0;
                        Device_info.Serial_info.test_sta++;
                    }
                    else if (Device_info.Serial_info.test_sta == 9)//400.2
                    {
                        //serialPort1_Write("SENS1:STAT ON");//左上选为tr1

                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(6).GetCell(19).SetCellValue(Device_info.Serial_info.test_3dB);//写入-3dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(7).GetCell(19).SetCellValue(Device_info.Serial_info.test_Loss);//写入中心频率插损
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(8).GetCell(19).SetCellValue(Device_info.Serial_info.test_40_30dB);//写入-30dB/-40dB
                        Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(9).GetCell(19).SetCellValue(Device_info.Serial_info.test_standWave);//写入驻波
                        if (Device_info.Serial_info.temperture_mode == 0) Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(19).SetCellValue("");//写入空值
                        else
                        {
                            string temp1;
                            //正号负号
                            if (Math.Abs(Device_info.Serial_info.test_FrqErr) < 0.05) temp1 = "0";
                            else if (Device_info.Serial_info.test_FrqErr > 0.05) temp1 = "+" + Device_info.Serial_info.test_FrqErr.ToString();
                            else temp1 = Device_info.Serial_info.test_FrqErr.ToString();
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(14).GetCell(19).SetCellValue(temp1);//写入频飘
                        }


                        if (Device_info.Serial_info.temperture_mode == 0)
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(3).GetCell(18).SetCellValue("常温");
                        else if (Device_info.Serial_info.temperture_mode == 1)
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(3).GetCell(18).SetCellValue("+75°C");
                        else if (Device_info.Serial_info.temperture_mode == 2)
                            Device_info.Serial_info.MyWorkbook.GetSheetAt(0).GetRow(3).GetCell(18).SetCellValue("-30°C");


                        using (FileStream fs = new FileStream(Device_info.DesktopPath + "\\产品检测记录.xls", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                            Device_info.Serial_info.MyWorkbook.Write(fs);

                        Device_info.Serial_info.MyWorkbook.Close();





                        //=================按键可用==============//
                        #region
                        //button11.Enabled = true;//一键配置
                        //butto.Enabled = true;//自动测试
                        //button18.Enabled = false;//终止自动测试
                       
                        //button3.Enabled = true;//-1
                        //button2.Enabled = true;//+1
                        //button5.Enabled = true;//-10
                        //button4.Enabled = true;//+10
                        //button7.Enabled = true;//108M
                        //button8.Enabled = true;//104M
                        //button6.Enabled = true;//174M
                        //button9.Enabled = true;//225M
                        //button14.Enabled = true;//260M
                        //button13.Enabled = true;//300M
                        //button15.Enabled = true;//340M
                        //button12.Enabled = true;//400M
                        //button17.Enabled = true;//自动生成报表
                        //button16.Enabled = true;//跳频
                        //button10.Enabled = false;//调频终止   
                        #endregion
                        progressBar1.Value = 0;
                        Device_info.Receive_mode = 0;//没有自动测试数据要接收
                        timer_TestReport.Stop();
                        timer_heart.Start();
                        MessageBox.Show("\"产品检测记录.xls\"写入完毕", "成功", MessageBoxButtons.OK);
                    }

                }

            }
        }
        //定时器：【串行】跳频
        public bool Hop_bool = true;
        private void Hop_Tick(object sender, EventArgs e)//定时器：跳频
        {
            if (Hop_bool)
            {
                double Frq1 = double.Parse(textBox5.Text);
                UInt16 hex1;
                if (Frq1 >= 225)
                    hex1 = System.Convert.ToUInt16((Frq1 - 225) / 0.4 + 166);
                else
                    hex1 = System.Convert.ToUInt16((Frq1 - 108) / 0.4);

                Device_info.SendData[1] = 0x01;//功能帧
                Device_info.SendData[2] = (byte)(hex1 / 256);
                Device_info.SendData[3] = (byte)(hex1 % 256);
                usbHID.WriteUSBHID(Device_info.SendData);

                Hop_bool = false;
            }
            else
            {
                double Frq2 = double.Parse(textBox6.Text);
                UInt16 hex2;

                if (Frq2 >= 225)
                    hex2 = System.Convert.ToUInt16((Frq2 - 225) / 0.4 + 166);
                else
                    hex2 = System.Convert.ToUInt16((Frq2 - 108) / 0.4);

                Device_info.SendData[1] = 0x01;//功能帧
                Device_info.SendData[2] = (byte)(hex2 / 256);
                Device_info.SendData[3] = (byte)(hex2 % 256);
                usbHID.WriteUSBHID(Device_info.SendData);

                Hop_bool = true;
            }
        }
        //定时器：【并行】自动测试
        public bool ParaAutotest_bool = false;
        private void timer_paraAuotest_Tick(object sender, EventArgs e)
        {
            if (Device_info.Parall_info.sample_cnt == 2048)
            {
                Device_info.Parall_info.Autotest_resForm.Show();
                ParaAutotest_dataproces();
                //按键变化
                SerialButton_All(true, true, true);
                ParallButton_All(true, true, true);
                DownloadButton_All(true);
                button18.Enabled = false;//终止按键关闭
                button19.Enabled = false;
                button10.Enabled = false;
                button48.Enabled = false;
                button26.Enabled = false;
                button40.Enabled = false;

                progressBar2.Value = 0;
                Device_info.Receive_mode = 0;//没有自动测试数据要接收
                timer_paraAuotest.Stop();
                timer_heart.Start();
            }
            if (Device_info.Parall_info.sample_cnt >= 1024 && Device_info.Config_info.Config_sta == 4)
            {
                Device_info.Config_info.Config_sta = 5;//并行第二段配置
                Device_info.Config_info.Config_Seq = 0;//清零计数

                timer_paraAuotest.Stop();
                timer_Cofig.Start();//开启配置
                return;
            }

            if (ParaAutotest_bool == false)//控制滤波器
            {
                ParaAutotest_bool = true;
                //滤波器控制，逐渐增加
                Device_info.SendData[1] = 0x02;//功能帧
                Device_info.SendData[2] = (byte)(Device_info.Parall_info.hex / 256);
                Device_info.SendData[3] = (byte)(Device_info.Parall_info.hex % 256);
                usbHID.WriteUSBHID(Device_info.SendData);
                Thread.Sleep(350);
            }
            else//读取矢网
            {
                ParaAutotest_bool = false;
                serialPort1_Write("CALC:MARK:FUNC:RES?");
            }
        }
        //定时器：【并行】自动生成报表
        private void timer_paraTestRepc_Tick(object sender, EventArgs e)
        {
            progressBar2.Value = progressBar2.Maximum * (Device_info.Parall_info.test_sta * 14 + Device_info.Parall_info.test_step) / (10 * 15 + 4);

            if (Device_info.Parall_info.test_sta == 0)//配置起始频率50M
            {
                serialPort1_Write("SENS1:FREQ:STAR 50E6");
                Device_info.Parall_info.test_sta++;
                Thread.Sleep(500);
            }
            else if (Device_info.Parall_info.test_sta == 1)//配置终止频率500M
            {
                serialPort1_Write("SENS1:FREQ:STOP 500E6");
                Device_info.Parall_info.test_sta++;
                Thread.Sleep(500);
            }
            else if (Device_info.Parall_info.test_sta >= 2)
            {
                //第一步发送配置 BW1 
                if (Device_info.Parall_info.test_step == 0)
                {
                    UInt16 hex = 0;
                    if (Device_info.Parall_info.test_sta == 2)//108
                    {
                        hex = System.Convert.ToUInt16((108 - 108) / 0.264);
                        Device_info.SendData[1] = 0x02;//功能帧
                        Device_info.SendData[2] = (byte)(hex / 256);
                        Device_info.SendData[3] = (byte)(hex % 256);
                        usbHID.WriteUSBHID(Device_info.SendData);
                        Thread.Sleep(1000);
                    }

                    else if (Device_info.Parall_info.test_sta == 3)//129.9
                        hex = System.Convert.ToUInt16((129.9 - 108) / 0.264);
                    else if (Device_info.Parall_info.test_sta == 4)//150.2
                        hex = System.Convert.ToUInt16((150.2 - 108) / 0.264);
                    else if (Device_info.Parall_info.test_sta == 5)//174
                        hex = System.Convert.ToUInt16((174 - 108) / 0.264);
                    else if (Device_info.Parall_info.test_sta == 6)//225
                        hex = System.Convert.ToUInt16((225 - 225) / 0.7 + 256);
                    else if (Device_info.Parall_info.test_sta == 7)//250.2
                        hex = System.Convert.ToUInt16((250.2 - 225) / 0.7 + 256);
                    else if (Device_info.Parall_info.test_sta == 8)//299.9
                        hex = System.Convert.ToUInt16((299.9 - 225) / 0.7 + 256);
                    else if (Device_info.Parall_info.test_sta == 9)//350.3
                        hex = System.Convert.ToUInt16((350.3 - 225) / 0.7 + 256);
                    else if (Device_info.Parall_info.test_sta == 10)//400
                        hex = System.Convert.ToUInt16((400 - 225) / 0.7 + 256);

                    Device_info.SendData[1] = 0x02;//功能帧
                    Device_info.SendData[2] = (byte)(hex / 256);
                    Device_info.SendData[3] = (byte)(hex % 256);
                    usbHID.WriteUSBHID(Device_info.SendData);
                    Thread.Sleep(1000);

                    Device_info.Parall_info.test_step = 1;


                }
                //第二部配置滤波器 500ms
                else if (Device_info.Parall_info.test_step == 1)
                {
                    string temp = "CALC1:MARK1:BWID " + Device_info.Parall_info.Bw1.ToString();
                    serialPort1_Write(temp);

                    Device_info.Parall_info.test_step++;
                }

                //发送读取命令bw cent q loss 500ms,等待串口
                else if (Device_info.Parall_info.test_step == 2)
                {
                    serialPort1_Write("CALC:MARK:FUNC:RES?");
                }
                //驻波
                else if (Device_info.Parall_info.test_step == 3)
                {
                    serialPort1_Write("CALC2:MARK1:Y?");

                }
                //All OFF
                else if (Device_info.Parall_info.test_step == 4)
                {
                    serialPort1_Write("CALC1:MARK:AOFF");
                    Device_info.Parall_info.test_step++;
                }
                //Mark1 ON
                else if (Device_info.Parall_info.test_step == 5)
                {
                    serialPort1_Write("CALC1:MARK1 ON");
                    Device_info.Parall_info.test_step++;
                }


                //0.95
                else if (Device_info.Parall_info.test_step == 6)
                {
                    string num = (Device_info.Parall_info.test_Frq * 0.95).ToString();
                    serialPort1_Write("CALC1:MARK1:X " + num + " MHZ");
                    Device_info.Parall_info.test_step++;
                }
                //读Loss
                else if (Device_info.Parall_info.test_step == 7)
                {
                    serialPort1_Write("CALC1:MARK1:Y?");
                }

                //0.90
                else if (Device_info.Parall_info.test_step == 8)
                {
                    string num = (Device_info.Parall_info.test_Frq * 0.9).ToString();
                    serialPort1_Write("CALC1:MARK1:X " + num + " MHZ");
                    Device_info.Parall_info.test_step++;
                }
                //读Loss
                else if (Device_info.Parall_info.test_step == 9)
                {
                    serialPort1_Write("CALC1:MARK1:Y?");
                }

                //1.05
                else if (Device_info.Parall_info.test_step == 10)
                {
                    string num = (Device_info.Parall_info.test_Frq * 1.05).ToString();
                    serialPort1_Write("CALC1:MARK1:X " + num + " MHZ");
                    Device_info.Parall_info.test_step++;
                }
                //读Loss
                else if (Device_info.Parall_info.test_step == 11)
                {
                    serialPort1_Write("CALC1:MARK1:Y?");
                }

                //1.1
                else if (Device_info.Parall_info.test_step == 12)
                {
                    string num = (Device_info.Parall_info.test_Frq * 1.1).ToString();
                    serialPort1_Write("CALC1:MARK1:X " + num + " MHZ");
                    Device_info.Parall_info.test_step++;
                }
                //读Loss
                else if (Device_info.Parall_info.test_step == 13)
                {
                    serialPort1_Write("CALC1:MARK1:Y?");
                }
                else if (Device_info.Parall_info.test_step == 14)
                {
                    int row = Device_info.Parall_info.test_sta - 2 + 5;
                    int cow = 0;
                    if (Device_info.Parall_info.temperture_mode == 0)//常温
                    {
                        Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(23).SetCellValue(Device_info.Parall_info.test_Standwave);//驻波
                        cow = 0;
                    }
                    else if (Device_info.Parall_info.temperture_mode == 1)//高温 
                    {
                        if (checkBox1.Checked)
                        {
                            if (Device_info.Parall_info.test_sta >= 2 && Device_info.Parall_info.test_sta <= 5)
                            {
                                Device_info.Parall_info.test_Frq += (double)Device_info.Parall_info.random.Next(10, 30) / 100;
                            }
                            else
                            {
                                Device_info.Parall_info.test_Frq += (double)Device_info.Parall_info.random.Next(20, 40) / 100;
                            }
                            Device_info.Parall_info.test_Loss += (double)Device_info.Parall_info.random.Next(5, 10) / 100;
                            Device_info.Parall_info.test_3d += (double)Device_info.Parall_info.random.Next(5, 10) / 100;
                            Device_info.Parall_info.neg5 += (double)Device_info.Parall_info.random.Next(10, 20) / 100;
                            Device_info.Parall_info.neg10 += (double)Device_info.Parall_info.random.Next(10, 20) / 100;
                            Device_info.Parall_info.pos5 += (double)Device_info.Parall_info.random.Next(10, 20) / 100;
                            Device_info.Parall_info.pos10 += (double)Device_info.Parall_info.random.Next(10, 20) / 100;
                        }
                        cow = 1;

                    }

                    else if (Device_info.Parall_info.temperture_mode == 2)//低温
                    {

                        if (checkBox1.Checked)
                        {
                            if (Device_info.Parall_info.test_sta >= 2 && Device_info.Parall_info.test_sta <= 5)
                            {
                                Device_info.Parall_info.test_Frq -= (double)Device_info.Parall_info.random.Next(10, 30) / 100;
                            }
                            else
                            {
                                Device_info.Parall_info.test_Frq -= (double)Device_info.Parall_info.random.Next(20, 40) / 100;
                            }
                            Device_info.Parall_info.test_Loss -= (double)Device_info.Parall_info.random.Next(5, 10) / 100;
                            Device_info.Parall_info.test_3d -= (double)Device_info.Parall_info.random.Next(5, 10) / 100;
                            Device_info.Parall_info.neg5 -= (double)Device_info.Parall_info.random.Next(10, 20) / 100;
                            Device_info.Parall_info.neg10 -= (double)Device_info.Parall_info.random.Next(10, 20) / 100;
                            Device_info.Parall_info.pos5 -= (double)Device_info.Parall_info.random.Next(10, 20) / 100;
                            Device_info.Parall_info.pos10 -= (double)Device_info.Parall_info.random.Next(10, 20) / 100;

                        }
                        cow = -1;
                    }


                    Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(3 + cow).SetCellValue(Device_info.Parall_info.test_Frq);//Frq
                    Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(6 + cow).SetCellValue(Device_info.Parall_info.test_Loss);//Loss
                    Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(9 + cow).SetCellValue(Device_info.Parall_info.test_3d);//Bw
                    Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(12 + cow).SetCellValue(Device_info.Parall_info.neg10);//-10
                    Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(15 + cow).SetCellValue(Device_info.Parall_info.pos10);//+10
                    Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(18 + cow).SetCellValue(Device_info.Parall_info.neg5);//-5
                    Device_info.Parall_info.MyWorkbook.GetSheetAt(0).GetRow(row).GetCell(21 + cow).SetCellValue(Device_info.Parall_info.pos5);//+5






                    if (Device_info.Parall_info.test_sta < 10)
                    {
                        Device_info.Parall_info.test_step = 0;
                        Device_info.Parall_info.test_sta++;
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(Device_info.DesktopPath + "\\并行产品检测记录.xls", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                            Device_info.Parall_info.MyWorkbook.Write(fs);

                        Device_info.Parall_info.MyWorkbook.Close();

                        progressBar2.Value = 0;
                        Device_info.Receive_mode = 0;//没有自动测试数据要接收
                        timer_paraTestRepc.Stop();
                        timer_heart.Start();
                        MessageBox.Show("\"并行产品检测记录.xls\"写入完毕", "成功", MessageBoxButtons.OK);
                    }




                }
            }
        }
        //定时器：【并行】跳频
        bool paraHop_bool = false;
        private void timer_paraHop_Tick(object sender, EventArgs e)
        {
            if (paraHop_bool)
            {
                double Frq1 = double.Parse(textBox7.Text);
                UInt16 hex1;
                if (Frq1 >= 225)
                    hex1 = System.Convert.ToUInt16((Frq1 - 225) / 0.7 + 256);
                else
                    hex1 = System.Convert.ToUInt16((Frq1 - 108) / 0.264);

                Device_info.SendData[1] = 0x02;//功能帧
                Device_info.SendData[2] = (byte)(hex1 / 256);
                Device_info.SendData[3] = (byte)(hex1 % 256);
                usbHID.WriteUSBHID(Device_info.SendData);

                paraHop_bool = false;
            }
            else
            {
                double Frq2 = double.Parse(textBox3.Text);
                UInt16 hex2;

                if (Frq2 >= 225)
                    hex2 = System.Convert.ToUInt16((Frq2 - 225) / 0.7 + 256);
                else
                    hex2 = System.Convert.ToUInt16((Frq2 - 108) / 0.264);

                Device_info.SendData[1] = 0x02;//功能帧
                Device_info.SendData[2] = (byte)(hex2 / 256);
                Device_info.SendData[3] = (byte)(hex2 % 256);
                usbHID.WriteUSBHID(Device_info.SendData);

                paraHop_bool = true;
            }
        }
        #endregion

        #region button逻辑
        #region 开始页面按钮
        private void button1_Click(object sender, EventArgs e)//输出路径
        {

            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Device_info.DesktopPath = dialog.SelectedPath;
                label_DesktopPath.Text = Device_info.DesktopPath;
            }

        }
        private void button20_Click(object sender, EventArgs e)//开启心跳
        {
            if (button20.Text == "关闭")
            {
                timer_heart.Stop();
                timer_Cofig.Stop();
                timer_Autotest.Stop();
                timer_Hop.Stop();
                timer_TestReport.Stop();

                timer_paraAuotest.Stop();
                timer_paraHop.Stop();
                timer_paraTestRepc.Stop();

                button20.Text = "开启";
            }
            else if (button20.Text == "开启")
            {
                timer_heart.Start();
                timer_Cofig.Stop();
                timer_Autotest.Stop();
                timer_Hop.Stop();
                timer_TestReport.Stop();

                timer_paraAuotest.Stop();
                timer_paraHop.Stop();
                timer_paraTestRepc.Stop();

                button20.Text = "关闭";
            }

        }
       
        #endregion
        #region 串行页面按钮
        private void button1_Click_1(object sender, EventArgs e)//自动测试
        {
            Device_info.Receive_mode = 1;//串口接收的是自动测试数据

            //serialPort1_Write("CALC1:PAR1:SEL");//左上，选择trace1
            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = 0;
            Device_info.SendData[3] = 0;
            usbHID.WriteUSBHID(Device_info.SendData);

            Device_info.Config_info.Config_sta = 2;//配置第一段
            Device_info.Config_info.Config_Seq = 0;//配置计数清零

            //按键变化
            SerialButton_All(true, true, false);
            ParallButton_All(true, true, false);
            DownloadButton_All(false);
            button18.Enabled = true;//终止按键打开


            Device_info.Serial_info.Autotest_Form = new Form2();
            timer_heart.Stop();
            timer_Cofig.Start();//开启配置

        }
        private void button18_Click(object sender, EventArgs e)//自动测试终止
        {
            //按键变化
            SerialButton_All(true, true, true);
            ParallButton_All(true, true, true);
            DownloadButton_All(true);

            button18.Enabled = false;//终止按键关闭
            button19.Enabled = false;
            button10.Enabled = false;
            button48.Enabled = false;
            button26.Enabled = false;
            button40.Enabled = false;
            

            timer_Cofig.Stop();
            timer_Autotest.Stop();
            progressBar1.Value = 0;

            timer_heart.Start();
        }
        private void button2_Click(object sender, EventArgs e)//手动测试+1
        {

            string str = textBox4.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex + 1 <= 604) hex++;
            serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

            //理论频率
            if (hex > 165)
                label18.Text = (225 + (hex - 166) * 0.4).ToString() + "MHz";
            else
                label18.Text = (108 + hex * 0.4).ToString() + "MHz";

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox4.Text = hex.ToString();

          


        }
        private void button3_Click(object sender, EventArgs e)//手动测试-1
        {

            string str = textBox4.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex != 0) hex--;
            serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

            //理论频率
            if (hex > 165)
                label18.Text = (225 + (hex - 166) * 0.4).ToString() + "MHz";
            else
                label18.Text = (108 + hex * 0.4).ToString() + "MHz";


            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox4.Text = hex.ToString();


         
        }
        private void button4_Click(object sender, EventArgs e)//手动测试+10
        {
            string str = textBox4.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex + 10 <= 604) hex += 10;

            serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            //理论频率
            if (hex > 165)
                label18.Text = (225 + (hex - 166) * 0.4).ToString() + "MHz";
            else
                label18.Text = (108 + hex * 0.4).ToString() + "MHz";


            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox4.Text = hex.ToString();


        }
        private void button5_Click(object sender, EventArgs e)//手动测试-10
        {
            string str = textBox4.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex >= 10) hex -= 10;
            serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            //理论频率
            if (hex > 165)
                label18.Text = (225 + (hex - 166) * 0.4).ToString() + "MHz";
            else
                label18.Text = (108 + hex * 0.4).ToString() + "MHz";


            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox4.Text = hex.ToString();


        }
        private void button16_Click(object sender, EventArgs e)//跳频
        {

            //按键变化
            SerialButton_All(true, true, false);
            ParallButton_All(true, true, false);
            DownloadButton_All(false);
            button10.Enabled = true;//终止按键打开


            timer_heart.Stop();
            timer_Hop.Start();
        }
        private void button10_Click(object sender, EventArgs e)//终止跳频
        {
            ////按键变化
            //SerialButton_All(true, true, true);
            //ParallButton_All(true, true, true);
            //DownloadButton_All(true);
            //button18.Enabled = false;//终止按键关闭
            //button19.Enabled = false;
            //button10.Enabled = false;
            //button48.Enabled = false;
            //button26.Enabled = false;
            //button40.Enabled = false;

            //交给心跳，进行重绘button
            STMcnectHis = !Device_info.connect_STM;
            NAcnectHis = !Device_info.connect_NA;
            
            progressBar1.Value = 0;

            timer_Hop.Stop();
            timer_heart.Start();
        }
        private void button11_Click(object sender, EventArgs e)//一键配置
        {
            if (Device_info.connect_NA)
            {
                string BW1_dBm = textBox_BW1dB.Text.ToString();
                string BW2_dBm = textBox_BW2dB.Text.ToString();

                string Star1_Frq = textBox_STAR1Frq.Text.ToString();
                string Stop1_Frq = textBox_STOP1Frq.Text.ToString();

                string Star2_Frq = textBox_STAR2Frq.Text.ToString();
                string Stop2_Frq = textBox_STOP2Frq.Text.ToString();

                string PWR_dBm = textBox_PWRdB.Text.ToString();
                string Offset = textBox_OFFSET.Text.ToString();


                //存储BW1，BW2，起始，终止，温飘
                Device_info.Serial_info.Bw1 = double.Parse(BW1_dBm);
                Device_info.Serial_info.Bw2 = double.Parse(BW2_dBm);

                Device_info.Serial_info.Start1Frq = double.Parse(Star1_Frq);
                Device_info.Serial_info.Start2Frq = double.Parse(Star2_Frq);

                Device_info.Serial_info.Stop1Frq = double.Parse(Stop1_Frq);
                Device_info.Serial_info.Stop2Frq = double.Parse(Stop2_Frq);

                Device_info.Serial_info.OffSet = double.Parse(Offset);
                Device_info.Serial_info.PWR_dBm = double.Parse(PWR_dBm);

              
                Device_info.Config_info.Config_content[1] ="SOUR1:POW:LEV:IMM:AMPL "+ Device_info.Serial_info.PWR_dBm.ToString() + "E0";
                Device_info.Config_info.Config_content[3] = "SENS1:FREQ:STAR "+ Device_info.Serial_info.Start1Frq.ToString() + "E6";
                Device_info.Config_info.Config_content[4] = "SENS1:FREQ:STOP "+ Device_info.Serial_info.Stop1Frq.ToString() + "E6";
                Device_info.Config_info.Config_content[5] = "CALC1:MARK1:BWID "+Device_info.Serial_info.Bw1.ToString() + "E0";



                //按键变化
                SerialButton_All(true, false, false);
                ParallButton_All(true, false, false);


                Device_info.Config_info.Config_Seq = 0;//配置计数清零
                Device_info.Config_info.Config_sta = 1;//一键配置仪器状态
                progressBar1.Value = 0;//进度条为0
                timer_heart.Stop();
                timer_Cofig.Start();//开启配置
            }

            else
                MessageBox.Show("请先连接失网", "错误", MessageBoxButtons.OK);
        }
        private void button17_Click(object sender, EventArgs e)//自动生成报表
        {
            string fileName = Device_info.DesktopPath + "\\产品检测记录.xls";
            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(fileName))
                        Device_info.Serial_info.MyWorkbook = new HSSFWorkbook(stream);
                }
                catch
                {
                    MessageBox.Show("请关闭\"产品检测记录.xls\"", "错误", MessageBoxButtons.OK);
                    return;
                }



                //Device_info.Serial_info.MyWorkbook.Close();
                Device_info.Serial_info.test_step = 0;
                Device_info.Serial_info.test_sta = 0;


                //按键变化
                SerialButton_All(true, true, false);
                ParallButton_All(true, true, false);
                DownloadButton_All(false);
                button19.Enabled = true;//终止按键打开

                Device_info.Serial_info.temperture_mode = comboBox1.SelectedIndex;
                Device_info.Receive_mode = 2;//串口接收数据是自动生成报表的

                timer_heart.Stop();
                timer_TestReport.Start();
            }
            else
            {

                MessageBox.Show("请先在" + Device_info.DesktopPath + "创建\"产品检测记录.xls\"", "", MessageBoxButtons.OK);


            }
        }
        private void button19_Click(object sender, EventArgs e)//自动生成报表，终止
        {

            //按键变化
            SerialButton_All(true, true, true);
            ParallButton_All(true, true, true);
            DownloadButton_All(true);
            button18.Enabled = false;//终止按键关闭
            button19.Enabled = false;
            button10.Enabled = false;
            button48.Enabled = false;
            button26.Enabled = false;
            button40.Enabled = false;


            Device_info.Serial_info.MyWorkbook.Close();//关闭Excel工作簿
            progressBar1.Value = 0;
            timer_TestReport.Stop();
            timer_heart.Start();
        }
        private void button7_Click(object sender, EventArgs e)//180
        {

            UInt16 hex = System.Convert.ToUInt16((108 - 108) / 0.4);
            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button8_Click(object sender, EventArgs e)//140
        {
            UInt16 hex = System.Convert.ToUInt16((140 - 108) / 0.4);

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button6_Click(object sender, EventArgs e)//174
        {
            UInt16 hex = System.Convert.ToUInt16((174 - 108) / 0.4);

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button9_Click(object sender, EventArgs e)//225
        {
            UInt16 hex = System.Convert.ToUInt16((225 - 225) / 0.4 + 166);

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button14_Click(object sender, EventArgs e)//260.2
        {
            UInt16 hex = System.Convert.ToUInt16((260.2 - 225) / 0.4 + 166);

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button13_Click(object sender, EventArgs e)//300.2
        {
            UInt16 hex = System.Convert.ToUInt16((300.2 - 225) / 0.4 + 166);

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button15_Click(object sender, EventArgs e)//340.2
        {
            UInt16 hex = System.Convert.ToUInt16((340.2 - 225) / 0.4 + 166);

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button12_Click(object sender, EventArgs e)//400.2
        {
            UInt16 hex = System.Convert.ToUInt16((400.2 - 225) / 0.4 + 166);

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }        
        private void button24_Click(object sender, EventArgs e)//手动置数-1
        {
            string str = textBox1.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            if (hex != 0) hex--;
            //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            textBox1.Text = hex.ToString("X");
            if (comboBox2.SelectedIndex == 1)//225-512
                hex |= 0x1000;

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);


        }
        private void button25_Click(object sender, EventArgs e)//手动置数+1
        {
            string str = textBox1.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            hex++;
            //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            textBox1.Text = hex.ToString("X");
            if (comboBox2.SelectedIndex == 1)//225-512
                hex |= 0x1000;

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button22_Click(object sender, EventArgs e)//手动置数-10
        {
            string str = textBox1.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            if (hex >= 10) hex -= 10;
            //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            textBox1.Text = hex.ToString("X");
            if (comboBox2.SelectedIndex == 1)//225-512
                hex |= 0x1000;

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button23_Click(object sender, EventArgs e)//手动置数+10
        {
            string str = textBox1.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            hex += 10;
            //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            textBox1.Text = hex.ToString("X");
            if (comboBox2.SelectedIndex == 1)//225-512
                hex |= 0x1000;

            Device_info.SendData[1] = 0x01;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        #endregion
        #region 并行页面按钮
        private void button46_Click(object sender, EventArgs e)//手动置数+1
        {
            string str = textBox16.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            if (hex < 1023) hex++;
            //srialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            textBox16.Text = hex.ToString("X");
            if (comboBox4.SelectedIndex == 1)//225-512
                hex |= 0x400;

            Device_info.SendData[1] = 0x02;//并行功能
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button45_Click(object sender, EventArgs e)//手动置数-1
        {
            string str = textBox16.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            if (hex != 0) hex--;

            textBox16.Text = hex.ToString("X");
            if (comboBox4.SelectedIndex == 1)//225-512
                hex |= 0x400;

            Device_info.SendData[1] = 0x02;//并行功能
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button44_Click(object sender, EventArgs e)//手动置数+10
        {
            string str = textBox16.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            if (hex + 10 <= 1023) hex += 10;
            //srialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            textBox16.Text = hex.ToString("X");
            if (comboBox4.SelectedIndex == 1)//225-512
                hex |= 0x400;

            Device_info.SendData[1] = 0x02;//并行功能
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button43_Click(object sender, EventArgs e)//手动置数-10
        {
            string str = textBox16.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            if (hex - 10 >= 0) hex -= 10;
            //srialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            textBox16.Text = hex.ToString("X");
            if (comboBox4.SelectedIndex == 1)//225-512
                hex |= 0x400;

            Device_info.SendData[1] = 0x02;//并行功能
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button42_Click(object sender, EventArgs e)//一键配置
        {
            if (Device_info.connect_NA)
            {
                string BW1_dBm = textBox15.Text.ToString();


                string Star1_Frq = textBox11.Text.ToString();
                string Stop1_Frq = textBox10.Text.ToString();

                string Star2_Frq = textBox9.Text.ToString();
                string Stop2_Frq = textBox8.Text.ToString();

                string PWR_dBm = textBox14.Text.ToString();
                string Offset = textBox13.Text.ToString();


                //存储BW1，起始，终止，温飘
                Device_info.Parall_info.Bw1 = Convert.ToDouble(BW1_dBm);


                Device_info.Parall_info.Start1Frq = Convert.ToDouble(Star1_Frq);
                Device_info.Parall_info.Start2Frq = Convert.ToDouble(Star2_Frq);

                Device_info.Parall_info.Stop1Frq = Convert.ToDouble(Stop1_Frq);
                Device_info.Parall_info.Stop2Frq = Convert.ToDouble(Stop2_Frq);

                Device_info.Parall_info.OffSet = Convert.ToDouble(Offset);
                Device_info.Parall_info.PWR_dBm = Convert.ToDouble(PWR_dBm);


                Device_info.Config_info.Config_content[1] = "SOUR1:POW:LEV:IMM:AMPL " + Device_info.Parall_info.PWR_dBm.ToString() + "E0";
                Device_info.Config_info.Config_content[3] = "SENS1:FREQ:STAR " + Device_info.Parall_info.Start1Frq.ToString() + "E6";
                Device_info.Config_info.Config_content[4] = "SENS1:FREQ:STOP " + Device_info.Parall_info.Stop1Frq.ToString() + "E6";
                Device_info.Config_info.Config_content[5] = "CALC1:MARK1:BWID " + Device_info.Parall_info.Bw1.ToString() + "E0";


          
                //按键变化
                SerialButton_All(true, false, false);
                ParallButton_All(true, false, false);

                Device_info.Config_info.Config_Seq = 0;//配置计数清零
                Device_info.Config_info.Config_sta = 1;//一键配置仪器状态
                progressBar2.Value = 0;//进度条为0
                timer_heart.Stop();
                timer_Cofig.Start();//开启配置
            }
        }
        private void button47_Click(object sender, EventArgs e)//自动测试
        {
            Device_info.Receive_mode = 4;//串口接收的是自动测试数据

            //按键变化
            SerialButton_All(true, true, false);
            ParallButton_All(true, true, false);
            DownloadButton_All(false);
            button48.Enabled = true;//终止按键打开


            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = 0;
            Device_info.SendData[3] = 0;
            usbHID.WriteUSBHID(Device_info.SendData);

            Device_info.Config_info.Config_sta = 4;//并行配置第一段
            Device_info.Config_info.Config_Seq = 0;//配置计数清零

            Device_info.Parall_info.Autotest_resForm = new Form3();
            timer_heart.Stop();
            timer_Cofig.Start();//开启配置
        }
        private void button48_Click(object sender, EventArgs e)//自动测试，终止
        {
            //按键变化
            SerialButton_All(true, true, true);
            ParallButton_All(true, true, true);
            DownloadButton_All(true);

            button18.Enabled = false;//终止按键关闭
            button19.Enabled = false;
            button10.Enabled = false;
            button48.Enabled = false;
            button26.Enabled = false;
            button40.Enabled = false;

            timer_Cofig.Stop();
            timer_paraAuotest.Stop();
            progressBar2.Value = 0;

            timer_heart.Start();
        }        
        private void button38_Click(object sender, EventArgs e)//手动测试-1
        {
            string str = textBox2.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex != 0) hex--;
            //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

            //理论频率
            if (hex > 255)
                label46.Text = (225 + (hex - 256) * 0.7).ToString() + "MHz";
            else
                label46.Text = (108 + hex * 0.264).ToString() + "MHz";


            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox2.Text = hex.ToString();

        }
        private void button39_Click(object sender, EventArgs e)//手动测试+1
        {
            string str = textBox2.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex + 1 <= 511) hex++;
            //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

            //理论频率
            if (hex > 255)
                label46.Text = (225 + (hex - 256) * 0.7).ToString() + "MHz";
            else
                label46.Text = (108 + hex * 0.264).ToString() + "MHz";


            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox2.Text = hex.ToString();

        }
        private void button36_Click(object sender, EventArgs e)//手动测试-10
        {
            string str = textBox2.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex >= 10) hex -= 10;

            //理论频率
            if (hex > 255)
                label46.Text = (225 + (hex - 256) * 0.7).ToString() + "MHz";
            else
                label46.Text = (108 + hex * 0.264).ToString() + "MHz";


            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox2.Text = hex.ToString();
        }
        private void button37_Click(object sender, EventArgs e)//手动测试+10
        {
            string str = textBox2.Text;
            UInt16 hex = UInt16.Parse(str);
            if (hex + 10 <= 511) hex += 10;

            serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1
            //理论频率
            if (hex > 255)
                label46.Text = (225 + (hex - 256) * 0.7).ToString() + "MHz";
            else
                label46.Text = (108 + hex * 0.264).ToString() + "MHz";


            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
            textBox2.Text = hex.ToString();
        }
        private void button34_Click(object sender, EventArgs e)//108
        {
            UInt16 hex = System.Convert.ToUInt16((108 - 108) / 0.264);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button33_Click(object sender, EventArgs e)//129.9
        {
            UInt16 hex = System.Convert.ToUInt16((129.9 - 108) / 0.264);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button35_Click(object sender, EventArgs e)//150.2
        {
            UInt16 hex = System.Convert.ToUInt16((150.2 - 108) / 0.264);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button32_Click(object sender, EventArgs e)//174
        {
            UInt16 hex = System.Convert.ToUInt16((174 - 108) / 0.264);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button30_Click(object sender, EventArgs e)//225
        {
            UInt16 hex = System.Convert.ToUInt16((225 - 225) / 0.7 + 256);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button29_Click(object sender, EventArgs e)//250.2
        {
            UInt16 hex = System.Convert.ToUInt16((250.2 - 225) / 0.7 + 256);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button31_Click(object sender, EventArgs e)//299.9
        {
            UInt16 hex = System.Convert.ToUInt16((299.9 - 225) / 0.7 + 256);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button28_Click(object sender, EventArgs e)//350.3
        {
            UInt16 hex = System.Convert.ToUInt16((350.3 - 225) / 0.7 + 256);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button54_Click(object sender, EventArgs e)//400
        {
            UInt16 hex = System.Convert.ToUInt16((400 - 225) / 0.7 + 256);
            Device_info.SendData[1] = 0x02;//功能帧
            Device_info.SendData[2] = (byte)(hex / 256);
            Device_info.SendData[3] = (byte)(hex % 256);
            usbHID.WriteUSBHID(Device_info.SendData);
        }
        private void button27_Click(object sender, EventArgs e)//自动生成报表
        {
            string fileName = Device_info.DesktopPath + "\\并行产品检测记录.xls";
            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(fileName))
                        Device_info.Parall_info.MyWorkbook = new HSSFWorkbook(stream);
                }
                catch
                {
                    MessageBox.Show("请关闭\"并行产品检测记录.xls\"", "错误", MessageBoxButtons.OK);
                    return;
                }



                Device_info.Parall_info.test_step = 0;
                Device_info.Parall_info.test_sta = 0;

                //按键变化
                SerialButton_All(true, true, false);
                ParallButton_All(true, true, false);
                DownloadButton_All(false);
                button26.Enabled = true;//终止按键打开

                Device_info.Parall_info.temperture_mode = comboBox3.SelectedIndex;
                Device_info.Receive_mode = 5;//并行串口接收模式

                timer_heart.Stop();
                timer_paraTestRepc.Start();
            }
            else
            {

                MessageBox.Show("请先在" + Device_info.DesktopPath + "创建\"产品检测记录.xls\"", "", MessageBoxButtons.OK);


            }
        }
        private void button26_Click(object sender, EventArgs e)//自动生成报表，终止
        {
            //按键变化
            SerialButton_All(true, true, true);
            ParallButton_All(true, true, true);
            DownloadButton_All(true);
            button18.Enabled = false;//终止按键关闭
            button19.Enabled = false;
            button10.Enabled = false;
            button48.Enabled = false;
            button26.Enabled = false;
            button40.Enabled = false;


            Device_info.Parall_info.MyWorkbook.Close();//关闭Excel工作簿
            progressBar2.Value = 0;
            timer_paraTestRepc.Stop();
            timer_heart.Start();
        }
        private void button41_Click(object sender, EventArgs e)//跳频
        {

            //按键变化
            SerialButton_All(true, true, false);
            ParallButton_All(true, true, false);
            DownloadButton_All(false);
            button40.Enabled = true;//终止按键打开

            timer_paraHop.Start();
            timer_heart.Stop();
        }
        private void button40_Click(object sender, EventArgs e)//跳频，终止
        {
            //交给心跳，进行重绘button
            STMcnectHis = !Device_info.connect_STM;
            NAcnectHis = !Device_info.connect_NA;


            progressBar2.Value = 0;

            timer_paraHop.Stop();
            timer_heart.Start();
        }
        #endregion
        #region 下载页面按钮
        private void button49_Click(object sender, EventArgs e)//选择文件
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = Device_info.DesktopPath;//默认打开C：
            fileDialog.Filter = "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
            //fileDialog1.FilterIndex = 1;//如果您设置 FilterIndex 属性，则当显示对话框时，将选择该筛选器。
            //fileDialog1.RestoreDirectory = true;//取得或设定值，指出对话方块是否在关闭前还原目前的目录。
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                Device_info.Parall_info.DownloadFilePath = fileDialog.FileName;
                label27.Text = fileDialog.SafeFileName;
                StreamReader fileRead = new StreamReader(Device_info.Parall_info.DownloadFilePath);
                richTextBox1.Text = fileRead.ReadToEnd();
                fileRead.Close();
            }

        }
        private void button51_Click(object sender, EventArgs e)//擦出
        {

            label_erase.Text = "";
            //擦除
            Device_info.SendData[1] = 0x05;//擦除
            usbHID.WriteUSBHID(Device_info.SendData);



        }
        private void button50_Click(object sender, EventArgs e)//下载
        {
            if (Device_info.Parall_info.DownloadFilePath == null)
            {
                MessageBox.Show("选择一项下载文件");
                return;
            }
            else
            {
                StreamReader File = new StreamReader(Device_info.Parall_info.DownloadFilePath, Encoding.Default);

                int Rowcnt = 0;//行数
                while (File.ReadLine() != null)
                {
                    Rowcnt++;
                }
                Device_info.Parall_info.DownloadFileRow = Rowcnt;
                File.Close();
            }
            label_dnload.Text = "";
            StreamReader DownloadFile = new StreamReader(Device_info.Parall_info.DownloadFilePath, Encoding.Default);

            Device_info.SendData[1] = 0x03;//下载
            Device_info.SendData[2] = 0xAA;//起始信号
            usbHID.WriteUSBHID(Device_info.SendData);
            UInt16 i;
            for (i = 0; i < Device_info.Parall_info.DownloadFileRow; i += 2)
            {
                try
                {
                    progressBar3.Value = progressBar3.Maximum * i / Device_info.Parall_info.DownloadFileRow;
                    string Line1 = DownloadFile.ReadLine();
                    string Line2 = DownloadFile.ReadLine();
                    string[] strs1 = Line1.Split(' ');
                    string[] strs2 = Line2.Split(' ');
                    UInt16 hex1 = Convert.ToUInt16(strs1[3], 16);
                    UInt16 hex2 = Convert.ToUInt16(strs2[3], 16);

                    Device_info.SendData[1] = 0x03;//下载
                    Device_info.SendData[2] = 0xBB;//发送中

                    Device_info.SendData[3] = (byte)(hex1 / 256);//data1
                    Device_info.SendData[4] = (byte)(hex1 % 256);

                    Device_info.SendData[5] = (byte)(hex2 / 256);//data2
                    Device_info.SendData[6] = (byte)(hex2 % 256);

                    usbHID.WriteUSBHID(Device_info.SendData);
                    Thread.Sleep(1);
                }
                catch
                {
                    break;
                }
            }
            progressBar3.Value = 0;
            if (i == Device_info.Parall_info.DownloadFileRow)
            {
                label_dnload.Text = "√";
                label_dnload.ForeColor = Color.Green;
                Device_info.SendData[1] = 0x03;//下载
                Device_info.SendData[2] = 0xCC;//终止信号
                usbHID.WriteUSBHID(Device_info.SendData);



                Device_info.SendData[1] = 0x04;//验证
                usbHID.WriteUSBHID(Device_info.SendData);
            }
            else
            {
                label_dnload.Text = "×";
                label_dnload.ForeColor = Color.Red;
            }




        }
        private void button52_Click(object sender, EventArgs e)//验证
        {
            if (Device_info.Parall_info.DownloadFilePath == null)
            {
                MessageBox.Show("选择一项下载文件");
                return;
            }
            else
            {
                label_cali.Text = "";
                StreamReader File = new StreamReader(Device_info.Parall_info.DownloadFilePath, Encoding.Default);

                int Rowcnt = 0;//行数
                while (File.ReadLine() != null)
                {
                    Rowcnt++;
                }
                Device_info.Parall_info.DownloadFileRow = Rowcnt;
                File.Close();
                Device_info.SendData[1] = 0x04;//验证
                usbHID.WriteUSBHID(Device_info.SendData);
            }


        }
        private void button53_Click(object sender, EventArgs e)//一键下载
        {
            //擦除

            label_cali.Text = "";
            label_dnload.Text = "";
            label_erase.Text = "";

            progressBar3.Value = 0;
            Device_info.SendData[1] = 0x05;//擦除
            usbHID.WriteUSBHID(Device_info.SendData);

            Thread.Sleep(1000);
            if (Device_info.Parall_info.DownloadFilePath == null)
            {
                MessageBox.Show("选择一项下载文件");
                return;
            }
            else
            {
                StreamReader File = new StreamReader(Device_info.Parall_info.DownloadFilePath, Encoding.Default);

                int Rowcnt = 0;//行数
                while (File.ReadLine() != null)
                {
                    Rowcnt++;
                }
                Device_info.Parall_info.DownloadFileRow = Rowcnt;
                File.Close();
            }

            StreamReader DownloadFile = new StreamReader(Device_info.Parall_info.DownloadFilePath, Encoding.Default);

            Device_info.SendData[1] = 0x03;//下载
            Device_info.SendData[2] = 0xAA;//起始信号
            usbHID.WriteUSBHID(Device_info.SendData);
            UInt16 i;
            for (i = 0; i < Device_info.Parall_info.DownloadFileRow; i += 2)
            {
                try
                {
                    progressBar3.Value = progressBar3.Maximum * i / Device_info.Parall_info.DownloadFileRow;
                    string Line1 = DownloadFile.ReadLine();
                    string Line2 = DownloadFile.ReadLine();
                    string[] strs1 = Line1.Split(' ');
                    string[] strs2 = Line2.Split(' ');
                    UInt16 hex1 = Convert.ToUInt16(strs1[3], 16);
                    UInt16 hex2 = Convert.ToUInt16(strs2[3], 16);

                    Device_info.SendData[1] = 0x03;//下载
                    Device_info.SendData[2] = 0xBB;//发送中

                    Device_info.SendData[3] = (byte)(hex1 / 256);//data1
                    Device_info.SendData[4] = (byte)(hex1 % 256);

                    Device_info.SendData[5] = (byte)(hex2 / 256);//data2
                    Device_info.SendData[6] = (byte)(hex2 % 256);

                    usbHID.WriteUSBHID(Device_info.SendData);
                    Thread.Sleep(10);
                }
                catch
                {
                    break;
                }

            }
            if (i == Device_info.Parall_info.DownloadFileRow)
            {
                label_dnload.Text = "√";
                label_dnload.ForeColor = Color.Green;
                Device_info.SendData[1] = 0x03;//下载
                Device_info.SendData[2] = 0xCC;//终止信号
                usbHID.WriteUSBHID(Device_info.SendData);
                Thread.Sleep(2000);


                Device_info.SendData[1] = 0x04;//验证
                usbHID.WriteUSBHID(Device_info.SendData);
            }
            else
            {
                label_dnload.Text = "×";
                label_dnload.ForeColor = Color.Red;
            }





        }
        #endregion
        #endregion

        #region 事件：编辑框回车
        //事件：编辑框的回车按键
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)//
        {
            if (e.KeyChar == '\r')
            {
                string str = textBox1.Text;
                UInt16 hex = Convert.ToUInt16(str, 16);

                //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

                if (comboBox2.SelectedIndex == 1)//225-512
                    hex |= 0x1000;


                Device_info.SendData[1] = 0x01;//功能帧
                Device_info.SendData[2] = (byte)(hex / 256);
                Device_info.SendData[3] = (byte)(hex % 256);
                usbHID.WriteUSBHID(Device_info.SendData);
            }
        }
        //事件：编辑框的回车按键
        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)//事件：手动测试的回车按键
        {
            if (e.KeyChar == '\r')
            {
                string str = textBox4.Text;
                UInt16 hex = UInt16.Parse(str);

                serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

                //理论频率
                if (hex > 165)
                    label18.Text = (225 + (hex - 166) * 0.4).ToString() + "MHz";
                else
                    label18.Text = (108 + hex * 0.4).ToString() + "MHz";


                Device_info.SendData[1] = 0x01;//功能帧
                Device_info.SendData[2] = (byte)(hex / 256);
                Device_info.SendData[3] = (byte)(hex % 256);
                usbHID.WriteUSBHID(Device_info.SendData);
                textBox4.Text = hex.ToString();

                Thread.Sleep(500);
                serialPort1_Write("CALC1:MARK1:BWID:DATA?");//查询实际频率，等待接收
                Device_info.Receive_mode = 3;//串口接收的是手动测试数据

            }

        }
        //事件：编辑框的回车按键
        private void textBox16_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                string str = textBox16.Text;
                UInt16 hex = Convert.ToUInt16(str, 16);

                //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

                if (comboBox4.SelectedIndex == 1)//225-512
                    hex |= 0x400;


                Device_info.SendData[1] = 0x02;//并行功能
                Device_info.SendData[2] = (byte)(hex / 256);
                Device_info.SendData[3] = (byte)(hex % 256);
                usbHID.WriteUSBHID(Device_info.SendData);
            }
        }
        //事件：手动置数下拉框改变
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)//事件：手动置数的下拉框改变
        {
            string str = textBox1.Text;
            UInt16 hex = Convert.ToUInt16(str, 16);

            //serialPort1_Write("CALC1:PAR1:SEL");//2,左上选为tr1

            if (comboBox2.SelectedIndex == 1)//225-512
                hex |= 0x1000;

            if (Device_info.connect_STM == true)
            {
                Device_info.SendData[1] = 0x01;//功能帧
                Device_info.SendData[2] = (byte)(hex / 256);
                Device_info.SendData[3] = (byte)(hex % 256);
                usbHID.WriteUSBHID(Device_info.SendData);
            }

        }
        //事件：编辑框的回车按键
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                string str = textBox2.Text;
                UInt16 hex = UInt16.Parse(str);
                if (hex > 511) return;

                //理论频率
                if (hex > 255)
                    label46.Text = (225 + (hex - 256) * 0.7).ToString() + "MHz";
                else
                    label46.Text = (108 + hex * 0.264).ToString() + "MHz";


                Device_info.SendData[1] = 0x02;//功能帧
                Device_info.SendData[2] = (byte)(hex / 256);
                Device_info.SendData[3] = (byte)(hex % 256);
                usbHID.WriteUSBHID(Device_info.SendData);





            }
        }
        #endregion


    }
}