using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Dapper;
using ImportData.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImportData
{
    class Program
    {
        const string garageName = "AD Leblanc2";
        const string connectionString = "Data Source=tcp:s8ch2o0eft.database.windows.net,1433;Initial Catalog=OCHPlanner2;User ID=mecanimax@s8ch2o0eft;Password=Mecan1m@x;Trusted_Connection=False;";

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.WriteLine("Début de l'importation");
            var garageId = 5853;
            Console.WriteLine("Obtenir les clients du fichier csv");
            var clients = GetClients();
            Console.WriteLine("Obtenir les vehicules du fichier csv");
            var vehicles = GetVehicles();
            Console.WriteLine("Obtenir les historique du fichier csv");
            //var historiques = GetHistoriques();
            //var fix1 = GetHistoriquesFix1();

            //Console.WriteLine("Obtenir les produits du fichier csv");
            //var products = GetProducts();

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des clients              ***");
            Console.WriteLine("********************************************");

            ImportOwners(garageId, clients, 250);
            Console.WriteLine("Fin de l'importation des clients");

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des véhicules            ***");
            Console.WriteLine("********************************************");
            ImportVehicles(garageId, vehicles, 75);
            Console.WriteLine("Fin de l'importation des véhicules");

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des historiques             ***");
            Console.WriteLine("********************************************");

            //ImportHistoriques(garageId, historiques, vehicles);
            Console.WriteLine("Fin de l'importation des historiques");

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

        public static void ImportOwners(int garageId, IEnumerable<ClientModel> clients, int batchSize)
        {
            var sqlTemplate = @"
                MERGE [dbo].[VehicleOwner] AS target
                USING (
                    VALUES {0} -- Batch values go here
                ) AS source (Name, GarageId, Import_Id, Company, Address, Phone, Email)
                ON (target.GarageId = source.GarageId AND target.Import_Id = source.Import_Id) --target.Name = source.Name AND
                WHEN MATCHED THEN
                    UPDATE SET
                        Name = source.Name,
                        Company = source.Company,
                        Address = source.Address,
                        Phone = source.Phone,
                        Email = source.Email
                WHEN NOT MATCHED THEN
                    INSERT (Company, Name, Address, Phone, Email, GarageId, Import_Id)
                    VALUES (source.Company, source.Name, source.Address, source.Phone, source.Email, source.GarageId, source.Import_Id);";

            var ids = new List<int>();
            var batches = SplitIntoBatches(clients, batchSize);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var batchNo = 1;
                        foreach (var batch in batches)
                        {
                            Console.WriteLine($"traitement de la batch #{batchNo++}");

                            // Dynamically build the VALUES clause for the batch
                            var valuesClause = string.Join(", ", batch.Select((o, i) =>
                                $"(@Name{i}, @GarageId{i}, @ImportId{i}, @Company{i}, @Address{i}, @Phone{i}, @Email{i})"));

                            var batchSql = string.Format(sqlTemplate, valuesClause);

                            // Add parameters for the batch
                            var parameters = new DynamicParameters();
                            for (int i = 0; i < batch.Count; i++)
                            {
                                parameters.Add($"@Name{i}", batch[i].Nom);
                                parameters.Add($"@GarageId{i}", garageId);
                                parameters.Add($"@ImportId{i}", batch[i].Id);
                                parameters.Add($"@Company{i}", batch[i].Compagnie);
                                parameters.Add($"@Address{i}", batch[i].Adresse);
                                parameters.Add($"@Phone{i}", batch[i].Phone);
                                parameters.Add($"@Email{i}", batch[i].Email);
                            }

                            // Execute the batch
                            connection.Query<int>(batchSql, parameters, transaction);
                        }

                        transaction.Commit(); // Commit any remaining changes
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static void ImportVehicles(int garageId, IEnumerable<VehicleModel> vehicles, int batchSize)
        {
            // Step 1: Fetch VehicleOwnerId mappings
            var vehicleOwnerMappings = GetVehicleOwnerIdMappings(garageId);

            // Step 2: Resolve VehicleOwnerId using Import_Id
            foreach (var vehicle in vehicles)
            {
                if (vehicleOwnerMappings.TryGetValue(vehicle.VehicleOwnerId.ToString(), out var resolvedId))
                {
                    vehicle.VehicleOwnerId = resolvedId.ToString(); // Replace with resolved Id
                }
                else
                {
                    vehicle.VehicleOwnerId = null; // Or handle missing mapping case
                }
            }

            vehicles = vehicles.Where(p => p.VehicleOwnerId != null);

            // Step 3: Perform the batching and MERGE
            var sqlTemplate = @"
                MERGE [dbo].[Vehicle2] AS target
                USING (
                    VALUES {0} -- Batch values go here
                ) AS source (
                    Vincode, Description, Year, Make, Model, Engine, Transmission, Propulsion,
                    BrakeSystem, Steering, Color, UnitNo, LicencePlate, Seating, Odometer,
                    SelectedUnit, EntryDate, MonthlyMileage, OilTypeId, MaintenancePlanId,
                    VehicleOwnerId, VehicleDriverId, GarageId
                )
                ON target.Vincode = source.Vincode AND target.GarageId = source.GarageId
                WHEN MATCHED THEN
                    UPDATE SET
                        [Description] = source.Description,
                        [Year] = source.Year,
                        [Make] = source.Make,
                        [Model] = source.Model,
                        [Engine] = source.Engine,
                        [Transmission] = source.Transmission,
                        [Propulsion] = source.Propulsion,
                        [BrakeSystem] = source.BrakeSystem,
                        [Steering] = source.Steering,
                        [Color] = source.Color,
                        [UnitNo] = source.UnitNo,
                        [LicencePlate] = source.LicencePlate,
                        [Seating] = source.Seating,
                        [Odometer] = source.Odometer,
                        [EntryDate] = source.EntryDate,
                        [MonthlyMileage] = source.MonthlyMileage,
                        [VehicleOwnerId] = source.VehicleOwnerId,
                        [VehicleDriverId] = source.VehicleDriverId
                WHEN NOT MATCHED THEN
                    INSERT (
                        [Vincode], [Description], [Year], [Make], [Model], [Engine], [Transmission],
                        [Propulsion], [BrakeSystem], [Steering], [Color], [UnitNo], [LicencePlate],
                        [Seating], [Odometer], [SelectedUnit], [EntryDate], [MonthlyMileage],
                        [OilTypeId], [MaintenancePlanId], [VehicleOwnerId], [VehicleDriverId], [GarageId]
                    )
                    VALUES (
                        source.Vincode, source.Description, source.Year, source.Make, source.Model,
                        source.Engine, source.Transmission, source.Propulsion, source.BrakeSystem,
                        source.Steering, source.Color, source.UnitNo, source.LicencePlate, source.Seating,
                        source.Odometer, source.SelectedUnit, source.EntryDate, source.MonthlyMileage,
                        source.OilTypeId, source.MaintenancePlanId, source.VehicleOwnerId, source.VehicleDriverId,
                        source.GarageId
                    )
                OUTPUT INSERTED.Vincode;";

            var vincodes = new List<string>();
            var batches = SplitIntoBatches(vehicles, batchSize);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var batchNo = 1;
                        foreach (var batch in batches)
                        {
                            Console.WriteLine($"traitement de la batch #{batchNo++}");

                            // Dynamically build the VALUES clause for the batch
                            var valuesClause = string.Join(", ", batch.Select((v, i) =>
                                $"(@Vincode{i}, @Description{i}, @Year{i}, @Make{i}, @Model{i}, @Engine{i}, @Transmission{i}, @Propulsion{i}, " +
                                $"@BrakeSystem{i}, @Steering{i}, @Color{i}, @UnitNo{i}, @LicencePlate{i}, @Seating{i}, @Odometer{i}, @SelectedUnit{i}, " +
                                $"@EntryDate{i}, @MonthlyMileage{i}, @OilTypeId{i}, @MaintenancePlanId{i}, @VehicleOwnerId{i}, @VehicleDriverId{i}, @GarageId{i})"));

                            var batchSql = string.Format(sqlTemplate, valuesClause);

                            // Add parameters for the batch
                            var parameters = new DynamicParameters();
                            for (int i = 0; i < batch.Count; i++)
                            {
                                parameters.Add($"@Vincode{i}", batch[i].VinCode.Length > 20 ? batch[i].VinCode.Substring(0, 20) : batch[i].VinCode); 
                                parameters.Add($"@Description{i}", batch[i].Description.Length > 200 ? batch[i].Description.Substring(200) : batch[i].Description);
                                parameters.Add($"@Year{i}", batch[i].Year);
                                parameters.Add($"@Make{i}", batch[i].Make.Length > 75 ? batch[i].Make.Substring(75) : batch[i].Make);
                                parameters.Add($"@Model{i}", batch[i].Model.Length > 75 ? batch[i].Model.Substring(75) : batch[i].Model);
                                parameters.Add($"@Engine{i}", batch[i].Engine.Length > 75 ? batch[i].Engine.Substring(75) : batch[i].Engine);
                                parameters.Add($"@Transmission{i}", batch[i].Transmission.Length > 75 ? batch[i].Transmission.Substring(75) : batch[i].Transmission);
                                parameters.Add($"@Propulsion{i}", batch[i].Propulsion.Length > 75 ? batch[i].Propulsion.Substring(75) : batch[i].Propulsion);
                                parameters.Add($"@BrakeSystem{i}", batch[i].BrakeSystem.Length > 75 ? batch[i].BrakeSystem.Substring(75) : batch[i].BrakeSystem);
                                parameters.Add($"@Steering{i}", batch[i].Steering.Length > 75 ? batch[i].Steering.Substring(75) : batch[i].Steering);
                                parameters.Add($"@Color{i}", batch[i].Color.Length > 75 ? batch[i].Color.Substring(75) : batch[i].Color);
                                parameters.Add($"@UnitNo{i}", batch[i].UnitNo.Length > 25 ? batch[i].UnitNo.Substring(0, 25) : batch[i].UnitNo);
                                parameters.Add($"@LicencePlate{i}", batch[i].Licence.Length > 12 ? batch[i].Licence.Substring(0, 25) : batch[i].Licence);
                                parameters.Add($"@Seating{i}", batch[i].Seating);
                                parameters.Add($"@Odometer{i}", batch[i].Odometer);
                                parameters.Add($"@SelectedUnit{i}", batch[i].SelectedUnit);
                                parameters.Add($"@EntryDate{i}", new DateTime(batch[i].Year, 6, 1).ToString("yyyy-MM-dd"));
                                parameters.Add($"@MonthlyMileage{i}", batch[i].MonthlyMileage);
                                parameters.Add($"@OilTypeId{i}", 0);
                                parameters.Add($"@MaintenancePlanId{i}", 0);
                                parameters.Add($"@VehicleOwnerId{i}", batch[i].VehicleOwnerId);
                                parameters.Add($"@VehicleDriverId{i}", 0);
                                parameters.Add($"@GarageId{i}", garageId);
                            }

                            // Execute the batch
                            connection.Query<string>(batchSql, parameters, transaction);
                        }

                        transaction.Commit();
                    }
                    catch(Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private static IEnumerable<List<T>> SplitIntoBatches<T>(IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }


       
        private static Dictionary<string, int> GetVehicleOwnerIdMappings(int garageId)
        {
            var query = $"SELECT Import_Id, Id FROM VehicleOwner WHERE GarageId = {garageId} AND Import_Id IS NOT NULL";
            using (var connection = new SqlConnection(connectionString))
            {
                var result = connection.Query<(string ImportId, int Id)>(query);
                return result.ToDictionary(x => x.ImportId, x => x.Id);
            }
        }
               
        private static void ImportHistoriques(int garageId, IEnumerable<HistoriqueModel> model, IEnumerable<VehicleModel> vehicles)
        {
            try
            {
                var entretiens = model.Where(p => p.VinCode != string.Empty);

                //Get inserted vehicules for this garage
                var insertedVehicles = GetInsertedVehicles(garageId).Select(p => p.VinCode).ToList();

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
                                       
                    var vinCodes = vehicles
                        .Where(p => insertedVehicles.Contains(p.VinCode) &&
                            !string.IsNullOrWhiteSpace(p.VehicleOwnerId?.Trim()) &&
                            !string.IsNullOrWhiteSpace(p.VinCode)).Select(p => p.VinCode)
                        .ToList();

                    //var tt = vinCodes.Where(p => p == "JF1GPAA67CG241719");
                    filteredEntretiens = filteredEntretiens.Where(p => vinCodes.Contains(p.VinCode)).ToList();
                     
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

                    var counter = 0;
                    foreach (var g in resultGroupedByVin)
                    {
                        counter++;

                        if(counter % 100 == 0) { Console.WriteLine($"traitement de la batch #{counter}"); }

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
                                    string lastServiceDate = mpdByCode.LastServiceDate;

                                    if (DateTime.TryParse(lastServiceDate, out DateTime lastServiceDateTime) &&
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

        private static IEnumerable<VehicleModel> GetInsertedVehicles(int garageId)
        {
            var sql = @"SELECT *
                      FROM [dbo].[Vehicle2]
                      WHERE GarageId = @GarageId";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var result = connection.Query<VehicleModel>(sql,
                    new
                    {
                        GarageId = garageId
                    },
                    commandType: CommandType.Text);

                return result;
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

            using (var reader = new StreamReader(
                   $"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\vehicules.csv",
                   Encoding.GetEncoding("Windows-1252")))

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

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\historiques.csv"))
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

            using (var reader = new StreamReader(
                   $"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\Clients.csv",
                   Encoding.GetEncoding("Windows-1252"))) 

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
            else if(DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date2))
            {
                return date2.ToString("yyyy-MM-dd");
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
