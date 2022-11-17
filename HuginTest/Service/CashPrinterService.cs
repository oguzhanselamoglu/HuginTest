using Hugin.Common;
using Hugin.POS.CompactPrinter.FP300;
using HuginTest.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HuginTest.Service
{
    public class CashPrinterService : IBridge
    {
        #region Defination
        public event LogEventHandler LogChanged;

        private static string fiscalId = "FP12345678";
        private static ICompactPrinter printer = null;
        private static bool isMatchedBefore = false;
        private static IConnection conn;
        public string IpAddress { get; set; }
        public string PortNumber { get; set; }
        public string FiscalId { get; set; }
        private static FiscalCmd lastCmd;
        public string LogPath { get; set; }
        public IConnection Connection
        {
            get
            {
                return conn;
            }
            set
            {
                conn = value;
            }
        }

        public ICompactPrinter Printer
        {
            get
            {
                return printer;
            }
        }
        #endregion

        public CashPrinterService(string ipAddress, string port, string fisCalId)
        {
            IpAddress = ipAddress;
            PortNumber = port;
            FiscalId = fisCalId;
        }
        public void Log(string log)
        {
            LogChanged(log);
            WriteLog(log);

        }

        private void WriteLog(string log)
        {
            string path = @"G:\OS\Server\Sedna Hotel";
            StreamWriter V_File_Write;
            FileStream V_File_Create;
            string V_FileName;
           
                V_FileName = "Pos" + DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString() + ".txt";
          
            if (File.Exists(path + @"\" + V_FileName) == false)
            {
                V_File_Create = File.Create(path + @"\" + V_FileName);
                V_File_Create.Close();
                V_File_Create.Dispose();
            }

            V_File_Write = File.AppendText(path + @"\" + V_FileName);
            V_File_Write.WriteLine(log);
            V_File_Write.Close();
            V_File_Write.Dispose();

        }
        public bool Connect()
        {
            bool x = false;

            string errPrefix = FormMessage.CONNECTION_ERROR + ": ";
            try
            {
                if (this.Connection == null)
                {

                    int port = Convert.ToInt32(PortNumber);
                    this.Connection = new TCPConnection(IpAddress, port);


                    this.Log(FormMessage.CONNECTING + "... (" + FormMessage.PLEASE_WAIT + ")");
                    this.Connection.Open();

                    errPrefix = FormMessage.MATCHING_ERROR + ": ";
                    MatchExDevice();

                    SetFiscalId(FiscalId);
                    //btnConnect.Text = FormMessage.DISCONNECT;
                    this.Log(FormMessage.CONNECTED);

                    x = true;
                }
                else
                {
                    this.Connection.Close();
                    this.Connection = null;
                    //btnConnect.Text = FormMessage.CONNECT;
                    this.Log(FormMessage.DISCONNECTED);
                    x = false;
                }
            }
            catch (System.Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS + ": " + errPrefix + ex.Message);
                x = false;
                if (conn != null)
                {
                    //btnConnect.Text = FormMessage.DISCONNECT;
                }
            }
            return x;
        }

        public void SignCasher(int cashierNo, string password)
        {
            int id = cashierNo;



            try
            {
                ParseResponse(new CPResponse(this.Printer.SignInCashier(id, password)));
            }
            catch (System.Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
            }
        }

        public void CreateSale(SaleItem item)
        {
            //bool isSpecialPrice, Decimal specialPrice,bool isWeighable, Decimal quantity,bool saveAndSale,string productName,string barcode,
            int pluNo = item.PluId;
            
            Decimal price = Decimal.MinusOne;
            int weighable = -1;

            if (item.IsSpecialPrice)
                price = item.SpecialPrice;

            if (item.IsWeighable)
                weighable = 1;

            try
            {
                CPResponse response = null;
                if (item.SaveAndSale)
                {
                    response = new CPResponse(this.Printer.PrintItem(pluNo, item.Quantity, price, FixTurkishUpperCase(item.ProductName),item.Barcode, item.Dept, weighable));
                }
                else
                {
                    response = new CPResponse(this.Printer.PrintItem(pluNo, item.Quantity, price, null, null, -1, -1));
                }

                if (response.ErrorCode == 0)
                {
                    this.Log(String.Format(FormMessage.SUBTOTAL.PadRight(12, ' ') + ":{0}", response.GetNextParam()));
                }
            }
            catch (Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
            }
        }

        public void CancelSale()
        {
            try
            {
                CPResponse response = new CPResponse(this.Printer.VoidReceipt());

                if (response.ErrorCode == 0)
                {
                    this.Log(FormMessage.VOIDED_DOC_ID.PadRight(12, ' ') + ":" + response.GetNextParam());
                }
            }
            catch (System.Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
            }
        }

        public void PrintPayment(PaymentItem item)
        {
            try
            {
                int index = -1;
                CPResponse response = null;

                //PAYMENT TYPE
                int paymentType = item.PaymentIndex;

                //IF PAYMENT IS FOREIGN CURRENCY OR CREDIT
                if (item.PaymentType == FormMessage.CURRENCY || item.PaymentType == FormMessage.CREDIT)
                {
                    //Index
                    index = item.SubPaymentIndex;
                }

                //PAYMENT AMOUNT
                decimal amount = item.Amount;

                // SEND COMMAND
                response = new CPResponse(this.Printer.PrintPayment(paymentType, index, amount));

                if (response.ErrorCode == 0)
                {
                    this.Log(String.Format(FormMessage.SUBTOTAL.PadRight(12, ' ') + ":{0}", response.GetNextParam()));

                    this.Log(String.Format(FormMessage.PAID_TOTAL.PadRight(12, ' ') + ":{0}", response.GetNextParam()));
                    
                }

            }
            catch (System.Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
            }
        }

        public void CloseDocument()
        {
            try
            {
                CPResponse response = new CPResponse(this.Printer.CloseReceipt(false));

                if (response.ErrorCode == 0)
                {
                    this.Log(FormMessage.DOCUMENT_ID.PadRight(12, ' ') + ":" + response.GetNextParam());
                }
            }
            catch (Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
            }
        }
        #region Helper
        public List<string> LoadCreditData()
        {
            int successCount = 0;
            List<string> CreditData = new List<string>();
            for (int i = 0; i < ProgramConfig.MAX_CREDIT_COUNT; i++)
            {
                try
                {
                    CPResponse response = new CPResponse(this.Printer.GetCreditInfo(i));

                    if (response.ErrorCode == 0)
                    {
                        String name = response.GetNextParam();
                        CreditData.Add(name);
                         successCount++;
                    }
                }
                catch (TimeoutException)
                {
                    this.Log(FormMessage.TIMEOUT_ERROR);
                }
                catch
                {
                    this.Log(FormMessage.OPERATION_FAILS);
                }
            }
            return CreditData;
        }
    
        public List<FCurrency> GetCurrencies()
        {
            int successCount = 0;
            List<FCurrency> currency = new List<FCurrency>();
            for (int i = 0; i < ProgramConfig.MAX_FCURRENCY_COUNT; i++)
            {
                try
                {
                    CPResponse response = new CPResponse(this.Printer.GetCurrencyInfo(i));

                    if (response.ErrorCode == 0)
                    {
                        FCurrency curr = new FCurrency();
                        curr.ID = i;
                        curr.Name = response.GetNextParam();
                        curr.Rate = decimal.Parse(response.GetNextParam());
                        currency.Add(curr);
                    }
                }
                catch
                {
                }
            }
            return currency;
        }
        private void MatchExDevice()
        {
            SetFiscalId(FiscalId);


            DeviceInfo serverInfo = new DeviceInfo();
            serverInfo.IP = System.Net.IPAddress.Parse(GetIPAddress());
            serverInfo.IPProtocol = IPProtocol.IPV4;

            serverInfo.Brand = "HUGIN";



            serverInfo.Model = "HUGIN COMPACT";
            serverInfo.Port = Convert.ToInt32(PortNumber);
            serverInfo.TerminalNo = FiscalId.PadLeft(8, '0');
            serverInfo.Version = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime.ToShortDateString();
            serverInfo.SerialNum = CreateMD5(GetMBId()).Substring(0, 8);


            if (conn.IsOpen)
            {
                if (isMatchedBefore)
                {
                    printer.SetCommObject(conn.ToObject());
                    return;
                }
                try
                {
                    printer = new CompactPrinter();
                    printer.FiscalRegisterNo = fiscalId;



                    if (!printer.Connect(conn.ToObject(), serverInfo))
                    // Authorozition with licence key
                    //if (!printer.Connect(conn.ToObject(), serverInfo, System.Configuration.ConfigurationSettings.AppSettings["LicenseKey"]))
                    {
                        throw new OperationCanceledException(FormMessage.UNABLE_TO_MATCH);
                    }

                    // Check supported printer size and set if it is different
                    if (printer.PrinterBufferSize != conn.BufferSize)
                    {
                        conn.BufferSize = printer.PrinterBufferSize;
                    }
                    printer.SetCommObject(conn.ToObject());
                    isMatchedBefore = true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                CPResponse.Bridge = this;

            }

        }
        private string GetIPAddress()
        {
            System.Net.IPHostEntry host;
            string localIP = "?";
            host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (System.Net.IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
        private string GetMBId()
        {
            System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            System.Management.ManagementObjectCollection moc = mos.Get();
            string motherBoard = "";
            foreach (System.Management.ManagementObject mo in moc)
            {
                motherBoard = (string)mo["SerialNumber"];
            }

            return motherBoard;
        }
        public static void SetFiscalId(string strId)
        {
            int id = int.Parse(strId.Substring(2));

            if (id == 0 || id > 99999999)
            {
                throw new Exception("Geçersiz mali numara.");
            }
            fiscalId = strId;

            if (printer != null)
                printer.FiscalRegisterNo = fiscalId;
        }
        private void ParseResponse(CPResponse response)
        {
            try
            {
                if (response.ErrorCode == 0)
                {
                    string retVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(retVal))
                    {
                        this.Log(String.Format(FormMessage.DATE.PadRight(12, ' ') + ":{0}", retVal));
                    }

                    retVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(retVal))
                    {
                        this.Log(String.Format(FormMessage.TIME.PadRight(12, ' ') + ":{0}", retVal));
                    }
                    retVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(retVal))
                    {
                        this.Log(String.Format("NOTE".PadRight(12, ' ') + ":{0}", retVal));
                    }
                    retVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(retVal))
                    {
                        this.Log(String.Format(FormMessage.AMOUNT.PadRight(12, ' ') + ":{0}", retVal));
                    }
                    retVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(retVal))
                    {
                        this.Log(FormMessage.DOCUMENT_ID.PadRight(12, ' ') + ":" + retVal);
                    }

                    retVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(retVal))
                    {
                    }

                    retVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(retVal))
                    {
                        String authNote = "";
                        try
                        {
                            switch (int.Parse(retVal))
                            {
                                case 0:
                                    authNote = FormMessage.SALE;
                                    break;
                                case 1:
                                    authNote = "PROGRAM";
                                    break;
                                case 2:
                                    authNote = FormMessage.SALE + " & Z";
                                    break;
                                case 3:
                                    authNote = FormMessage.ALL;
                                    break;
                                default:
                                    authNote = "";
                                    break;
                            }

                            this.Log(FormMessage.AUTHORIZATION.PadRight(12, ' ') + ":" + authNote);
                        }
                        catch { }
                    }
                }

            }
            catch (System.Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS);
            }
        }
        private string FixTurkishUpperCase(string text)
        {
            // stack current culture
            System.Globalization.CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;

            // Set Turkey culture
            System.Globalization.CultureInfo turkey = new System.Globalization.CultureInfo("tr-TR");
            System.Threading.Thread.CurrentThread.CurrentCulture = turkey;

            string cultured = text.ToUpper();

            // Pop old culture
            System.Threading.Thread.CurrentThread.CurrentCulture = currentCulture;

            return cultured;
        }
        #endregion
        public void Log()
        {
            StringBuilder builder = new StringBuilder();


            // 1 Command
            // 2 Sequnce Number
            // 3 FPU State
            // 4 Error Code
            // 5 Error Message

            if (printer != null)
            {
                string lastlog = printer.GetLastLog();

                //txtLog.SelectionColor = Color.CornflowerBlue;
                builder.AppendLine("***************************************************");

                if (!String.IsNullOrEmpty(lastlog))
                {
                    if (!lastlog.Contains("|"))
                    {
                        Log(lastlog);
                        return;
                    }

                    string[] parsedLog = lastlog.Split('|');

                    if (parsedLog.Length == 5)
                    {

                        string command = parsedLog[0];
                        string sequnce = parsedLog[1];
                        string state = parsedLog[2];
                        string errorCode = parsedLog[3];
                        string errorMsg = parsedLog[4];

                        if (command != "NULL")
                        {

                            if (sequnce.Length == 1)
                                builder.Append(String.Format("{0} {1}:", sequnce, FormMessage.COMMAND.PadRight(12, ' ')));
                            else if (sequnce.Length == 2)
                                builder.Append(String.Format("{0} {1}:", sequnce, FormMessage.COMMAND.PadRight(11, ' ')));
                            else
                                builder.Append(String.Format("{0} {1}:", sequnce, FormMessage.COMMAND.PadRight(10, ' ')));



                            builder.AppendLine(command  );


                            builder.Append("  " + FormMessage.FPU_STATE.PadRight(12, ' ') + ":");

                            builder.AppendLine(state  );
                        }

                        builder.Append("  " + FormMessage.RESPONSE.PadRight(12, ' ') + ":");





                        builder.Append(errorMsg );
                        Log(builder.ToString());
                    }

                }
            }
        }

        public void Disconnect()
        {
            string errPrefix = FormMessage.CONNECTION_ERROR + ": ";
            try
            {
                if (this.Connection != null)
                {
                    this.Connection.Close();
                    this.Connection = null;
                    //btnConnect.Text = FormMessage.CONNECT;
                    this.Log(FormMessage.DISCONNECTED);
                }
            }
            catch (System.Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS + ": " + errPrefix + ex.Message);

                if (conn != null)
                {
                    //btnConnect.Text = FormMessage.DISCONNECT;
                }
            }
        }
        public void PrintLastReceipt()
        {
            lastCmd = FiscalCmd.LAST_RECEIPT_INFO;
            try
            {
                CPResponse response = new CPResponse(this.Printer.GetLastDocumentInfo(false));
                SendCommand(response);
            }
            catch
            {
                this.Log(FormMessage.OPERATION_FAILS);
            }
        }

        public string LastZInfo()
        {
            string zInfo = "";
            lastCmd = FiscalCmd.LAST_Z_INFO;
            try
            {
                var response = new CPResponse(this.Printer.GetLastDocumentInfo(true));
                SendCommand(response);
                if (response.ParamList.Count>0)
                {
                    zInfo = response.ParamList[1];
                }
            }
            catch
            {
                this.Log(FormMessage.OPERATION_FAILS);
            }
            return zInfo;
        }

        public void PrintReceipt(int z,int d)
        {
            try
            {
                CPResponse response = new CPResponse(this.Printer.PrintEJPeriodic(z,d,z,d,3));
                
            }
            catch (Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS);
            }
        }
        private void SendCommand(CPResponse response)
        {
            try
            {
                if (response.ErrorCode == 0 && response.ParamList != null)
                {
                    string paramVal = "";

                    if (lastCmd != FiscalCmd.DRAWER_INFO)
                    {
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(FormMessage.DOCUMENT_ID.PadLeft(12, ' ') + ": " + paramVal);
                        }
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(FormMessage.Z_ID.PadLeft(12, ' ') + ": " + paramVal);
                        }
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(FormMessage.EJ_ID.PadLeft(12, ' ') + ": " + paramVal);
                        }
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(FormMessage.DOCUMENT_TYPE.PadLeft(12, ' ') + ": " + paramVal);
                        }
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(String.Format(FormMessage.DATE.PadLeft(12, ' ') + ": {0}", paramVal));
                        }

                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(String.Format(FormMessage.TIME.PadLeft(12, ' ') + ": {0}", paramVal));
                        }
                    }
                    // TOPLAM BİLGİLERİ                   
                    paramVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(paramVal))
                    {
                        this.Log("--- " + FormMessage.TOTAL_INFO + " ---");
                        this.Log(String.Format(FormMessage.TOTAL_RECEIPT_COUNT + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(paramVal))
                    {
                        this.Log(String.Format(FormMessage.TOTAL_AMOUNT + ": {0}", paramVal));
                    }

                    // SATIŞ BİLGİLERİ                          
                    paramVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(paramVal))
                    {
                        this.Log("--- " + FormMessage.SALE_INFO + " ---");
                        this.Log(String.Format(FormMessage.TOTAL_SALE_RECEIPT_COUNT + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(paramVal))
                    {
                        this.Log(String.Format(FormMessage.TOTAL_SALE_AMOUNT + ": {0}", paramVal));
                    }

                    // İPTAL BİLGİLERİ                   
                    paramVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(paramVal))
                    {
                        this.Log("--- " + FormMessage.VOID_INFO + " ---");
                        this.Log(String.Format(FormMessage.TOTAL_VOID_RECEIPT_COUNT + ": {0}", paramVal));
                    }
                    paramVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(paramVal))
                    {
                        this.Log(String.Format(FormMessage.TOTAL_VOID_AMOUNT + ": {0}", paramVal));
                    }

                    // İNDİRİM BİLGİLERİ                 
                    paramVal = response.GetNextParam();
                    if (!String.IsNullOrEmpty(paramVal))
                    {
                        this.Log("--- " + FormMessage.ADJUSTMENT_INFO + " ---");
                        this.Log(String.Format(FormMessage.TOTAL_ADJUSTED_AMOUNT + ": {0}", paramVal));
                    }

                    // ÖDEME BİLGİLERİ 
                    this.Log("--- " + FormMessage.PAYMENT_INFO + " ---");
                    int i = 0;
                    while (response.CurrentParamIndex < response.ParamList.Count)
                    {
                        i++;
                        this.Log("** " + FormMessage.PAYMENT + " " + i + " **");
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            int paymentType = int.Parse(paramVal);
                            this.Log(String.Format(FormMessage.PAYMENT_TYPE.PadLeft(15, ' ') + ": {0}", Common.Payments[paymentType]));
                        }
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(String.Format(FormMessage.PAYMENT_INDEX.PadLeft(15, ' ') + ": {0}", paramVal));
                        }
                        paramVal = response.GetNextParam();
                        if (!String.IsNullOrEmpty(paramVal))
                        {
                            this.Log(String.Format(FormMessage.PAYMENT_AMOUNT.PadLeft(15, ' ') + ": {0}", paramVal));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                this.Log(FormMessage.OPERATION_FAILS);
            }
        }
    }
}
