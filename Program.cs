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

namespace ImportData
{
    class Program
    {
        const string garageName = "Chartrand St-Jean";

        static void Main(string[] args)
        {
            Console.WriteLine("Début de l'importation");
            var garageId = 5669;
            Console.WriteLine("Obtenir les clients du fichier csv");
            var clients = GetClients();
            Console.WriteLine("Obtenir les vehicules du fichier csv");
            var vehicles = GetVehicles();
            //Console.WriteLine("Obtenir les historique du fichier csv");
           // var historiques = GetHistoriques();
            //var fix1 = GetHistoriquesFix1();

            //Console.WriteLine("Obtenir les produits du fichier csv");
            //var products = GetProducts();

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des clients et véhicule ***");
            Console.WriteLine("********************************************");

            ImportVehiculeClient(garageId, vehicles, clients);
            //ImportVehiculeClientWithMaintenance(garageId, 5631, vehicles, clients, historiques);

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des historiques             ***");
            Console.WriteLine("********************************************");

             //ImportHistoriques(garageId, historiques);

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

        private static WorkOrderDatabaseModel GetHistoVehicule(IEnumerable<HistoriqueModel> historique, IEnumerable<MaintenanceTypeModel> plans, int garageId)
        {
            var wordOrderDetail = new StringBuilder();
            var wodList = new List<WorkOrderDetailDatabaseModel>();

            var result = new WorkOrderDatabaseModel()
            {
                CreateDate = historique.FirstOrDefault().Date,
                VinCode = historique.FirstOrDefault().VinCode,
                GarageId = garageId,
                Status = 2,
                Mileage = historique.FirstOrDefault().Odometer
            };

            historique.ToList().ForEach(h =>
            {
                //if maintenanceType not found, do not add
                var maintenanceTypeId = plans.FirstOrDefault(p => p.Code.Trim().ToUpper() == h.Type.Trim().ToUpper());

                if (maintenanceTypeId != null)
                {
                    wodList.Add(new WorkOrderDetailDatabaseModel()
                    {
                        DateDone = h.Date,
                        MaintenanceTypeId = maintenanceTypeId.Id,
                        MileageDone = h.Odometer,
                        WorkDone = true
                    });
                }
            });

            wordOrderDetail.Append(JsonConvert.SerializeObject(wodList));
            result.WorkOrderDetail = wordOrderDetail.ToString();
            return result;
        }

        public static void ImportVehiculeClient(int garageId, IEnumerable<VehicleModel> vehicles, IEnumerable<ClientModel> clients)
        {
            try
            {
                var sqlOwner = @"INSERT INTO [dbo].[VehicleOwner]
                                ([Company] 
                                ,[Name]
                                ,[Address]
                                ,[Phone] 
                                ,[Email]
                                ,[GarageId])
                                OUTPUT INSERTED.Id
                                VALUES
                                (@OwnerCompany
                                , @OwnerName
                                , @OwnerAddress
                                , @OwnerPhone
                                , @OwnerEmail
                                , @GarageId)";


                var sql = @"INSERT INTO [dbo].[Vehicle2]
		               ([Vincode]
                      ,[Description]
                      ,[Year]
                      ,[Make]
                      ,[Model]
                      ,[Engine]
                      ,[Transmission]
                      ,[Propulsion]
                      ,[BrakeSystem]
                      ,[Steering]
                      ,[Color]
                      ,[UnitNo]
                      ,[LicencePlate]
                      ,[Seating]
                      ,[Odometer]
                      ,[SelectedUnit]
                      ,[EntryDate]
                      ,[MonthlyMileage]
                      ,[OilTypeId]
                      ,[MaintenancePlanId]
                      ,[VehicleOwnerId]
                      ,[VehicleDriverId]
                      ,[GarageId])
                    OUTPUT INSERTED.Id
	                VALUES(
		                 @Vincode
                        ,@Description
                        ,@Year
                        ,@Make
                        ,@Model
                        ,@Engine
                        ,@Transmission
                        ,@Propulsion
                        ,@BrakeSystem
                        ,@Steering
                        ,@Color
                        ,@UnitNo
                        ,@LicencePlate
                        ,@Seating
                        ,@Odometer
                        ,@SelectedUnit
                        ,@EntryDate
                        ,@MonthlyMileage
                        ,@OilTypeId
                        ,@MaintenancePlanId
                        ,@VehicleOwnerId
                        ,@VehicleDriverId
                        ,@GarageId)";



                using (var connection = new SqlConnection("Data Source=tcp:s8ch2o0eft.database.windows.net,1433;Initial Catalog=OCHPlanner2;User ID=mecanimax@s8ch2o0eft;Password=Mecan1m@x;Trusted_Connection=False;"))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var client in clients)
                        {
                            // Check if client.Phone starts with "+1"
                            if (client.Phone.StartsWith("+1"))
                            {
                                // Remove the "+1" prefix
                                client.Phone = client.Phone.Substring(2);
                            }

                            //insert owner
                            var OwnerInserted = connection.QuerySingle<int>(sqlOwner,
                                new
                                {
                                    OwnerCompany = client.Compagnie,
                                    OwnerName = client.Nom,
                                    OwnerAddress = client.Adresse,
                                    OwnerPhone =  client.Phone,
                                    OwnerEmail = client.Email,
                                    GarageId = garageId

                                },
                                commandType: CommandType.Text,
                                transaction: transaction);

                            Console.WriteLine($"Traitement du client {client.Nom}");

                            //Get vehicules for client
                            var clientVehicule = vehicles.Where(p => p.VehicleOwnerId.Equals(client.Id, StringComparison.InvariantCultureIgnoreCase)).ToList();
                            if (clientVehicule.Any())
                            {
                                var vehlist = new List<VehicleDatabaseModel>();
                                var historyList = new List<WorkOrderDatabaseModel>();

                                clientVehicule.ForEach(p =>
                                {
                                    Console.WriteLine($"Traitement du vehicule {p.Description}");

                                    vehlist.Add(new VehicleDatabaseModel()
                                    {
                                        VinCode = p.VinCode.Length > 20 ? p.VinCode.Substring(0, 20) : p.VinCode,
                                        Description = p.Description.Length > 200 ? p.Description.Substring(0, 200) : p.Description,
                                        Year = p.Year,
                                        Make = p.Make.Length > 75 ? p.Make.Substring(0, 75) : p.Make,
                                        Model = p.Model.Length > 75 ? p.Model.Substring(0, 75) : p.Model,
                                        Engine = p.Engine.Length > 75 ? p.Engine.Substring(0, 75) : p.Engine,
                                        Propulsion = p.Propulsion.Length > 75 ? p.Propulsion.Substring(0, 75) : p.Propulsion,
                                        Transmission = p.Transmission.Length > 75 ? p.Transmission.Substring(0, 75) : p.Transmission,
                                        BrakeSystem = p.BrakeSystem.Length > 75 ? p.BrakeSystem.Substring(0, 75) : p.BrakeSystem,
                                        Steering = p.Steering.Length > 75 ? p.Steering.Substring(0, 75) : p.Steering,
                                        Color = p.Color.Length > 75 ? p.Color.Substring(0, 75) : p.Color,
                                        UnitNo = p.UnitNo.Length > 25 ? p.UnitNo.Substring(0, 25) : p.UnitNo,
                                        LicencePlate = p.Licence.Length > 12 ? p.Licence.Substring(0, 12) : p.Licence,
                                        Seating = p.Seating,
                                        Odometer = p.Odometer,
                                        SelectedUnit = p.SelectedUnit.Length > 2 ? p.SelectedUnit.Substring(0, 2) : p.SelectedUnit,
                                        EntryDate = new DateTime(p.Year, 6, 1).ToString("yyyy-MM-dd"),
                                        MonthlyMileage = p.MonthlyMileage,
                                        OilTypeId = 0,
                                        MaintenancePlanId = 0,
                                        VehicleOwnerId = OwnerInserted,
                                        VehicleDriverId = 0,
                                        GarageId = garageId
                                    });

                                });

                                var affectedRows = connection.Execute(sql, vehlist, transaction: transaction);
                            }

                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du traitement");
                throw ex;
            }
        }

        public static void ImportVehiculeClientWithMaintenance(int garageId, int baseMaintenancePlanId, IEnumerable<VehicleModel> vehicles, IEnumerable<ClientModel> clients, IEnumerable<HistoriqueModel> historiques, IEnumerable<HistoriqueFixModel>? fix1 = null)
        {
            try
            {
                if (fix1 == null)
                    fix1 = new List<HistoriqueFixModel>();

                var sqlPlanType = "SELECT [Id],[Code] FROM [dbo].[MaintenanceType2] where GarageId = @GarageId";

                var sqlMaintenanceDetailPlanBase = @"SELECT MP.[Id]
	                    ,MT2.[Code]
                        ,MP.[MaintenancePlanId]
                        ,MP.[MaintenanceTypeId]
                        ,MP.[Interval]
                        ,MP.[Km]
                        ,MP.[Miles]
                        ,MP.[LastServiceDate]
                        ,MP.[LastServiceMileage]
                    FROM [dbo].[MaintenancePlanDetail2] MP
                    INNER JOIN MaintenanceType2 MT2
                    ON MT2.Id = MP.MaintenanceTypeId
                    WHERE MaintenancePlanId = @baseMPID";

                var sqlCreateMaintenancePlan = @"INSERT INTO [dbo].[MaintenancePlan2]
                            ([Name]
                            ,[GarageId]
                            ,[IsTemplate])
                        OUTPUT INSERTED.Id
                        VALUES
                            (@Name
                            ,@GarageId
                            ,@IsTemplate)";

                var sqlCreateMaintenancePlanDetail = @"INSERT INTO [dbo].[MaintenancePlanDetail2]
                       ([MaintenancePlanId]
                       ,[MaintenanceTypeId]
                       ,[Interval]
                       ,[Km]
                       ,[Miles]
                       ,[LastServiceDate]
                       ,[LastServiceMileage])
                 VALUES
                       (@MaintenancePlanId
                       ,@MaintenanceTypeId
                       ,@Interval
                       ,@Km
                       ,@Miles
                       ,@LastServiceDate
                       ,@LastServiceMileage)";

                var sqlWorkOrder = @"INSERT INTO [dbo].[WorkOrder]
                           ([CreateDate]
                           ,[Vincode]
                           ,[Mileage]
                           ,[Status]
                           ,[WorkOrderDetail]
                           ,[GarageId])
                     VALUES
                           (@CreateDate
                           ,@Vincode
                           ,@Mileage
                           ,@Status
                           ,@WorkOrderDetail
                           ,@GarageId)";

                var sqlOwner = @"INSERT INTO [dbo].[VehicleOwner]
                                ([Company] 
                                ,[Name]
                                ,[Address]
                                ,[Phone] 
                                ,[Email]
                                ,[GarageId])
                                OUTPUT INSERTED.Id
                                VALUES
                                (@OwnerCompany
                                , @OwnerName
                                , @OwnerAddress
                                , @OwnerPhone
                                , @OwnerEmail
                                , @GarageId)";


                var sql = @"INSERT INTO [dbo].[Vehicle2]
		               ([Vincode]
                      ,[Description]
                      ,[Year]
                      ,[Make]
                      ,[Model]
                      ,[Engine]
                      ,[Transmission]
                      ,[Propulsion]
                      ,[BrakeSystem]
                      ,[Steering]
                      ,[Color]
                      ,[UnitNo]
                      ,[LicencePlate]
                      ,[Seating]
                      ,[Odometer]
                      ,[SelectedUnit]
                      ,[EntryDate]
                      ,[MonthlyMileage]
                      ,[OilTypeId]
                      ,[MaintenancePlanId]
                      ,[VehicleOwnerId]
                      ,[VehicleDriverId]
                      ,[GarageId])
                    OUTPUT INSERTED.Id
	                VALUES(
		                 @Vincode
                        ,@Description
                        ,@Year
                        ,@Make
                        ,@Model
                        ,@Engine
                        ,@Transmission
                        ,@Propulsion
                        ,@BrakeSystem
                        ,@Steering
                        ,@Color
                        ,@UnitNo
                        ,@LicencePlate
                        ,@Seating
                        ,@Odometer
                        ,@SelectedUnit
                        ,@EntryDate
                        ,@MonthlyMileage
                        ,@OilTypeId
                        ,@MaintenancePlanId
                        ,@VehicleOwnerId
                        ,@VehicleDriverId
                        ,@GarageId)";



                using (var connection = new SqlConnection("Data Source=tcp:s8ch2o0eft.database.windows.net,1433;Initial Catalog=OCHPlanner2;User ID=mecanimax@s8ch2o0eft;Password=Mecan1m@x;Trusted_Connection=False;"))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        var plans = connection.Query<MaintenanceTypeModel>(sqlPlanType, new { GarageId = garageId }, commandType: CommandType.Text, transaction: transaction);
                        var basePlanDetail = connection.Query<MaintenancePlanDetailModel>(sqlMaintenanceDetailPlanBase, new { baseMPID = baseMaintenancePlanId }, commandType: CommandType.Text, transaction: transaction);

                        foreach (var client in clients)
                        {
                            //insert owner
                            var OwnerInserted = connection.QuerySingle<int>(sqlOwner,
                                new
                                {
                                    OwnerCompany = client.Compagnie,
                                    OwnerName = client.Nom,
                                    OwnerAddress = client.Adresse,
                                    OwnerPhone = client.Phone,
                                    OwnerEmail = client.Email,
                                    GarageId = garageId

                                },
                                commandType: CommandType.Text,
                                transaction: transaction);

                            Console.WriteLine($"Traitement du client {client.Nom}");

                            //Get vehicules for client
                            var clientVehicule = vehicles.Where(p => p.VehicleOwnerId.Equals(client.Id, StringComparison.InvariantCultureIgnoreCase)).ToList();
                            if (clientVehicule.Any())
                            {
                                var vehlist = new List<VehicleDatabaseModel>();
                                var historyList = new List<WorkOrderDatabaseModel>();

                                clientVehicule.ForEach(p =>
                                {

                                    Console.WriteLine($"Traitement des historiques du vehicule {p.Description}");
                                    var vehHisto = historiques.Where(h => h.VinCode == p.VinCode);
                                    var maintenancePlanInserted = 0;

                                    if (vehHisto.Any())
                                    {
                                        //insert maintenance plan
                                        maintenancePlanInserted = connection.QuerySingle<int>(sqlCreateMaintenancePlan,
                                            new
                                            {
                                                Name = vehHisto.FirstOrDefault().VinCode,
                                                GarageId = garageId,
                                                IsTemplate = 0

                                            },
                                            commandType: CommandType.Text,
                                            transaction: transaction);

                                        //Group by Date
                                        var histoGrouped = vehHisto.GroupBy(p => p.Date.Date);
                                        var maintenanceDetailToInsert = new List<MaintenancePlanDetailModel>();

                                        histoGrouped.ToList().ForEach(hist =>
                                        {
                                            historyList.Add(GetHistoVehicule(hist, plans, garageId));
                                        });

                                        basePlanDetail.ToList().ForEach(basem =>
                                        {
                                            var mpdm = new MaintenancePlanDetailModel()
                                            {
                                                MaintenancePlanId = maintenancePlanInserted,
                                                MaintenanceTypeId = basem.MaintenanceTypeId,
                                                Interval = basem.Interval,
                                                Km = basem.Km,
                                                Miles = basem.Miles
                                            };

                                            if (vehHisto.Any(h => h.Type == basem.Code))
                                            {
                                                var histSingle = vehHisto.First(d => d.Type == basem.Code);
                                                mpdm.LastServiceDate = histSingle.Date.ToString("yyyy-MM-dd");
                                                mpdm.LastServiceMileage = histSingle.Odometer;
                                            }

                                            maintenanceDetailToInsert.Add(mpdm);
                                        });

                                        var affectedMPD = connection.Execute(sqlCreateMaintenancePlanDetail, maintenanceDetailToInsert, transaction: transaction);
                                    }

                                    Console.WriteLine($"Traitement du vehicule {p.Description}");

                                    vehlist.Add(new VehicleDatabaseModel()
                                    {
                                        VinCode = p.VinCode.Length > 20 ? p.VinCode.Substring(0, 20) : p.VinCode,
                                        Description = p.Description.Length > 200 ? p.Description.Substring(0, 200) : p.Description,
                                        Year = p.Year,
                                        Make = p.Make.Length > 75 ? p.Make.Substring(0, 75) : p.Make,
                                        Model = p.Model.Length > 75 ? p.Model.Substring(0, 75) : p.Model,
                                        Engine = p.Engine.Length > 75 ? p.Engine.Substring(0, 75) : p.Engine,
                                        Propulsion = p.Propulsion.Length > 75 ? p.Propulsion.Substring(0, 75) : p.Propulsion,
                                        Transmission = p.Transmission.Length > 75 ? p.Transmission.Substring(0, 75) : p.Transmission,
                                        BrakeSystem = p.BrakeSystem.Length > 75 ? p.BrakeSystem.Substring(0, 75) : p.BrakeSystem,
                                        Steering = p.Steering.Length > 75 ? p.Steering.Substring(0, 75) : p.Steering,
                                        Color = p.Color.Length > 75 ? p.Color.Substring(0, 75) : p.Color,
                                        UnitNo = p.UnitNo.Length > 25 ? p.UnitNo.Substring(0, 25) : p.UnitNo,
                                        LicencePlate = p.Licence.Length > 12 ? p.Licence.Substring(0, 12) : p.Licence,
                                        Seating = p.Seating,
                                        Odometer = fix1.Any(f => f.VinCode == p.VinCode) ? fix1.First(f => f.VinCode == p.VinCode).Odometer : p.Odometer,
                                        SelectedUnit = p.SelectedUnit.Length > 2 ? p.SelectedUnit.Substring(0, 2) : p.SelectedUnit,
                                        EntryDate = new DateTime(p.Year, 6, 1).ToString("yyyy-MM-dd"),
                                        MonthlyMileage = p.MonthlyMileage,
                                        OilTypeId = 0,
                                        MaintenancePlanId = maintenancePlanInserted,
                                        VehicleOwnerId = OwnerInserted,
                                        VehicleDriverId = 0,
                                        GarageId = garageId
                                    });

                                });

                                var affectedRows = connection.Execute(sql, vehlist, transaction: transaction);
                                var affectedWORows = connection.Execute(sqlWorkOrder, historyList, transaction: transaction);
                            }

                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du traitement");
                throw ex;
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
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\vehicles.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var vehicules = csv.GetRecords<VehicleModel>();
                return vehicules.ToList();
            }
        }

        private static IEnumerable<HistoriqueModel> GetHistoriques()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                TrimOptions = TrimOptions.Trim
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\Maintenance2.csv"))
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
            };

            using (var reader = new StreamReader($"C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\{garageName}\\Clients.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var clients = csv.GetRecords<ClientModel>();
                return clients.ToList();
            }
        }

    }

    internal class TransactionLineMap : ClassMap<HistoriqueModel>
    {
        public TransactionLineMap()
        {
            Map(m => m.Odometer);
            Map(m => m.VinCode);
            Map(m => m.Type);
            Map(m => m.Date)
                .TypeConverter<CsvHelper.TypeConversion.DateTimeConverter>()
                .TypeConverterOption.Format("dd-MM-yy");
        }
    }

    internal class TransactionFixLineMap : ClassMap<HistoriqueFixModel>
    {
        public TransactionFixLineMap()
        {
            Map(m => m.Odometer);
            Map(m => m.VinCode);
            Map(m => m.Date)
                .TypeConverter<CsvHelper.TypeConversion.DateTimeConverter>()
                .TypeConverterOption.Format("dd-MM-yy");
        }
    }
}
