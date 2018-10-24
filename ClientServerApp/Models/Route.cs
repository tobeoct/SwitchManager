using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClientServerApp.Models
{
    public class Route
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        public int SinkNodeId { get; set; }

        public SinkNode SinkNode { get; set; }

        [Required]
        public long CardPan { get; set; }

        [Required]
        public string Description { get; set; }
    }
}