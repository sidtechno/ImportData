using System;

namespace ImportData.Model
{
    public class WorkOrderDatabaseModel
    {
        public int Id { get; set; }
        public string VinCode { get; set; }
        public DateTime CreateDate { get; set; }
        public int Mileage { get; set; }
        public int Status { get; set; }
        public string WorkOrderDetail { get; set; }
        public int GarageId { get; set; }
    }

    public class WorkOrderDetailDatabaseModel
    {
        public int MaintenanceTypeId { get; set; }
        public bool WorkDone { get; set; }
        public DateTime DateDone { get; set; }
        public int MileageDone { get; set; }
    }
}
