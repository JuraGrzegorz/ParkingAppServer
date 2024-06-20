namespace TheWebApiServer.Requests
{
    public class UpdateUserCars
    {
        public int CarId { get; set; }
        public string CarBrand { get; set; }
        public string CarModel { get; set; }
        public string CarRegistration { get; set; }
    }
}
