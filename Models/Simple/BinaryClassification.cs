﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BrightWire.Models.Simple
{
    public class BinaryTableClassification
    {
        public string Classification { get; private set; }
        public IDataTable Table { get; private set; }

        public BinaryTableClassification(string classification, IDataTable dataTable)
        {
            Classification = classification;
            Table = dataTable;
        }
    }
}
