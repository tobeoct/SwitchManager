using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClientServerApp.Models
{
    public class SourceNode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string HostName { get; set; }

        [Required]
        public string IPAddress { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public int SchemeId { get; set; }

        public Scheme Scheme { get; set; }
    }
}