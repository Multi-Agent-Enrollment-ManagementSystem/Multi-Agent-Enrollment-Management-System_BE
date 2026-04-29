using System;
using System.Collections.Generic;

namespace MAEMS.Infrastructure.Models;

public partial class RegisterEvent
{
    public int RegisterId { get; set; }

    public int ArticleId { get; set; }

    public string FullName { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Article Article { get; set; }
}