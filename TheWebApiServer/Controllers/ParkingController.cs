using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheWebApiServer.Data;
using TheWebApiServer.Model;
using TheWebApiServer.Requests;

namespace TheWebApiServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ParkingController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private static readonly object _lock = new object();
        public ParkingController(DataContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("UserAddCar")]
        [Authorize]
        public async Task<IActionResult> UserAddCar([FromBody] AddUserCar model)
        {
            var car=await _context.cars
                .Where(x=>x.Registration==model.Registration)
                .FirstOrDefaultAsync();
            if (car != null)
            {
                return StatusCode(201, "This registration is already assigned to another car");
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            _context.cars.Add(new Cars
            {
                Registration = model.Registration,
                CarBrand=model.CarBrand,
                CarModel=model.CarModel,
                UserId = (await _userManager.GetUserAsync(HttpContext.User)).Id
            });
            _context.SaveChanges();
            return Ok("Car succesfully added");
        }

        [HttpGet("GetUserCars")]
        [Authorize]
        public async Task<IActionResult> GetUserCars()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userCars = await _context.cars
                .Where(x => x.UserId == user.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Registration,
                    x.CarBrand,
                    x.CarModel

                })
                .ToListAsync();
                
            return Ok(userCars);
        }
       
        [HttpDelete("DeleteUserCar")]
        [Authorize]
        public async Task<IActionResult> DeleteUserCar(DeleteUserCar model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            
            Cars car=await _context.cars.FirstOrDefaultAsync(x=>x.Id==model.carId && x.UserId==user.Id);
            if (car == null)
            {
                return NotFound("car not found");
            }
            _context.cars.Remove(car);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("UpdateUserCar")]
        [Authorize]
        public async Task<IActionResult> UpdateUserCar(UpdateUserCars model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var car = await _context.cars.FirstOrDefaultAsync(x => x.Id == model.CarId && x.UserId == user.Id);
            if (car == null)
            {
                return NotFound("Samochód użytkownika nie został znaleziony.");
            }

            car.CarBrand = model.CarBrand;
            car.CarModel = model.CarModel;
            car.Registration = model.CarRegistration;

            await _context.SaveChangesAsync();
            return Ok();
        }


        [HttpGet("GetTicket")]
        [Authorize]
        public async Task<IActionResult> GetTicket([FromQuery]int carId)
        {
            //sprawdzac czy ten samochod jest juz na parkingu



            var user = await _userManager.GetUserAsync(HttpContext.User);
            Cars car = await _context.cars.FirstOrDefaultAsync(x => x.Id == carId && x.UserId == user.Id);
            if (car == null)
            {
                return NotFound("Samochód użytkownika nie został znaleziony.");
            }
            lock (_lock) 
            {
                var parkingPlaces = _context.parkingPlace.ToList();
                foreach (var parkingPlace in parkingPlaces)
                {
                    var IsContested = _context.occupiedParkingPlace.Any(x => x.ParkingPlace.Id == parkingPlace.Id && x.LeaveTime == null);
                    if (!IsContested)
                    {
                        var curDate = DateTime.Now;
                        _context.occupiedParkingPlace.Add(new OccupiedParkingPlace()
                        {
                            OccupiedTime = curDate,
                            ParkingPlaceId = parkingPlace.Id,
                            CarId = car.Id,
                            LeaveTime = null
                        });
                        _context.SaveChanges();
                        var formattedDate = curDate.ToString("dd.MM.yyyy HH:mm:ss");
                        return Ok(new { 
                            PlaceId = parkingPlace.Id,
                            OccupiedTime= formattedDate,
                            RegistrationNumber=car.Registration
                        });
                    }
                }
            }
            return NotFound("nie ma miejsca");
        }


        [HttpPost("LeavePlace")]
        [Authorize]
        public async Task<IActionResult> LeavePlace([FromBody] LeavePlaceByCar model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var carId =await _context.cars
                .Where(x=>x.Registration==model.RegistrationNumber && x.User==user)
                .Select(x=>x.Id)
                .FirstOrDefaultAsync();
            if (carId == null)
            {
                return NotFound("error");
            }

            
            var occupiedPlace= await _context.occupiedParkingPlace.FirstOrDefaultAsync(x=>x.CarId== carId && x.LeaveTime==null);
            if(occupiedPlace==null)
            {
                return NotFound("error");
            }

            DateTime leaveTime = DateTime.Now;
           
            int ammount=(int)CalculateParkingFee(occupiedPlace.OccupiedTime, leaveTime);
            
           
            var curUserTreasure=await _context.treasure.Where(x=>x.User==user).FirstOrDefaultAsync();
            if (curUserTreasure == null)
            {
                return BadRequest("user error");
            }

            if ((curUserTreasure.Amount- ammount) <0)
            {
                return StatusCode(201, ammount);
            }

            occupiedPlace.LeaveTime = DateTime.Now;
            _context.occupiedParkingPlace.Update(occupiedPlace);

            curUserTreasure.Amount = (curUserTreasure.Amount - ammount);
            _context.treasure.Update(curUserTreasure);

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("CurentPeymentInfo")]
        [Authorize]
        public async Task<IActionResult> CurentPeymentInfo([FromQuery]string CarRegistration)
        {

            var user = await _userManager.GetUserAsync(HttpContext.User);
            var carId = await _context.cars
                .Where(x => x.Registration == CarRegistration && x.User == user)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            if (carId == null)
            {
                return NotFound("error");
            }


            var occupiedPlace = await _context.occupiedParkingPlace.FirstOrDefaultAsync(x => x.CarId == carId && x.LeaveTime == null);
            if (occupiedPlace == null)
            {
                return NotFound("error");
            }

            int ammount = (int)CalculateParkingFee(occupiedPlace.OccupiedTime, DateTime.Now);

            return Ok(new
            {
                parkingCost= ammount
            });
        }


        [HttpGet("GetParkingStatus")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetParkingStatus()
        {
            var parkingPlaces = _context.parkingPlace
                .Select(pp => new
                {
                    pp.Id,
                }).ToList();

            var occupiedParkingPlaces = _context.occupiedParkingPlace
                .Where(op => op.LeaveTime == null)
                .Select(op => new
                {
                    op.ParkingPlaceId,
                    op.Cars.Registration,
                    op.OccupiedTime
                }).ToList();

            var result = parkingPlaces.Select(pp =>
            {
                var occupiedPlace = occupiedParkingPlaces.FirstOrDefault(op => op.ParkingPlaceId == pp.Id);
                return new
                {
                    ParkingId = pp.Id,
                    IsOccupied = occupiedPlace != null,
                    CarRegistration = occupiedPlace?.Registration,
                    OccupiedTime = occupiedPlace?.OccupiedTime.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }).ToList();

            return Ok(result);
        }


        [HttpGet("GetParkingSpaceHistory")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetParkingSpaceHistory()
        {
            var parkingSpaceHistory = _context.occupiedParkingPlace
                .Where(x => x.OccupiedTime.Date == DateTime.Today)
                .GroupBy(x => x.ParkingPlaceId)
                .Select(group => new
                {
                    ParkingPlaceId = group.Key,
                    ParkedCars = group.Count()
                })
                .OrderBy(x => x.ParkingPlaceId)
                .ToList();

            return Ok(parkingSpaceHistory);
        }

        [HttpGet("GetCurParkingSpaceHistory")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetCurParkingSpaceHistory([FromQuery] int parkingPlaceId)
        {
            var curPlace=_context.parkingPlace.Any(x=>x.Id==parkingPlaceId);
            if (!curPlace)
            {
                return BadRequest("nie ma takiego miejsca");
            }

            var curPlaceInfo=_context.occupiedParkingPlace
                .Where(x => x.OccupiedTime.Date == DateTime.Today && x.ParkingPlaceId==parkingPlaceId)
                .Select(x => new
                {
                    parkingPlaceId=x.ParkingPlaceId,
                    registerNumber=x.Cars.Registration,
                    occupiedTime=x.OccupiedTime,
                    leaveTime=x.LeaveTime
                })
                .ToList();

            return Ok(curPlaceInfo);
        }


        public  static double CalculateParkingFee(DateTime startTime, DateTime endTime)
        {
            double totalAmount = 3;

            while (startTime < endTime)
            {
                DateTime nextHour = startTime.AddHours(1);
                if (nextHour > endTime)
                {
                    nextHour = endTime;
                }

                double hours = (nextHour - startTime).TotalHours;

                if (startTime.Hour >= 8 && startTime.Hour < 15)
                {
                    totalAmount += hours * 5;
                }
                else
                {
                    totalAmount += hours * 2;
                }

                startTime = nextHour;
            }

            return totalAmount;
        }
    }
}
