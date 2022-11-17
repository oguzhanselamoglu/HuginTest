using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HuginTest.Service
{
    public interface IConnection
    {
        void Open();
        bool IsOpen { get; }
        void Close();
        int FPUTimeout { get; set; }
        object ToObject();
        int BufferSize { get; set; }
    }

    enum ContentType
    {
        NONE,
        REPORT,
        FILE
    }
}
