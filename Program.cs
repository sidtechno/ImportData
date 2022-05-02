using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using ImportData.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ImportData
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Début de l'importation");

            Console.WriteLine("Obtenir les clients du fichier csv");
            var clients = GetClients();
            Console.WriteLine("Obtenir les vehicules du fichier csv");
            var vehicles = GetVehicles();
            //Console.WriteLine("Obtenir les produits du fichier csv");
            //var products = GetProducts();

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des clients et véhicule ***");
            Console.WriteLine("********************************************");

            ImportVehiculeClient(vehicles, clients);

            Console.WriteLine("********************************************");
            Console.WriteLine("*** Importation des produits             ***");
            Console.WriteLine("********************************************");

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

        public static void ImportVehiculeClient(IEnumerable<VehicleModel> vehicles, IEnumerable<ClientModel> clients)
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
                            //insert owner
                            var OwnerInserted = connection.QuerySingle<int>(sqlOwner,
                                new
                                {
                                    OwnerCompany = client.Compagnie,
                                    OwnerName = client.Nom,
                                    OwnerAddress = client.Adresse,
                                    OwnerPhone = client.Phone,
                                    OwnerEmail = client.Email,
                                    GarageId = 5496

                                },
                                commandType: CommandType.Text,
                                transaction: transaction);

                            Console.WriteLine($"Traitement du client {client.Nom}");

                            //Get vehicules for client
                            var clientVehicule = vehicles.Where(p => p.VehicleOwnerId.Equals(client.Id, StringComparison.InvariantCultureIgnoreCase)).ToList();
                            if (clientVehicule.Any())
                            {
                                var vehlist = new List<VehicleDatabaseModel>();

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
                                        GarageId = 5496
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

        private static IEnumerable<ProductModel> GetProducts()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };

            using (var reader = new StreamReader("C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\PMSA\\produits.csv"))
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

            using (var reader = new StreamReader("C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\PMSA\\vehicule.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var vehicules = csv.GetRecords<VehicleModel>();
                return vehicules.ToList();
            }
        }

        private static IEnumerable<ClientModel> GetClients()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };

            using (var reader = new StreamReader("C:\\Projects\\GSOLPRO\\OchPlanner3-Importation\\PMSA\\Clients.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var clients = csv.GetRecords<ClientModel>();
                return clients.ToList();
            }
        }

    }
}
