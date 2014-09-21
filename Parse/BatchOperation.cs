using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse
{
    public class BSuccess
    {
        public string createdAt { get; set; }
        public string objectId { get; set; }
        public string updatedAt { get; set; }
    }

    public class BError
    {
        public string code { get; set; }
        public string error { get; set; }
    }

    public class BatchOperation
    {
        public BSuccess success { get; set; }
        public BError error { get; set; }
        public string operation { get; set; }
    }
}
