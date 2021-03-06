﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lead.Detect.DatabaseHelper
{

    public static class Program
    {

        [STAThread]
        public static void Main()
        {
            SqlLiteHelper.InitDatabase();
            SqlLiteHelper.DB.Insert(new ProductDataEntity() {StartTime = "0001"});
            Console.WriteLine("finish");

            foreach (var productTestDataEntity in SqlLiteHelper.DB.From<ProductDataEntity>().ToList())
            {
                Console.WriteLine(productTestDataEntity.StartTime.ToString());
            }
            Console.ReadKey();
        }
    }
}
