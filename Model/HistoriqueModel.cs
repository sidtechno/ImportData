using System;

namespace ImportData.Model
{
    public class HistoriqueModel
    {
        public string VinCode { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public int Odometer { get; set; }

    }

    public class HistoriqueFixModel
    {
        public string VinCode { get; set; }
        public DateTime Date { get; set; }
        public int Odometer { get; set; }

    }
}
