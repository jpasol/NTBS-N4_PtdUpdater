using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using ClosedXML;
namespace Paid2Date
{
    class Program
    {
        static void Main(string[] args)
        {
            ObservableCollection<Model.Yard_Container> yard_Containers = Model.Yard_Container.GetYardContainer();

            //generate first pass import
            //generate first pass special services
            ObservableCollection<Model.Paid_Container> paid_Containers = Model.Paid_Container.GetPaidContainer();
            ObservableCollection<Model.Extended_Container> ext_Containers = Model.Extended_Container.GetExtended_Containers();

            //update Paidthruday of each yardcontainer using paid containers
            #region update Paidthruday of each yardcontainer using paid containers
            foreach (Model.Yard_Container yardContainer in yard_Containers)
            {
                try
                {
                    int payment = 0;
                    string paidthruDate = null;
                    string plugin = null;
                    string plugout = null;
                    try
                    {//try to get number of recorded payment, and recorded freeuntil
                        IEnumerable<Model.Paid_Container> list = paid_Containers.Where(paid => (yardContainer.ContainerNumber.Trim() == paid.ContainerNumber.Trim())
                                                     && (
                                                     (yardContainer.ATA <= paid.SystemDate)
                                                     || (yardContainer.TimeIn <= paid.SystemDate)));
                        payment = list.Count();
                        paidthruDate = list.FirstOrDefault().StorageEnd.ToString();
                        plugin = list.FirstOrDefault().PlugIn.ToString();
                        plugout = list.FirstOrDefault().PlugOut.ToString();

                    }
                    catch { }

                    if (payment > 0)                  //paid
                    {

                        //if freeuntil != null then gatepass payment ; if null then it's manual invoiced
                        //return freeuntil(gatepass) or ldd+9(manual)
                        yardContainer.IsArrastrePaid = true;
                        yardContainer.PaidThruDate = yardContainer.LastFreeDay;

                        if (Convert.ToDateTime(yardContainer.LastFreeDay).Year < 2000) //no recorded lfd in N4
                        {
                            yardContainer.PaidThruDate =
                                !string.IsNullOrEmpty(paidthruDate) ? Convert.ToDateTime(paidthruDate)
                                :
                                yardContainer.Category == "IMPRT" && Convert.ToDateTime(yardContainer.LDD).Year > 2000 ?
                                Convert.ToDateTime(yardContainer.LDD).AddDays(9)  //paid thru date = free until (ldd+9)
                                : Convert.ToDateTime(yardContainer.TimeIn).AddDays(9);
                        }

                        //return plugin
                        yardContainer.PlugIn =
                                !string.IsNullOrEmpty(plugin) ? Convert.ToDateTime(plugin) :
                                yardContainer.Category == "IMPRT" ? Convert.ToDateTime("1970-01-01 00:00:00")
                                : yardContainer.TimeIn;

                        //return plugout
                        yardContainer.PlugOut =
                                !string.IsNullOrEmpty(plugout) ? Convert.ToDateTime(plugout) :
                                yardContainer.Category == "IMPRT" ? Convert.ToDateTime("1970-01-01 00:00:00")
                                : yardContainer.TimeIn;
                    }

                }
                catch { }

            }
            #endregion


            //EXTEND using Paidthruday special services TIMEIN <= SYSDTTM

            #region EXTEND using Paidthruday special services TIMEIN <= SYSDTTM
            foreach (Model.Yard_Container yardContainer in yard_Containers.Where(ctn => ctn.IsArrastrePaid == true))
            {
                try
                {//try extending each container using the first recorded payment within specified conditions; using its quantity as added days 
                    yardContainer.PaidThruDate =
                        yardContainer.Category == "IMPRT" ?
                        Convert.ToDateTime(yardContainer.PaidThruDate).
                        AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                        && (yardContainer.ATA <= ext.SystemDate)
                                                                        && (ext.ChargeType.Contains("STOI"))).Sum(val => val.Quantity))
                        :
                        Convert.ToDateTime(yardContainer.TimeIn).
                        AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                        && (yardContainer.TimeIn <= ext.SystemDate)
                                                        && (ext.ChargeType.Contains("STOEX"))).Sum(val => val.Quantity));

                    yardContainer.PlugOut =
                        yardContainer.Category == "IMPRT"?
                        Convert.ToDateTime(yardContainer.PlugOut).
                        AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                        && (yardContainer.ATA <= ext.SystemDate)
                                                                        && (("MCRFC1,MCRFC6").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                        :
                        Convert.ToDateTime(yardContainer.PlugOut).
                        AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                        && (yardContainer.TimeIn <= ext.SystemDate)
                                                                        && (("MCRFC1,MCRFC6").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                        ;

                    yardContainer.PlugOut =
                        yardContainer.Category == "IMPRT" ?
                        Convert.ToDateTime(yardContainer.PlugOut).
                        AddHours(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                        && (yardContainer.ATA <= ext.SystemDate)
                                                                        && (("MCRFC2,MCRFC3").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                        :
                        Convert.ToDateTime(yardContainer.PlugOut).
                        AddHours(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                        && (yardContainer.TimeIn <= ext.SystemDate)
                                                                        && (("MCRFC2,MCRFC3").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                        ;
                }
                catch { }
            }
            #endregion

            //EXTEND using Paidthruday special services blnum remark
            //none yet


            //output csv

            #region output csv
            Output.Output_ReportDataTable output = new Output.Output_ReportDataTable();
            foreach (Model.Yard_Container yardContainer in yard_Containers)
            {
                output.AddOutput_ReportRow(Container_Number: yardContainer.ContainerNumber,
                    Category: yardContainer.Category,
                    Transit_State: yardContainer.TransitState,
                    Paid_Through_Date: Convert.ToDateTime(yardContainer.PaidThruDate),
                    Time_In: Convert.ToDateTime(yardContainer.TimeIn),
                    Plugout: Convert.ToDateTime(yardContainer.PlugOut),
                    IsArrastrePaid: yardContainer.IsArrastrePaid);

                yardContainer.UpdateN4Unit();
            }



            ClosedXML.Excel.XLWorkbook wb = new ClosedXML.Excel.XLWorkbook();
            wb.Worksheets.Add(output, "WorksheetName");
            wb.SaveAs("output.xlsx");
            #endregion






        }
    }
}
