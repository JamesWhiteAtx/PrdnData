using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CST.Prdn.Data;
using System.Data.Objects;

namespace WhatTheFuck
{
    class Program
    {
        static void Main(string[] args)
        {
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                decimal d = 12.3M;
                int i = d.ToInt();
                var jobQry = from j in PrdnDBContext.ProductionJobs select j;

                ObjectQuery oq1 = (ObjectQuery)jobQry;
                string s = oq1.ToTraceString();
                Console.WriteLine(s);

                foreach (var j in jobQry)
                {
                    Console.WriteLine(j.ID);
                }

                Console.ReadLine();
            }

            
        }
    }
}
