﻿using System.Data;

namespace DeliveryOrderAPI
{
    public class Helper
    {
        public decimal ConvDBToDec(double val)
        {
            try
            {
                return Convert.ToDecimal(val);
            }
            catch
            {
                return 0;
            }
        }

        public int ConvDBToInt(double val)
        {
            try
            {
                return Convert.ToInt32(val);
            }
            catch
            {
                return 0;
            }
        }
        public double ConvDecToDB(decimal? val)
        {
            try
            {
                return Convert.ToDouble(val);
            }
            catch
            {
                return 0;
            }
        }

        public int ConvIntEmptyToInt(int? val)
        {
            try
            {
                return Convert.ToInt32(val);
            }
            catch
            {
                return 0;
            }
        }
        public double ConvDBEmptyToDB(double? val)
        {
            try
            {
                return Convert.ToDouble(val);
            }
            catch
            {
                return 0;
            }
        }

        public double ConvStrToDB(string val)
        {
            try
            {
                return Convert.ToDouble(val);
            }
            catch
            {
                return 0;
            }
        }

        public decimal ConvStrToDec(string val)
        {
            try
            {
                return Convert.ToDecimal(val);
            }
            catch
            {
                return 0;
            }
        }

        public decimal ConvIntToDec(int val)
        {
            try
            {
                return Convert.ToDecimal(val);
            }
            catch
            {
                return 0;
            }
        }


        public bool HasColumn(DataTable dt, string columnName)
        {
            try
            {
                return dt.Columns.Contains(columnName);
            }
            catch
            {
                return false;
            }
        }
    }
}