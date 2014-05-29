﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Reflection;

namespace CST.Prdn.Data
{
    public partial class PrdnEntities : ObjectContext
    {
        public string DBName() 
        {
            return ExecuteStoreQuery<string>("SELECT NAME FROM V$DATABASE").FirstOrDefault();
        }

        public string DBNameObfuscate()
        {
            string dbName = DBName();
            if (dbName == null)
            {
                return "null";
            }
            else if (dbName.ToUpper() == "FSYS")
            {
                return "Production";
            }
            else if (dbName.ToUpper() == "TEST")
            {
                return "Testing";
            }
            else if (dbName.ToUpper() == "FTST")
            {
                return "Eff. Testing";
            }
            else if (dbName.ToUpper() == "FDEV")
            {
                return "Eff. Development";
            }
            else
            {
                char[] array = dbName.ToLower().ToCharArray();
                Array.Reverse(array);
                return "Unkn. " + new string(array);
            }
        }

        public string EncryptedStr(string plainText) {
            ObjectParameter outparm = new ObjectParameter("v_CRYPT_TEXT", typeof(string));
            GetEncryptText(plainText, outparm);
            return (string)outparm.Value;
        }

        public string DecryptedStr(string cryptText)
        {
            ObjectParameter outparm = new ObjectParameter("v_PLAIN_TEXT", typeof(string));
            GetDecryptText(cryptText, outparm);
            return (string)outparm.Value;
        }

        public decimal NextSerialDecimal()
        { 
            ObjectParameter outparm = new ObjectParameter("v_SERIAL_NUM", typeof(decimal));
            GetNextPrdnSerial(outparm);
            return (decimal)outparm.Value;
        }

        public string NextSerialStr()
        {
            decimal d = Decimal.Truncate(NextSerialDecimal());
            return d.ToString("#");
        }

        public decimal NextRunSequence(decimal runID)
        {
            var m = from j in ProductionJobs
                         where j.RunID == runID
                         group j by 1 into g 
                         select g.Max(x => x.RunSeqNo);

            decimal maxSeq = m.ToList().FirstOrDefault();

            return Decimal.Truncate(maxSeq + 1);
        }

        public IQueryable<string> NextPrdnOrdNoQry(DateTime? afterDate=null)
        {
            if (afterDate == null) { afterDate = DateTime.Today; }

            return from o in ProductionOrders
                   where o.ShipDay > afterDate
                   group o by 1 into g
                   select g.Min(x => x.OrderNo);
        }

        public string NextPrdnOrdNo(DateTime? afterDate=null)
        {
            if (afterDate == null) { afterDate = DateTime.Today; } 
            
            return NextPrdnOrdNoQry(afterDate).FirstOrDefault();
        }

        public ProductionOrder NextPrdnOrder(DateTime? afterDate=null)
        {
            if (afterDate == null) { afterDate = DateTime.Today; }

            var nextOrdNo = NextPrdnOrdNoQry(afterDate);

            var nextOrd = from o in ProductionOrders
                          where nextOrdNo.Contains(o.OrderNo)
                          select o;

            return nextOrd.FirstOrDefault();
        }

        public Worksheet CloneWorksheet(decimal? sourceID, decimal cloneID)
        {
            if (sourceID == null)
            {
                return null;
            }

            Worksheet ws = (from w in Worksheets.Include("WorksheetChars").Include("WorksheetComps")
                            where w.ID == sourceID
                            select w).FirstOrDefault();
            
            return CloneWorksheet(ws, cloneID);
        }

        public Worksheet CloneWorksheet(Worksheet ws, decimal cloneID) 
        {
            if (ws == null)
            {
                return null;
            }

            Worksheet wsClone = (Worksheet)ws.Clone();

            wsClone.ID = cloneID;

            foreach (WorksheetComp cmp in ws.WorksheetComps.Where(c => c.IsRoot))
            {
                WorksheetComp cmpClone = (WorksheetComp)cmp.Clone();
                cmpClone.WorksheetID = cloneID;
                if (cmp.ParentWorksheetID != null)
                {
                    cmpClone.ParentWorksheetID = cloneID;
                }
                wsClone.WorksheetComps.Add(cmpClone);
            }
            foreach (WorksheetChar chr in ws.WorksheetChars.Where(c => c.IsRoot))
            {
                WorksheetChar chrClone = (WorksheetChar)chr.Clone();
                chrClone.WorksheetID = cloneID;
                wsClone.WorksheetChars.Add(chrClone);
            }

            foreach (WorksheetComp cmp in ws.WorksheetComps.Where(c => !c.IsRoot))
            {
                WorksheetComp cmpClone = (WorksheetComp)cmp.Clone();
                cmpClone.WorksheetID = cloneID;
                if (cmp.ParentWorksheetID != null)
                {
                    cmpClone.ParentWorksheetID = cloneID;
                }
                wsClone.WorksheetComps.Add(cmpClone);
            }
            foreach (WorksheetChar chr in ws.WorksheetChars.Where(c => !c.IsRoot))
            {
                WorksheetChar chrClone = (WorksheetChar)chr.Clone();
                chrClone.WorksheetID = cloneID;
                wsClone.WorksheetChars.Add(chrClone);
            }

            return wsClone;
        }

        public Worksheet CloneReqWorksheet(string requestID, decimal cloneID)
        {
            var req = (from r in Requests
                       where r.ID == requestID
                       select new {r.WorksheetID}).FirstOrDefault();

            if (req == null)
            {
                return null;
            }

            return CloneWorksheet(req.WorksheetID, cloneID);
        }

        public OrderShipToInfo GetOrderShipToInfo(string orderNo)
        {
            var info = (from o in CstOrders
                         where o.OrderNo == orderNo
                         select new OrderShipToInfo
                         {
                             OrderNo = o.OrderNo,
                             CustDeliveryFlag = o.CustDeliveryFlag,
                             OrderTot = o.OrderLines.Sum(l => l.LineTotal) + (o.Tax ?? 0) + (o.Transport ?? 0) + (o.Shipping ?? 0),
                             ShipToCustID = o.ShipToCustID,
                             ShipToName = o.ShipToName,
                             ShipToAddr1 = o.ShipToAddr1,
                             ShipToAddr2 = o.ShipToAddr2,
                             ShipToAddr3 = o.ShipToAddr3,
                             ShipToAddr4 = o.ShipToAddr4,
                             ShipToCity = o.ShipToCity,
                             ShipToState = o.ShipToState,
                             ShipToPostal = o.ShipToPostal,
                             ShipToCountry = o.ShipToCountry
                         }).FirstOrDefault();

            return info;
        }

        public decimal? GetCstOrdTotal(string orderNo)
        {
            if (String.IsNullOrEmpty(orderNo))
            {
                return null;
            }

            var ordTot = (from o in CstOrders
                         where o.OrderNo == orderNo
                         select new 
                         {
                             OrderTot = o.OrderLines.Sum(l => l.LineTotal) + (o.Tax ?? 0) + (o.Transport ?? 0) + (o.Shipping ?? 0)
                         }).FirstOrDefault();

            if (ordTot != null)
            {
                return ordTot.OrderTot;
            }
            else { 
                return null; 
            }
        }
    }
    
    public interface IActiveFlagYN
    {
        String ActiveFlag { get; set; }
    }

    public interface ICodeName
    {
        Decimal ID { get; set; }
        String Code { get; set; }
        String Name { get; set; }
    }

    public interface ICodeNameAct : ICodeName, IActiveFlagYN
    { }

    public static class PrdnDataHelper
    {
        public const string BoolYNTue = "Y";
        public const string BoolYNFalse = "N";

        public static bool BoolYN(string flag) 
        {
            return ((flag != null)  && flag.Equals(BoolYNTue));
        }

        public static string BoolYNStr(bool active)
        {
            if (active)
            { return BoolYNTue; }
            else
            { return BoolYNFalse; }
        }

        public static void SetActive(this IActiveFlagYN obj, bool active)
        {
            obj.ActiveFlag = BoolYNStr(active);
            //if (active) { obj.ActiveFlag = BoolYNTue; } else { obj.ActiveFlag = BoolYNFalse; }
        }

        public static bool GetActive(this IActiveFlagYN obj)
        {
            return BoolYN(obj.ActiveFlag); // obj.ActiveFlag.Equals(BoolYNTue);
        }

        public const string StatusActive = "A";
        public const string StatusInactive = "I";

        public static bool BoolAI(string flag)
        {
            return ((flag != null) && flag.Equals(StatusActive));
        }

        public const int PrdnPriorityIDDefault = 1;
        public const string LeatherProdTypeCd = "300";
        public const string LeatherPatternProdTypeCd = "LEATHER PATTERN";
        public const string LeatherPatternSuffix = "-PT";
        public const string WarrantyProdTypeCd = "310";

        public const string UDCNotValCd = "N";
        public const string UDCSpcValCd = "Y";
        public const string UDCSpcValDescr = "Yes";
        public const string MaterialNormalValCd = "N";
        public const string MaterialLeatherlValCd = "Y";
        public const string MaterialVinylValCd = "V";

        //public const int PrdnCustIDCST = 1;
        public const int PrdnCustIDCST = 1;
        public const int PrdnCustIDRW = 2;
        public const string RWIsisDeptID = "150";
        public const string RWIsisDeptDescr = "Roadwire";

        //public static int CustIDFromIsisDept(string isisCustID)
        //{
        //    if (String.IsNullOrEmpty(isisCustID))
        //    {
        //        return 0;
        //    }
        //    else if (isisCustID == RWIsisDeptID)
        //    {
        //        return PrdnCustIDRW;
        //    }
        //    else
        //    {
        //        return PrdnCustIDCST;
        //    }
        //}

        public static bool IsEntityKeyProperty(this PropertyInfo property)
        {
            object[] attrs = property.GetCustomAttributes(false);
            foreach (object obj in attrs)
            {
                if (obj.GetType() == typeof(EdmScalarPropertyAttribute))
                {
                    EdmScalarPropertyAttribute attr = (EdmScalarPropertyAttribute)obj;
                    if (attr.EntityKeyProperty) {
                        return true;
                    }
                        
                }
            }
            return false;
        }

        public static EntityObject Clone(this EntityObject Entity)
        {
            var type = Entity.GetType();
            var clone = Activator.CreateInstance(type);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.SetProperty))
            {
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(EntityReference<>)) continue;
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(EntityCollection<>)) continue;
                if (property.PropertyType.IsSubclassOf(typeof(EntityObject))) continue;
                //if (property.IsEntityKeyProperty()) continue;

                if (property.CanWrite)
                {
                    property.SetValue(clone, property.GetValue(Entity, null), null);
                }
            }

            return (EntityObject)clone;
        }

        public static string ParmFormatList(int parms)
        {
            List<string> parmNames = new List<string>();
            for (int i = 0; i < parms; i++)
            {
                parmNames.Add(":p" + i.ToString());
            }
            return string.Join(",", parmNames);
        }
    }

    public partial class ProductionMfgr : ICodeNameAct
    {
        public bool Active { get { return this.GetActive();  } set { this.SetActive(value);  }}
    }

    public partial class ProductionLocation : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
    }

    public partial class ProductionReason : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
    }

    public partial class ProductionPriority : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
    }

    public partial class ProductionCustomer : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
    }

    public partial class ProductionType : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
    }
    
    public partial class LabelPrinter : IActiveFlagYN
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
    }
    
    // Production Orders //////////////////////////
    //[MetadataType(typeof(PrdnOrdViewModel))]
    public partial class ProductionOrder
    {
        public static string IncrementPrdnOrdNo(string prdnOrdNo)
        {
            int PoNoInt = Convert.ToInt32(prdnOrdNo);
            PoNoInt++;
            return PoNoInt.ToString();
        }
    }

    public partial class User : IActiveFlagYN
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
        
        public bool AlterPassword {
            get { return PrdnDataHelper.BoolYN(AlterPasswordFlag); }
            set { 
                AlterPasswordFlag = PrdnDataHelper.BoolYNStr(value); 
            } 
        }

        public string PlainPassword {
            get {
                using (PrdnEntities prdnEntities = new PrdnEntities())
                {
                    string plainPassword = prdnEntities.DecryptedStr(Password.Trim());
                    return plainPassword;
                }
            }
            set {
                using (PrdnEntities prdnEntities = new PrdnEntities())
                {
                    Password = prdnEntities.EncryptedStr(value);
                }
            }
        }

        public bool PlainPasswordMatch(string plaintext)
        {
            return (PlainPassword == plaintext);
        }

        public string GetLoginUpper()
        {
            if (Login != null)
            {
                return Login.ToUpper();
            }
            else
            {
                return null;
            }
        }

        public string FullName { get { return FirstName + " " + LastName; } }
    }

    public partial class App : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }

        public bool SysAdmin
        {
            get { return PrdnDataHelper.BoolYN(SysAdminFlag); }
            set { SysAdminFlag = PrdnDataHelper.BoolYNStr(value); }
        }

        public string CodeDashName { get { return Code + "-" + Name; } }
    }

    public partial class Group : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }

        public bool AppAdmin {
            get { return PrdnDataHelper.BoolYN(AppAdminFlag); }
            set { AppAdminFlag = PrdnDataHelper.BoolYNStr(value); }
        }

        public string CodeDashName { get { return Code + "-" + Name; } }

        public const char CodeSep = '/';

        public static string GetAppGroupCode(string appCode, string groupCode) 
        {
            return appCode + CodeSep + groupCode;
        }

        public static bool SplitAppGroupCode(string appGroupCode, out string appCode, out string groupCode)
        {
            bool bothParts = false;
            appCode = null;
            groupCode = null;

            string[] parts = appGroupCode.Split(CodeSep);
            if (parts.Length > 0)
            {
                appCode = parts[0];

                if (parts.Length > 1)
                {
                    groupCode = parts[1];
                    
                    bothParts = true;
                }
            }
            return bothParts;
        }

        public string AppGroupCode { get { return GetAppGroupCode(App.Code, Code); } }
    }

    public partial class PrdnAttachmentType : ICodeNameAct
    {
        public bool Active { get { return this.GetActive(); } set { this.SetActive(value); } }
    }

    // Requests ////////////////////////////////////////////

    public enum RequestStatus { NEW, PROCESSING, CONFIRMED, CANCELED, SCHEDULED };

    public enum RequestSpecWS { S, Y, N };

    public partial class Request
    {
        Request()
        {
            Status = RequestStatus.NEW;
        }

        public string SoldStockStr
        {
            get
            {
                if (SoldItemFlag.Equals(PrdnDataHelper.BoolYNTue))
                {
                    return "SOLD";
                }
                else
                {
                    return "STOCK";
                }
            }
        }

        public bool ModifiedKit
        { 
            get {
                return this.ModifiedKitFlag.Equals(PrdnDataHelper.BoolYNTue);
            }
        }

        protected static RequestStatus CalcStatus(string statusStr)
        {
            foreach (RequestStatus s in Enum.GetValues(typeof(RequestStatus)))
            {
                if (s.ToString().ToUpper().Equals(statusStr.ToUpper()))
                {
                    return s;
                }
            }

            return RequestStatus.NEW;
        }

        public RequestStatus Status {
            get {
                return CalcStatus(StatusStr); 
            }

            set { StatusStr = value.ToString().ToUpper(); } 
        }

        public void UpdateProcessingDtUsr(string user)
        {
            if (ProcessedDt == null)
            {
                ProcessedDt = DateTime.Now;
            }
            if (ProcessedCstUserID == null)
            {
                ProcessedCstUserID = user;
            }
        }

        public void UpdateStatusDtUsr(string user)
        {
            RequestStatus status = Status;
            if (status == RequestStatus.PROCESSING)
            {
                UpdateProcessingDtUsr(user);
            }
            else if (status == RequestStatus.CONFIRMED)
            {
                if (ConfirmDt == null)
                {
                    ConfirmDt = DateTime.Now;
                }
                if (ConfirmCstUserID == null)
                {
                    ConfirmCstUserID = user;
                }
                UpdateProcessingDtUsr(user);
            }
            else if (status == RequestStatus.CANCELED)
            {
                if (CancelDt == null)
                {
                    CancelDt = DateTime.Now;
                }
                if (CancelCstUserID == null)
                {
                    CancelCstUserID = user;
                }
                UpdateProcessingDtUsr(user);
            }
        }

        public static RequestSpecWS CalcSpecWS(string specStr)
        {
            foreach (RequestSpecWS s in Enum.GetValues(typeof(RequestSpecWS)))
            {
                if (s.ToString().Equals(specStr))
                {
                    return s;
                }
            }
            return RequestSpecWS.N;
        }

        public RequestSpecWS SpecialWS
        {
            get
            {
                return CalcSpecWS(SpecialWSStr);
            }

            set { SpecialWSStr = value.ToString(); }
        }

        public static string CalcSpecialWSDescr(RequestSpecWS specialWS)
        {
            if (specialWS == RequestSpecWS.S)
            {
                return "Standard";
            }
            else if (specialWS == RequestSpecWS.Y)
            {
                return "Non-Standard";
            }
            else
            {
                return "No";
            };
        }

        public string SpecialWSDescr
        {
            get
            {
                return CalcSpecialWSDescr(SpecialWS);
            }
        }

        public static TimeSpan CalcElapsed(RequestStatus status, DateTime requestDt, DateTime? confirmDt, DateTime? cancelDt, DateTime? scheduleDt) 
        { 
                DateTime startDt;

                if (status == RequestStatus.CONFIRMED)
                {
                    startDt = confirmDt ?? DateTime.Now;
                }
                else if (status == RequestStatus.CANCELED)
                {
                    startDt = cancelDt ?? DateTime.Now;
                }
                else if (status == RequestStatus.SCHEDULED)
                {
                    startDt = scheduleDt ?? DateTime.Now;
                }
                else 
                {
                    startDt = DateTime.Now;
                }

                return startDt - requestDt;
        }

        public TimeSpan Elapsed { 
            get {
                return CalcElapsed(Status, RequestDt, ConfirmDt, CancelDt, ScheduledDt) ;
            } 
       }

    }

    // Production Runs ////////////////////////////////////////////

    public partial class ProductionRun
    {
        public string RunCode { get { return PrdnOrderNo + PrdnType.Code; } }

        public string RunDescr
        {
            get { return RunCode + " " + PrdnType.Description + " " +
            PrdnOrder.ShipDay.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern); 
        }}
    }

    public partial class UserSettingsModel
    {
        public UserSettingsModel()
        {
            JobPageSize = DefaultPageSize;
            RequestPageSize = DefaultPageSize;
        }

        public decimal? UserID { get; set; }

        public string Login { get; set; }

        public string DefaultRunOrderNo { get; set; }

        public decimal? DefaultRunID { get; set; }

        public string DefaultRunDescr { get; set; }

        public int JobPageSize { get; set; }

        public int RequestPageSize { get; set; }

        public int? LabelPrinterID { get; set; }

        public static readonly int DefaultPageSize = 20;
    }

    public partial class UserDefaultPrdnRun
    {
        public decimal UserID { get; set; }

        public decimal? DefaultRunID { get; set; }

        public string DefaultRunDescr { get; set; }
    }

    public static class LeatherChars
    {
        public static string ColorCodesStr(string colorCD, string insert1CD, string insert2CD)
        {
            return SystemExtensions.JoinOnly(" ", new string[] { colorCD, insert1CD, insert2CD });
            /*StringBuilder builder = new StringBuilder();
            if (!String.IsNullOrEmpty(colorCD))
            {
                builder.Append(colorCD);
            }
            if (!String.IsNullOrEmpty(insert1CD))
            {
                builder.Append(" ");
                builder.Append(insert1CD);
            }
            if (!String.IsNullOrEmpty(insert2CD))
            {
                builder.Append(" ");
                builder.Append(insert2CD);
            }
            return builder.ToString();*/
        }

        public static int ColorCount(string colorCD, string insert1CD, string insert2CD)
        {
            int cnt = 0;

            if (!String.IsNullOrEmpty(colorCD))
            {
                cnt++;
            }
            if (!String.IsNullOrEmpty(insert1CD))
            {
                cnt++;
            }
            if (!String.IsNullOrEmpty(insert2CD))
            {
                cnt++;
            }

            return cnt;
        }

        public static string ColorCountStr(int colorCount) {
            if (colorCount > 1)
            {
                return colorCount.ToString() + "T";
            }
            else { return null; }
        }

        public static string UdcCdStr(string udcCD)
        {
            if (udcCD == PrdnDataHelper.UDCSpcValCd) {
                return "UDF"; 
            } else
            if (udcCD == PrdnDataHelper.UDCNotValCd) {
                return null;
            } else {
                return udcCD;
            }
        }

        public static string UdcDescrStr(string udcCD, string udcDescr)
        {
            if (udcCD == PrdnDataHelper.UDCSpcValCd)
            {
                if (String.Compare(udcDescr, PrdnDataHelper.UDCSpcValDescr, true) == 0)
                {
                    return "User Defined";
                }
                else
                {
                    return udcDescr;
                }
            }
            else
            if (udcCD == PrdnDataHelper.UDCNotValCd)
            {
                return null;
            }
            else
            {
                return udcDescr;
            }
        }
        
        public static string MatCdDisplay(string materialCD)
        { 
            if (materialCD == PrdnDataHelper.MaterialLeatherlValCd) {
                return "L";
            }
            else if (materialCD == PrdnDataHelper.MaterialVinylValCd) {
                return "V";
            } else {return null;}
        }

        public static string MatDescrDisplay(string materialCD, string materialDescr)
        { 
            if (materialCD == PrdnDataHelper.MaterialLeatherlValCd) {
                return materialDescr;
            }
            else if (materialCD == PrdnDataHelper.MaterialVinylValCd) {
                return materialDescr;
            } else {return null;}
        }

        public static string ColorCdStr(string colorCD, string insert1CD, string insert2CD)
        {
            int colorCount = ColorCount(colorCD, insert1CD, insert2CD);
            return SystemExtensions.JoinOnly(" ", new string[] { ColorCountStr(colorCount), ColorCodesStr(colorCD, insert1CD, insert2CD) });
        }

        public static string ColorDescrStr(string colorDescr, string insert1Descr, string insert2Descr)
        {
            return SystemExtensions.JoinOnly("/", new string[] { colorDescr, insert1Descr, insert2Descr });
        }
    }

    public static class LeatherComps
    {
        public static string AbrevStr(string prodCD600, string prodCD610, string prodCD620, string prodCD630, string prodCD640)
        {
            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrEmpty(prodCD600))
            {
                builder.Append("E");
            }
            if (!string.IsNullOrEmpty(prodCD610))
            {
                builder.Append("H");
            }
            if (!string.IsNullOrEmpty(prodCD620))
            {
                builder.Append("P");
            }
            if (!string.IsNullOrEmpty(prodCD630))
            {
                builder.Append("I");
            }
            if (!string.IsNullOrEmpty(prodCD640))
            {
                builder.Append("S");
            }
            return builder.ToString();
        }
    }

    public partial class WorksheetChar
    {
        public bool IsRoot
        {
            get { return String.IsNullOrEmpty(this.ParentCompProdCD); }
        }

    }

    public partial class WorksheetComp
    {
        public bool IsRoot
        {
            get { return String.IsNullOrEmpty(this.ParentCompProdCD); }
        }

    }

    public partial class WorksheetCharVW
    {
        public string ColorCdStr { get { return LeatherChars.ColorCdStr(ColorCD, Insert1CD, Insert2CD); } }
    }

    public partial class WorksheetCompVW
    {
        public string AbrevStr { get { return LeatherComps.AbrevStr(ProdCD600, ProdCD610, ProdCD620, ProdCD630, ProdCD640); } }
    }

    public partial class LeatherCharVW
    {
        public string ColorCdStr { get { return LeatherChars.ColorCdStr(ColorCD, Insert1CD, Insert2CD); } }

        public string ColorDescrStr { get { return LeatherChars.ColorDescrStr(ColorDescr, Insert1Descr, Insert2Descr); } }

        public string UdcCdStr { get { return LeatherChars.UdcCdStr(UDCCD); } }

        public string UdcDescrStr { get { return LeatherChars.UdcDescrStr(UDCCD, UDCDescr); } }

        public string MatCdDisplay { get { return LeatherChars.MatCdDisplay(MaterialCD); } }

        public string MatDescrDisplay { get { return LeatherChars.MatDescrDisplay(MaterialCD, MaterialDescr); } }

        public string ColorCdDisplay { get { 
            return SystemExtensions.JoinOnly(" ", new string[] { UdcCdStr, MatCdDisplay, ColorCdStr });
        } }

        public string ColorDescrDisplay { get {
                return SystemExtensions.JoinOnly(" ", new string[] { UdcDescrStr, MatDescrDisplay, ColorDescrStr });
        } }
    }

    public partial class LeatherCompVW
    {
        public string AbrevStr { get { return LeatherComps.AbrevStr(ProdCD600, ProdCD610, ProdCD620, ProdCD630, ProdCD640); } }
    }

    public class OrderShipToInfo
    {
        public string OrderNo { get; set; }

        public string CustDeliveryFlag { get; set; }

        public bool DropShip { get {
            return PrdnDataHelper.BoolYN(CustDeliveryFlag);
        } }

        public decimal OrderTot { get; set; }

        public string ShipToCustID { get; set; }

        public string ShipToName { get; set; }

        public string ShipToAddr1 { get; set; }

        public string ShipToAddr2 { get; set; }

        public string ShipToAddr3 { get; set; }

        public string ShipToAddr4 { get; set; }

        public string ShipToCity { get; set; }

        public string ShipToState { get; set; }

        public string ShipToPostal { get; set; }

        public string ShipToCountry { get; set; }

    }

    public enum ScanResult { CompletedAndPrinted, CompletedNotPrinted, CstItemCreatedAndPrinted, CstItemCreatedNotPrinted, InvalidScanValue, ItemExists, InvalidStatus, ScanException };

    public partial class ProductionScan
    {
        public ProductionScan() { }

        public ProductionScan(DateTime date, decimal userID, string value, ScanResult result, PrdnJobStatus? status, string msg)
            : base()
        {
            int i = result.ConvertToInt();
            Value = value;
            ScanDt = date;
            UserID = userID;
            Result = result;
            if (status != null)
            {
                JobStatusStr = ((PrdnJobStatus)status).DbValStr();    
            }

            Message = msg;
        }

        public ScanResult Result { 
            get { return ResultNum.ToInt().ConverToEnum<ScanResult>(); }
            set { ResultNum = value.ConvertToInt(); }
        }
    }
    
}

