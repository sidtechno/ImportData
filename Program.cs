using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using ImportData.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.TypeConversion;
using System.Text.RegularExpressions;

namespace ImportData
{
    class Program
    {
        const string garageName = "TEST VITRACC";
        const string connectionString = "Data Source=tcp:s8ch2o0eft.database.windows.net,1433;Initial Catalog=OCHPlanner2_Dev;User ID=mecanimax@s8ch2o0eft;Password=Mecan1m@x;Trusted_Connection=False;";

        static void Main(string[] args)
        {
            Console.WriteLine("Début de l'importation");
            var garageId = 5742;
            Console.WriteLine("Obtenir les clients du fichier csv");
            var clients = GetClients();
            Console.WriteLine("Obtenir les vehicules du fichier csv");
            var vehicles = GetVehicles();
            //Console.WriteLine("Obtenir les historique du fichier csv");
            var historiques = GetHistoriques();
            //var fix1 = GetHistoriquesFix1();

            //Console.WriteLine("Obtenir les produits du fichier csv");
            //var products = GetProducts();

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des clients et véhicule ***");
            Console.WriteLine("********************************************");

            ImportVehiculeClient(garageId, vehicles, clients);

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des historiques             ***");
            Console.WriteLine("********************************************");

            ImportHistoriques(garageId, historiques);

            //Console.WriteLine("********************************************");
            //Console.WriteLine("*** Importation des produits             ***");
            //Console.WriteLine("********************************************");

            //ImportProducts(products);

            Console.WriteLine("Fin de l'importation");

        }

        //public static void ImportProducts(IEnumerable<ProductModel> products)
        //{
        //    try
        //    {
        //        var sql = @"INSERT INTO [dbo].[Products] OUTPUT INSERTED.Id VALUES(@ProductNo, @Description, @CostPrice, @RetailPrice, @GarageId)";

        //        using (var connection = new SqlConnection("Data Source=tcp:s8ch2o0eft.database.windows.net,1433;Initial Catalog=OCHPlanner2;User ID=mecanimax@s8ch2o0eft;Password=Mecan1m@x;Trusted_Connection=False;"))
        //        {
        //            connection.Open();

        //            using (var transaction = connection.BeginTransaction())
        //            {
        //                foreach (var product in products)
        //                {
        //                    var result = connection.QuerySingle<int>(sql,
        //                    new
        //                    {
        //                        ProductNo = product.ProductNo,
        //                        Description = product.Description,
        //                        CostPrice = 0,
        //                        RetailPrice = 0,
        //                        GarageId = 5435
        //                    },
        //                    commandType: CommandType.Text,
        //                    transaction: transaction);
        //                }

        //                transaction.Commit();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Erreur lors du traitement");
        //        throw ex;
        //    }
        //}

        //private static WorkOrderDatabaseModel GetHistoVehicule(IEnumerable<HistoriqueModel> historique, IEnumerable<MaintenanceTypeModel> plans, int garageId)
        //{
        //    var wordOrderDetail = new StringBuilder();
        //    var wodList = new List<WorkOrderDetailDatabaseModel>();

        //    var result = new WorkOrderDatabaseModel()
        //    {
        //        CreateDate = historique.FirstOrDefault().Date,
        //        VinCode = historique.FirstOrDefault().VinCode,
        //        GarageId = garageId,
        //        Status = 2,
        //        Mileage = historique.FirstOrDefault().Odometer
        //    };

        //    historique.ToList().ForEach(h =>
        //    {
        //        //if maintenanceType not found, do not add
        //        var maintenanceTypeId = plans.FirstOrDefault(p => p.Code.Trim().ToUpper() == h.Code.Trim().ToUpper());

        //        if (maintenanceTypeId != null)
        //        {
        //            wodList.Add(new WorkOrderDetailDatabaseModel()
        //            {
        //                DateDone = h.Date,
        //                MaintenanceTypeId = maintenanceTypeId.Id,
        //                MileageDone = h.Odometer,
        //                WorkDone = true
        //            });
        //        }
        //    });

        //    wordOrderDetail.Append(JsonConvert.SerializeObject(wodList));
        //    result.WorkOrderDetail = wordOrderDetail.ToString();
        //    return result;
        //}

        public static void ImportVehiculeClient(int garageId, IEnumerable<VehicleModel> vehicles, IEnumerable<ClientModel> clients)
        {
            try
            {
                var sqlOwner = @"INSERT INTO [dbo].[VehicleOwner] ([Company], [Name], [Address], [Phone], [Email], [GarageId]) OUTPUT INSERTED.Id VALUES (@OwnerCompany, @OwnerName, @OwnerAddress, @OwnerPhone, @OwnerEmail, @GarageId)";
                var sqlVehicle = @"INSERT INTO [dbo].[Vehicle2] ([Vincode], [Description], [Year], [Make], [Model], [Engine], [Transmission], [Propulsion], [BrakeSystem], [Steering], [Color], [UnitNo], [LicencePlate], [Seating], [Odometer], [SelectedUnit], [EntryDate], [MonthlyMileage], [OilTypeId], [MaintenancePlanId], [VehicleOwnerId], [VehicleDriverId], [GarageId]) OUTPUT INSERTED.Id VALUES (@Vincode, @Description, @Year, @Make, @Model, @Engine, @Transmission, @Propulsion, @BrakeSystem, @Steering, @Color, @UnitNo, @LicencePlate, @Seating, @Odometer, @SelectedUnit, @EntryDate, @MonthlyMileage, @OilTypeId, @MaintenancePlanId, @VehicleOwnerId, @VehicleDriverId, @GarageId)";

                // Parallel processing for each client, each with its own SqlConnection and transaction
                Parallel.ForEach(clients, client =>
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                // Insert owner record
                                var ownerId = connection.QuerySingle<int>(sqlOwner, new
                                {
                                    OwnerCompany = client.Compagnie,
                                    OwnerName = client.Nom,
                                    OwnerAddress = client.Adresse,
                                    OwnerPhone = client.Phone,
                                    OwnerEmail = client.Email,
                                    GarageId = garageId
                                }, transaction);

                                // Fetch and process client vehicles
                                var clientVehicles = vehicles.Where(v => v.VehicleOwnerId.Equals(client.Id, StringComparison.InvariantCultureIgnoreCase)).ToList();

                                if (clientVehicles.Any())
                                {
                                    var vehicleData = new List<VehicleDatabaseModel>();

                                    foreach (var vehicle in clientVehicles)
                                    {
                                        vehicleData.Add(new VehicleDatabaseModel
                                        {
                                            VinCode = vehicle.VinCode,
                                            Description = vehicle.Description,
                                            Year = vehicle.Year,
                                            Make = vehicle.Make,
                                            Model = vehicle.Model,
                                            Engine = vehicle.Engine,
                                            Transmission = vehicle.Transmission,
                                            Propulsion = vehicle.Propulsion,
                                            BrakeSystem = vehicle.BrakeSystem,
                                            Steering = vehicle.Steering,
                                            Color = vehicle.Color,
                                            UnitNo = vehicle.UnitNo,
                                            LicencePlate = vehicle.Licence,
                                            Seating = vehicle.Seating,
                                            Odometer = vehicle.Odometer,
                                            SelectedUnit = vehicle.SelectedUnit,
                                            EntryDate = new DateTime(vehicle.Year, 6, 1).ToString("yyyy-MM-dd"),
                                            MonthlyMileage = vehicle.MonthlyMileage,
                                            OilTypeId = 0,
                                            MaintenancePlanId = 0,
                                            VehicleOwnerId = ownerId,
                                            VehicleDriverId = 0,
                                            GarageId = garageId
                                        });
                                    }

                                    // Insert data in bulk
                                    connection.Execute(sqlVehicle, vehicleData, transaction);
                                }

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                Console.WriteLine($"Error processing client {client.Nom}: {ex.Message}");
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du traitement: " + ex.Message);
                throw;
            }

        }

        private static void ImportHistoriques(int garageId, IEnumerable<HistoriqueModel> model)
        {
            try
            {
                var entretiens = model.Where(p => p.VinCode != string.Empty);

                // is the garage has at least one maintenance plan? if no, no need to import entretiens
                var maintenancePlans = GetMaintenancePlans(garageId);

                if (!maintenancePlans.Any()) { Console.WriteLine($"No maintenancePlan defined for this garage"); }
                if (maintenancePlans.Any()) //at least one maintenance plan in that garage
                {
                    //Get maintenance correspondance
                    var codeReferences = GetMaintenanceTypeReference(garageId);
                    var originCodes = codeReferences
                        .SelectMany(cr => cr.CodeOrigin.Split(';')) // Split the CodeOrigin by ';' and flatten the result
                        .Select(code => code.Trim()) // Trim any whitespace from the codes
                        .Distinct() // Optional: Remove duplicates if necessary
                        .ToList(); // Convert to list

                    // Create a dictionary to map each origin code to its respective OCHCode from codeReferences
                    var codeToOCHCodeMap = new Dictionary<string, string>();

                    foreach (var cr in codeReferences)
                    {
                        foreach (var origin in cr.CodeOrigin.Split(';').Select(code => code.Trim()))
                        {
                            if (!codeToOCHCodeMap.ContainsKey(origin))
                            {
                                codeToOCHCodeMap[origin] = cr.Code; // Assuming OCHCode is the property to be mapped
                            }
                        }
                    }

                    // Get only entretiens that correspond to a OriginCode
                    var filteredEntretiens = entretiens
                    .Where(e => codeToOCHCodeMap.ContainsKey(e.Code)) // Ensure the entretien code is in the map
                    .Select(e =>
                    {
                        e.OCHCode = codeToOCHCodeMap[e.Code]; // Set the OCHCode for each entretien
                        return e;
                    })
                    .OrderBy(o => o.VinCode)
                    .ToList();

                    var resultGroupedByVin = filteredEntretiens
                        // Group by both VinCode and Code
                        .GroupBy(e => new { e.VinCode, e.OCHCode })
                        .Select(g =>
                        {
                            // Find the entry with the maximum date for each group
                            var latestEntry = g.OrderByDescending(e => DateTime.Parse(e.Date)).ThenByDescending(e => e.Odometer).First();
                            return new
                            {
                                VinCode = g.Key.VinCode,
                                OCHCode = g.Key.OCHCode,
                                Date = latestEntry.Date,
                                Mileage = latestEntry.Odometer
                            };
                        })
                        // Group again by VinCode to consolidate into a single row per VinCode
                        .GroupBy(e => e.VinCode)
                        .Select(g =>
                        {
                            var details = g.Select(x => new { x.OCHCode, x.Date, x.Mileage }).ToList();
                            return new
                            {
                                VinCode = g.Key,
                                Details = details
                            };
                        })
                        .ToList();

                    var vehicleList = GetVehicleMaintenancePlans(garageId);

                    foreach (var g in resultGroupedByVin)
                    {
                        //Get MaintenancePlan for that VIN, if no maintenance plan, create new from initial
                        var maintenancePlan = vehicleList.FirstOrDefault(p => p.VinCode == g.VinCode);

                        if (maintenancePlan == null || maintenancePlan.MaintenancePlanId == 0)
                        {
                            //Console.WriteLine($"Create new maintenance plan from initial for VinCode: {g.VinCode}");
                            maintenancePlan = CreateInitialMaintenancePlan(g.VinCode.Trim(), garageId);
                        }

                        if (maintenancePlan != null)
                        {
                            var mpd = GetMaintenancePlanDetail(maintenancePlan.MaintenancePlanId);

                            foreach (var maintenanceCode in g.Details)
                            {

                                var mpdByCode = mpd.FirstOrDefault(p => p.Code == maintenanceCode.OCHCode);
                                if (mpdByCode != null)
                                {
                                    if (mpdByCode.LastServiceDate == null) { mpdByCode.LastServiceDate = DateTime.MinValue.ToShortDateString(); }

                                    // Convert string dates to DateTime
                                    if (DateTime.TryParse(mpdByCode.LastServiceDate, out DateTime lastServiceDateTime) &&
                                        DateTime.TryParse(maintenanceCode.Date, out DateTime newServiceDateTime))
                                    {
                                        // Compare DateTime objects
                                        if (newServiceDateTime > lastServiceDateTime)
                                        {
                                            //Console.WriteLine($"Update MaintenancePlanDetail for VinCode: {g.VinCode} , Maintenance Type: {maintenanceCode.OCHCode} with values: LastServiceDate = {maintenanceCode.Date}; LastServiceMileage = {maintenanceCode.Mileage}");

                                            mpdByCode.LastServiceDate = maintenanceCode.Date;
                                            mpdByCode.LastServiceMileage = Convert.ToInt32(maintenanceCode.Mileage);

                                            UpdateMaintenancePlanDetail(maintenancePlan.MaintenancePlanId, mpdByCode);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Failed to parse dates for comparison. lastServiceDateTime={mpdByCode.LastServiceDate}, newServiceDateTime={maintenanceCode.Date}");
                                    }
                                }

                            }
                        }
                    };
                }

                Console.WriteLine($"Import ended for Entretiens");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ImportEntretien Method: {ex.InnerException}");
                throw;
            }
        }

        private static IEnumerable<MaintenancePlanModel> GetMaintenancePlans(int garageId)
        {
            var sql = @"SELECT [Id]
                      ,[Name]
                      FROM [dbo].[MaintenancePlan2]
                      WHERE GarageId = @GarageId";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var result = connection.Query<MaintenancePlanModel>(sql,
                    new
                    {
                        GarageId = garageId
                    },
                    commandType: CommandType.Text);

                return result;
            }
        }

        private static void UpdateMaintenancePlanDetail(int maintenancePlanId, MaintenancePlanDetailModel maintenancePlanDetailModel)
        {
            try
            {

                //Insert MaintenancePlanDetail
                var sqlPlanDetailUpdate = @"UPDATE [dbo].[MaintenancePlanDetail2]
                           SET [LastServiceDate] = @LastServiceDate
                            ,[LastServiceMileage] = @LastServiceMileage
                           WHERE
                            [MaintenancePlanId] = @MaintenancePlanId
                           AND [MaintenanceTypeId] = @MaintenanceTypeId";

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        var maintenancePlanUpdated = connection.Execute(sqlPlanDetailUpdate,
                            new
                            {
                                LastServiceDate = string.IsNullOrWhiteSpace(maintenancePlanDetailModel.LastServiceDate) ? (string?)null : DateTime.Parse(maintenancePlanDetailModel.LastServiceDate).ToString("yyyy-MM-dd"),
                                LastServiceMileage = maintenancePlanDetailModel.LastServiceMileage,
                                MaintenancePlanId = maintenancePlanId,
                                MaintenanceTypeId = maintenancePlanDetailModel.MaintenanceTypeId
                            },
                            commandType: CommandType.Text,
                            transaction: transaction);

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static VehicleMaintenancePlanId? CreateInitialMaintenancePlan(string vinCode, int garageId)
        {
            try
            {
                var sql = "[web].[MaintenancePlan_Initial_Insert]";

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var result = connection.ExecuteScalar<int>(sql,
                        new
                        {
                            VinCode = vinCode,
                            GarageId = garageId,
                        },
                        commandType: CommandType.StoredProcedure);

                    var maintenancePlans = GetVehicleMaintenancePlans(garageId);


                    return maintenancePlans.FirstOrDefault(p => p.VinCode == vinCode);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static IEnumerable<MaintenanceTypeReferenceModel> GetMaintenanceTypeReference(int garageId)
        {
            var sql = @"SELECT MT.Id, MT.Code, MT.CodeOrigin, MT.Description
                    FROM MaintenancePlan2 MP
                    INNER JOIN MaintenancePlanDetail2 MPD
                    ON MPD.MaintenancePlanId = MP.Id
                    INNER JOIN MaintenanceType2 MT
                    ON MT.Id = MPD.MaintenanceTypeId
                    WHERE MP.GarageId = @GarageId
                    AND UPPER(MP.Name) = 'INITIAL'
                    AND MT.CodeOrigin IS NOT NULL";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var result = connection.Query<MaintenanceTypeReferenceModel>(sql,
                   new
                   {
                       GarageId = garageId
                   },
                   commandType: CommandType.Text);

                return result;
            }
        }

        private static IEnumerable<MaintenancePlanDetailModel> GetMaintenancePlanDetail(int maintenancePlanId)
        {
            var sql = @"SELECT TOP (1000) MPD.[Id]
                      ,MPD.[MaintenancePlanId]
                      ,MPD.[MaintenanceTypeId]
                      ,MPD.[Interval]
                      ,MPD.[Km]
                      ,MPD.[Miles]
                      ,MPD.[LastServiceDate]
                      ,MPD.[LastServiceMileage]
	                  ,MT.Code
                  FROM [dbo].[MaintenancePlanDetail2] MPD
                  INNER JOIN [dbo].[MaintenanceType2] MT
	                ON MT.Id = MPD.MaintenanceTypeId
                  WHERE [MaintenancePlanId] = @MaintenancePlanId";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var result = connection.Query<MaintenancePlanDetailModel>(sql,
                    new
                    {
                        MaintenancePlanId = maintenancePlanId
                    },
                    commandType: CommandType.Text);

                return result;
            }
        }
        private static IEnumerable<VehicleMaintenancePlanId> GetVehicleMaintenancePlans(int garageId)
        {
            var sql = @"SELECT [MaintenancePlanId], [VinCode]
                        FROM [dbo].[Vehicle2] 
                        WHERE [GarageId] = @GarageId";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var result = connection.Query<VehicleMaintenancePlanId>(sql,
                    new
                    {
                        GarageId = garageId
                    },
                    commandType: CommandType.Text);

                return result;
            }
        }

        private static IEnumerable<ProductModel> GetProducts()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\produits.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var vehicules = csv.GetRecords<ProductModel>();
                return vehicules.ToList();
            }
        }

        private static IEnumerable<VehicleModel> GetVehicles()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                DetectDelimiter = true,
                TrimOptions = TrimOptions.Trim,
                ShouldSkipRecord = record => string.IsNullOrWhiteSpace(record.Record[0]?.Trim()) || string.IsNullOrWhiteSpace(record.Record[1]?.Trim())
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\vehicules.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<VehicleMap>();
                var vehicules = csv.GetRecords<VehicleModel>();
                return vehicules.ToList();
            }
        }

        private static IEnumerable<HistoriqueModel> GetHistoriques()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                DetectDelimiter = true,
                TrimOptions = TrimOptions.Trim,
                ShouldSkipRecord = record => string.IsNullOrWhiteSpace(record.Record[0]?.Trim())
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\historique.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<TransactionLineMap>();
                var histo = csv.GetRecords<HistoriqueModel>();
                return histo.ToList();
            }
        }

        private static IEnumerable<HistoriqueFixModel> GetHistoriquesFix1()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                TrimOptions = TrimOptions.Trim
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\Histo fix1.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<TransactionFixLineMap>();
                var histo = csv.GetRecords<HistoriqueFixModel>();
                return histo.ToList();
            }
        }

        private static IEnumerable<ClientModel> GetClients()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                DetectDelimiter = true,
                TrimOptions = TrimOptions.Trim,
                ShouldSkipRecord = record => string.IsNullOrWhiteSpace(record.Record[0]?.Trim()) || string.IsNullOrWhiteSpace(record.Record[1]?.Trim())
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\Clients.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<ClientMap>();
                var clients = csv.GetRecords<ClientModel>();
                return clients.ToList();
            }
        }

    }

    internal class TransactionLineMap : ClassMap<HistoriqueModel>
    {
        public TransactionLineMap()
        {
            Map(m => m.Odometer).Name("Mileage");
            Map(m => m.VinCode);
            Map(m => m.Code);
            Map(m => m.Date).TypeConverter<CustomDateConverter>();
        }
    }

    internal class ClientMap : ClassMap<ClientModel>
    {
        public ClientMap()
        {
            Map(m => m.Id);
            Map(m => m.Nom);
            Map(m => m.Adresse).TypeConverter<CustomAddressConverter>();
            Map(m => m.Compagnie).Optional().Default(string.Empty);
            Map(m => m.Phone).TypeConverter<CustomPhoneConverter>().Optional().Default(string.Empty);
            Map(m => m.Email);
        }
    }

    internal class VehicleMap : ClassMap<VehicleModel>
    {
        public VehicleMap()
        {
            Map(m => m.VinCode);
            Map(m => m.Year).TypeConverter<IntMandatoryConverter>();
            Map(m => m.Make);
            Map(m => m.Model);
            Map(m => m.Engine);
            Map(m => m.Transmission);
            Map(m => m.Propulsion);
            Map(m => m.BrakeSystem);
            Map(m => m.Steering);
            Map(m => m.Color);
            Map(m => m.UnitNo);
            Map(m => m.Licence);
            Map(m => m.Seating).TypeConverter<IntMandatoryConverter>();
            Map(m => m.Odometer).TypeConverter<IntMandatoryConverter>();
            Map(m => m.SelectedUnit).Constant("KM");
            Map(m => m.MonthlyMileage).TypeConverter<IntMandatoryConverter>();
            Map(m => m.VehicleOwnerId);
        }
    }

    public class CustomPhoneConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var phone = text.Trim();
            return phone == "(   )    -" ? "" : phone;
        }
    }

    public class IntMandatoryConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            text = text.Trim();
            return string.IsNullOrWhiteSpace(text) ? 0 : int.Parse(text);
        }
    }

    public class CustomAddressConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) { return string.Empty; }
            var trimmedText = text.Trim();
            var regex = new Regex(@"^[\s,]*$");
            return regex.IsMatch(trimmedText) ? string.Empty : trimmedText;
        }
    }

    public class CustomDateConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (DateTime.TryParseExact(text, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date.ToString("yyyy-MM-dd");
            }
            return null;
        }
        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is DateTime date)
            {
                return date.ToString("yyyy-MM-dd");
            }
            return value?.ToString();
        }
    }

    internal class TransactionFixLineMap : ClassMap<HistoriqueFixModel>
    {
        public TransactionFixLineMap()
        {
            Map(m => m.Odometer).Name("Mileage");
            Map(m => m.VinCode);
            Map(m => m.Date)
                .TypeConverter<CsvHelper.TypeConversion.DateTimeConverter>()
                .TypeConverterOption.Format("dd-MM-yy");
        }
    }
}
