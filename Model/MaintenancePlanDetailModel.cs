using System;
using System.Text;

namespace ImportData.Model
{

    public class MaintenancePlanDetailModel
    {
        public string Code { get; set; }
        public int MaintenancePlanId { get; set; }
        public int MaintenanceTypeId { get; set; }
        public string Interval { get; set; }
        public int Km { get; set; }
        public int Miles { get; set; }
        public string LastServiceDate { get; set; }
        public int LastServiceMileage { get; set; }
    }

    public class MaintenancePlanDetailUpdateModel
    {
        public int MaintenancePlanId { get; set; } // ID of the maintenance plan to be updated
        public string Code { get; set; } // Maintenance code (OCHCode)
        public string LastServiceDate { get; set; } // New last service date in string format
        public int LastServiceMileage { get; set; } // New last service mileage
    }
}
