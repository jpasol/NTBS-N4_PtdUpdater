using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using ClosedXML;
using System.Threading;
using System.Threading.Tasks;
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
            Parallel.ForEach(yard_Containers, (yardContainer) =>
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
                                                       && yardContainer.Category == paid.Category && 
                                                       (
                                                       (yardContainer.Category.StartsWith("IMPRT") && yardContainer.ATA <= paid.SystemDate)
                                                       || (yardContainer.Category.StartsWith("EXPRT") &&  yardContainer.TimeIn <= paid.SystemDate)));
                         payment = list.Count();
                         paidthruDate = list.FirstOrDefault().StorageEnd.ToString();
                         plugin = list.FirstOrDefault().PlugIn.ToString();
                         plugout = list.FirstOrDefault().PlugOut.ToString();
                         
                         if(payment > 0)
                         {
                             if (yardContainer.LastFreeDay.Value.Year < 2000) yardContainer.LastFreeDay = list.FirstOrDefault().FreeUntil; //get LFD from GPS if none
                             yardContainer.IsArrastrePaid = true; //arrastre paid
                         }
                         

                     }
                     catch { }

                         //if freeuntil != null then gatepass payment ; if null then it's manual invoiced
                         //return freeuntil(gatepass) or ldd+9(manual) 
                         //yardContainer.PaidThruDate = yardContainer.LastFreeDay;

                         if (Convert.ToDateTime(yardContainer.LastFreeDay).Year < 2000) //no recorded lfd in N4
                         {
                             yardContainer.LastFreeDay = yardContainer.Category == "IMPRT" && Convert.ToDateTime(yardContainer.LDD).Year > 2000 ?
                                 Convert.ToDateTime(yardContainer.LDD).AddDays(9)  //free day = (ldd+9) iMPORT
                                 : yardContainer.Category == "EXPRT"? Convert.ToDateTime(yardContainer.TimeIn).AddDays(9) //EXPORT T_IN+9
/*                                 : yardContainer.Category == "STRGE"? Convert.ToDateTime(yardContainer.TimeIn).AddDays(4)*/ //STRGE T_IN+4
                                 : Convert.ToDateTime("1970-01-01 00:00:00");

                             //yardContainer.PaidThruDate =
                             //    !string.IsNullOrEmpty(paidthruDate) ? Convert.ToDateTime(paidthruDate)
                             //    :
                             //    yardContainer.LastFreeDay;
                         }

                         //return plugin
                         yardContainer.PlugIn =
                              !string.IsNullOrEmpty(plugin) ? Convert.ToDateTime(plugin) : //IMPRT
                              yardContainer.TimeIn; //assuming timein = plugin

                         //return plugout
                         yardContainer.PlugOut =
                                  !string.IsNullOrEmpty(plugout) ? Convert.ToDateTime(plugout) : //IMPRT
                              Convert.ToDateTime("1970-01-01 00:00:00");


                 }
                 catch { }

             });
            #endregion


            //EXTEND using Paidthruday manual invoice TIMEIN <= SYSDTTM

            #region EXTEND using Paidthruday special services TIMEIN <= SYSDTTM
            Parallel.ForEach(yard_Containers, (yardContainer) =>
             {
                 try
                 {//try extending each container using the first recorded payment within specified conditions; using its quantity as added days 
                                         
                     yardContainer.PaidThruDate =
                          yardContainer.Category.StartsWith("IMPRT") ?
                          Convert.ToDateTime(yardContainer.LastFreeDay).
                          AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                          && (yardContainer.ATA <= ext.SystemDate)
                                                                          && (ext.ChargeType.StartsWith("STOI"))).Sum(val => val.Quantity))
                          : yardContainer.Category.StartsWith("EXPRT") ?
                          Convert.ToDateTime(yardContainer.TimeIn).
                          AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                          && (yardContainer.TimeIn <= ext.SystemDate)
                                                          && (ext.ChargeType.StartsWith("STOE"))).Sum(val => val.Quantity))
                         : Convert.ToDateTime("1970-01-01 00:00:00");

                     yardContainer.PlugOut =
                         yardContainer.Category.StartsWith("IMPRT") ?
                         Convert.ToDateTime(yardContainer.PlugIn).
                         AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                         && (yardContainer.ATA <= ext.SystemDate)
                                                                         && (("MCRFC1,MCRFC6").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                         : yardContainer.Category.StartsWith("EXPRT") ?
                         Convert.ToDateTime(yardContainer.TimeIn).
                         AddDays(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                         && (yardContainer.TimeIn <= ext.SystemDate)
                                                                         && (("MCRFC1,MCRFC6").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                         : Convert.ToDateTime("1970-01-01 00:00:00");

                     yardContainer.PlugOut =
                         yardContainer.Category.StartsWith("IMPRT") ?
                         Convert.ToDateTime(yardContainer.PlugIn).
                         AddHours(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                         && (yardContainer.ATA <= ext.SystemDate)
                                                                         && (("MCRFC2,MCRFC3").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                         : yardContainer.Category.StartsWith("EXPRT") ?
                         Convert.ToDateTime(yardContainer.TimeIn).
                         AddHours(ext_Containers.Where(ext => (yardContainer.ContainerNumber.Trim() == ext.ContainerNumber.Trim())
                                                                         && (yardContainer.TimeIn <= ext.SystemDate)
                                                                         && (("MCRFC2,MCRFC3").Contains(ext.ChargeType))).Sum(val => val.Quantity))
                         : Convert.ToDateTime("1970-01-01 00:00:00");

                     //remove if not extended
                     if (yardContainer.PaidThruDate == yardContainer.LastFreeDay) yardContainer.PaidThruDate = Convert.ToDateTime("1970-01-01 00:00:00");
                     if (yardContainer.PaidThruDate == yardContainer.TimeIn) yardContainer.PaidThruDate = Convert.ToDateTime("1970-01-01 00:00:00");
                     if (yardContainer.PlugOut == yardContainer.TimeIn) yardContainer.PlugOut = Convert.ToDateTime("1970-01-01 00:00:00");

                 }
                 catch { }
                 
             });
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
                    IsArrastrePaid: yardContainer.IsArrastrePaid,
                    LastFreeDay : Convert.ToDateTime(yardContainer.LastFreeDay),
                    LastDischargeDate : Convert.ToDateTime(yardContainer.LDD),
                    isReefer: yardContainer.IsReefer);
                    

                yardContainer.UpdateN4Unit();
            }



            ClosedXML.Excel.XLWorkbook wb = new ClosedXML.Excel.XLWorkbook();
            wb.Worksheets.Add(output, "WorksheetName");
            wb.SaveAs("output.xlsx");
            #endregion






        }
    }
}
