using System.Collections.Generic;

namespace ImportData.Model
{
    public class MaintenancePlanModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GarageId { get; set; }
        public IEnumerable<MaintenancePlanDetailModel> MaintenancePlanDetailList { get; set; }
    }
}
