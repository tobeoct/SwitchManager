using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClientServerApp.Models
{
    public class Scheme
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }

        [Required] public string Name { get; set; }

        [Required] public int TransactionTypeId { get; set; }
        public TransactionType TransactionType { get; set; }

        [Required] public int RouteId { get; set; }
        public Route Route { get; set; }

        [Required] public int ChannelId { get; set; }
        public Channel Channel { get; set; }

        [Required] public int FeeId { get; set; }
        public Fee Fee { get; set; }

        [Required] public string Description { get; set; }

    }
}