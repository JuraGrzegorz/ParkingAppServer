using System.ComponentModel.DataAnnotations.Schema;

namespace TheWebApiServer.Model
{
    public class OccupiedParkingPlace
    {
        public int Id { get; set; }
        public DateTime OccupiedTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public int CarId { get; set; }
        [ForeignKey("CarId")]
        public Cars Cars { get; set; }
        public int ParkingPlaceId { get; set; }
        [ForeignKey("ParkingPlaceId")]
        public ParkingPlace ParkingPlace { get; set; }

    }
}
