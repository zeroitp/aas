namespace AasxServerStandardBib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;

public class ValidationTemplateDetails
{
    public ICollection<string> Keys { get; set; }
    public Guid Id { get; set; }
}