using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClientServerApp.Models
{
    public class Fee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public double? FlatAmount { get; set; }

        
        public double? PercentOfTrx { get; set; }
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }
    }
}