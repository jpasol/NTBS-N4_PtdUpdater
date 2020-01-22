using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Reports;
using System.Data;

namespace Paid2Date.Model
{
    class Extended_Container
    {    
        public string ContainerNumber;
        public string Remarks;
        public int DocReference;
        public int Quantity;
        public string ChargeType;
        public DateTime? SystemDate;


        public static ObservableCollection<Extended_Container> GetExtended_Containers()
        {
            ObservableCollection<Extended_Container> _extContainers;
            ADODB.Connection BLConnection = new Connections().BLConnection;
            //Connect
            BLConnection.Open();

            //Retrieve
            ADODB.Command retrieveCommand = new ADODB.Command();
            retrieveCommand.ActiveConnection = BLConnection;
            retrieveCommand.CommandText = $@"
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
  FROM [billing].[dbo].[CCRdtl] where status <> 'CAN' and (chargetyp like '%MCRFC%' or chargetyp like '%STOI%' or chargetyp like '%STOE%')
";
            System.Data.DataTable _EXTContainersTable = new System.Data.DataTable();
            System.Data.OleDb.OleDbDataAdapter adapter = new System.Data.OleDb.OleDbDataAdapter();
            adapter.Fill(_EXTContainersTable, retrieveCommand.Execute(out object dsadsad, 0, 0));


            //Convert datatable to observable collection
            _extContainers = Generate(_EXTContainersTable);

            //return 
            return _extContainers;


        }

        private static ObservableCollection<Extended_Container> Generate(DataTable RetrivedExtContainers)
        {
            ObservableCollection<Extended_Container> _generated = new ObservableCollection<Extended_Container>();

            foreach (DataRow dr in RetrivedExtContainers.Rows)
            {
                Extended_Container _extContainer = new Extended_Container();
                _extContainer.ContainerNumber = dr["cntnum"].ToString().Trim();
                _extContainer.Remarks = dr["remark"].ToString().Trim();
                _extContainer.DocReference = Convert.ToInt32(Double.Parse(dr["docrefno"].ToString().Trim()));
                _extContainer.Quantity = Convert.ToInt32(Double.Parse(dr["quantity"].ToString()));
                _extContainer.ChargeType = dr["chargetyp"].ToString().Trim();
                string date = dr["sysdttm"].ToString();
                _extContainer.SystemDate = string.IsNullOrEmpty(date) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(date);

                _generated.Add(_extContainer);
            }


            return _generated;
        }

    }
}
