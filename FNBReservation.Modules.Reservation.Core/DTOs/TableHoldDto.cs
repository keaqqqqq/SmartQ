// In FNBReservation.Modules.Reservation.Core/DTOs/TableHoldDto.cs
using System.ComponentModel.DataAnnotations;
public class UpdateHoldTimeRequestDto
{
    [Required]
    public Guid HoldId { get; set; }

    [Required]
    public Guid OutletId { get; set; }

    [Required]
    public int PartySize { get; set; }

    [Required]
    public DateTime NewReservationDateTime { get; set; }

    [Required]
    public string SessionId { get; set; }
}