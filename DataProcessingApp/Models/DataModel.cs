using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcessingApp.Models
{
    public class DataModel
    {
        [Key]
        public int Id { get; set; }
        public string Date { get; set; }
        public string LatinChars { get; set; }
        public string RussianChars { get; set; }
        public int EvenInt { get; set; }
        public double FloatNumber { get; set; }
    }
}
