﻿using Microsoft.AspNetCore.Authorization;
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
        
        public ParkingController(DataContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("UserAddCar")]
        [Authorize]
        public async Task<IActionResult> UserAddCarAsync([FromBody] AddUserCar model)
        {
           
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
        public async Task<IActionResult> GetUserCarsAsync()
        {
            //dodac validacje aut
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userCars = _context.cars
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


        [HttpGet("GetTicket")]
        public IActionResult GetTicket()
        {

            var parkingPlaces = _context.parkingPlace.ToList();
            foreach(var parkingPlace in parkingPlaces)
            {
                var IsContested =_context.occupiedParkingPlace.Any(x=>x.ParkingPlace.Id==parkingPlace.Id && x.LeaveTime==null);
                if(!IsContested)
                {
                    _context.occupiedParkingPlace.Add(new OccupiedParkingPlace()
                    {
                        OccupiedTime = DateTime.Now,
                        ParkingPlaceId = parkingPlace.Id,
                        CarId = 2,
                        LeaveTime=null
                    });
                    _context.SaveChanges();
                    return Ok(new {PlaceId=parkingPlace.Id});
                }
            }
            return Ok("Un know Error");
        }

        //wyjechal
        [HttpPost("LeavePlace")]
        public IActionResult LeavePlace([FromBody] int CarId)
        {

            
            return Ok();
        }


        [HttpPost("SendCarPhoto")]
        public IActionResult SendCarPhoto([FromBody] byte[] photo)
        {

            return Ok();
        }

        [HttpGet("GetParkingStatus")]
        public IActionResult GetParkingStatus()
        {
            var parkingPlaces = _context.parkingPlace.ToList();
            var occupiedParkingPlaces = _context.occupiedParkingPlace.Where(x => x.LeaveTime == null).ToList();

            var result = new List<object>();

            foreach (var place in parkingPlaces)
            {
                var isOccupied = occupiedParkingPlaces.Any(x=>x.ParkingPlace.Id==place.Id && x.LeaveTime==null);
                result.Add(new { ParkingId = place.Id, occupied = isOccupied });
            }

            return Ok(result);
        }
        [HttpGet("GetParkingSpaceHistory")]
        public IActionResult GetParkingSpaceHistory()
        {
            
            return Ok();
        }
        

    }
}
