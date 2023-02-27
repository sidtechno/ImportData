using System.Collections.Generic;
using System.Text;

namespace ImportData.Model
{
    public class VehicleModel
    {
        public string VinCode { get; set; }
        public int Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Engine { get; set; }
        public string Transmission { get; set; }
        public string Propulsion { get; set; }
        public string BrakeSystem { get; set; }
        public string Steering { get; set; }
        public string Color { get; set; }
        public string UnitNo { get; set; }
        public string Licence { get; set; }
        public int Seating { get; set; }
        public int Odometer { get; set; }
        public string SelectedUnit { get; set; }
        public int MonthlyMileage { get; set; }
        public string VehicleOwnerId { get; set; }

        public string Description
        {
            get
            {
                if (Seating == 0)
                {
                    return $"{Year} " +
                            $"{Make} " +
                            $"{Model} " +
                            $"{Engine} " +
                            $"{Transmission} " +
                            $"{Propulsion} " +
                            $"{BrakeSystem} " +
                            $"{Steering}".Trim();
                }
                else
                {
                    return $"{Year} " +
                        $"{Make} " +
                        $"{Model} " +
                        $"{Engine} " +
                        $"{Transmission} " +
                        $"{Propulsion} " +
                        $"{BrakeSystem} " +
                        $"{Steering} " +
                        $"{Seating}".Trim();
                }
            }
        }
    }
}
