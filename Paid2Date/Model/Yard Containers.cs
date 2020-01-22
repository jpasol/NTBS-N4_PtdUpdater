using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Reports;
using System.Data;

namespace Paid2Date.Model
{
    class Yard_Container
    {
        public int Gkey;
        public string ContainerNumber;
        public string Category;
        public string FreightKind;
        public string TransitState;
        public string BillofLading;
        public Boolean IsArrastrePaid;
        public Boolean IsReefer;
        public DateTime? PlugIn;
        public DateTime? PlugOut;
        public DateTime? PaidThruDate;
        public DateTime? LDD;
        public DateTime? TimeIn;
        public DateTime? ATA;
        public DateTime? ETD;
        public DateTime? LastFreeDay;

        public void UpdateN4Unit()
        {
                string Storage = PaidThruDate.Value.Year > 2000 ? $@"'{PaidThruDate.ToString()}'" : "null";
                string Electricity = PlugOut.Value.Year > 2000  ? $@"'{PlugOut.ToString()}'" : "null";
                string arrastrePayment = IsArrastrePaid ? $@"'{DateTime.Now.ToString()}'" : "null";
                //Storage = PaidThruDate.Value.Year == LastFreeDay.Value.Year &&
                //            PaidThruDate.Value.Month == LastFreeDay.Value.Month &&
                //            PaidThruDate.Value.Day == LastFreeDay.Value.Day ? "null" : Storage;
             
                //PaidThruDate = Storage == "null" ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.ParseExact(Storage.Replace("'",""), "M/d/yyyy h:mm:ss tt", null); //update values
                //PlugOut = Electricity == "null" ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.ParseExact(Electricity.Replace("'", ""), "M/d/yyyy h:mm:ss tt", null); //update values
                //LastFreeDay = arrastrePayment == "null" ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.ParseExact(arrastrePayment.Replace("'", ""), "M/d/yyyy h:mm:ss tt", null); //update values


            ADODB.Connection DEVN4Connection = new Connections().DEVN4Connection;
                //Connect
                DEVN4Connection.Open();

                //Update
                ADODB.Command updateCommand = new ADODB.Command();
                updateCommand.ActiveConnection = DEVN4Connection;
                updateCommand.CommandText = $@"
UPDATE [apex].[dbo].[inv_unit_fcy_visit]
   SET [flex_date01] = {Storage}
	    ,[flex_date02] = {Electricity}
        ,[flex_date03] = {arrastrePayment}
 WHERE unit_gkey = {Gkey}
";
                updateCommand.Execute(out object dsad, 0, 0);
                DEVN4Connection.Close();
        }


        public static ObservableCollection<Yard_Container> GetYardContainer()
        {
            ObservableCollection<Yard_Container> _yardContainers;
            ADODB.Connection N4Connection = new Connections().N4Connection;
            //Connect
            N4Connection.Open();

            //Retrieve
            ADODB.Command retrieveCommand = new ADODB.Command();
            retrieveCommand.ActiveConnection = N4Connection;
            retrieveCommand.CommandText = $@"
SELECT iu.id 'Container Number'
,category 'Category'
,freight_kind 'Freight Kind'
,transit_state 'Transit State'
,time_in
,time_discharge_complete
,iu.gkey
,last_free_day
,requires_power
,ata

FROM [apex].[dbo].inv_unit iu 
inner join inv_unit_fcy_visit iufv on iufv.unit_gkey = iu.gkey
left join argo_carrier_visit acv on iufv.actual_ib_cv = acv.gkey
left join argo_visit_details avd on acv.cvcvd_gkey = avd.gkey

where transit_state like '%YARD%'
";
            System.Data.DataTable _yardContainersTable = new System.Data.DataTable();
            System.Data.OleDb.OleDbDataAdapter adapter = new System.Data.OleDb.OleDbDataAdapter();
            adapter.Fill(_yardContainersTable, retrieveCommand.Execute(out object dsadsad, 0, 0));


            //Convert datatable to observable collection
            _yardContainers = Generate(_yardContainersTable);

            //return 
            return _yardContainers;


        }

        private static ObservableCollection<Yard_Container> Generate(DataTable RetrivedYardContainers)
        {
            ObservableCollection<Yard_Container> _generated = new ObservableCollection<Yard_Container>();

            foreach (DataRow dr in RetrivedYardContainers.Rows)
            {
                Yard_Container _yardContainer = new Yard_Container();
                _yardContainer.ContainerNumber = dr["Container Number"].ToString().Trim();
                _yardContainer.Category = dr["Category"].ToString().Trim();
                _yardContainer.FreightKind = dr["Freight Kind"].ToString().Trim();
                _yardContainer.TransitState = dr["Transit State"].ToString().Trim();
                _yardContainer.Gkey = Convert.ToInt32(dr["Gkey"].ToString().Trim());
                _yardContainer.IsReefer = Convert.ToBoolean(dr["requires_power"].ToString().Trim());

                string ldd = dr["time_discharge_complete"].ToString().Trim();
                _yardContainer.LDD = string.IsNullOrEmpty(ldd) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(ldd);

                string date = dr["time_in"].ToString().Trim();
                _yardContainer.TimeIn = string.IsNullOrEmpty(date) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(date);

                string lfd = dr["last_free_day"].ToString().Trim();
                _yardContainer.LastFreeDay = string.IsNullOrEmpty(lfd) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(lfd);

                string ata = dr["ata"].ToString().Trim();
                _yardContainer.ATA = string.IsNullOrEmpty(ata) ? Convert.ToDateTime("1970-01-01 00:00:00") : DateTime.Parse(ata);

                _yardContainer.PaidThruDate = Convert.ToDateTime("1970-01-01 00:00:00");
                _yardContainer.PlugOut = Convert.ToDateTime("1970-01-01 00:00:00");
                

                _generated.Add(_yardContainer);
            }  


            return _generated;
        }

    }
}
