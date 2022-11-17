using Hugin.POS.CompactPrinter.FP300;
using HuginTest.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HuginTest
{
    public interface IBridge
    {
#if CPP
        void Log(String log);
        void Log();
        PrinterWrapper.CompactPrinterWrapper Printer { get; }
        BridgeConn Connection { get; set; }
#else
        IConnection Connection { get; }
        void Log(String log);
        void Log();
        ICompactPrinter Printer { get; }
#endif
    }
}
