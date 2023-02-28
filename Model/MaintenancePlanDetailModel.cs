using System;
using System.Collections.Generic;
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
}
