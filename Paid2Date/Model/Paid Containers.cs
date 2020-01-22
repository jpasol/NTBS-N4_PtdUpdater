using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reports;
using System.Data;
using System.Collections.ObjectModel;

namespace Paid2Date.Model
{
    class Paid_Container
    {
        public string ContainerNumber;
        public string GatepassNumber;
        public string Registry;
        public string CompanyCode;
        public string BillofLading;
        public string CCRNumber;
        public string Remarks;
        public string Category;
        public int StorageQty;
        public DateTime CROValidity;
        public DateTime? FreeUntil = null;
        public DateTime? LastDischargeDate = null;
        public DateTime? PlugIn = null;
        public DateTime? PlugOut = null;
        public DateTime? StorageBegin = null;
        public DateTime? StorageEnd = null;
        public DateTime SystemDate;


        public static ObservableCollection<Paid_Container> GetPaidContainer()
        {
            ObservableCollection<Paid_Container> _paidContainers;
            ADODB.Connection BLConnection = new Connections().BLConnection;
            ADODB.Connection BLConnection2 = new Connections().BLConnection;
            ADODB.Connection BLConnection3 = new Connections().BLConnection;
            //Connect
            BLConnection.Open();
            BLConnection2.Open();
            BLConnection3.Open();

            //Retrieve
            #region GPS
            ADODB.Command retrieveCommandGPS = new ADODB.Command();
            retrieveCommandGPS.ActiveConnection = BLConnection;
            retrieveCommandGPS.CommandText = $@"
SELECT [refnum]
      ,[seqnum]
      ,[gpsnum]
      ,[gpstyp]
      ,[cntnum]
      ,[bilnum]
      ,[regnum]
      ,[crodte]
      ,[stoday]
      ,[freday]
      ,[stosta]
      ,[stoamt]
      ,[plugin]
      ,[plugou]
      ,[lstdch]
      ,[stobeg]
      ,[freeuntil]
      ,[stoend]
      ,[status]
      ,[sysdte]
      ,[CompanyCode]
  FROM [billing].[dbo].[CYMgps] where status <> 'CAN'
";
            #endregion

            #region CCR
            ADODB.Command retrieveCommandCCR = new ADODB.Command();
            retrieveCommandCCR.ActiveConnection = BLConnection2;
            retrieveCommandCCR.CommandText = $@"
SELECT [refnum]
      ,[seqnum]
      ,[itmnum]
      ,[ccrnum]
      ,[ccrtyp]
      ,[chargetyp]
      ,[descr]
      ,[docrefno]
      ,[entnum]
      ,[regnum]
      ,[cntnum]
      ,[cntsze]
      ,[fulemp]
      ,[amt]
      ,[vatamt]
      ,[wtax]
      ,[vatcde]
      ,[stostat]
      ,[lngth]
      ,[width]
      ,[height]
      ,[ums]
      ,[quantity]
      ,[dgrcls]
      ,[dgramt]
      ,[revton]
      ,[ovzamt]
      ,[enrfrdttm]
      ,[enstodttm]
      ,[stordys]
      ,[rfrhrs]
      ,[remark]
      ,[guarntycde]
      ,[status]
      ,[shplin]
      ,[vslcde]
      ,[pod]
      ,[userid]
      ,[sysdttm]
      ,[updcde]
      ,[outdttm]
      ,[IsN4ReeferPaymentUpdated]
      ,[CompanyCode]
  FROM [billing].[dbo].[CCRdtl] where (chargetyp like '%CBIMP%' or chargetyp like '%CBEXP%') and status <> 'CAN'
";
            #endregion

            #region CYX
            ADODB.Command retrieveCommandCYX = new ADODB.Command();
            retrieveCommandCYX.ActiveConnection = BLConnection3;
            retrieveCommandCYX.CommandText = $@"
SELECT [refnum]
      ,[seqnum]
      ,[itmnum]
      ,[cntnum]
      ,[ccrnum]
      ,[cntsze]
      ,[fulemp]
      ,[dgrcls]
      ,[vslcde]
      ,[whfamt]
      ,[arramt]
      ,[ovzamt]
      ,[dgramt]
      ,[arrvat]
      ,[arrtax]
      ,[vatcde]
      ,[cntovzl]
      ,[cntovzw]
      ,[cntovzh]
      ,[ovzums]
      ,[revton]
      ,[trncde]
      ,[whfcde]
      ,[guarntycde]
      ,[dolrte]
      ,[exprtr]
      ,[broker]
      ,[entnum]
      ,[commod]
      ,[remark]
      ,[trknam]
      ,[pltnum]
      ,[trkchs]
      ,[status]
      ,[ovrccr]
      ,[ppanum]
      ,[userid]
      ,[sysdttm]
      ,[updcde]
      ,[outdttm]
      ,[supvsr]
      ,[IsN4BillingPermissionGranted]
      ,[wghamt]
      ,[IsN4BillingDGPermissionGranted]
      ,[IsN4BillingOOGPermissionGranted]
      ,[CompanyCode]
  FROM [billing].[dbo].[CCRcyx] where status <> 'CAN'
"; 
            #endregion

            System.Data.DataTable _paidContainersTableGPS = new System.Data.DataTable();
            System.Data.DataTable _paidContainersTableCCR = new System.Data.DataTable();
            System.Data.DataTable _paidContainersTableCYX = new System.Data.DataTable();

            System.Data.OleDb.OleDbDataAdapter adapterGPS = new System.Data.OleDb.OleDbDataAdapter();
            System.Data.OleDb.OleDbDataAdapter adapterCCR = new System.Data.OleDb.OleDbDataAdapter();
            System.Data.OleDb.OleDbDataAdapter adapterCYX = new System.Data.OleDb.OleDbDataAdapter();

            adapterGPS.Fill(_paidContainersTableGPS, retrieveCommandGPS.Execute(out object dsdsad, 0, 0));
            adapterGPS.Fill(_paidContainersTableCCR, retrieveCommandCCR.Execute(out object dsdsad2, 0, 0)); //2 implicit initialization
            adapterGPS.Fill(_paidContainersTableCYX, retrieveCommandCYX.Execute(out object dsdsad3, 0, 0)); //2 implicit initialization

            //Convert datatable to observable collection
            _paidContainers = Generate(_paidContainersTableGPS,_paidContainersTableCCR, _paidContainersTableCYX);

            //return 
            return _paidContainers;


        }

        private static ObservableCollection<Paid_Container> Generate(DataTable RetrivedPaidContainersGPS, DataTable RetrievedPaidContainersCCR, DataTable RetrievedPaidContainersCYX)
        {
            ObservableCollection<Paid_Container> _generated = new ObservableCollection<Paid_Container>();
            #region CYMGPS
            foreach (DataRow dr in RetrivedPaidContainersGPS.Rows)
            {

                Paid_Container _paidContainer = new Paid_Container();
                _paidContainer.ContainerNumber = dr["cntnum"].ToString().Trim();
                _paidContainer.GatepassNumber = dr["gpsnum"].ToString().Trim();
                _paidContainer.Registry = dr["regnum"].ToString().Trim();
                _paidContainer.CompanyCode = dr["CompanyCode"].ToString().Trim();
                _paidContainer.BillofLading = dr["bilnum"].ToString().Trim();
                _paidContainer.Category = "IMPRT";

                string _free = dr["freeuntil"].ToString();
                string _stobeg = dr["stobeg"].ToString();
                string _stoend = dr["stoend"].ToString();
                string _sysdte = dr["sysdte"].ToString();
                string _crodte = dr["crodte"].ToString();
                string _plugin = dr["plugin"].ToString();
                string _plugou = dr["plugou"].ToString();
                string _lstdsc = dr["lstdch"].ToString();

                _paidContainer.FreeUntil = string.IsNullOrEmpty(_free) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_free);
                _paidContainer.StorageBegin = string.IsNullOrEmpty(_stobeg) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_stobeg);
                _paidContainer.StorageEnd = string.IsNullOrEmpty(_stoend) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_stoend);
                _paidContainer.SystemDate = string.IsNullOrEmpty(_sysdte) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_sysdte);
                _paidContainer.CROValidity = string.IsNullOrEmpty(_crodte) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_crodte);
                _paidContainer.PlugIn = string.IsNullOrEmpty(_plugin) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_plugin);
                _paidContainer.PlugOut = string.IsNullOrEmpty(_plugou) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_plugou);
                _paidContainer.LastDischargeDate = string.IsNullOrEmpty(_lstdsc) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_lstdsc);
                _generated.Add(_paidContainer);

            }
            #endregion
            #region CCRDTL
            foreach (DataRow dr in RetrievedPaidContainersCCR.Rows)
            {

                Paid_Container _paidContainer = new Paid_Container();
                _paidContainer.ContainerNumber = dr["cntnum"].ToString().Trim();
                _paidContainer.CCRNumber = dr["ccrnum"].ToString().Trim();
                _paidContainer.Remarks = dr["remark"].ToString().Trim();
                _paidContainer.Category = dr["chargetyp"].ToString().Trim().Contains("CBIMP") ? "IMPRT" : "EXPRT";

                int.TryParse(dr["quantity"].ToString().Trim(), out _paidContainer.StorageQty);

                string _sysdttm = dr["sysdttm"].ToString();
                _paidContainer.SystemDate = string.IsNullOrEmpty(_sysdttm) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_sysdttm);
                _generated.Add(_paidContainer);

                #endregion

                
            }

            #region CCRCYX
            foreach (DataRow dr in RetrievedPaidContainersCYX.Rows)
            {

                Paid_Container _paidContainer = new Paid_Container();
                _paidContainer.ContainerNumber = dr["cntnum"].ToString().Trim();
                _paidContainer.CCRNumber = dr["ccrnum"].ToString().Trim();
                _paidContainer.Category = "EXPRT";

                string _sysdttm = dr["sysdttm"].ToString();
                _paidContainer.SystemDate = string.IsNullOrEmpty(_sysdttm) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(_sysdttm);
                _generated.Add(_paidContainer);

            } 
            #endregion

            return _generated;
        }

    }
}
