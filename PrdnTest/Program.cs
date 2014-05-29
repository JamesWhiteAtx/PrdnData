﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Objects;
using CST.Prdn.Data;
using System.Data.SqlClient;

namespace PrdnTest
{
    public class PrdnJobStatusViewModel
    {
        public static string[] UpperStatuses(string statusStr)
        {
            return statusStr.ToUpper().Split(new Char[] { ',' });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            JobList();
        }

        private static void JobList()
        {

            decimal? runID = 84;
            string statusStr = "Pending,Scheduled,Processing,Completed";
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var jobs = from j in PrdnDBContext.ProductionJobs
                            //.Include("Customer")
                            //.Include("Product.LeatherCharVW")
                            //.Include("Product.LeatherCompVW")
                            //.Include("Worksheet.WorksheetCharVW")
                            //.Include("Worksheet.WorksheetCompVW")
                            //.Include("Priority")
                            //.Include("PrdnInvItem")
                        where j.RunID == runID
                        //orderby j.RunSeqNo
                        select j;

                if (!String.IsNullOrWhiteSpace(statusStr))
                {
                    List<string> stati = PrdnJobStatusViewModel.UpperStatuses(statusStr).ToList();
                    jobs = from j in jobs
                           where stati.Contains(j.StatusStr)
                           select j;
                }

                jobs = from j in jobs
                       orderby j.RunSeqNo
                       select j;

                ObjectQuery oq1 = (ObjectQuery)jobs;
                string str = oq1.ToTraceString();
                Console.WriteLine(str);

                int c = jobs.Count();

                Console.WriteLine(c.ToString());
                Console.ReadLine();

            }
        }

        private static void JobTot()
        {
            var viewJob = new
            {
                ID = (decimal?)null,
                OrderNo = "2030034",
                //OrderLineID = (decimal?)3283079,
                OrderLine = (decimal?)1,
                OrderLineInt = (int?)1,
                ProdCD = "604519",
                CstRequestID = "277674"
            };

            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var lnQry = from l in PrdnDBContext.CstOrderLines
                            where l.OrderLine == viewJob.OrderLine
                            select new
                            {
                                l.OrderNo,
                                l.OrderLine,
                                l.OrderLineID,
                                l.ProdCD
                            };

                var query = from o in PrdnDBContext.CstOrders
                            where o.OrderNo == viewJob.OrderNo
                            join l in lnQry on o.OrderNo equals l.OrderNo into temp
                            from ln in temp.DefaultIfEmpty()
                            select new
                            {
                                o.OrderNo,
                                OrderTot = o.OrderLines.Sum(l => l.LineTotal) + (o.Tax ?? 0) + (o.Transport ?? 0) + (o.Shipping ?? 0),
                                line = ln
                            };

                var ordMatch = (query).FirstOrDefault();


                ObjectQuery oq1 = (ObjectQuery)query;
                string str = oq1.ToTraceString();
                Console.WriteLine(str);

                //Console.WriteLine(order.Tot.ToString());
                //Cnsole.ReadLine();
            }

        }


        static void ValOrd()
        { 
            var viewJob = new {
                ID = (decimal?)null,
                OrderNo = "2030034",
                //OrderLineID = (decimal?)3283079,
                OrderLine = (decimal?)1, 
                OrderLineInt = (int?)1, 
                ProdCD = "604519"
            };
            

            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {

                //var ordQry = from o in PrdnDBContext.CstOrders
                             //where o.OrderNo == viewJob.OrderNo
                             //select new { o.OrderNo };

                var lnQry = from l in PrdnDBContext.CstOrderLines
                            where l.OrderLine == viewJob.OrderLine
                            select new
                            {
                                l.OrderNo,
                                l.OrderLine,
                                l.OrderLineID,
                                l.ProdCD
                            };

                var query = from o in PrdnDBContext.CstOrders
                            where o.OrderNo == viewJob.OrderNo
                                join l in lnQry on o.OrderNo equals l.OrderNo into temp
                                from ln in temp.DefaultIfEmpty()
                                select new
                                {
                                    o.OrderNo,
                                    line = ln
                                };

                var ordMatch = (query).FirstOrDefault();

                ObjectQuery oq1 = (ObjectQuery)query;
                string str = oq1.ToTraceString();
                Console.WriteLine(str);


                if (ordMatch == null)
                {
                    Console.WriteLine("order no bad");
                }
                else if (ordMatch.line == null)
                {
                    Console.WriteLine("line bad");
                }
                else if (ordMatch.line.ProdCD != viewJob.ProdCD)
                {
                    Console.WriteLine("prod bad");
                }

                else
                {
                    Console.WriteLine("right oh");
                }
                Console.ReadLine();

            }
        }

        static bool OrdJob()
        {

            var viewJob = new {
                ID = (decimal?)null,
                OrderNo = "2030034",
                OrderLineID = (decimal?)3283079,
                OrderLine = (decimal?)1,
                OrderLineInt = (int?)1, 
                ProdCD = "604519",
                CstRequestID = "277674"
            };

            string dbCancStr = PrdnJobStatus.Canceled.DbValStr();

            bool isValid = true;

            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                if (!String.IsNullOrEmpty(viewJob.CstRequestID))
                {
                    var jobReqQry = from j in PrdnDBContext.ProductionJobs
                                 where j.CstRequestID == viewJob.CstRequestID
                                 && j.StatusStr != dbCancStr
                                 select j;

                    if (viewJob.ID != null)
                    {
                        jobReqQry = from j in jobReqQry
                                 where j.ID != viewJob.ID
                                 select j;
                    }

                    var otherReqJob = (from j in jobReqQry
                                        select new
                                        {
                                            j.ID,
                                            j.SerialNo,
                                            j.Run.PrdnOrderNo,
                                            j.Run.PrdnType.Code,
                                            j.StatusStr,
                                        }).FirstOrDefault();

                    if (otherReqJob != null)
                    {
                        isValid = false;
                    }

                }

                if (isValid && !String.IsNullOrEmpty(viewJob.OrderNo) && (viewJob.OrderLine != null) && !String.IsNullOrEmpty(viewJob.ProdCD))
                {
                    var jobOrdQry = from j in PrdnDBContext.ProductionJobs
                                   where j.OrderNo == viewJob.OrderNo
                                   && j.OrderLine == viewJob.OrderLine
                                   && j.ProdCD == viewJob.ProdCD
                                   && j.StatusStr != dbCancStr
                                   select j;

                    if (viewJob.ID != null)
                    {
                        jobOrdQry = from j in jobOrdQry
                                    where j.ID != viewJob.ID
                                    select j;
                    }

                    var otherOrdJob = (from j in jobOrdQry
                                       select new
                                       {
                                           j.ID,
                                           j.SerialNo,
                                           j.Run.PrdnOrderNo,
                                           j.Run.PrdnType.Code,
                                           j.StatusStr,
                                       }).FirstOrDefault();

                    if (otherOrdJob != null)
                    {
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        static void POs()
        {
            int year = 2012;
            int month = 12;

            DateTime fdom = new DateTime(year: year, month: month, day: 1);
            DateTime fdoNxtM = fdom.AddMonths(1);

            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var items = from p in PrdnDBContext.ProductionOrders
                            where p.ShipDay >= fdom && p.ShipDay < fdoNxtM
                            select p;

                ObjectQuery oq1 = (ObjectQuery)items;
                string str = oq1.ToTraceString();
                Console.WriteLine(str);
            }
        }
        static void ItemOpts()
        { 
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var items = from i in PrdnDBContext.PrdnInvItems.Include("Product")
                            where i.SerialNo == "2001252"
                            select new
                            {
                                i.InvItemID,
                                i.SerialNo,
                                i.Product.ProdCD,
                                i.Product.ParentProdCD,
                                i.Product.Description
                            };

                ObjectQuery oq1 = (ObjectQuery)items;
                string str = oq1.ToTraceString();
                Console.WriteLine(str);
            }
        }

        static void Scans()
        {
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                DateTime midnight = DateTime.Today.AddDays(1);

                var scans = from s in PrdnDBContext.ProductionScans.Include("Users")
                            join j in PrdnDBContext.ProductionJobs on s.Value equals j.SerialNo into scanJobs
                            from job in scanJobs.DefaultIfEmpty()
                            join i in PrdnDBContext.PrdnInvItems on s.Value equals i.SerialNo into scanItems
                            from item in scanItems.DefaultIfEmpty()
                            orderby s.ScanDt descending
                            select new
                            {
                                s.ID,
                                s.ScanDt,
                                s.Value,
                                s.Message,
                                UserID = s.User.ID,
                                s.User.Login,
                                JobID = job == null ? 0 : job.ID,
                                JobStatus = job == null ? "" : job.StatusStr,
                                ItemID = item == null ? "" : item.InvItemID
                            };

                scans = from s in scans
                        where s.ScanDt < midnight
                        select s;

                ObjectQuery oq1 = (ObjectQuery)scans;
                string str = oq1.ToTraceString();
                Console.WriteLine(str);

                foreach (var s in scans)
                {
                    Console.WriteLine(s.ID + " '" + s.Value + "' " + s.ScanDt + " " + s.Login + " " + s.JobID + " " + s.JobStatus + " " + s.Message);
                }

                Console.ReadLine();
            }
        }

        static void JonInv()
        {
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var jobs = from j in PrdnDBContext.ProductionJobs.Include("PrdnInvItem")
                           //join itm in PrdnDBContext.PrdnInvItemVW
                           //on j.SerialNo equals itm.SerialNo
                           //into jobItm
                           where j.ID == 73 || j.ID == 13
                           select j;

                ObjectQuery oq1 = (ObjectQuery)jobs;
                string s = oq1.ToTraceString();
                Console.WriteLine(s);

                foreach (var j in jobs)
                {
                    Console.WriteLine(j.ID + " " + j.SerialNo + " " + j.IsNotNull(x => x.PrdnInvItem) );
                }

                Console.ReadLine();
            }
        }

        static void PrdnProdTypes()
        {
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var types = (from type in PrdnDBContext.ProductionTypes
                            where (type.ActiveFlag == PrdnDataHelper.BoolYNTue)
                            orderby type.ProdTypeCD
                            select new
                            {
                                Code = type.ProductType.ProdTypeCD,
                                Name = type.ProductType.Description
                            }).Distinct(); 

                ObjectQuery oq1 = (ObjectQuery)types;
                string s = oq1.ToTraceString();
                Console.WriteLine(s);

                foreach (var item in types)
                {
                    Console.WriteLine(item.Code);
                }

                Console.ReadLine();
            }
        }

        public class PrdnRunViewModel
        {
            public decimal ID { get; set; }
            public string PrdnOrderNo { get; set; }
            public decimal PrdnTypeID { get; set; }
            public string Description { get; set; }
            public bool HasJobs { get; set; }
            public string ProdTypeCD { get; set; }
            public string ProdTypeDescr { get; set; }
        }


        static void PrdnOrdRunList()
        {
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var runs = from r in PrdnDBContext.ProductionRuns
                           orderby r.PrdnOrderNo, r.PrdnType.SortOrder, r.PrdnType.Code
                           select new PrdnRunViewModel
                           {
                               ID = r.ID,
                               PrdnOrderNo = r.PrdnOrderNo,
                               PrdnTypeID = r.PrdnTypeID,
                               Description = r.Description,
                               HasJobs = r.Jobs.Any(),
                               ProdTypeCD = r.PrdnType.ProdTypeCD,
                               ProdTypeDescr = "(" + r.PrdnType.ProdTypeCD + ") " + r.PrdnType.ProductType.Description
                           };

                runs = from r in runs
                           where r.ID == 10
                           select r;

                ObjectQuery oq1 = (ObjectQuery)runs;
                string s = oq1.ToTraceString();
                Console.WriteLine(s);

                Console.ReadLine();

            }

        }

        static void PrdnOrdRunLookup()
        {
            string prdnOrderNo = "512";
            string typeCd = "A"; 
            string prodTypeCD = "300";
            //bool inclucdePrdnOrNos = true;

            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var runs = from r in PrdnDBContext.ProductionRuns
                            .Include("PrdnOrder")
                            .Include("PrdnType")
                             where r.PrdnOrder.OrderNo.StartsWith(prdnOrderNo)
                             select r;

                if (typeCd != null)
                {
                    runs = from r in runs
                           where r.PrdnType.Code.StartsWith(typeCd)
                           select r;
                }

                if (prodTypeCD != null)
                {
                    runs = from r in runs
                           where r.PrdnType.ProdTypeCD == prodTypeCD
                           select r;
                }

                runs = from r in runs
                       orderby r.PrdnOrder.OrderNo
                       select r;
                
                var list = from r in runs.ToList()
                           select new
                           {
                               r.PrdnOrder.OrderNo,
                               r.PrdnOrder.ShipDay,
                               runID = r.ID.ToString(),
                               prdType = r.PrdnType.Code,
                               PrdnTypeDescr = r.PrdnType.Description
                           };


                foreach (var l in list)
                {
                    Console.WriteLine(l.OrderNo
                        +" "+ l.ShipDay.ToString()
                        +" "+ l.runID
                        +" "+ l.prdType
                        +" "+ l.PrdnTypeDescr
                        );
                }

                Console.ReadLine();
            }
        }

        static void NullReqAtt()
        {
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var att = from a in PrdnDBContext.RequestAttachments
                          where a.ID == "277712" && a.Attachment != null
                          select new
                          {
                              a.ID,
                              a.FileName,
                              MimeContentType = a.MimeType.ContentType,
                              MimeSubType = a.MimeTypeCD,
                              a.MimeType,
                          };

                ObjectQuery oq1 = (ObjectQuery)att;
                string s = oq1.ToTraceString();
                Console.WriteLine(s);

                Console.ReadLine();

            }
        }

        static void roles()
        {
            //int userID = 1;
            using (PrdnEntities PrdnDBContext = new PrdnEntities())
            {
                var groups = from u in PrdnDBContext.Users
                             from g in u.Groups
                             where u.ID == 1
                             orderby g.App.Code, g.Code
                             select g;
                
                foreach (var group in groups)
                {
                    Console.WriteLine(group.AppGroupCode);
                }

                Console.ReadLine();
            }
        }
               
        static void groupNotUser()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();

            int userID = 1;

            var userGroups = from u in PrdnDBContext.Users
                             from g in u.Groups
                             where u.ID == userID
                             select g;

            var exclGroups = from g in PrdnDBContext.Groups
                          where !userGroups.Any(ug => ug.ID == g.ID)
                          select g;

            var srtGrps = from g in exclGroups
                          orderby g.Code
                          select g;

            var exclApps = from a in PrdnDBContext.Apps
                         where exclGroups.Any(gq => gq.AppID == a.ID)
                         select a;

            var srtApps = from a in exclApps
                          orderby a.Code
                          select a;

            ObjectQuery oq1 = (ObjectQuery)exclApps;
            string s = oq1.ToTraceString();
            Console.WriteLine(s);

            Console.ReadLine();
        }

        static void Encrypt()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();

            string crypt = PrdnDBContext.EncryptedStr("PASS");

            Console.WriteLine(crypt);

            string plain = PrdnDBContext.DecryptedStr(crypt);


            Console.WriteLine(plain);
            Console.ReadLine();
        }

        private static void uerGroups()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();


            var groupUsers = from g in PrdnDBContext.Groups
                        from u in g.Users
                        where g.ID == 1
                        select u;

            //groupUsers.Any(gu => gu.ID == 22);

            var users = from u in PrdnDBContext.Users
                        where !groupUsers.Any(gu => gu.ID == u.ID)
                        select u;

            ObjectQuery oq1 = (ObjectQuery)users;
            string s = oq1.ToTraceString();
            Console.WriteLine(s);

            foreach (var u in users)
            {
                Console.WriteLine(u.ID);
            }


            Console.ReadLine();
        }

        public static void ParmInList(PrdnEntities prdnDBContext, out string inClause, out Object[] dbParms, params Object[] values)
        {
            dbParms = new Object[values.Length];
            List<string> parmNames = new List<string>();

            using (var cmdTemp = prdnDBContext.Connection.CreateCommand())
            {
                for (int i = 0; i < values.Length; i++)
                {
                    var parmName = "p" + i.ToString();
                    parmNames.Add(parmName);
                    //parameters[i] = new Oracle.DataAccess.Client.OracleParameter { ParameterName = parmName, Value = values[i] };
                    var dbParam = cmdTemp.CreateParameter();
                    dbParam.ParameterName = parmName;
                    dbParam.Value = values[i];
                    dbParms[i] = dbParam;
                }
            }
            inClause = string.Join(",", parmNames);
        }

        public static string ParmFormatList(int parms)
        { 
            List<string> parmNames = new List<string>();
            for (int i = 0; i < parms; i++)
            {
                parmNames.Add(":p" + i.ToString());
            }
            return string.Join(",", parmNames) ; 
        }

        private static void parama()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();
            
            //string delIDS = "999, 998, 997";

            //string sql = String.Format("DELETE FROM FG_PRDN_JOB_ATTACHMENT WHERE FG_PRDN_JOB_ATT_ID IN ({0})", delIDS);
            //PrdnDBContext.ExecuteStoreCommand(sql);

            //sql = "DELETE FROM jpw WHERE coli IN (:p0, :p1)";
            
            //Object[] parms = {
            //    new Oracle.DataAccess.Client.OracleParameter { ParameterName = ":p0", Value = 1 },
            //    new Oracle.DataAccess.Client.OracleParameter { ParameterName = ":p0", Value = 2 }
            //};
            //PrdnDBContext.ExecuteStoreCommand(sql, parms);

            //string inClause;
            //Object[] parameters;
            
            //ParmInList(PrdnDBContext, out inClause, out parameters, values);
            //string sql = "DELETE FROM jpw WHERE coli IN (" + inClause + ")";
            //PrdnDBContext.ExecuteStoreCommand(sql, parameters);

            //List<int> values = new List<int> { 4, 5, 6 };
            //string sql = "DELETE FROM jpw WHERE coli IN (" + ParmFormatList(values.Count()) + ")";
            //PrdnDBContext.ExecuteStoreCommand(sql, values.Cast<object>().ToArray());

            decimal id = 99999M;
            PrdnDBContext.ExecuteStoreCommand("DELETE FROM FG_PRDN_JOB_ATTACHMENT WHERE FG_PRDN_JOB_ID = :p0", id);
        }

        //PrdnDBContext.ExecuteStoreCommand(sql);
        //var argsDeleteWebUserXref1 = new DbParameter[] {         new SqlParameter { ParameterName = "WebUserId", Value = "" }  ;
        //PrdnDBContext.ExecuteStoreCommand(sql, argsDeleteWebUserXref1);


        private static void ProdImg()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();
      
            DateTime now = DateTime.Now;
            var prods = from p in PrdnDBContext.Products.Include("ProdImages")
                    where p.ProdTypeCD == "620" //&& (p.DiscontinueDt == null || p.DiscontinueDt > now)
                    orderby p.ProdCD
                    select p;

            var prods2 = from p in prods
                        select new {
                            p.ProdCD,
                            p.Description,
                            Display = p.ProdCD + " - " + p.Description,
                            UserDefined = (p.UserTextFlag == PrdnDataHelper.BoolYNTue),
                            p.ProdImages
                        };

            var prods3 = from p in prods2.ToList() 
                         select new {
                            p.ProdCD,
                            p.Description,
                            p.Display,
                            p.UserDefined,
                            ImageID = (from i in p.ProdImages select i.ImageID).FirstOrDefault()
                        };                         
                         

            //ObjectQuery oq1 = (ObjectQuery)prods2;
            //string s = oq1.ToTraceString();
            //Console.WriteLine(s);

            foreach (var prod in prods3)
            {
                Console.WriteLine(prod.Display
                    + " " + prod.ImageID
                    //+ " " + day.ShipPrdnOrders.Sum(p => (int?)p.Runs.Count)
               );
            }

            Console.ReadLine();

        }


        private static void NextOrd()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();

            ProductionOrder ord = PrdnDBContext.NextPrdnOrder();

            Console.WriteLine((ord != null) ? ord.OrderNo : "nada");
            Console.ReadLine();

        }

        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        static void RequestList()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();

            List<string> states = new List<string>()
            {"NEW", "PROCESSING"};

            var requests = from r in PrdnDBContext.Requests
                           select r;

            requests = from r in requests
                       where states.Contains(r.StatusStr)
                       select r;

            var q = from r in requests
                    orderby r.RequestDt descending
                    select r.ID
                    ;

            ObjectQuery oq1 = (ObjectQuery)q;
            string s = oq1.ToTraceString();
            
            Console.WriteLine(s);

            Console.ReadLine();
        }

        static void NextSerial()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();

            Console.WriteLine(PrdnDBContext.NextSerialStr());
            Console.ReadLine();
        }

        static void Types()
        {
            PrdnEntities PrdnDBContext = new PrdnEntities();

            var types = from t in PrdnDBContext.ProductionTypes
                        orderby t.Code
                        select new
                        {
                            Text = t.Code + " " + t.Description,
                            t.ID
                        };



            ObjectQuery oq1 = (ObjectQuery)types;
            string s = oq1.ToTraceString();
            Console.WriteLine(s);

            Console.ReadLine();
        }

        static void PoLookup()
        {
            string prdnOrdNo;

            PrdnEntities prdnDbContext = new PrdnEntities();
            var nextPrdnOrd = from p in prdnDbContext.ProductionOrders
                    where p.ShipDay > DateTime.Today
                    group p by 1 into g
                    select g.Min(x => x.OrderNo);

            prdnOrdNo = nextPrdnOrd.ToList().FirstOrDefault();

            var maxPrdnOrd = from p in prdnDbContext.ProductionOrders
                              group p by 1 into g
                              select g.Max(x => x.OrderNo);

            prdnOrdNo = maxPrdnOrd.ToList().FirstOrDefault();

            ObjectQuery oq1 = (ObjectQuery)maxPrdnOrd;
            string s = oq1.ToTraceString();
            Console.WriteLine(prdnOrdNo);

            Console.ReadLine();
        }

        static void DatesTest()
        {
            PrdnEntities prdnDB = new PrdnEntities();

            var days = from calDay in prdnDB.CalendarDays
                       group calDay by 1 into g // Notice here, grouping by a constant value
                       select new
                       {
                           MinDate = g.Min(p => p.CalDay),
                           MaxDate = g.Max(p => p.CalDay)
                       };

            ObjectQuery oq2 = (ObjectQuery)days;
            string s = oq2.ToTraceString();
            Console.WriteLine(s);

            var dates = days.ToList().FirstOrDefault();

            if (dates != null)
            {


                Console.WriteLine(dates.MinDate.ToShortDateString()
                    + " " + dates.MaxDate.ToShortDateString());
            }
            Console.ReadLine();
        }

        static void SumTest()
        {
            PrdnEntities prdnDB = new PrdnEntities();

            var q = from day in prdnDB.CalendarDays.Include("ShipPrdnOrders").Include("ShipPrdnOrders.Runs")
                    select day
                    ;
                            

            //var l = q.ToList();

            ObjectQuery oq1 = (ObjectQuery)q;
            string s = oq1.ToTraceString();
            Console.WriteLine(s);

            foreach (CalendarDay day in q)
            {
                Console.WriteLine(day.CalDay.ToShortDateString()
                    +" "+ day.ShipPrdnOrders.Count()
                    +" " + day.ShipPrdnOrders.Sum(p => (int?)p.Runs.Count)
               );
            }

            Console.ReadLine();
        }

        public class PrdnCalendarDay
        {
            private bool existsInDB;

            public bool ExistsInDB
            {
                get { return existsInDB; }
                set { existsInDB = value; }
            }

            public DateTime CalDay { get; set; }

            public IEnumerable<CST.Prdn.Data.ProductionOrder> ShipPrdnOrders { get; set; }

            public int TotalRuns { get; set; }

            public bool FutureRunsExist { get; set; }

            public bool ShipDay { get { return !String.IsNullOrEmpty(ShipPrdnOrdNo); } }

            public string ShipPrdnOrdNo
            {
                get
                {
                    if (ShipPrdnOrders != null)
                    {
                        CST.Prdn.Data.ProductionOrder shipPo = ShipPrdnOrders.FirstOrDefault();
                        if (shipPo != null)
                        { return shipPo.OrderNo; }
                    }
                    return String.Empty;
                }
            }

            const string format = "MMMddyyyy";
            
            public bool Weekend()
            {
                return ((CalDay.DayOfWeek == DayOfWeek.Saturday) || (CalDay.DayOfWeek == DayOfWeek.Sunday));
            }

        }

        public static IEnumerable DaysInCalendar(DateTime firstDayOfCalendar, DateTime lastDayOfCalendar)
        {
            DateTime day = firstDayOfCalendar;
            while (day.Date <= lastDayOfCalendar)
            {
                yield return day.Date;
                day = day.AddDays(1).Date;
            }
        }

        static void CalTest()
        {
            DateTime firstDayOfCalendar = new DateTime(2012, 4, 29);
            DateTime lastDayOfCalendar = new DateTime(2012, 6, 2);

            var daysOfCal = from DateTime day in DaysInCalendar(firstDayOfCalendar, lastDayOfCalendar)
                            select day;

            PrdnEntities prdnDB = new PrdnEntities();

            // Oraclde ODP.Net generates bad sql for 10g, corelated subquery two levels deep
            //var dbCalDays = from day in prdnDB.CalendarDays
            //                where ((day.CalDay >= firstDayOfCalendar) && (day.CalDay <= lastDayOfCalendar))
            //                orderby day.CalDay
            //                select new PrdnCalendarDay()
            //                {
            //                    ExistsInDB = true,
            //                    CalDay = day.CalDay,
            //                    ShipPrdnOrders = day.ShipPrdnOrders,
            //                    TotalRuns = day.ShipPrdnOrders.Sum(p => (int?)p.Runs.Count) ?? 0
            //                };

            var dbCalDays = (from day in prdnDB.CalendarDays.Include("ShipPrdnOrders").Include("ShipPrdnOrders.Runs")
                    select day).ToList();
                    ;

            List<PrdnCalendarDay> dbPrdnCalDays = new List<PrdnCalendarDay>();

            foreach (var day in dbCalDays)
	        {
                dbPrdnCalDays.Add(
                    new PrdnCalendarDay()
                    {
                        ExistsInDB = true,
                        CalDay = day.CalDay,
                        ShipPrdnOrders = day.ShipPrdnOrders,
                        TotalRuns = day.ShipPrdnOrders.Sum(p => (int?)p.Runs.Count) ?? 0
                    }
                );
		 
	        }

            var prdnCalDays = from d in daysOfCal
                              join p in dbPrdnCalDays on d equals p.CalDay into outer
                              from o in outer.DefaultIfEmpty()
                              select o ?? new PrdnCalendarDay()
                              {
                                  ExistsInDB = false,
                                  CalDay = d,
                              };

            //ObjectQuery oq2 = (ObjectQuery)prdnCalDays;
            //s = oq2.ToTraceString();
            //Console.WriteLine(s);
            //Console.ReadLine();

            foreach (var prdnCalDay in prdnCalDays)
            {
                Console.WriteLine(prdnCalDay.CalDay.ToString());
            }

            Console.ReadLine();
        }

        static void List()
        {
            using (PrdnEntities ctx = new PrdnEntities())
            {
                var q = from x in ctx.ProductionJobs
                        orderby x.ID descending
                        //where x.StatusStr == sts
                        select new
                        {
                            x.ID,
                            x.Product.ProdCD,
                            x.Product.ProductType.ProdTypeCD,
                            x.Product.ProductType.Description
                        };

                ObjectQuery oq = (ObjectQuery)q;
                string s = oq.ToTraceString();
                Console.WriteLine(s);
                Console.ReadLine();
            }
        }

        
        static void daty()
        {
            using (PrdnEntities ctx = new PrdnEntities())
            {
                var q = from p in ctx.CalendarDays
                        orderby p.CalDay
                        select p;

                ObjectQuery oq = (ObjectQuery)q;
                string s = oq.ToTraceString();
                Console.WriteLine(s);
                Console.ReadLine();

                CalendarDay c = new CalendarDay();
                c.CalDay = DateTime.Today;
                ctx.CalendarDays.AddObject(c);
                ctx.SaveChanges();
            }
        }

    }
}
