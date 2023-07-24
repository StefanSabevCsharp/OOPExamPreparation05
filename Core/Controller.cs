using EDriveRent.Core.Contracts;
using EDriveRent.Models;
using EDriveRent.Models.Contracts;
using EDriveRent.Repositories;
using EDriveRent.Utilities.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDriveRent.Core
{
    public class Controller : IController
    {
        private UserRepository userRepository;
        private VehicleRepository vehicleRepository;
        private RouteRepository routeRepository;
        public Controller()
        {
            userRepository = new UserRepository();
            vehicleRepository = new VehicleRepository();
            routeRepository = new RouteRepository();
        }
        public string RegisterUser(string firstName, string lastName, string drivingLicenseNumber)
        {
            IUser user = userRepository.FindById(drivingLicenseNumber);

            if (user != null)
            {
                return $"{string.Format(OutputMessages.UserWithSameLicenseAlreadyAdded, drivingLicenseNumber)}";
            }
            IUser newUser = new User(firstName, lastName, drivingLicenseNumber);
            userRepository.AddModel(newUser);

            return $"{string.Format(OutputMessages.UserSuccessfullyAdded, firstName, lastName, drivingLicenseNumber)}";
        }

        public string UploadVehicle(string vehicleType, string brand, string model, string licensePlateNumber)
        {
            if (vehicleType != nameof(PassengerCar) && vehicleType != nameof(CargoVan))
            {
                return $"{string.Format(OutputMessages.VehicleTypeNotAccessible, vehicleType)}";
            }
            IVehicle vehicle = vehicleRepository.FindById(licensePlateNumber);
            if (vehicle != null)
            {
                return $"{string.Format(OutputMessages.LicensePlateExists, licensePlateNumber)}";
            }
            IVehicle vehicleToAdd = null;
            if (vehicleType == nameof(PassengerCar))
            {
                vehicleToAdd = new PassengerCar(brand, model, licensePlateNumber);

            }
            else if (vehicleType == nameof(CargoVan))
            {
                vehicleToAdd = new CargoVan(brand, model, licensePlateNumber);
            }
            vehicleRepository.AddModel(vehicleToAdd);

            return $"{string.Format(OutputMessages.VehicleAddedSuccessfully, brand, model, licensePlateNumber)}";
        }

        public string AllowRoute(string startPoint, string endPoint, double length)
        {
            IRoute route = routeRepository.GetAll().FirstOrDefault(r => r.StartPoint == startPoint && r.EndPoint == endPoint && r.Length == length);
            if (route != null)
            {
                return $"{string.Format(OutputMessages.RouteExisting, startPoint, endPoint, length)}";
            }
            IRoute route2 = routeRepository.GetAll().FirstOrDefault(r => r.StartPoint == startPoint && r.EndPoint == endPoint && r.Length <= length);
            if (route2 != null)
            {
                return $"{string.Format(OutputMessages.RouteIsTooLong, startPoint, endPoint, length)}";
            }
            int totalRoutes = routeRepository.GetAll().Count;

            IRoute newRoute = new Route(startPoint, endPoint, length, totalRoutes + 1);
            routeRepository.AddModel(newRoute);

            IRoute longerRoute = routeRepository.GetAll().FirstOrDefault(r => r.StartPoint == startPoint && r.EndPoint == endPoint && r.Length > length);

            if (longerRoute != null)
            {
                longerRoute.LockRoute();

            }
                return $"{string.Format(OutputMessages.NewRouteAdded, startPoint, endPoint, length)}";

        }

        public string MakeTrip(string drivingLicenseNumber, string licensePlateNumber, string routeId, bool isAccidentHappened)
        {
            IUser user = userRepository.FindById(drivingLicenseNumber);
            if (user.IsBlocked)
            {
                return $"{string.Format(OutputMessages.UserBlocked,drivingLicenseNumber)}";
            }
            IVehicle vehicle = vehicleRepository.FindById(licensePlateNumber);
            if (vehicle.IsDamaged)
            {
                return $"{string.Format(OutputMessages.VehicleDamaged, licensePlateNumber)}";
            }
            IRoute route = routeRepository.FindById(routeId);
            if (route.IsLocked)
            {
                return $"{string.Format(OutputMessages.RouteLocked, routeId)}";
            }
            vehicle.Drive(route.Length);
            if(isAccidentHappened)
            {
                vehicle.ChangeStatus();
                user.DecreaseRating();
            }
            else
            {
                user.IncreaseRating();
            }
            return $"{vehicle}";
        }



        public string RepairVehicles(int count)
        {
            IEnumerable<IVehicle> damagedVehicles = vehicleRepository.GetAll().Where(v => v.IsDamaged)
                .OrderBy(v => v.Brand)
                .OrderBy(v => v.Model)
                .Take(count);
            int countOfDamagedVehicles = damagedVehicles.Count();
            foreach(IVehicle vehicle in damagedVehicles)
            {
                vehicle.ChangeStatus();
                vehicle.Recharge();
            }
            return $"{string.Format(OutputMessages.RepairedVehicles, countOfDamagedVehicles)}";
        }



        public string UsersReport()
        {
            IEnumerable<IUser> users = userRepository.GetAll()
                .OrderByDescending(x => x.Rating)
                .OrderBy(x => x.LastName)
                .OrderBy(x => x.FirstName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("*** E-Drive-Rent ***");

            foreach(IUser user in users)
            {
                sb.AppendLine(user.ToString());
            }
            return sb.ToString().TrimEnd();
                
        }
    }
}
