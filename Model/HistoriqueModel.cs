using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;

namespace ImportData.Model
{
    public class HistoriqueModel
    {
        public string VinCode { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public int Odometer { get; set; }
        [Ignore]
        public string OCHCode { get; set; }
    }

    public class ImportHistoriqueModel
    {
        public int garageId { get; set; }
        public IEnumerable<HistoriqueModel> entretiens { get; set; }
    }

    public class HistoriqueFixModel
    {
        public string VinCode { get; set; }
        public DateTime Date { get; set; }
        public int Odometer { get; set; }

    }
}
