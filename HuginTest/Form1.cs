using HuginTest.Model;
using HuginTest.Service;
using System;
using System.Linq;
using System.Windows.Forms;

namespace HuginTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        CashPrinterService service;

        private delegate void LogDelegate2();
        private delegate void LogDelegate(String log);



        private void button1_Click(object sender, EventArgs e)
        {
            service = new CashPrinterService(txtTCPIP.Text, txtTcpPort.Text, txtFiscalId.Text);
            service.LogPath = @"G:\OS\Server\Sedna Hotel";
            service.LogChanged += Service_LogChanged;

            if (service.Connection==null)
            {
                service.Connect();
                btnConnect.Text = "Disconnect";
            }
            else
            {
                service.Disconnect();
                btnConnect.Text = "Connect";
            }
            
            



        }

        private void Service_LogChanged(string message)
        {
            //throw new NotImplementedException();
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new LogDelegate(Service_LogChanged), message);
            }
            else
            {
                txtLog.AppendText("# " + message + "\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }

        private void btnSignInCashier_Click(object sender, EventArgs e)
        {
            service.SignCasher((int)nmrCashierNo.Value, txtPassword.Text);
            #region Old
            //int id = (int)nmrCashierNo.Value;

            //// Password
            //string password = txtPassword.Text;

            //try
            //{
            //    ParseResponse(new CPResponse(this.Printer.SignInCashier(id, password)));
            //}
            //catch (System.Exception ex)
            //{
            //    this.Log(FormMessage.OPERATION_FAILS + ": " + ex.Message);
            //} 
            #endregion
        }

        private void cbxSaveAndSale_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxSaveAndSale.Checked)
            {
                Service_LogChanged("SAVE_AND_SALE_INFO");
            }

            pnlExtraPlufields.Enabled = cbxSaveAndSale.Checked;
        }

        private void btnSale_Click(object sender, EventArgs e)
        {
           


                SaleItem item = new SaleItem
                {
                    PluId = (int)nmrPlu.Value,
                    Barcode = txtBarcode.Text ,
                    Dept = (int)nmrDept.Value,
                    IsSpecialPrice = checkBoxForPrice.Checked,
                    IsWeighable = checkBoxWeighable.Checked,
                    ProductName = txtName.Text ,
                    Quantity = nmrQuantity.Value,
                    SaveAndSale = cbxSaveAndSale.Checked,
                    SpecialPrice = nmrPrice.Value
                };


                service.CreateSale(item);
            

        }

        private void checkBoxForPrice_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBoxForPrice.Checked)
            {
                nmrPrice.Enabled = true;
                lblAmount.Enabled = true;
            }
            else
            {
                nmrPrice.Enabled = false;
                lblAmount.Enabled = false;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            service.CancelSale();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cbxPaymentType.Items.AddRange(Common.Payments);
            cbxPaymentType.SelectedIndex = 0;

            
        }

        private void RefreshCurrencyList()
        {
            cbxSubPayments.Items.Clear();
            //lblPayIndx.Text = FormMessage.CURRENCIES;

            var currencies = service.GetCurrencies();
            foreach (var item in currencies)
            {
                cbxSubPayments.Items.Add(String.Format("{0}  {1}", item.Name, item.Rate));
            }

            cbxSubPayments.SelectedIndex = 0;
        }
        public int RefreshCredits()
        {
            int creditCount = 0;
            var CreditData = service.LoadCreditData();
            if (CreditData.Any())
            {
                creditCount = CreditData.Count;
                cbxSubPayments.Items.Clear();
                //lblPayIndx.Text = FormMessage.CREDITS;
                for (int i = 0; i < creditCount; i++)
                {
                    if (!String.IsNullOrEmpty(CreditData[i]))
                    {
                        cbxSubPayments.Items.Add(CreditData[i]);
                    }
                }
                cbxSubPayments.SelectedIndex = 0;



            }


            return creditCount;
        }
        private void cbxPaymentType_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRefreshCredit.Visible = false;
            //lblFCurrValue.Visible = false;

            switch (cbxPaymentType.Text)
            {
                case FormMessage.CURRENCY:
                    RefreshCurrencyList();
                    //lblPayIndx.Visible = true;
                    cbxSubPayments.Visible = true;
                    //lblFCurrValue.Visible = true;
                    break;
                case FormMessage.CREDIT:
                    int creditCount = 0;
                    btnRefreshCredit.Visible = true;
                    creditCount = RefreshCredits();
                    cbxSubPayments.Visible = true;
                    break;
                default:
                    //lblPayIndx.Visible = false;
                    cbxSubPayments.Visible = false;
                    break;
            }
        }

        private void btnPayment_Click(object sender, EventArgs e)
        {
            PaymentItem item = new PaymentItem
            {
                PaymentIndex = cbxPaymentType.SelectedIndex,
                PaymentType = cbxPaymentType.Text,
                Amount = nmrPaymentAmount.Value,
                SubPaymentIndex = cbxSubPayments.SelectedIndex
            };
            service.PrintPayment(item);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            service.CloseDocument();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            service.PrintLastReceipt();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int lastZ = Convert.ToInt32(service.LastZInfo()) + 1;
            int document = (int)numericUpDown1.Value;
            service.PrintReceipt(lastZ, document);
        }

        private void btnLastZInfo_Click(object sender, EventArgs e)
        {
            service.LastZInfo();
        }
    }
}
