using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Bkl.Models
{
    [Table("bkl_notification_contact")]
    public partial class BklNotificationContact : BaseEntity
    {
        public BklNotificationContact() : base()
        { 
        } 
        [MaxLength(50), Required] public string ContactName { get; set; }
        [MaxLength(50), Required] public string ContactType { get; set; }
        [MaxLength(50), Required] public string ContactInfo { get; set; }
        [Required] public long FactoryId { get; set; } 
        [Required] public long UserId { get; set; }

    }
}
